// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AgoRapide.Core;
using AgoRapide.API;

/// <summary>
/// TODO: Make into abstract base class inheriting <see cref="BaseCore"/>. Meaningless to have as an interface.
/// </summary>
namespace AgoRapide.Database {
    /// <summary>
    /// TODO: RENAME FILE TO BaseDatabase.
    /// 
    /// TOOD: Move functionality from <see cref="PostgreSQLDatabase"/> into <see cref="BaseDatabase"/>
    /// TODO: Abstract the basic <see cref="Npgsql.NpgsqlCommand"/> and similar, in order to support multiple databases
    /// TODO: without implementing full sub classes of <see cref="BaseDatabase"/>.
    /// 
    /// TODO: Add TryGetEntityIds and GetEntityIds with <see cref="QueryId"/> as parameter just like done with 
    /// <see cref="GetEntities{T}"/> and <see cref="TryGetEntities{T}"/>
    /// </summary>
    public abstract class BaseDatabase : BaseCore, IDisposable {

        [ClassMember(Description = "Name of main table in database. Also used for naming other objects in database like sequence_p_id.")]
        protected string _tableName { get; private set; }

        [ClassMember(Description =
            "Used for logging purposes. Will for instance show in pgAdmin Tools | Server status. " +
            "Use a type that best describes your application, for instance typeof(CustomerController) or similar. "
        )]
        protected Type _applicationType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"><see cref="_tableName"/></param>
        /// <param name="applicationType"><see cref="_applicationType"/></param>
        public BaseDatabase(string tableName, Type applicationType) {
            Log(nameof(tableName) + ": " + tableName + ", " + nameof(applicationType) + ": " + applicationType.ToString());
            _tableName = tableName;
            _applicationType = applicationType;
        }

        public void UseInMemoryCache<T>(BaseSynchronizer synchronizer) {
        }
        /// <summary>
        /// An implementation should support use of the following <see cref="CoreP"/> properties: 
        /// <see cref="CoreP.Username"/>
        /// <see cref="CoreP.Password"/>
        /// <see cref="CoreP.AuthResult"/>
        /// <see cref="CoreP.RejectCredentialsNextTime"/>
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        public abstract bool TryVerifyCredentials(string username, string password, out BaseEntity currentUser);

        /// <summary>
        /// Convenience method, easier alternative to <see cref="TryGetEntities{T}"/>
        /// 
        /// Only use this method for <see cref="QueryId.IsSingle"/> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract bool TryGetEntity<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, out T entity, out ErrorResponse errorResponse) where T : BaseEntity, new();

        /// <summary>
        /// Convenience method, easier alternative to <see cref="TryGetEntities{T}"/>
        /// 
        /// Only use this method for <see cref="QueryId.IsMultiple"/> for which <see cref="TryGetEntities{T}"/> is never expected to return false. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <returns></returns>
        public abstract List<T> GetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired) where T : BaseEntity, new();

        public abstract List<BaseEntity> GetEntities(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, Type requiredType);

        /// <summary>
        /// Generic alternative to <see cref="TryGetEntities"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="entities"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public abstract bool TryGetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, out List<T> entities, out ErrorResponse errorResponse) where T : BaseEntity, new();

        /// <summary>
        /// TODO: We could consider having the whole <see cref="AgoRapide.API.Request"/> object as parameter here but
        /// TODO: on the other hand that could couple the API and Database too tightly together.
        /// 
        /// Returns false / <paramref name="errorResponse"/> if nothing found but <paramref name="id"/> indicates that something was expected 
        /// returned, for instance when <see cref="QueryIdInteger"/> or <see cref="QueryId.IsSingle"/>
        /// 
        /// With <paramref name="id"/> as <see cref="QueryIdKeyOperatorValue"/> then true is returned even if only an empty list was found. 
        /// 
        /// Throws exception (usually through <see cref="BaseDatabase.TryGetEntityById"/>) if entity not corresponding to <typeparamref name="T"/> is found. 
        /// 
        /// See also generic <see cref="TryGetEntities{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="requiredType">
        /// Must be set. 
        /// TODO: Note how (as of Feb 2017) <see cref="QueryId.IsMultiple"/>-search will always be according to exactly this type. 
        /// TODO: That is, no implementation limit the search in database to (pseudo-code): 
        /// TODO:    WHERE requiredType.IsAssignableFrom(typeStoredInDatabase) ...
        /// TODO: but uses this instead:
        /// TODO:    WHERE requiredType.Equals(typeStoredInDatabase) 
        /// </param>
        /// <param name="entities"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public abstract bool TryGetEntities(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, Type requiredType, out List<BaseEntity> entities, out ErrorResponse errorResponse);

        /// <summary>
        /// See <see cref="CoreAPIMethod.History"/>. 
        /// Implementator should return results with ORDER BY <see cref="DBField.id"/> DESC
        /// 
        /// TODO: Implement some LIMIT statement or throw exception if too many, or add an explanatory message
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract List<Property> GetEntityHistory(BaseEntity entity);

        /// <summary>
        /// TODO: NOT YET IMPLEMENTED IN IMPLEMENTATION
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="entity"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public abstract bool TryVerifyAccess(BaseEntity currentUser, BaseEntity entity, AccessType accessTypeRequired, out string errorResponse);

        public abstract T GetEntityById<T>(long id) where T : BaseEntity, new();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requiredType">May be null</param>
        /// <returns></returns>
        public abstract BaseEntity GetEntityById(long id, Type requiredType);
        public abstract bool TryGetEntityById<T>(long id, out T entity) where T : BaseEntity, new();
        /// <summary>
        /// TODO: Rename into TryGetEntityDirect? 
        /// 
        /// Normally do not use this method but use <see cref="TryGetEntities{T}"/> / <see cref="TryGetEntity{T}"/> instead since
        /// they also check access rights. 
        /// 
        /// Only returns false for scenario where entity was not found in database. 
        /// The implementator should redirect to <see cref="TryGetPropertyById"/> if <paramref name="requiredType"/> points to a <see cref="Property"/>-type 
        /// (cache would then normally be ignored in such a case)
        /// 
        /// For other failure scenarios (like <paramref name="requiredType"/> not matching) an exception will be thrown. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requiredType"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract bool TryGetEntityById(long id, Type requiredType, out BaseEntity entity);

        public abstract Dictionary<CoreP, Property> GetChildProperties(Property parentProperty);

        public abstract Property GetPropertyById(long id);
        public abstract bool TryGetPropertyById(long id, out Property property);

        /// <summary>
        /// Gets all root properties of a given type. Result should be in increasing order.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract List<long> GetRootPropertyIds(Type type);

        /// <summary>
        /// Gets all entities of type <typeparamref name="T"/>. 
        /// TODO: Add overload which can be limited to source / workspace or similar. 
        /// TODO: Or use <see cref="TryGetEntities"/> for that purpose. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract List<T> GetAllEntities<T>() where T : BaseEntity, new();

        public long CreateEntity<T>(long cid, Result result) => CreateEntity(cid, typeof(T), properties: (IEnumerable<(PropertyKeyWithIndex key, object value)>)null, result: result);
        public long CreateEntity(long cid, Type entityType, Result result) => CreateEntity(cid, entityType, properties: (IEnumerable<(PropertyKeyWithIndex key, object value)>)null, result: result);
        public long CreateEntity<T>(long cid, Parameters properties, Result result) => CreateEntity(cid, typeof(T), properties.Properties.Values.Select(p => (p.Key, p.Value)), result);
        public long CreateEntity(long cid, Type entityType, Parameters properties, Result result) => CreateEntity(cid, entityType, properties.Properties.Values.Select(p => (p.Key, p.Value)), result);
        public long CreateEntity<T>(long cid, IEnumerable<(PropertyKeyWithIndex key, object value)> properties, Result result) => CreateEntity(cid, typeof(T), properties, result);
        public long CreateEntity<T>(long cid, Dictionary<CoreP, Property> properties, Result result) => CreateEntity(cid, typeof(T), properties.Values.Select(p => (p.Key, p.Value)), result);
        
        /// <summary>
        /// Returns <see cref="DBField.id"/>
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="entityType"></param>
        /// <param name="properties">May be null or empty. Turn this into an Properties collection? Or just a BaseEntity template or similar?</param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        public abstract long CreateEntity(long cid, Type entityType, IEnumerable<(PropertyKeyWithIndex key, object value)> properties, Result result);

        // public abstract void SwitchIfHasEntityToRepresent(ref BaseEntity entity);

        public abstract void AssertUniqueness(PropertyKeyWithIndex key, object value);
        /// <summary>
        /// Only relevant for <paramref name="key"/> <see cref="PropertyKeyAttribute.IsUniqueInDatabase"/> 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="existingProperty">Useful for including in exception response (for logging purposes)</param>
        /// <param name="errorResponse">Suitable for returning to API client</param>
        /// <returns></returns>
        public abstract bool TryAssertUniqueness(PropertyKeyWithIndex key, object value, out Property existingProperty, out string errorResponse);

        /// <summary>
        /// Returns id (database primary-key) of property created
        /// 
        /// Note that often <see cref="UpdateProperty{T}"/> can be used instead of <see cref="CreateProperty"/>
        /// 
        /// The implementation should assert (case insensitive) uniqueness of <paramref name="value"/> 
        /// when <see cref="PropertyKeyAttribute.IsUniqueInDatabase"/> for <paramref name="key"/>
        /// </summary>
        /// <param name="cid">
        /// Note how null is allowed but is strongly discouraged. Null should only be relevant at application startup. 
        /// Use <see cref="ApplicationPart.GetClassMember"/> in order to get a <paramref name="cid"/>. 
        /// <see cref="DBField.cid"/> 
        /// </param>
        /// <param name="pid"><see cref="DBField.pid"/> </param>
        /// <param name="fid"><see cref="DBField.fid"/> </param>
        /// <param name="key">
        /// <see cref="DBField.key"/>. 
        /// </param>
        /// <param name="value">TODO: Consider strongly typed overloads which leads to less processing here</param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        public abstract long CreateProperty(long? cid, long? pid, long? fid, PropertyKeyWithIndex key, object value, Result result);

        // public abstract void UpdateProperty<T>(long cid, BaseEntity entity, PropertyKey key, T value, Result result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operatorId">
        /// Note how null is allowed but is strongly discouraged. Null should only be relevant at application startup. 
        /// Use <see cref="ApplicationPart.GetClassMember"/> in order to get a <paramref name="cid"/>. 
        /// <paramref name="operatorId"/> will be used as either <see cref="DBField.vid"/> or <see cref="DBField.iid"/>. 
        /// </param>
        /// <param name="property"></param>
        /// <param name="operation"></param>
        /// <param name="result">May be null</param>
        public abstract void OperateOnProperty(long? operatorId, Property property, PropertyOperation operation, Result result);

        public abstract void Dispose();

        /// <summary>
        /// See <see cref="CoreAPIMethod.UpdateProperty"/>
        /// 
        /// Note that often <see cref="UpdateProperty{T}"/> should be used instead of <see cref="CreateProperty"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cid"><see cref="DBField.cid"/> </param>
        /// <param name="entity"></param>
        /// <param name="key">
        /// For <see cref="PropertyKeyAttribute.IsMany"/> if <paramref name="key"/> is instance of <see cref="PropertyKeyWithIndex"/> 
        /// then the implementator shall take into account <see cref="PropertyKeyWithIndex.Index"/>
        /// If not it shall call <see cref="Property.GetNextIsManyId"/>. 
        /// </param>
        /// <param name="value"></param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        public void UpdateProperty<T>(long cid, BaseEntity entity, PropertyKey key, T value, Result result) {
            // Log(""); Note how we only log when property is actually created or updated
            var detailer = new Func<string>(() => nameof(entity) + ": " + entity.Id + ", " + nameof(key) + ": " + key + ", " + nameof(value) + ": " + value + ", " + nameof(cid) + ": " + cid);
            if (entity.Properties == null) throw new NullReferenceException(nameof(entity) + "." + nameof(entity.Properties) + ", " + detailer());

            var creator = new Func<PropertyKeyWithIndex, object, Property>((finalKey, valueToCreate) => {
                var retval = GetPropertyById(CreateProperty(cid, entity.Id, null, finalKey, valueToCreate, result));
                finalKey.AssertEquals(retval.Key, () => retval.ToString());
                return retval;
            });

            var entityOrIsManyParentUpdater = new Action<BaseEntity, CoreP, object>((entityOrIsManyParent, keyAsCoreP, valueToUpdate) => {

                var keyToUse = key as PropertyKeyWithIndex; // Note use of "strict" variant here
                if (keyToUse == null) keyToUse = key.PropertyKeyIsSet ? key.PropertyKeyWithIndex : throw new PropertyKeyWithIndex.InvalidPropertyKeyException("Unable to turn " + key + " (of type " + key.GetType() + ") into a " + typeof(PropertyKeyWithIndex) + " because !" + nameof(key.PropertyKeyIsSet) + detailer.Result("\r\nDetails: "));

                if (
                    entityOrIsManyParent.Properties.TryGetValue(keyAsCoreP, out var existingProperty) &&
                    existingProperty.Id > 0 /// This last check is very important, it might be that the property was only added in-memory
                    ) {
                    var existingValue = existingProperty.V<T>();
                    if (existingValue.Equals(valueToUpdate)) {
                        // Note how this is not logged
                        if (existingProperty.Id <= 0) {
                            // Skip this, property is in-memory only 
                        } else {
                            OperateOnProperty(cid, existingProperty, PropertyOperation.SetValid, result);
                        }
                        result?.Count(typeof(Property), CountP.StillValid);
                    } else { // Vanlig variant
                             // TODO: Use of default value.ToString() here is not optimal
                             // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                             // TODO: At least for presenting DateTime-objects and double in a preferred manner
                        Log(detailer() + ". Property changed from '" + existingValue + "' to '" + valueToUpdate + "'", result);
                        var changedProperty = creator(keyToUse, valueToUpdate);
                        result?.Count(typeof(Property), CountP.Changed);
                        entityOrIsManyParent.Properties[keyAsCoreP] = changedProperty; // TOOD: result-Property from creator has not been initialized properly now (with Parent for instance)
                    }
                } else {
                    // TODO: Use of default value.ToString() here is not optimal
                    // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                    // TODO: At least for presenting DateTime-objects and double in a preferred manner
                    Log(detailer() + ". Property was not known. Initial value: '" + valueToUpdate + "'", result);
                    var newProperty = creator(keyToUse, valueToUpdate);
                    entityOrIsManyParent.Properties[keyAsCoreP] = newProperty; // TOOD: result-Property from creator has not been initialized properly now (with Parent for instance)
                    result?.Count(typeof(Property), CountP.Total);
                }
            });

            if (key.Key.A.IsMany) {
                var isManyParent = entity.Properties.GetOrAddIsManyParent(key);
                var propertyKey = key as PropertyKeyWithIndex;
                if (propertyKey == null || propertyKey.Index == 0) {

                    switch (value) {
                        case System.Collections.IList list: { // TODO: This code is not optimal
                                InvalidTypeException.AssertList(typeof(T), key, null);
                                /// This is an "all-in-one-go" update. Compare each old element with each new element. 

                                /// Do <see cref="PropertyOperation.SetInvalid"/> and <see cref="PropertyOperation.SetValid"/> as appropriate for existing
                                /// properties and add new ones                            

                                if (key.Key.A.Type.IsValueType) {
                                    if (key.Key.A.Type.IsEnum) {
                                        /// OK because stored as string in database anyway <see cref="DBField.strv"/>
                                    } else {
                                        /// TODO: Implement for all types, not only reference types. As code below stands it has to be written repeatedly for each value type. 
                                        /// TOOD: Add support for other types, at least long, double, boolean, DateTime. Others can just be converted to string
                                        throw new NotImplementedException(key.Key.A.Type + " " + nameof(Type.IsValueType) + detailer.Result("\r\nDetails: "));
                                    }
                                }
                                // InvalidTypeException.AssertEquals(key.Key.A.Type, typeof(string), () => "Update for List<> only supported for List<string>");

                                var remainingToAdd = list.ToList<string>();
                                var toInvalidate = new List<Property>();
                                var toBeDeleted = new List<CoreP>();
                                isManyParent.Properties.ForEach(existingProperty => { /// Note that entityOrIsManyParentUpdater can not be used because it relies on <typeparam name="T"/>
                                    var s = existingProperty.Value.V<string>();
                                    if (remainingToAdd.Contains(s)) { /// Note how we compare with string while entityOrIsManyParentUpdater compares directly against <typeparam name="T"/>
                                        if (existingProperty.Value.Id <= 0) {
                                            // Skip this, property is in-memory only 
                                        } else {
                                            OperateOnProperty(cid, existingProperty.Value, PropertyOperation.SetValid, result);
                                        }
                                        remainingToAdd.Remove(s);
                                    } else {
                                        if (existingProperty.Value.Id <= 0) {
                                            // Skip this, property is in-memory only 
                                        } else {
                                            OperateOnProperty(cid, existingProperty.Value, PropertyOperation.SetInvalid, result);
                                        }
                                        // result?.Count(CoreP.PSetInvalidCount); // TODO: Correct statistics here
                                        toBeDeleted.Add(existingProperty.Key);
                                    }
                                });
                                toBeDeleted.ForEach(c => isManyParent.Properties.Remove(c));
                                remainingToAdd.ForEach(s => {
                                    var keyWithIndex = isManyParent.GetNextIsManyId();
                                    isManyParent.Properties[keyWithIndex.IndexAsCoreP] = creator(keyWithIndex, s);
                                });
                                break;
                            }
                        default: {
                                // This is understood as create new property with next available id
                                // Like Person/42/AddProperty/PhoneNumber/1234
                                var keyWithIndex = isManyParent.GetNextIsManyId();
                                isManyParent.Properties[keyWithIndex.IndexAsCoreP] = creator(keyWithIndex, value);
                                break;
                            }
                    }
                } else {
                    // This corresponds to client knowing exact which id to use 
                    // Like Person/42/AddProperty/PhoneNumber#3/1234
                    entityOrIsManyParentUpdater(isManyParent, propertyKey.IndexAsCoreP, value);
                }
            } else {
                entityOrIsManyParentUpdater(entity, key.Key.CoreP, value);
            }
        }

        protected long GetId(MemberInfo memberInfo) => GetIdNonStrict(memberInfo) ?? throw new NullReferenceException(MethodBase.GetCurrentMethod().Name + ". Check for " + nameof(ApplicationPart.GetFromDatabaseInProgress) + ". Consider calling " + nameof(GetIdNonStrict) + " instead");

        /// <summary>
        /// Returns <see cref="DBField.id"/> of <see cref="ClassMember"/> corresponding to <paramref name="memberInfo"/> 
        /// for use as <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> 
        /// (note that usually you should use the "currentUser".id for this purpose). 
        /// 
        /// Note how null is returned when <see cref="ApplicationPart.GetFromDatabaseInProgress"/>
        /// </summary>
        /// <param name="caller"></param>
        /// <returns></returns>
        protected long? GetIdNonStrict(MemberInfo memberInfo) {
            if (ApplicationPart.GetFromDatabaseInProgress) {
                /// This typical happens when called from <see cref="ReadAllPropertyValuesAndSetNoLongerCurrentForDuplicates"/> because that one wants to
                /// <see cref="PropertyOperation.SetInvalid"/> some <see cref="Property"/> for a <see cref="ClassMember"/>.
                return null;
            }
            // THIS WILL MOST PROBABLY NOT WORK BECAUSE OVER OVERLOADS
            return ApplicationPart.GetClassMember(memberInfo, this).Id;
        }

        /// <summary>
        /// TODO: Should we add checks for AccessRights here? 
        /// TOIDO: See comments for <see cref="CoreP.EntityToRepresent"/>
        /// 
        /// Changes to entity given in <see cref="CoreP.EntityToRepresent"/> if that property exists for the entity given
        /// If not returns entity given
        /// 
        /// Through this concept the API can give the view of one API client (user) 
        /// based on the credentials of another API-client (administrative user). 
        /// In practise this means that your support department may see exactly the same data as your 
        /// customer sees in your application, without the customer having to give away his / her password.
        /// 
        /// This is typically used to "impersonate" customers through an admin-user. Used by 
        /// <see cref="BaseController.TryGetCurrentUser>"/>.
        /// 
        /// See <see cref="CoreP.EntityToRepresent"/> and <see cref="CoreP.RepresentedByEntity"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public void SwitchIfHasEntityToRepresent(ref BaseEntity entity) {
            if (entity.TryGetPV(CoreP.EntityToRepresent.A(), out long representedEntityId)) {
                Log("entityId: " + entity.Id + ", switching to " + representedEntityId);
                var representedByEntity = entity;
                var entityToRepresent = GetEntityById<BaseEntity>(representedEntityId);
                // TODO: Should we add checks for AccessRights here? 
                /// See comments for <see cref="CoreP.EntityToRepresent"/>
                entityToRepresent.AddProperty(CoreP.RepresentedByEntity.A(), representedByEntity.Id);
                entityToRepresent.RepresentedByEntity = representedByEntity;
                entity = entityToRepresent;
            }
        }

        ///// <summary>
        ///// </summary>
        ///// <param name="text"></param>
        ///// <param name="result">May be null</param>
        ///// <param name="caller"></param>
        //protected void Log(string text, Result result, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
        //    Log(text, caller);
        //    result?.LogInternal(text, GetType(), caller);
        //}

        public class OpenDatabaseConnectionException : ApplicationException {
            public OpenDatabaseConnectionException(string message) : base(message) { }
            public OpenDatabaseConnectionException(string message, Exception inner) : base(message, inner) { }
        }

        public class NoResultFromDatabaseException : ApplicationException {
            public NoResultFromDatabaseException(string message) : base(message) { }
            public NoResultFromDatabaseException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class ExactOneEntityNotFoundException : ApplicationException {
            public ExactOneEntityNotFoundException() : base() { }
            public ExactOneEntityNotFoundException(string message) : base(message) { }
            public ExactOneEntityNotFoundException(long id) : base("Entity id " + id + " not found") { }
        }

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class ExactOnePropertyNotFoundException : ApplicationException {
            public ExactOnePropertyNotFoundException(string message) : base(message) { }
            public ExactOnePropertyNotFoundException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class UniquenessException : ApplicationException {
            public UniquenessException(string message) : base(message) { }
            public UniquenessException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class InvalidPasswordException<T> : ApplicationException where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            public InvalidPasswordException(T property) : this(property, null, null) { }
            public InvalidPasswordException(T property, string message) : this(property, message, null) { }
            public InvalidPasswordException(T property, string message, Exception inner) : base(property.GetEnumValueAttribute().EnumValueExplained + (string.IsNullOrEmpty(message) ? "" : (". Details: " + message)), inner) { }
        }

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class PropertyNotFoundException : ApplicationException {
            public PropertyNotFoundException(long id) : base("Property with id '" + id + "' not found") { }
        }
    }
}