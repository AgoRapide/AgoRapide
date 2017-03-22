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
    /// <see cref="APIMethod{TProperty}"/><br>
    /// <see cref="ClassAndMethod{TProperty}"/><br>
    /// <see cref="EnumClass{TProperty}"/><br>
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public abstract class ApplicationPart<TProperty> : BaseEntityTWithLogAndCount<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public static ConcurrentDictionary<string, ApplicationPart<TProperty>> AllApplicationParts = new ConcurrentDictionary<string, ApplicationPart<TProperty>>();

        /// <summary>
        /// Reads all entities of type <typeparamref name="T"/> from database.
        /// 
        /// Must be called at startup like 
        ///   AgoRapide.ApplicationPart{P}.GetFromDatabase{AgoRapide.ApplicationPart{P}}
        ///  and
        ///   AgoRapide.ApplicationPart{P}.GetFromDatabase{AgoRapide.ApiMethod{P}}
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="logger"></param>
        public static void GetFromDatabase<T>(IDatabase<TProperty> db, Action<string> logger) where T : ApplicationPart<TProperty>, new() {
            // TODO: REMOVE COMMENT!
            // This check is unneccesary as long as ApplicationPart is abstract
            // if (typeof(T).Equals(typeof(ApplicationPart<TProperty>))) throw new InvalidTypeException(typeof(T), "Illegal T, use one of the subclasses");

            db.GetAllEntities<T>().ForEach(ap => {
                var key = ap.PV<string>(M(CoreProperty.Key));
                if (!AllApplicationParts.TryAdd(key, ap)) {
                    // This is a known weakness as of Jan 2017 since creation of ApplicationPart is not thread safe regarding database operations
                    logger("Duplicate " + ap.GetType() + " found (" + key + "), suggestion: Keep " + AllApplicationParts[key].Id + " but delete " + ap.Id + " (since that is the one being ignored now)");
                }
            });
        }

        public static T GetOrAdd<T>(Type type, string member, IDatabase<TProperty> db) where T : ApplicationPart<TProperty>, new() => GetOrAdd<T>(type, member, db, enrichAndReturnThisObject: null);
        /// <summary>
        /// Gets an <see cref="ApplicationPart{TProperty}"/> "representing" the given <paramref name="type"/> and <paramref name="member"/>. 
        /// 
        /// Adds as necessary to database if none already exists. 
        /// Creates a "root" <see cref="ApplicationPart{TProperty}"/> called something like 
        /// "AgoRapide.ApplicationPart{TProperty}.GetOrAdd" if not exist. Its <see cref="BaseEntity.Id"/> will be used as
        /// <see cref="DBField.cid"/> for all other <see cref="ApplicationPart{TProperty}"/>.
        /// 
        /// <see cref="GetFromDatabase{T}"/> must already have been called.
        /// </summary>
        /// <param name="type">
        /// For <see cref="APIMethodOrigin.SemiAutogenerated"/> <see cref="APIMethod{TProperty}"/> this will be the type of the controller that implements the method.<br>
        /// For <see cref="APIMethodOrigin.Autogenerated"/> <see cref="APIMethod{TProperty}"/> this will be the type of <see cref="BaseController{TProperty}"/>.<br>
        /// For <see cref="ClassAndMethod{TProperty}"/> this will be the type of the actual class.<br>
        /// For <see cref="EnumClass{TProperty}"/> this will be the type of the actual enum.<br>
        /// </param>
        /// <param name="member">
        /// May be null or empty. 
        /// </param>
        /// <param name="db">Used if a new <see cref="ApplicationPart{TProperty}"/> has to be added</param>
        /// <param name="enrichAndReturnThisObject">
        /// May be null. 
        /// 
        /// Used when the object already exists but the caller want it enriched with database information 
        /// The object read by <see cref="GetFromDatabase{T}"/> is only used temporarily in order to transfer general properties stored in database
        /// to <paramref name="enrichAndReturnThisObject"/>
        /// </param>
        /// <returns></returns>
        public static T GetOrAdd<T>(Type type, string member, IDatabase<TProperty> db, T enrichAndReturnThisObject) where T : ApplicationPart<TProperty>, new() {
            // TODO: REMOVE COMMENT!
            // This check is unneccesary as long as ApplicationPart is abstract
            // if (typeof(T).Equals(typeof(ApplicationPart<TProperty>))) throw new InvalidTypeException(typeof(T), "Illegal T, use one of the subclasses");

            // Note how choice of key (and name) may very well cause overlap (two different types with same member mapping to same ApplicationPart since
            // we are using ToStringShort which removes namespace information). This is considered an acceptable tradeoff in return of
            // getting a clear understandable name.
            var key = type.ToStringShort() + (string.IsNullOrEmpty(member) ? "" : ".") + member; /// <see cref="EnumClass{TProperty}"/> may call us without member in which case full stop . is not needed
            var retvalTemp = AllApplicationParts.GetOrAdd(key, k => {
                // Note that this operation is not thread-safe in the manner that the operation against the database
                // may be executed multiple times (the superfluous result of this lambda will then just end up being ignored)
                // (This is only a problem the first time (in the database lifetime) that a given type+member is being used)
                // Duplicates found at application startup should be logged with instructions for deletion. 
                var id = db.CreateProperty(
                    cid: (typeof(ApplicationPart<TProperty>).ToStringShort() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name).Equals(k) ? (long?)null :
                        // Careful, recursive call! Check how lines above and below matches each other and also matches code 'var key = type + "." + member' at start of method
                        GetOrAdd<ClassAndMethod<TProperty>>(typeof(ApplicationPart<TProperty>), System.Reflection.MethodBase.GetCurrentMethod().Name, db).Id,
                    pid: null,
                    fid: null,
                    key: M(CoreProperty.Type),
                    value: typeof(T).ToStringDB(),
                    result: null);
                db.CreateProperty(id, id, null, M(CoreProperty.Name), key, null); // Name may be overriden, for instance for ApiMethod for which RouteTemplate is used instead for name
                db.CreateProperty(id, id, null, M(CoreProperty.Key), key, null);
                var a = type.GetAgoRapideAttribute();
                db.CreateProperty(id, id, null, M(CoreProperty.AccessLevelRead), a.AccessLevelRead, null); 
                db.CreateProperty(id, id, null, M(CoreProperty.AccessLevelWrite), a.AccessLevelWrite, null); 
                return db.GetEntityById<T>(id);
            });
            if (!(retvalTemp is T)) throw new InvalidObjectTypeException(retvalTemp, typeof(T), nameof(key) + ": " + key + ", " + nameof(retvalTemp) + ": " + retvalTemp.ToString());
            var retval = (T)retvalTemp;
            if (enrichAndReturnThisObject == null) return retval;
            enrichAndReturnThisObject.Id = retval.Id;
            enrichAndReturnThisObject.Created = retval.Created;
            enrichAndReturnThisObject.RootProperty = retval.RootProperty;
            if (enrichAndReturnThisObject.Properties == null) enrichAndReturnThisObject.Properties = new Dictionary<TProperty, Property<TProperty>>();
            retval.Properties.ForEach(p => {
                if (enrichAndReturnThisObject.Properties.ContainsKey(p.Key)) throw new KeyAlreadyExistsException<TProperty>(p.Key, nameof(enrichAndReturnThisObject) + " should not contain any properties at this stage");
                enrichAndReturnThisObject.Properties.AddValue2(p.Key, p.Value);
            });
            // enrichAndReturnThisObject.Properties.AddValue(M(CoreProperty.RootProperty), retval.RootProperty);
            return enrichAndReturnThisObject;
        }
    }
}