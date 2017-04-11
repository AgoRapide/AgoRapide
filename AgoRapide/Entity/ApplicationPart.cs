﻿using System;
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
    /// Inheriting classes:<br>
    /// <see cref="APIMethod"/><br>
    /// <see cref="ClassAndMethod"/><br>
    /// <see cref="EnumClass"/><br>
    /// </summary>
    public abstract class ApplicationPart : BaseEntityWithLogAndCount {
        public static ConcurrentDictionary<string, ApplicationPart> AllApplicationParts = new ConcurrentDictionary<string, ApplicationPart>();

        /// <summary>
        /// Hack for <see cref="IDatabase"/>-implementation in order for <see cref="GetOrAdd{T}"/> not to be called. 
        /// (that is, in order for not to create <see cref="ClassAndMethod"/> unnecessarily just because <see cref="GetFromDatabase{T}"/> has not finished). 
        /// </summary>
        public static bool GetFromDatabaseInProgress;

        /// <summary>
        /// Reads all entities of type <typeparamref name="T"/> from database.
        /// 
        /// Must be called single threaded at startup like 
        ///   AgoRapide.ApplicationPart{P}.GetFromDatabase{AgoRapide.ApplicationPart{P}}
        ///  and
        ///   AgoRapide.ApplicationPart{P}.GetFromDatabase{AgoRapide.ApiMethod{P}}
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


        public static T GetOrAdd<T>(Type type, string member, IDatabase db) where T : ApplicationPart, new() => GetOrAdd<T>(type, member, db, enrichAndReturnThisObject: null);
        /// <summary>
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
        /// For <see cref="APIMethodOrigin.SemiAutogenerated"/> <see cref="APIMethod"/> this will be the type of the controller that implements the method.<br>
        /// For <see cref="APIMethodOrigin.Autogenerated"/> <see cref="APIMethod"/> this will be the type of <see cref="BaseController"/>.<br>
        /// For <see cref="ClassAndMethod"/> this will be the type of the actual class.<br>
        /// For <see cref="EnumClass"/> this will be the type of the actual enum.<br>
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
            // TODO: REMOVE COMMENT!
            // This check is unneccesary as long as ApplicationPart is abstract
            // if (typeof(T).Equals(typeof(ApplicationPart))) throw new InvalidTypeException(typeof(T), "Illegal T, use one of the subclasses");

            // Note how choice of key (and name) may very well cause overlap (two different types with same member mapping to same ApplicationPart since
            // we are using ToStringShort which removes namespace information). This is considered an acceptable tradeoff in return of
            // getting a clear understandable name.
            var identifier = type.ToStringShort() + (string.IsNullOrEmpty(member) ? "" : ".") + member; /// <see cref="EnumClass"/> may call us without member in which case full stop . is not needed
            var retvalTemp = AllApplicationParts.GetOrAdd(identifier, i => {
                // Note that this operation is not thread-safe in the manner that the operation against the database
                // may be executed multiple times (the superfluous result of this lambda will then just end up being ignored)
                // (This is only a problem the first time (in the database lifetime) that a given type+member is being used)
                // Duplicates found at application startup should be logged with instructions for deletion. 
                var id = db.CreateProperty(
                    cid: (typeof(ApplicationPart).ToStringShort() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name).Equals(i) ? (long?)null :
                        // Careful, recursive call! Check how lines above and below matches each other and also matches code 'var identifier = type + "." + member' at start of method
                        GetOrAdd<ClassAndMethod>(typeof(ApplicationPart), System.Reflection.MethodBase.GetCurrentMethod().Name, db).Id,
                    pid: null,
                    fid: null,
                    key: CoreP.Type.A().PropertyKey,
                    value: typeof(T).ToStringDB(),
                    result: null);
                var a = type.GetAgoRapideAttributeForClass();
                new List<(CoreP coreP, object obj)> {
                    (CoreP.Name, identifier ), // Name may be overriden, for instance for ApiMethod for which RouteTemplate is used instead for name
                    (CoreP.Identifier, identifier),
                    (CoreP.AccessLevelRead, a.AccessLevelRead),
                    (CoreP.AccessLevelWrite, a.AccessLevelWrite)
                }.ForEach(t => db.CreateProperty(id, id, null, t.coreP.A().PropertyKey, t.obj, null));
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
    }
}