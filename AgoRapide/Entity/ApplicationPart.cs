using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// TODO: MOVE INTO CORE-FOLDER!
    /// 
    /// Inheriting classes:<br>
    /// <see cref="APIMethod"/><br>
    /// <see cref="ClassMember"/><br>
    /// <see cref="EnumValue"/><br>
    /// <see cref="Configuration"/><br>
    /// 
    /// All these classes are stored in the database for documentation purposes (write only).
    /// </summary>
    public abstract class ApplicationPart : BaseEntityWithLogAndCount {

        private BaseAttribute _a;
        public BaseAttribute A { get => _a ?? throw new NullReferenceException(nameof(A)); private set => _a = value ?? throw new NullReferenceException(nameof(value)); }

        public ApplicationPart(BaseAttribute a) => _a = a;

        public static ConcurrentDictionary<string, ApplicationPart> AllApplicationParts = new ConcurrentDictionary<string, ApplicationPart>();

        /// <summary>
        /// Hack for <see cref="IDatabase"/>-implementation in order for <see cref="GetOrAdd"/> not to be called. 
        /// (that is, in order for not to create <see cref="ClassMember"/> unnecessarily just because <see cref="GetFromDatabase{T}"/> has not finished). 
        /// </summary>
        public static bool GetFromDatabaseInProgress;

        /// <summary>
        /// Reads all entities of type <typeparamref name="T"/> from database.
        /// 
        /// Must be called single threaded at startup like 
        ///   AgoRapide.ApplicationPart.GetFromDatabase{AgoRapide.ClassMember}
        ///  and
        ///   AgoRapide.ApplicationPart.GetFromDatabase{AgoRapide.ApiMethod}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        public static void GetFromDatabase<T>(IDatabase db, Action<string> logger) where T : ApplicationPart, new() {
            try {
                GetFromDatabaseInProgress = true;

                db.GetAllEntities<T>().ForEach(ap => {
                    var identifier = ap.PV<string>(CoreP.Identifier.A());
                    if (!AllApplicationParts.TryAdd(identifier, ap)) {
                        // This is a known weakness as of Jan 2017 since creation of ApplicationPart is not thread safe regarding database operations
                        logger("Duplicate " + ap.GetType() + " found (" + identifier + "), suggestion: Keep " + AllApplicationParts[identifier].Id + " but delete " + ap.Id + " (since that is the one being ignored now)");
                    }
                });
            } finally {
                GetFromDatabaseInProgress = false;
            }
        }


        /// <summary>
        /// Used randomly within application lifetime. 
        /// 
        /// Usually used for getting an <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> in 
        /// connection with operations against the database (storing which <see cref="ClassMember"/> did the actual change).
        /// </summary>
        /// <param name="classMemberAttribute"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static ClassMember GetOrAdd(System.Reflection.MemberInfo memberInfo, IDatabase db) => GetOrAdd<ClassMember>(memberInfo.DeclaringType, memberInfo.ToString(), db, null);

        /// <summary>
        /// Only used at application startup.
        /// 
        /// Gets an <see cref="ApplicationPart"/> "representing" the given <paramref name="type"/> and <paramref name="member"/>. 
        /// 
        /// Adds as necessary to database if none already exists. 
        /// Creates a "root" <see cref="ApplicationPart"/> called something like 
        /// "AgoRapide.ApplicationPart.GetOrAdd" if not exist. Its <see cref="BaseEntity.Id"/> will be used as
        /// <see cref="DBField.cid"/> for all other <see cref="ApplicationPart"/>.
        /// 
        /// <see cref="GetFromDatabase{T}"/> must already have been called.
        /// </summary>
        /// <param name="type">
        /// </param>
        /// <param name="member">
        /// May be null or empty. 
        /// </param>
        /// <param name="db">Used if a new <see cref="ApplicationPart"/> has to be added</param>
        /// <param name="enrichAndReturnThisObject">
        /// May be null. 
        /// 
        /// Used when the object already exists but the caller want it enriched with database information 
        /// The object read by <see cref="GetFromDatabase{T}"/> is only used temporarily in order to transfer general properties stored in database
        /// to <paramref name="enrichAndReturnThisObject"/>
        /// </param>
        /// <returns></returns>
        public static T GetOrAdd<T>(Type type, string member, IDatabase db, T enrichAndReturnThisObject) where T : ApplicationPart, new() {

            var identifier = GetIdentifier(type, member);
            var retvalTemp = AllApplicationParts.GetOrAdd(identifier, i => {
                InvalidIdentifierException.AssertValidIdentifier(i); // TODO: As of Apr 2017 this will fail for abstract types.

                // Note that this operation is not thread-safe in the manner that the operation against the database
                // may be executed multiple times (the superfluous result of this lambda will then just end up being ignored)
                // (This is only a problem the first time (in the database lifetime) that a given type + member is being used)
                // Duplicates found at application startup should be logged with instructions for deletion. 
                var id = db.CreateProperty(
                    cid: GetIdentifier(typeof(ApplicationPart), System.Reflection.MethodBase.GetCurrentMethod().Name).Equals(i) ? (long?)null :
                        // Careful, recursive call! Check how lines above and below matches each other and also matches code 'var identifier = type + "." + member' at start of method
                        GetOrAdd(System.Reflection.MethodBase.GetCurrentMethod(), db).Id,
                    pid: null,
                    fid: null,
                    key: CoreP.RootProperty.A().PropertyKeyWithIndex,
                    value: typeof(T).ToStringDB(),
                    result: null);
                var properties = new List<(CoreP coreP, object obj)> {
                    (CoreP.Name, identifier), // Name may be overriden, for instance for ApiMethod for which RouteTemplate is used instead for name
                    (CoreP.Identifier, identifier),
                };

                /// TODO: Consider generic <see cref="BaseAttribute"/>.Properties like
                /// <see cref="APIMethodAttribute.Properties"/> and <see cref="ConfigurationAttribute.Properties"/>
                if (type.IsEnum) {
                    var a = type.GetEnumAttribute();
                    /// Nothing of relevance in <see cref="EnumAttribute"/>
                } else {
                    var a = type.GetClassAttribute();
                    properties.Add((CoreP.AccessLevelRead, a.AccessLevelRead));
                    properties.Add((CoreP.AccessLevelWrite, a.AccessLevelWrite));
                }
                properties.ForEach(t => db.CreateProperty(id, id, null, t.coreP.A().PropertyKeyWithIndex, t.obj, null));
                return db.GetEntityById<T>(id);
            });
            var retval = retvalTemp as T;
            if (retval == null) throw new InvalidObjectTypeException(retvalTemp, typeof(T), nameof(identifier) + ": " + identifier + ", " + nameof(retvalTemp) + ": " + retvalTemp.ToString());
            if (enrichAndReturnThisObject == null) return retval;
            enrichAndReturnThisObject.Use(e => {
                e.Id = retval.Id;
                e.Created = retval.Created;
                e.RootProperty = retval.RootProperty;
                if (e.Properties == null) e.Properties = new Dictionary<CoreP, Property>();
                retval.Properties.ForEach(p => {
                    if (e.Properties.ContainsKey(p.Key)) throw new KeyAlreadyExistsException<CoreP>(p.Key, nameof(enrichAndReturnThisObject) + " should not contain any properties at this stage");
                    e.Properties.AddValue2(p.Key, p.Value);
                });
            });
            return enrichAndReturnThisObject;
        }

        /// <summary>
        /// Note how choice of key (and name) may very well cause overlap (two different types with same member mapping to same ApplicationPart since
        /// we are using ToStringShort which removes namespace information). This is considered an acceptable tradeoff in return of
        /// getting a clear understandable name.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="member">May be null or empty</param>
        /// <returns></returns>
        private static string GetIdentifier(Type type, string member) {
            var typeToString = type.ToStringShort();
            if (typeToString.Equals(typeof(APIMethod)) && member != null && member.StartsWith(typeToString)) return member; // Typical for APIMethod, avoid things like APIMethod_APIMethod__QueryId_
            if (string.IsNullOrEmpty(member)) return typeToString;
            return typeToString + "_" + member.Replace("<", "_").Replace(">", "_"); // Replace of < and > is necessary because of lambdas / anonymous methods 
            // TODO: Add more Replaces for Method signatures. 
        }
    }
}