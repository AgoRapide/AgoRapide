using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;
using AgoRapide.Core;
using AgoRapide.API;
using System.Reflection;

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

        /// <summary>
        /// TODO: Consider making private
        /// </summary>
        public static ConcurrentDictionary<string, ApplicationPart> AllApplicationParts = new ConcurrentDictionary<string, ApplicationPart>();

        /// <summary>
        /// Hack for <see cref="IDatabase"/>-implementation in order for <see cref="GetClassMember"/> not to be called. 
        /// (that is, in order for not to create <see cref="ClassMember"/> unnecessarily just because <see cref="GetFromDatabase{T}"/> has not finished). 
        /// </summary>
        public static bool GetFromDatabaseInProgress;

        /// <summary>
        /// Reads all entities of type <t
        /// ypeparamref name="T"/> from database.
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
        /// Synchronizes 
        /// 1) The database, 
        /// 2) This object and 
        /// 3) The cached value in <see cref="AllApplicationParts"/> 
        /// against <see cref="A"/>
        /// 
        /// Note that becausae of the generics involved in calling <see cref="GetOrAdd{T}"/> we can not have the implementator here 
        /// but must leave the implementation to each individual sub class. 
        /// <see cref="APIMethodAttribute"/>.
        /// </summary>
        /// <param name="db"></param>
        public abstract void ConnectWithDatabase(IDatabase db);

        /// <summary>
        /// Usually used for getting an <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> in 
        /// connection with operations against the database (storing which <see cref="ClassMember"/> did the actual change).
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        [ClassMember(Description = "Used randomly within application lifetime.")]
        public static ClassMember GetClassMember(MemberInfo memberInfo, IDatabase db) => GetOrAdd<ClassMember>(memberInfo.GetClassMemberAttribute(), db, null);

        /// <summary>
        /// Creates a "root" <see cref="ApplicationPart"/> if not exist. Its <see cref="BaseEntity.Id"/> will be used as
        /// <see cref="DBField.cid"/> for all other <see cref="ApplicationPart"/>.
        /// 
        /// <see cref="GetFromDatabase{T}"/> must already have been called, if not duplicate objects will be created in database
        /// </summary>
        /// 
        /// <param name="attribute"></param>
        /// <param name="db">Used if a new <see cref="ApplicationPart"/> has to be added</param>
        /// <param name="enrichAndReturnThisObject">
        /// May be null. 
        /// 
        /// Used when a desired instance already exists but the caller want it enriched with database information. 
        /// If given then <see cref="BaseAttribute.Properties"/> from <paramref name="attribute"/> will also be updated in database. 
        /// (in other words, passing this parameter is a little resource intensive and should therefore only be done at application startup)
        /// </param>
        /// <returns></returns>
        [ClassMember(
            Description =
                "Returns an -" + nameof(ApplicationPart) + "- representing the given attribute. " +
                "Adds as necessary to database if none already exists. ",
            LongDescription =
                "Normally only called directly at application startup. " +
                "After application startup only called indirectly via -" + nameof(GetOrAdd) + "- for -" + nameof(MemberInfo) + "-, " +
                "except from -" + nameof(EnumMapper.TryAddA) + "- for API originated enums"
        )]
        protected static T GetOrAdd<T>(BaseAttribute attribute, IDatabase db, T enrichAndReturnThisObject) where T : ApplicationPart, new() {
            ClassMember cid = null; /// This method as <see cref="DBField.cid"/>
            var retvalTemp = AllApplicationParts.GetOrAdd(attribute.Identifier, i => {
                InvalidIdentifierException.AssertValidIdentifier(i); // TODO: As of Apr 2017 this will fail for abstract types.

                var thisA = MethodBase.GetCurrentMethod().GetClassMemberAttribute();
                cid = thisA.Identifier.Equals(i) ?
                    null : // Avoid recursive call
                    GetOrAdd<ClassMember>(thisA, db, null); /// Get this method as <see cref="ApplicationPart"/>

                /// Note that this operation is not thread-safe in the manner that the operation against the database
                /// may be executed multiple times (the superfluous result of this lambda will then just end up being ignored)
                /// (This is only a problem the first time (in the database lifetime) that a given <see cref="ApplicationPart"/> is being used)
                /// Duplicates found at application startup should be logged with instructions for deletion. 
                var r = db.GetEntityById<T>(db.CreateProperty(
                    cid: cid == null ? (long?)null : cid.Id, /// When creating "ourselves" we must accept null as <see cref="DBField.cid"/>
                    pid: null,
                    fid: null,
                    key: CoreP.RootProperty.A().PropertyKeyWithIndex,
                    value: typeof(T).ToStringDB(),
                    result: null
                ));
                if (cid == null) {
                    cid = r as ClassMember ?? throw new InvalidObjectTypeException(r, typeof(ClassMember), nameof(cid) + " should only be null when " + nameof(T) + " is " + typeof(ClassMember));
                    /// This is not possible to do, since A is <see cref="BaseAttribute.GetStaticNotToBeUsedInstance"/>
                    /// if (cid.A.Identifier != thisA.Identifier) throw new ApplicationException("Mismatching identifiers:\r\n" + cid.A.Identifier + "\r\nand\r\n" + thisA.Identifier);                    
                }

                if (enrichAndReturnThisObject == null) {
                    /// This is a bit unexpected, but anyway, since <see cref="IDatabase.UpdateProperty{T}"/> will not be called below, 
                    /// create properties now. 
                    attribute.Properties.Values.ForEach(p => {
                        db.CreateProperty(
                            cid: cid.Id,
                            pid: r.Id,
                            fid: null,
                            key: p.Key,
                            value: p.Value,
                            result: null
                        );
                    });
                }
                return r;
            });
            var retval = retvalTemp as T;
            if (retval == null) throw new InvalidObjectTypeException(retvalTemp, typeof(T), 
                nameof(attribute.Identifier) + ": " + attribute.Identifier + ", " + nameof(retvalTemp) + ": " + retvalTemp.ToString() + "\r\n" +
                "Possible cause: Duplicate identifiers generated by different instances of " + nameof(BaseAttribute.Identifier));
            if (enrichAndReturnThisObject == null) return retval;

            // Update all properties in database
            if (cid == null) cid = GetOrAdd<ClassMember>(MethodBase.GetCurrentMethod().GetClassMemberAttribute(), db, null); /// Get this method as <see cref="ApplicationPart"/>

            /// TODO: BIG WEAKNESS HERE. We do not know the generic value of what we are asking for
            /// TODO: The result will be to store as <see cref="DBField.strv"/> instead of a more precise type.
            /// TODO: Implement some kind of copying of properties in order to avoid this!
            /// TODO: (or rather, solve the general problem of using generics with properties)
            attribute.Properties.Values.ForEach(p => {
                if (typeof(bool).Equals(p.Key.Key.A.Type)) {
                    db.UpdateProperty(cid.Id, retval, p.Key, p.V<bool>(), result: null);
                    // TODO: Fix code below. No reason for not supporting all types (long, double, datetime and so on)
                    // TODO: Maybe replace this check with a new extension-method called IsStoredAsStringInDatabase or similar...
                } else if (typeof(Type).Equals(p.Key.Key.A.Type) || p.Key.Key.A.Type.IsEnum || typeof(string).Equals(p.Key.Key.A.Type)) {
                    db.UpdateProperty(cid.Id, retval, p.Key, p.V<string>(), result: null);
                } else {
                    throw new InvalidTypeException(p.Key.Key.A.Type, "Not implemented copying of properties. Details: " + p.ToString());
                }
            });
            
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

            /// Update in cache also. 
            AllApplicationParts[attribute.Identifier] = enrichAndReturnThisObject;

            return enrichAndReturnThisObject;
        }

        ///// <summary>
        ///// Note how choice of key (and name) may very well cause overlap (two different types with same member mapping to same ApplicationPart since
        ///// we are using ToStringShort which removes namespace information). This is considered an acceptable tradeoff in return of
        ///// getting a clear understandable name.
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="member">May be null or empty</param>
        ///// <returns></returns>
        //private static string GetIdentifier(Type type, string member) {
        //    var typeToString = type.ToStringShort();
        //    if (typeToString.Equals(typeof(APIMethod)) && member != null && member.StartsWith(typeToString)) return member; // Typical for APIMethod, avoid things like APIMethod_APIMethod__QueryId_
        //    if (string.IsNullOrEmpty(member)) return typeToString;
        //    return typeToString + "_" + member.Replace("<", "_").Replace(">", "_"); // Replace of < and > is necessary because of lambdas / anonymous methods 
        //    // TODO: Add more Replaces for Method signatures. 
        //}
    }
}