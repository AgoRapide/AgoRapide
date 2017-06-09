// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;
using AgoRapide.API;
using System.Reflection;

namespace AgoRapide.Core {

    /// <summary>
    /// Inheriting classes:<br>
    /// <see cref="APIMethod"/><br>
    /// <see cref="Class"/><br>
    /// <see cref="ClassMember"/><br>
    /// <see cref="Enum"/><br>
    /// <see cref="EnumValue"/><br>
    /// <see cref="Configuration"/><br>
    /// </summary>
    [Class(
        Description = "Represents some internal part of your application.",
        LongDescription = "Compare to  to -" + nameof(APIDataObject) + "- which represents actual data entities that your API is supposed to provide.",
        /// TODO: Implement inheritance of <see cref="ClassAttribute"/>-members. 
        /// TODO: Fixed 26 May 2017 but check that works properly. 
        CacheUse = CacheUse.All // Since there is a limited number of elements. 
    )]
    public abstract class ApplicationPart : BaseEntityWithLogAndCount {

        private BaseAttribute _a;
        public BaseAttribute A { get => _a ?? throw new NullReferenceException(nameof(A)); private set => _a = value ?? throw new NullReferenceException(nameof(value)); }

        public ApplicationPart(BaseAttribute a) => _a = a;

        /// <summary>
        /// TODO: Consider making private
        /// </summary>
        public static ConcurrentDictionary<string, ApplicationPart> AllApplicationParts = new ConcurrentDictionary<string, ApplicationPart>();

        /// <summary>
        /// Hack for <see cref="BaseDatabase"/>-implementation in order for <see cref="GetClassMember"/> not to be called. 
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
        public static void GetFromDatabase<T>(BaseDatabase db, Action<string> logger) where T : ApplicationPart, new() {
            try {
                GetFromDatabaseInProgress = true;

                db.GetAllEntities<T>().ForEach(ap => {
                    // var identifier = ap.PV<string>(CoreP.IdString.A());
                    var identifier = ap.IdString.ToString(); /// Note how this assumes that <see cref="CoreP.QueryId"/> actually has been set, if not we will just get <see cref="BaseEntity.Id"/> back now
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
        /// by calling <see cref="Get{T}"/>
        /// 
        /// Note that becausae of the generics involved in calling <see cref="Get{T}"/> we can not have the implementator here 
        /// but must leave the implementation to each individual sub class. 
        /// </summary>
        /// <param name="db"></param>
        public abstract void ConnectWithDatabase(BaseDatabase db);

        /// <summary>
        /// Usually used for getting an <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> in 
        /// connection with operations against the database (storing which <see cref="ClassMember"/> did the actual change). 
        /// 
        /// Note that will work also for <paramref name="memberInfo"/> which have not been found by <see cref="Class.RegisterAndIndexClass{T}"/>
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        [ClassMember(Description = "Used randomly within application lifetime.")]
        public static ClassMember GetClassMember(MemberInfo memberInfo, BaseDatabase db) => Get<ClassMember>(memberInfo.GetClassMemberAttribute(), db, null);

        /// <summary>
        /// TODO: Add to <see cref="Util.EntityCache"/> somewhere here.
        /// 
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
                "After application startup only called indirectly via -" + nameof(Get) + "- for -" + nameof(MemberInfo) + "-, " +
                "except from -" + nameof(PropertyKeyMapper.TryAddA) + "- for API originated enums"
        )]
        protected static T Get<T>(BaseAttribute attribute, BaseDatabase db, T enrichAndReturnThisObject) where T : ApplicationPart, new() {
            ClassMember cid = null; /// This method as <see cref="DBField.cid"/>
            var retvalTemp = AllApplicationParts.GetOrAdd(attribute.Id.IdString.ToString(), i => {
                InvalidIdentifierException.AssertValidIdentifier(i); // TODO: As of Apr 2017 this will fail for abstract types.

                // Alternative 1) This becomes very messy, something like ApplicationPart___c__DisplayClass10_0_T__AgoRapide_Core_ApplicationPart__GetOrAdd_b__0_System_String_
                //   var thisA = MethodBase.GetCurrentMethod().GetClassMemberAttribute();
                // Alternative 2) Instead ask for the "parent" method lower down in the stack
                var thisA = MethodBase.GetCurrentMethod().GetClassMemberAttribute(nameof(Get)); /// Do not rename method <see cref="Get{T}"/> into GetOrAdd as it would collide with <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd"/>

                cid = thisA.Id.IdString.ToString().Equals(i) ?
                    null : // Avoid recursive call
                    Get<ClassMember>(thisA, db, null); /// Get this method as <see cref="ApplicationPart"/>

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

                attribute.Properties.Flatten().ForEach(p => { /// Note that <see cref="BaseDatabase.UpdateProperty{T}"/> is much more complicated to use because of the generics involved.
                    db.CreateProperty(
                        cid: cid.Id,
                        pid: r.Id,
                        fid: null,
                        key: p.Key,
                        value: p.Value,
                        result: null
                    );
                });
                // Unnecessary, included in attribute.Properties
                //db.CreateProperty(
                //    cid: cid.Id,
                //    pid: r.Id,
                //    fid: null,
                //    key: CoreP.QueryId.A().PropertyKeyWithIndex,
                //    value: attribute.Id.IdString,
                //    result: null
                //);
                /// Read r once more since properties are not reflected yet 
                /// (since we called <see cref="BaseDatabase.CreateProperty"/> and not <see cref="BaseDatabase.UpdateProperty{T}"/>
                r = db.GetEntityById<T>(r.Id);
                r._a = attribute; // HACK: Since dummy constructor was used, set attribute now.

                // }

                /// TODO: Add to <see cref="Util.EntityCache"/> somewhere here.
                return r;
            });
            var retval = retvalTemp as T;
            if (retval == null) throw new InvalidObjectTypeException(retvalTemp, typeof(T),
                nameof(attribute.Id) + ": " + attribute.Id + ", " + nameof(retvalTemp) + ": " + retvalTemp.ToString() + "\r\n" +
                "Possible cause: Duplicate identifiers generated by different instances of " + nameof(BaseAttribute.Id));

            /// TODO: Add to <see cref="Util.EntityCache"/> somewhere here.
            if (enrichAndReturnThisObject == null) return retval;
            /// TODO: Add to <see cref="Util.EntityCache"/> somewhere here.

            /// Update in cache also. 
            /// TODO: Check thread-safety of this. Next thread may already have gotten the cached value, before we put back the updated value here
            AllApplicationParts[attribute.Id.IdString.ToString()] = enrichAndReturnThisObject;
            /// TODO: BAD THREAD SAFETY. <param name="enrichAndReturnThisObject"/> has not been initialized yet.
            /// TODO: Add to <see cref="Util.EntityCache"/> somewhere here.

            enrichAndReturnThisObject.Use(e => { /// Turn <param name="enrichAndReturnThisObject"/> "into" the object found in database
                e.Id = retval.Id;
                e.Created = retval.Created;
                e.RootProperty = retval.RootProperty;
                if (e.Properties == null) e.Properties = new Dictionary<CoreP, Property>();
                retval.Properties.ForEach(p => {
                    // if (e.Properties.ContainsKey(p.Key)) throw new KeyAlreadyExistsException<CoreP>(p.Key, nameof(enrichAndReturnThisObject) + " should not contain any properties at this stage");
                    e.Properties.AddValue2(p.Key, p.Value,() => nameof(enrichAndReturnThisObject) + " should not contain any properties at this stage");
                });
            });

            return enrichAndReturnThisObject;
        }
    }
}