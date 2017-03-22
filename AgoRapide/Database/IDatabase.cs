﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide.Database {
    /// <summary>
    /// TODO: Add TryGetEntityIds and GetEntityIds with <see cref="QueryId{TProperty}"/> as parameter just like done with 
    /// <see cref="GetEntities{T}"/> and <see cref="TryGetEntities{T}"/>
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public interface IDatabase<TProperty> : IDisposable where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// An implementation should support use of 
        /// <see cref="CoreProperty.Username"/>
        /// <see cref="CoreProperty.Password"/>
        /// <see cref="CoreProperty.AuthResult"/>
        /// <see cref="CoreProperty.RejectCredentialsNextTime"/>
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        bool TryVerifyCredentials(string username, string password, out BaseEntityT<TProperty> currentUser);

        /// <summary>
        /// Convenience method, easier alternative to <see cref="TryGetEntities{T}"/>
        /// 
        /// Only use this method for <see cref="QueryId{TProperty}.IsSingle"/> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="useCache"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool TryGetEntity<T>(BaseEntity currentUser, QueryId<TProperty> id, AccessType accessTypeRequired, bool useCache, out T entity, out Tuple<ResultCode, string> errorResponse) where T : BaseEntityT<TProperty>, new();

        /// <summary>
        /// Convenience method, easier alternative to <see cref="TryGetEntities{T}"/>
        /// 
        /// Only use this method for <see cref="QueryId{TProperty}.IsMultiple"/> for which <see cref="TryGetEntities{T}"/> is never expected to return false. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        List<T> GetEntities<T>(BaseEntity currentUser, QueryId<TProperty> id, AccessType accessTypeRequired, bool useCache) where T : BaseEntityT<TProperty>, new();

        /// <summary>
        /// Generic alternative to <see cref="TryGetEntities"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="useCache"></param>
        /// <param name="entities"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        bool TryGetEntities<T>(BaseEntity currentUser, QueryId<TProperty> id, AccessType accessTypeRequired, bool useCache, out List<T> entities, out Tuple<ResultCode, string> errorResponse) where T : BaseEntityT<TProperty>, new();

        /// <summary>
        /// TODO: We could consider having the whole <see cref="AgoRapide.API.Request{TProperty}"/> object as parameter here but
        /// TODO: on the other hand that could couple the API and Database too tightly together.
        /// 
        /// Returns false / <paramref name="errorResponse"/> if nothing found but <paramref name="id"/> indicates that something was expected 
        /// returned, for instance when <see cref="IntegerQueryId{TProperty}"/> or <see cref="QueryId{TProperty}.IsSingle"/>
        /// 
        /// With <paramref name="id"/> as <see cref="PropertyValueQueryId{TProperty}"/> then true is returned even if only an empty list was found. 
        /// 
        /// Throws exception (usually through <see cref="IDatabase{TProperty}.TryGetEntityById"/>) if entity not corresponding to <typeparamref name="T"/> is found. 
        /// 
        /// See also generic <see cref="TryGetEntities{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="useCache"></param>
        /// <param name="requiredType">
        /// Must be set. 
        /// TODO: Note how (as of Feb 2017) <see cref="QueryId{TProperty}.IsMultiple"/>-search will always be according to exactly this type. 
        /// TODO: That is, no implementation limit the search in database to (pseudo-code): 
        /// TODO:    WHERE requiredType.IsAssignableFrom(typeStoredInDatabase) ...
        /// TODO: but uses this instead:
        /// TODO:    WHERE requiredType.Equals(typeStoredInDatabase) 
        /// </param>
        /// <param name="entities"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        bool TryGetEntities(BaseEntity currentUser, QueryId<TProperty> id, AccessType accessTypeRequired, bool useCache, Type requiredType, out List<BaseEntityT<TProperty>> entities, out Tuple<ResultCode, string> errorResponse);

        /// <summary>
        /// See <see cref="CoreMethod.History"/>. 
        /// Implementator should return results with ORDER BY <see cref="DBField.id"/> DESC
        /// 
        /// TODO: Implement some LIMIT statement or throw exception if too many, or add an explanatory message
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        List<Property<TProperty>> GetEntityHistory(BaseEntityT<TProperty> entity);

        /// <summary>
        /// TODO: NOT YET IMPLEMENTED IN IMPLEMENTATION
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="entity"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        bool TryVerifyAccess(BaseEntity currentUser, BaseEntityT<TProperty> entity, AccessType accessTypeRequired, out string errorResponse);

        T GetEntityById<T>(long id) where T : BaseEntityT<TProperty>, new();
        T GetEntityById<T>(long id, bool useCache) where T : BaseEntityT<TProperty>, new();
        BaseEntityT<TProperty> GetEntityById(long id, bool useCache, Type requiredType);
        bool TryGetEntityById<T>(long id, bool useCache, out T entity) where T : BaseEntityT<TProperty>, new();
        /// <summary>
        /// TODO: Rename into TryGetEntityDirect? 
        /// 
        /// Normally do not use this method but use <see cref="TryGetEntities{T}"/> / <see cref="TryGetEntity{T}"/> instead since
        /// they also check access rights. 
        /// 
        /// Only returns false for scenario where entity was not found in database. 
        /// The implementator should redirect to <see cref="TryGetPropertyById"/> if <paramref name="requiredType"/> points to a <see cref="Property{TProperty}"/>-type 
        /// (cache would then normally be ignored in such a case)
        /// 
        /// For other failure scenarios (like <paramref name="requiredType"/> not matching) an exception will be thrown. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="useCache"></param>
        /// <param name="requiredType"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool TryGetEntityById(long id, bool useCache, Type requiredType, out BaseEntityT<TProperty> entity);

        Dictionary<TProperty, Property<TProperty>> GetChildProperties(Property<TProperty> parentProperty);

        Property<TProperty> GetPropertyById(long id);
        bool TryGetPropertyById(long id, out Property<TProperty> property);
        void OperateOnProperty(long operatorId, Property<TProperty> property, PropertyOperation operation, Result<TProperty> result);

        /// <summary>
        /// Gets all root properties of a given type. Result should be in increasing order.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        List<long> GetRootPropertyIds(Type type);

        /// <summary>
        /// Gets all entities of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        List<T> GetAllEntities<T>() where T : BaseEntityT<TProperty>, new();

        long CreateEntity<T>(long cid, Result<TProperty> result) where T : BaseEntityT<TProperty>;
        long CreateEntity(long cid, Type entityType, Result<TProperty> result);
        long CreateEntity<T>(long cid, Parameters<TProperty> properties, Result<TProperty> result) where T : BaseEntityT<TProperty>;
        long CreateEntity(long cid, Type entityType, Parameters<TProperty> properties, Result<TProperty> result);
        long CreateEntity<T>(long cid, IEnumerable<Tuple<TProperty, object>> properties, Result<TProperty> result) where T : BaseEntityT<TProperty>;
        /// <summary>
        /// Returns <see cref="DBField.id"/>
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="entityType"></param>
        /// <param name="properties">May be null or empty. Turn this into an Properties collection? Or just a BaseEntity template or similar?</param>
        /// <param name="result"></param>
        /// <returns></returns>
        long CreateEntity(long cid, Type entityType, IEnumerable<Tuple<TProperty, object>> properties, Result<TProperty> result);

        /// <summary>
        /// Changes to entity given in <see cref="CoreProperty.EntityToRepresent"/> if that property exists for the entity given
        /// If not returns entity given
        /// 
        /// Through this concept the API can give the view of one API client (user) 
        /// based on the credentials of another API-client (administrative user). 
        /// In practise this means that your support department may see exactly the same data as your 
        /// customer sees in your application, without the customer having to give away his / her password.
        /// 
        /// This is typically used to "impersonate" customers through an admin-user. Used by 
        /// <see cref="BaseController{TProperty}.TryGetCurrentUser> and BAPIController.GetCurrentUser 
        /// 
        /// See <see cref="CoreProperty.EntityToRepresent"/> and <see cref="CoreProperty.RepresentedByEntity"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        void SwitchIfHasEntityToRepresent(ref BaseEntityT<TProperty> entity);

        void AssertUniqueness(TProperty key, object value);
        /// <summary>
        /// Only relevant for <paramref name="key"/> <see cref="AgoRapideAttribute.IsUniqueInDatabase"/> 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="existingProperty">Useful for including in exception response (for logging purposes)</param>
        /// <param name="errorResponse">Suitable for returning to API client</param>
        /// <returns></returns>
        bool TryAssertUniqueness(TProperty key, object value, out Property<TProperty> existingProperty, out string errorResponse);

        /// <summary>
        /// Returns id (database primary-key) of property created
        /// 
        /// Note that often <see cref="UpdateProperty{T}"/> can be used instead of <see cref="CreateProperty"/>
        /// 
        /// The implementation should assert (case insensitive) uniqueness of <paramref name="value"/> 
        /// when <see cref="AgoRapideAttribute.IsUniqueInDatabase"/> for <paramref name="key"/>
        /// </summary>
        /// <param name="cid"><see cref="DBField.cid"/> </param>
        /// <param name="pid"><see cref="DBField.pid"/> </param>
        /// <param name="fid"><see cref="DBField.fid"/> </param>
        /// <param name="key">
        /// <see cref="DBField.key"/>. 
        /// For <see cref="AgoRapideAttribute.IsMany"/> this will be <see cref="int.MaxValue"/> minus index
        /// </param>
        /// <param name="value">TODO: Consider strongly typed overloads which leads to less processing here</param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        long CreateProperty(long? cid, long? pid, long? fid, TProperty key, object value, Result<TProperty> result);

        /// <summary>
        /// See <see cref="CoreMethod.UpdateProperty"/>
        /// 
        /// Note that often <see cref="UpdateProperty{T}"/> should be used instead of <see cref="CreateProperty"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cid"><see cref="DBField.cid"/> </param>
        /// <param name="entity"></param>
        /// <param name="p"></param>
        /// <param name="value"></param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        void UpdateProperty<T>(long cid, BaseEntityT<TProperty> entity, TProperty key, T value, Result<TProperty> result);

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="property"></param>
        ///// <param name="iid"><see cref="DBField.iid"/></param>
        //void SetPropertyNoLongerCurrent(Property<TProperty> property, long iid);
    }

    public class ExactOneEntityNotFoundException : ApplicationException {
        public ExactOneEntityNotFoundException() : base() { }
        public ExactOneEntityNotFoundException(string message) : base(message) { }
        public ExactOneEntityNotFoundException(long id) : base("Entity id " + id + " not found") { }
    }

    public class ExactOnePropertyNotFoundException : ApplicationException {
        public ExactOnePropertyNotFoundException(string message) : base(message) { }
        public ExactOnePropertyNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    public class UniquenessException : ApplicationException {
        public UniquenessException(string message) : base(message) { }
        public UniquenessException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidPasswordException<TProperty> : ApplicationException where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public InvalidPasswordException(TProperty property) : this(property, null, null) { }
        public InvalidPasswordException(TProperty property, string message) : this(property, message, null) { }
        public InvalidPasswordException(TProperty property, string message, Exception inner) : base(property.GetAgoRapideAttribute().PExplained + (string.IsNullOrEmpty(message) ? "" : (". Details: " + message)), inner) { }
    }

    public class InvalidPropertyKeyException<TProperty> : ApplicationException where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public InvalidPropertyKeyException() : base() { }
        /// <summary>
        /// </summary>
        /// <param name="key">May be null</param>
        public InvalidPropertyKeyException(string key) : this(key, null) { }
        /// <summary>
        /// </summary>
        /// <param name="key">May be null</param>
        /// <param name="id"></param>
        public InvalidPropertyKeyException(string key, long? id) : base(
            "The key " + (key ?? "[NULL]") + " is not recognized as a valid " + typeof(TProperty).ToString() + "-enum " +
            ((key?.Contains("#") ?? false) ? ("(it was also most probably just checked that it is not a " + nameof(AgoRapideAttribute.IsMany) + "-property either)") : "") +
            (id != null ? ("Possible resolution: Set Property with id " + id + " as no-longer-current in database or delete altogether with SQL-code DELETE FROM p WHERE id = " + id) : "")
            ) { } // TODO: Add link to APIMethod for set-no-longer-current.
    }

    public class PropertyNotFoundException : ApplicationException {
        public PropertyNotFoundException(long id) : base("Property with id '" + id + "' not found") { }
    }
}