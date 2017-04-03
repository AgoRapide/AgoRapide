using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AgoRapide.Core;

namespace AgoRapide.Database {

    /// <summary>
    /// PostgreSQL database implementation of <see cref="IDatabase"/> (see that for documentation). 
    /// 
    /// You should probably inherit this class, and use that subclass in your project, in order to have the necessary flexibility.
    /// (even better would of course be some interface implementation in order not to depend on PostgreSQL)
    /// 
    /// TODO: Add TryGetEntityIds and GetEntityIds with <see cref="QueryId"/> as parameter just like done with 
    /// <see cref="GetEntities{T}"/> and <see cref="TryGetEntities{T}"/>
    /// </summary>
    public class PostgreSQLDatabase : BaseCore, IDatabase {

        protected string _connectionString;
        protected Type _applicationType;

        /// <summary>
        /// Always open
        /// </summary>
        protected Npgsql.NpgsqlConnection _cn1;
        // protected Npgsql.NpgsqlConnection _cn2;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logPath"></param>
        /// <param name="applicationType">
        /// Used for logging purposes. Will for instance show in pgAdmin Tools | Server status.
        /// </param>
        public PostgreSQLDatabase(string connectionString, Type applicationType) {
            Log("applicationId: " + applicationType.ToString()); // Do not log connectionString (may contain password). Also do not log logPath (assumed not interesting)
            if (!connectionString.EndsWith(";")) connectionString += ";";
            if (connectionString.ToLower().Contains("applicationname")) throw new Exception("Illegal to set 'ApplicationName = ... ' in connection string (will be set by " + nameof(OpenConnection) + ")");
            _connectionString = connectionString;
            _applicationType = applicationType;
            OpenConnection(out _cn1, "_cn1");
        }

        public List<T> GetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, bool useCache) where T : BaseEntityT, new() {
            id.AssertIsMultiple();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, useCache, out List<T> entities, out var errorResponse)) throw new InvalidCountException(id + ". Details: " + errorResponse.ResultCode + ", " + errorResponse.Message);
            return entities;
        }

        public bool TryGetEntity<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, bool useCache, out T entity, out ErrorResponse errorResponse) where T : BaseEntityT, new() {
            id.AssertIsSingle();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, useCache, out List<T> temp, out errorResponse)) {
                entity = null;
                return false;
            }
            temp.AssertExactOne(() => nameof(id) + ": " + id.ToString());
            entity = temp[0];
            return true;
        }

        public bool TryGetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, bool useCache, out List<T> entities, out ErrorResponse errorResponse) where T : BaseEntityT, new() {
            if (!TryGetEntities(currentUser, id, accessTypeRequired, useCache, typeof(T), out var temp, out errorResponse)) {
                entities = null;
                return false;
            }
            entities = temp.Select(e => (T)e).ToList();
            return true;
        }

        public bool TryGetEntities(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, bool useCache, Type requiredType, out List<BaseEntityT> entities, out ErrorResponse errorResponse) {
            Log(nameof(id) + ": " + (id?.ToString() ?? throw new ArgumentNullException(nameof(id))) + ", " + nameof(accessTypeRequired) + ": " + accessTypeRequired + ", " + nameof(useCache) + ": " + useCache + ", " + nameof(requiredType) + ": " + (requiredType?.ToStringShort() ?? throw new ArgumentNullException(nameof(requiredType))));
            switch (id) {
                case IntegerQueryId integerId: /// Note how <see cref="QueryId.SQLWhereStatement"/> is not used in this case. 
                    if (!TryGetEntityById(integerId.Id, useCache, requiredType, out BaseEntityT temp)) {
                        entities = null;
                        errorResponse = new ErrorResponse(ResultCode.data_error, requiredType.ToStringVeryShort() + " with " + nameof(id) + " " + id + " not found");
                        return false;
                    }
                    if (!TryVerifyAccess(currentUser, temp, accessTypeRequired, out var strErrorResponse)) {
                        entities = null;
                        errorResponse = new ErrorResponse(ResultCode.access_error, strErrorResponse);
                        return false;
                    }
                    entities = new List<BaseEntityT> { temp };
                    errorResponse = null;
                    return true;
            }

            var cmd = new Npgsql.NpgsqlCommand(
                "SELECT DISTINCT(pid) FROM p WHERE\r\n" +
                /// TODO: Turn <param name="requiredType"/> into ... WHERE IN ( ... ) for all sub-classes.
                DBField.pid + " IN\r\n" +
                "(SELECT " + DBField.id + " FROM p WHERE " + DBField.key + " = '" + CoreP.Type + "' AND " + DBField.strv + " = '" + requiredType.ToStringDB() + "') AND\r\n" +
                // string.IsNullOrEmpty(id.SQLWhereStatement) er aktuelt for når id er All.
                id.SQLWhereStatement + (string.IsNullOrEmpty(id.SQLWhereStatement) ? "" : " AND\r\n") +
                DBField.invalid + " IS NULL "
                // + "ORDER BY " + DBField.id
                , _cn1
            );
            if (requiredType.Equals(typeof(BaseEntityT))) throw new InvalidTypeException(requiredType, "Meaningless value because the query\r\n\r\n" + cmd.CommandText + "\r\n\r\nwill never return anything");

            AddIdParameters(id, cmd);
            // Log(cmd.CommandText);
            var allEntities = ReadAllIds(cmd).Select(pid => GetEntityById(pid, useCache, requiredType)).ToList();
            Log(nameof(allEntities) + ".Count: " + allEntities.Count);
            id.AssertCountFound(allEntities.Count);
            var lastAccessErrorResponse = "";
            entities = allEntities.Where(e => TryVerifyAccess(currentUser, e, accessTypeRequired, out lastAccessErrorResponse)).ToList();
            Log(nameof(entities) + ".Count: " + entities.Count + " (after call to " + nameof(TryVerifyAccess) + ")");
            if (id.IsSingle) { /// Relevant for <see cref="AgoRapideAttribute.IsUniqueInDatabase"/>
                if (allEntities.Count == 0) {
                    entities = null;
                    errorResponse = new ErrorResponse(ResultCode.data_error, requiredType.ToStringVeryShort() + " with " + nameof(id) + " " + id + " not found");
                    return false;
                } else if (entities.Count == 0 && allEntities.Count > 0) {
                    entities = null;
                    errorResponse = new ErrorResponse(ResultCode.access_error, lastAccessErrorResponse);
                    return false;
                }
                errorResponse = null;
            } else {
                /// Note how we never return <see cref="ResultCode.data_error"/> nor <see cref="ResultCode.access_error"/> 
                /// here because it would be difficult to find a consistent rationale for how to do that
                errorResponse = null;
            }
            return true;
        }

        /// <summary>
        /// TODO: Implement some LIMIT statement or throw exception if too many, or add an explanatory message
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<Property> GetEntityHistory(BaseEntityT entity) {
            Log(nameof(entity.Id) + ": " + entity.Id);
            var p = entity as Property;
            var cmd = p == null ?
                new Npgsql.NpgsqlCommand(PropertySelect + " WHERE " + DBField.pid + " = " + entity.Id + " ORDER BY " + DBField.id + " DESC", _cn1) : // All history for the entity
                new Npgsql.NpgsqlCommand(PropertySelect + " WHERE " + DBField.pid + " = " + p.ParentId + " AND " + DBField.key + " = '" + p.KeyDB + "' ORDER BY " + DBField.id + " DESC", _cn1); // History for property only
            /// TODO: For p == null consider verifying access for each and every property.
            return ReadAllPropertyValues(cmd);
        }

        void AssertAccess(BaseEntity currentUser, BaseEntityT entity, AccessType accessTypeRequired) {
            if (!TryVerifyAccess(currentUser, entity, accessTypeRequired, out var errorResponse)) throw new AccessViolationException(nameof(currentUser) + " " + currentUser.Id + " " + nameof(currentUser.AccessLevelGiven) + " " + currentUser.AccessLevelGiven + " insufficent for " + entity.Id + " (" + nameof(accessTypeRequired) + ": " + accessTypeRequired + "). Details: " + errorResponse);
        }

        /// <summary>
        /// TODO: NOT YET IMPLEMENTED
        /// 
        /// Idea for implementation:
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="entity"></param>
        /// <param name="accessTypeRequired"></param>
        /// <returns></returns>
        public bool TryVerifyAccess(BaseEntity currentUser, BaseEntityT entity, AccessType accessTypeRequired, out string errorResponse) {
            errorResponse = null;
            return true;
        }

        /// <summary>
        /// Adds the <see cref="QueryId.SQLWhereStatementParameters"/> 
        /// from <paramref name="id"/> to <paramref name="cmd"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cmd"></param>
        private void AddIdParameters(QueryId id, Npgsql.NpgsqlCommand cmd) =>
            id.SQLWhereStatementParameters.ForEach(p => {
                cmd.Parameters.Add(new Npgsql.NpgsqlParameter(p.key, new Func<NpgsqlTypes.NpgsqlDbType>(() => {
                    if (p.value is double) return NpgsqlTypes.NpgsqlDbType.Double;
                    if (p.value is DateTime) return NpgsqlTypes.NpgsqlDbType.Timestamp;
                    if (p.value is string) return NpgsqlTypes.NpgsqlDbType.Text;
                    throw new InvalidObjectTypeException(p.Item2, nameof(id) + ": " + id.ToString());
                })()) { Value = p.value });
            });

        public T GetEntityById<T>(long id) where T : BaseEntityT, new() => GetEntityById<T>(id, useCache: false);
        public T GetEntityById<T>(long id, bool useCache) where T : BaseEntityT, new() => TryGetEntityById(id, useCache, typeof(T), out var retval) ? (T)(object)retval : throw new ExactOneEntityNotFoundException(id);
        public BaseEntityT GetEntityById(long id, bool useCache, Type requiredType) => TryGetEntityById(id, useCache, requiredType, out var retval) ? retval : throw new ExactOneEntityNotFoundException(id);

        public bool TryGetEntityById<T>(long id, bool useCache, out T entity) where T : BaseEntityT, new() {
            if (!TryGetEntityById(id, useCache, typeof(T), out var retval)) {
                entity = null;
                return false;
            }
            entity = (T)retval;
            return true;
        }

        public bool TryGetEntityById(long id, bool useCache, Type requiredType, out BaseEntityT entity) {
            Log(nameof(id) + ": " + id + ", " + nameof(useCache) + ": " + useCache + ", " + nameof(requiredType) + ": " + requiredType?.ToStringShort() ?? "[NULL]");
            if (id <= 0) throw new Exception("id <= 0 (" + id + ")");
            if (requiredType != null && typeof(Property).IsAssignableFrom(requiredType)) {
                // TODO: Should we also cache single properties?
                var retvalTemp = TryGetPropertyById(id, out var propertyTemp);
                if (retvalTemp) InvalidTypeException.AssertAssignable(propertyTemp.GetType(), requiredType, () => nameof(requiredType) + " (" + requiredType + ") does not match Property type as found in database (" + propertyTemp.GetType() + ")");
                entity = propertyTemp;
                return retvalTemp;
                // throw new InvalidTypeException(requiredType, "Do not call this method for properties, use " + nameof(TryGetPropertyById) + " directly instead.");
            }

            if (useCache && Util.EntityCache.TryGetValue(id, out var entityTemp)) {
                if (entityTemp == null) {
                    entity = null;
                    return false;
                }
                entity = entityTemp as BaseEntityT ?? throw new InvalidTypeException(entityTemp.GetType(), typeof(BaseEntityT));
                if (requiredType != null && !requiredType.IsAssignableFrom(entity.GetType())) throw new InvalidTypeException(entity.GetType(), requiredType, "Entity found in cache does not match required type");
                return true;
            }

            if (!TryGetPropertyById(id, out var root)) {
                Util.EntityCache[id] = null;
                entity = null;
                return false;
            }
            if (!root.Key.Key.CoreP.Equals(CoreP.Type)) {
                if (requiredType.Equals(typeof(BaseEntityT))) {
                    // OK, return what we have got. 

                    // TODO: Should we also cache single properties?
                    entity = root;
                    return true;
                }
                throw new InvalidEnumException(root.Key.Key.CoreP, "Expected " + EnumMapper.GetA(CoreP.Type).PExplained + " but got " + nameof(root.KeyDB) + ": " + root.KeyDB + ". " +
                    (requiredType == null ?
                        ("Possible cause: Method " + System.Reflection.MethodBase.GetCurrentMethod().Name + " was called without " + nameof(requiredType) + " and a redirect to " + nameof(TryGetPropertyById) + " was therefore not possible") :
                        ("Possible cause: " + nameof(id) + " does not point to an 'entity root-property'")
                    )
                );
            }

            var rootType = root.V<Type>();
            if (requiredType != null) {
                InvalidTypeException.AssertAssignable(requiredType, typeof(BaseEntityT), () => "Regards parameter " + nameof(requiredType));
                InvalidTypeException.AssertAssignable(rootType, requiredType, () => nameof(requiredType) + " (" + requiredType + ") does not match " + nameof(rootType) + " (" + rootType + " (as found in database as " + root.V<string>() + "))");
            }

            if (rootType.IsAbstract) throw new InvalidTypeException(rootType, nameof(rootType) + " (as found in database as " + root.V<string>() + ")");
            Log("System.Activator.CreateInstance(requiredType) (" + rootType.ToStringShort() + ")");
            // Note how "where T: new()" constraint helps to ensure that we have a parameter less constructor now
            // We could of course also check with rootType.GetConstructor first.
            var retval = Activator.CreateInstance(rootType) as BaseEntityT ?? throw new InvalidTypeException(rootType, "Very unexpected since was just asserted OK");

            retval.Id = id;
            retval.RootProperty = root;
            retval.Created = root.Created;
            retval.Properties = GetChildProperties(root);
            retval.Properties.Values.ForEach(p => p.Parent = retval);
            retval.Properties.AddValue(CoreP.RootProperty, root);
            retval.AddProperty(CoreP.DBId.A(), id);

            // We can not use the short representation because Property will (if asked like V<Type> or similar), try
            // to reconstruct the actual type.
            // retval.AddProperty(M(CoreP.Type), retval.GetType().ToStringShort());
            // Instead we must use the full representation:

            /// Unnecessary because root itself will contain the type.
            // retval.AddProperty(M(CoreP.Type), retval.GetType().ToStringDB()); 

            Util.EntityCache[id] = retval; // Note how entity itself is stored in cache, not root-property
            entity = retval;
            return true;
        }

        public Property GetPropertyById(long id) => TryGetPropertyById(id, out var retval) ? retval : throw new PropertyNotFoundException(id);
        public bool TryGetPropertyById(long id, out Property property) {
            Log(nameof(id) + ": " + id);
            var cmd = new Npgsql.NpgsqlCommand(PropertySelect + " WHERE " + DBField.id + " = " + id, _cn1);
            lock (cmd.Connection) {
                Npgsql.NpgsqlDataReader r;
                try {
                    r = cmd.ExecuteReader();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                if (!r.Read()) {
                    property = null;
                    r.Close();
                    return false;
                }
                property = ReadOneProperty(r);
                if (r.Read()) throw new ExactOnePropertyNotFoundException("Multiple properties found for id " + id);
                r.Close();
                return true;
            }
        }

        public void OperateOnProperty(long operatorId, Property property, PropertyOperation operation, Result result) {
            Log(nameof(operatorId) + ": " + operatorId + ", " + nameof(property) + ": " + property.Id + ", " + nameof(operation) + ": " + operation);
            Npgsql.NpgsqlCommand cmd;
            switch (operation) {
                case PropertyOperation.SetValid:
                    cmd = new Npgsql.NpgsqlCommand("UPDATE p SET " +
                        DBField.valid + " = :" + DBField.valid + ", " + // TODO: Use the database engine's clock here instead?
                        DBField.vid + " = " + operatorId + " " +
                        "WHERE " + DBField.id + " = " + property.Id, _cn1);
                    cmd.Parameters.Add(new Npgsql.NpgsqlParameter(DBField.valid.ToString(), NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = DateTime.Now }); break; // TODO: Use the database engine's clock here instead?
                case PropertyOperation.SetInvalid:
                    cmd = new Npgsql.NpgsqlCommand("UPDATE p SET " +
                        DBField.invalid + " = :" + DBField.invalid + ", " + // TODO: Use the database engine's clock here instead?
                        DBField.iid + " = " + operatorId + " " +
                        "WHERE " + DBField.id + " = " + property.Id, _cn1);
                    cmd.Parameters.Add(new Npgsql.NpgsqlParameter(DBField.invalid.ToString(), NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = DateTime.Now }); break; // TODO: Use the database engine's clock here instead?
                default: throw new InvalidEnumException(operation);
            }
            ExecuteNonQuery(cmd, expectedRows: 1, doLogging: false);
            switch (operation) {
                case PropertyOperation.SetValid: break;
                case PropertyOperation.SetInvalid:

                    // TODO: Take into consideration how to remove from cache and also how to remove from
                    // TODO: properties collection when IsMany
                    if (property.Key.Key.A.IsMany) throw new NotImplementedException(nameof(property.Key.Key.A.IsMany));

                    if (property.ParentId > 0) {
                        // Remove whole of parent from cache since its initialization result may no longer be correct
                        // (It would be naive to assume that we can only remove the property itself)

                        // TODO: Take into consideration how to remove from cache and also how to remove from
                        // TODO: properties collection when IsMany
                        if (Util.EntityCache.TryRemove(property.ParentId, out var entity)) {
                            switch (entity) { // And also remove from parent's property collection in case object exists somewhere (as a singleton class or similar)

                                // TODO: Take into consideration how to remove from cache and also how to remove from
                                // TODO: properties collection when IsMany
                                case BaseEntityT e2: if (e2.Properties != null && e2.Properties.ContainsKey(property.Key.Key.CoreP)) e2.Properties.Remove(property.Key.Key.CoreP); break;
                            }
                        }
                    }
                    break;
                default: throw new InvalidEnumException(operation);
            }
            result?.Count(CoreP.PAffectedCount);
        }

        public List<long> GetRootPropertyIds(Type type) {
            Log(nameof(type) + ": " + type.ToStringShort());
            var cmd = new Npgsql.NpgsqlCommand(
                "SELECT " + DBField.id + " FROM p WHERE " +
                DBField.key + " = '" + CoreP.Type.A().Key.PToString + "' AND " +
                DBField.strv + " = '" + type.ToStringDB() + "' AND " +
                DBField.invalid + " IS NULL " +
                "ORDER BY " + DBField.id + " ASC", _cn1);
            var retval = new List<long>();
            lock (cmd.Connection) {
                Npgsql.NpgsqlDataReader r;
                try {
                    r = cmd.ExecuteReader();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                while (r.Read()) retval.Add(r.GetInt64(0));
                r.Close();
            }
            Log(nameof(retval) + ".Count: " + retval.Count);
            return retval;
        }

        public List<T> GetAllEntities<T>() where T : BaseEntityT, new() {
            Log(typeof(T).ToStringShort());
            var retval = GetRootPropertyIds(typeof(T)).Select(id => GetEntityById<T>(id)).ToList();
            Log(nameof(retval) + ".Count: " + retval.Count);
            return retval;
        }

        public long CreateEntity<T>(long cid, Result result) where T : BaseEntityT => CreateEntity(cid, typeof(T), properties: (IEnumerable<(PropertyKey key, object value)>)null, result: result);
        public long CreateEntity(long cid, Type entityType, Result result) => CreateEntity(cid, entityType, properties: (IEnumerable<(PropertyKey key, object value)>)null, result: result);
        public long CreateEntity<T>(long cid, Parameters properties, Result result) where T : BaseEntityT => CreateEntity(cid, typeof(T), properties.Properties.Values.Select(p => (p.Key, p.ADotTypeValue())), result);
        public long CreateEntity(long cid, Type entityType, Parameters properties, Result result) => CreateEntity(cid, entityType, properties.Properties.Values.Select(p => (p.Key, p.ADotTypeValue())), result);
        public long CreateEntity<T>(long cid, IEnumerable<(PropertyKey key, object value)> properties, Result result) where T : BaseEntityT => CreateEntity(cid, typeof(T), properties, result);
        public long CreateEntity(long cid, Type entityType, IEnumerable<(PropertyKey key, object value)> properties, Result result) {
            Log(nameof(cid) + ": " + cid + ", " + nameof(entityType) + ": " + entityType.ToStringShort() + ", " + nameof(properties) + ": " + (properties?.Count().ToString() ?? "[NULL]"));
            InvalidTypeException.AssertAssignable(entityType, typeof(BaseEntityT), detailer: null);
            var retval = new Result();
            var pid = CreateProperty(cid, null, null, new PropertyKey(CoreP.Type.A()), entityType, result);
            if (properties != null) {
                foreach (var v in properties) {
                    CreateProperty(cid, pid, null, v.key, v.value, result);
                }
            }
            return pid;
        }

        ///// <summary>
        ///// See <see cref="Property"/> for more information. 
        ///// </summary>
        //protected string propertySelect = "SELECT " +
        //    "id, " +      //  0                                                           bigint
        //    "created, " + //  1 Timestamp when created in database                        timestamp without time zone
        //    "cid, " +     //  2 creator id (entity which created this property)           bigint
        //    "pid, " +     //  3 parent id                                                 bigint
        //    "fid, " +     //  4 foreign id (when this property is a relation              bigint
        //    "key, " +     //  5                                                           text
        //    "lngv, " +    //  6 Long value                                                bigint
        //    "dblv, " +    //  7 Double value                                              double
        //    "blnv, " +    //  8 Bool value                                                boolean
        //    "dtmv, " +    //  9 Date time value                                           timestamp without time zone
        //    "geov" +      // 10 Geometry value. TODO: ADD!                                text. TODO: ADD!
        //    "strv, " +    // 11 String value (also used for enums)                        text
        //    "valid, " +   // 12 Timestamp when last known valid                           timestamp without time zone
        //    "vid, " +     // 13 validator id (entity which last validated this property)  bigint
        //    "invalid, " + // 12 Timestamp when invalidated (null if still valid)          timestamp without time zone
        //    "iid " +      // 13 invalidator id (entity which invalidated this property)   bigint
        //    "FROM p ";

        /// <summary>
        /// Populates property object with information from database
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        protected Property ReadOneProperty(Npgsql.NpgsqlDataReader r) {
            // Log(""); Logging now generates too much data
            var retval = new Property {
                Id = r.GetInt64((int)DBField.id),
                Created = r.GetDateTime((int)DBField.created),
                CreatorId = r.IsDBNull((int)DBField.cid) ? 0 : r.GetInt64((int)DBField.cid),
                ParentId = r.IsDBNull((int)DBField.pid) ? 0 : r.GetInt64((int)DBField.pid),
                ForeignId = r.IsDBNull((int)DBField.fid) ? 0 : r.GetInt64((int)DBField.fid),
                KeyDB = r.IsDBNull((int)DBField.key) ? throw new InvalidPropertyKeyException(null, r.GetInt64((int)DBField.id)) : r.GetString((int)DBField.key),
                LngValue = r.IsDBNull((int)DBField.lngv) ? (long?)null : r.GetInt64((int)DBField.lngv),
                DblValue = r.IsDBNull((int)DBField.dblv) ? (double?)null : r.GetDouble((int)DBField.dblv),
                BlnValue = r.IsDBNull((int)DBField.blnv) ? (bool?)null : r.GetBoolean((int)DBField.blnv),
                DtmValue = r.IsDBNull((int)DBField.dtmv) ? (DateTime?)null : r.GetDateTime((int)DBField.dtmv),
                GeoValue = r.IsDBNull((int)DBField.geov) ? null : r.GetString((int)DBField.geov),
                StrValue = r.IsDBNull((int)DBField.strv) ? null : r.GetString((int)DBField.strv),
                Valid = r.IsDBNull((int)DBField.valid) ? (DateTime?)null : r.GetDateTime((int)DBField.valid),
                ValidatorId = r.IsDBNull((int)DBField.vid) ? (long?)null : r.GetInt64((int)DBField.vid),
                Invalid = r.IsDBNull((int)DBField.invalid) ? (DateTime?)null : r.GetDateTime((int)DBField.invalid),
                InvalidatorId = r.IsDBNull((int)DBField.iid) ? (long?)null : r.GetInt64((int)DBField.iid),

                // 13 Oct 2015 We could consider doing this, that is, ALWAYS read children if they may exist
                // (instead it is being done as desired by BaseController.GetProperties)
                // BE CAREFUL ABOUT PERFORMANCE (WHEN READING A LONG LIST OF PERSONS OR DEALS FOR INSTANCE)
                // (retval.CanHaveChildren) AddChildrenToProperty(retval);
            }.Initialize();

            // No reason for doing this: KeyDB is "correct" when reading from database
            //// TODO: IMPROVE ON THIS. Check consequences for IsMany. Maybe a KeyLng for Property.
            //var keyLng = (long)(object)retval.KeyT;
            //if (retval.KeyDB.Equals(keyLng.ToString())) {
            //    // TODO: USE TRYMAPREVERSE, WITH EXCEPTION HANDLING HERE
            //    retval.KeyDB = _cpm.MapReverse((TProperty)(object)keyLng).ToString();
            //}

            // TODO: FIX THIS!
            if (retval.ParentId == 0) {
                /// This is an entity root property. We can not put that into cache because the same id will be used
                /// to store the entity itself. We do not have any use for it either (<see cref="CreateProperty"/> does not need it)
            } else {
                /// Note how putting properties in cache is used for invalidating cached entries by <see cref="CreateProperty"/>
                /// The cache is never used in itself when reading properties from database (GetPropertyById for instance will never use cache)
                Util.EntityCache[retval.Id] = retval;
            }
            return retval;
        }

        /// <summary>
        /// Pragmatic simple and easy to understand mechanism for verification of credentials.
        /// Only to be used in systems requiring low levels of security.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        public bool TryVerifyCredentials(string username, string password, out BaseEntityT currentUser) {
            // Note how password is NOT logged.
            // Log(nameof(email) + ": " + email + ", " + nameof(password) + ": " + (string.IsNullOrEmpty(password) ? "[NULL_OR_EMPTY]" : " [SET]"));

            currentUser = null;
            if (string.IsNullOrEmpty(username) || username.Length > 100) return false;
            if (string.IsNullOrEmpty(password) || password.Length > 100) return false;
            username = username.ToLower();
            Log("Searching " + CoreP.Username.A().Key.PToString + " = '" + username + "'");
            var cmd = new Npgsql.NpgsqlCommand("SELECT pid FROM p WHERE " + DBField.key + " = '" + CoreP.Username.A().Key.PToString + "' AND " + DBField.strv + " = :" + CoreP.Username + " AND " + DBField.invalid + " IS NULL", _cn1);
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter(CoreP.Username.ToString(), NpgsqlTypes.NpgsqlDbType.Text) { Value = username });
            if (!TryExecuteScalarLong(cmd, out var entityId)) return false;
            if (entityId == 0) return false; // Not really necessary check

            // Note that although it is expensive to read the whole entity now, a practical AgoRapide implementation would
            // probably need the whole object anyway for later purposes, and a typical Authentication mechanism could make
            // the returned object available for such purposes.
            // (check for instance code in Startup.cs, BasicAuthenticationAttribute.AuthenticateAsync
            //    context.Request.Properties["AgoRapideCurrentUser"] = currentUser
            // )
            if (!TryGetEntityById(entityId, useCache: false, requiredType: null, entity: out currentUser)) return false;

            if (currentUser.PV<string>(CoreP.Username.A(), "") != username) { // Read log text carefully. It is only AFTER call to TryGetEntityById that current was set to FALSE for old properties. In other words, it is normal to read another email now 
                Log(
                    "It looks like " + CoreP.Username.A().Key.PExplained + " " +
                    "was just changed for entity " + currentUser.Id + " " +
                    "resulting in more than one current property in database. " +
                    "Returning FALSE now " +
                    "since the last one (the one now current) (" + currentUser.PV(CoreP.Username.A(), "") + ") " +
                    "does not correspond to the one given (" + username + ")");
                return false;
            }
            if (!currentUser.TryGetPV<string>(CoreP.Password.A(), out var correctPasswordHashedWithSalt) || string.IsNullOrEmpty(correctPasswordHashedWithSalt)) {
                Log("Password not set for " + currentUser.ToString());
                return false;
            }
            if (password.Equals(correctPasswordHashedWithSalt)) throw new InvalidPasswordException<CoreP>(CoreP.Password, nameof(password) + ".Equals(" + nameof(correctPasswordHashedWithSalt) + "). Either 1) Password was not correct stored in database (check that " + nameof(CreateProperty) + " really salts and hashes passwords), or 2) The caller actually supplied an already salted and hashed password.");

            var passwordHashedWithSalt = Util.GeneratePasswordHashWithSalt(currentUser.Id, password);
            if (correctPasswordHashedWithSalt != passwordHashedWithSalt) {
                // A bit expensive to store in database, but useful information. 
                // Note how the NUMBER of failed attempts are not logged since only the (last) valid-date in the database is stored for repeated failures. 
                // Note that if you are concerned about hacking / DDOS scenarios or similar you should definitely implement a more robust authentication mechanism.
                UpdateProperty(GetId(), currentUser, CoreP.AuthResult.A(), value: false, result: null);
                return false;
            }

            if (currentUser.PV(CoreP.RejectCredentialsNextTime.A(), defaultValue: false)) {
                // TODO: Instead of just using cid = currentUser.Id let this class discover its own id used as cid and iid
                UpdateProperty(GetId(), currentUser, new PropertyKey(CoreP.RejectCredentialsNextTime.A().Key), value: false, result: null);
                return false;
            }

            Log("Returning TRUE");
            // Note how the NUMBER of successful attempts are not logged since only the (last) valid-date in the database is stored for repeated successes. 
            // TODO: Instead of just using cid = currentUser.Id let this class discover its own id used as cid and iid
            UpdateProperty(GetId(), currentUser, CoreP.AuthResult.A(), value: true, result: null);

            SwitchIfHasEntityToRepresent(ref currentUser);
            return true;
        }

        public void AssertUniqueness(PropertyKey key, object value) {
            if (!TryAssertUniqueness(key, value, out var existing, out var errorResponse)) throw new UniquenessException(errorResponse + "\r\nDetails: " + existing.ToString());
        }
        public bool TryAssertUniqueness(PropertyKey a, object value, out Property existingProperty, out string errorResponse) {
            var key = a.Key.CoreP;
            Log(nameof(key) + ": " + key + ", " + nameof(value) + ": " + value);
            a.Key.A.AssertIsUniqueInDatabase();

            // TODO: DUPLICATED CODE!
            var defaultKeyToString = key.ToString();
            // TODO: Add support for IsMany. Make possible to store key as #1, #2 and so on in database.
            var keyAsString = a.Key.PToString;

            Npgsql.NpgsqlCommand cmd;
            switch (value) {
                /// TODO: This code is surely duplicated somewhere else                    
                /// Typical is <see cref="CoreP.IsAnonymous"/>
                case string strValue:
                    cmd = new Npgsql.NpgsqlCommand(PropertySelect + " WHERE " + DBField.key + " = '" + keyAsString + "' AND " + DBField.strv + " ILIKE :" + DBField.strv + " AND " + DBField.invalid + " IS NULL", _cn1);
                    cmd.Parameters.Add(new Npgsql.NpgsqlParameter(DBField.strv.ToString(), NpgsqlTypes.NpgsqlDbType.Text) { Value = strValue }); break;
                case bool blnValue:
                    cmd = new Npgsql.NpgsqlCommand(PropertySelect + " WHERE " + DBField.key + " = '" + keyAsString + "' AND " + DBField.blnv + " = " + (blnValue ? "TRUE" : "FALSE") + " AND " + DBField.invalid + " IS NULL", _cn1); break;
                default: throw new InvalidObjectTypeException(value, nameof(a.Key.A.IsUniqueInDatabase) + " only implemented for string and bool");
            }
            var existing = ReadAllPropertyValues(cmd);
            switch (existing.Count) {
                case 0:
                    existingProperty = null;
                    errorResponse = null;
                    return true;
                case 1:
                    existingProperty = existing[0];
                    errorResponse = nameof(a.Key.A.IsUniqueInDatabase) + " property " + keyAsString + " = '" + value + "' already exists in database. You must chose a different value for " + keyAsString + ".";
                    return false;
                default:
                    throw new UniquenessException(
                        "Found " + existing.Count + " existing properties for " + nameof(a.Key.A.IsUniqueInDatabase) + " property " + keyAsString + " = '" + value + "'\r\n" +
                        "Expected at most only one existing property.\r\n" +
                        "Resolution: Delete from database all but one of the existing properties with the following SQL expression:\r\n" +
                        "  DELETE FROM p WHERE id IN (" + string.Join(", ", existing.Select(p => p.Id)) + ")\r\n" +
                        "The actual existing properties are:\r\n" +
                        string.Join("\r\n", existing.Select(p => p.ToString())));
            }
        }

        public long CreateProperty(long? cid, long? pid, long? fid, PropertyKey keyA, object value, Result result) {
            if (keyA.Key.A.IsMany) throw new NotImplementedException(nameof(keyA.Key.A.IsMany));

            Npgsql.NpgsqlCommand cmd;
            if (keyA.Key.A.IsUniqueInDatabase) {
                if (keyA.Key.A.IsMany) throw new NotImplementedException(nameof(keyA.Key.A.IsMany));
                AssertUniqueness(keyA, value);
            }
            if (keyA.Key.A.IsMany) throw new NotImplementedException(nameof(keyA.Key.A.IsMany));

            var idStrings = new Func<(string logtext, string names, string values)>(() => {
                if (cid == null && pid == null && fid == null) {
                    Log(nameof(cid) + ", " + nameof(pid) + " and " + nameof(fid) + " are all null. Setting " + nameof(cid) + " = 0. " +
                        "This should only occur once for your database in order for " + typeof(ApplicationPart) + "." + nameof(ApplicationPart.GetOrAdd) + " to create an instance 'for itself'. " +
                        "In all other instances of calls to " + System.Reflection.MethodBase.GetCurrentMethod().Name + " it should be possible to at least have a value for " + nameof(cid) + " (creatorId) " +
                        "(by using " + typeof(ApplicationPart) + "." + nameof(ApplicationPart.GetOrAdd) + "). " +
                        "In other words, this log message should never repeat itself", result);
                    cid = 0;
                }
                if (cid != null && pid == null && fid == null) return (
                    // item1 is logtext
                    nameof(cid) + ": " + cid,
                    // item2 is {names} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + DBField.cid,
                    // item3 is {values} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + cid.ToString()
                );
                if (cid != null && pid != null && fid == null) return (
                    // item1 is logtext
                    nameof(cid) + ": " + cid +
                    ", " + nameof(pid) + ": " + pid,
                    // item2 is {names} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + DBField.cid +
                    ", " + DBField.pid,
                    // item3 is {values} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + cid.ToString() +
                    ", " + pid.ToString()
                );
                if (cid != null && pid != null && fid != null) return (
                    // item1 is logtext
                    nameof(cid) + ": " + cid +
                    ", " + nameof(pid) + ": " + pid +
                    ", " + nameof(fid) + ": " + fid,
                    // item2 is {names} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + DBField.cid +
                    ", " + DBField.pid +
                    ", " + DBField.fid,
                    // item3 is {values} to put into INSERT INTO p({names}) VALUES({values})
                    ", " + cid.ToString() +
                    ", " + pid.ToString() +
                    ", " + fid.ToString()
                );
                throw new NotImplementedException(
                    "For the time being either " +
                    "1) Only parameter " + nameof(cid) + " or " +
                    "2) Two parameters (" + nameof(cid) + ", " + nameof(pid) + "), or " +
                    "3) All parameters (" + nameof(cid) + ", " + nameof(pid) + ", " + nameof(fid) + "), " +
                    "must be given, not " + (cid == null ? " " : nameof(cid)) + (pid == null ? " " : nameof(pid)) + (fid == null ? " " : nameof(fid)));
            })();

            Log(idStrings.logtext +
                ", " + nameof(keyA.Key.PToString) + ": " + keyA.Key.PToString +
                ", " + nameof(value) + ": " + (keyA.Key.A.IsPassword ? "[WITHHELD]" : value.ToString()), result);
            var type = value.GetType();

            // TODO: Idea for performance improvement. We could read some sequence values in a separate
            // TODO: thread and store them in a concurrent queue for later thread safe retrieval
            cmd = new Npgsql.NpgsqlCommand("SELECT nextval('seq_property_id')", _cn1);
            var id = ExecuteScalarLong(cmd, () => cmd.CommandText);

            var valueStrings = new Func<(string nameOfDbField, string valueOrParameter, NpgsqlTypes.NpgsqlDbType? dbType)>(() => {
                switch (value) {
                    case long _long: return (nameof(DBField.lngv), "'" + _long.ToString() + "'", null);
                    case double _dbl: return (nameof(DBField.dblv), ":" + nameof(DBField.dblv), NpgsqlTypes.NpgsqlDbType.Double); // Leave conversion to Npgsql
                    case bool _bln: return (nameof(DBField.blnv), _bln ? "TRUE" : "FALSE", null);
                    case DateTime _dtm: return (nameof(DBField.dtmv), ":" + nameof(DBField.dtmv), NpgsqlTypes.NpgsqlDbType.Timestamp); // Leave conversion to Npgsql
                    case Type _type: return (nameof(DBField.strv), "'" + _type.ToStringDB() + "'", null); // Considered SQL injection safe
                    default:
                        if (value.GetType().IsEnum) return (nameof(DBField.strv), "'" + value.ToString() + "'", null); // Considered SQL injection safe
                        var _typeDescriber = value as ITypeDescriber;
                        if (_typeDescriber != null) value = value.ToString();
                        if (!(value is string)) throw new InvalidObjectTypeException(value, typeof(string));
                        return (nameof(DBField.strv), ":" + nameof(DBField.strv), NpgsqlTypes.NpgsqlDbType.Text); // Leave conversion to Npgsql (because of SQL injection issues)
                }
                // DELETE COMMENT. Lambda eliminated 01 Apr 2016. 
                // NOTE: Pattern matching not possible because variable is consumed in lambda above. See 
                // NOTE: https://github.com/dotnet/roslyn/issues/16066
                // NOTE for explanation
                // TOOD: Rewrite returner as local method and see of works then!
                //var _long = value as long?;
                //if (_long != null) return (nameof(DBField.lngv), "'" + _long.ToString() + "'", null);
                //var _dbl = value as double?;
                //if (_dbl != null) return (nameof(DBField.dblv), ":" + nameof(DBField.dblv), NpgsqlTypes.NpgsqlDbType.Double); // Leave conversion to Npgsql
                //var _bln = value as bool?;
                //if (_bln != null) return (nameof(DBField.blnv), (bool)_bln ? "TRUE" : "FALSE", null);
                //var _dtm = value as DateTime?;
                //if (_dtm != null) return (nameof(DBField.dtmv), ":" + nameof(DBField.dtmv), NpgsqlTypes.NpgsqlDbType.Timestamp); // Leave conversion to Npgsql
                //if (value.GetType().IsEnum) return (nameof(DBField.strv), "'" + value.ToString() + "'", null); // Considered SQL injection safe
                //var _type = value as Type;
                //if (_type != null) return (nameof(DBField.strv), "'" + _type.ToStringDB() + "'", null); // Considered SQL injection safe
                //var _typeDescriber = value as ITypeDescriber;
                //if (_typeDescriber != null) value = value.ToString();
                //if (!(value is string)) throw new InvalidObjectTypeException(value, typeof(string));
                //return (nameof(DBField.strv), ":" + nameof(DBField.strv), NpgsqlTypes.NpgsqlDbType.Text); // Leave conversion to Npgsql (because of SQL injection issues)
            })();
            Log("Storing as " + valueStrings.nameOfDbField + ", " + valueStrings.valueOrParameter, result);
            // Note how we do not bother with parameters for object types which do not have any SQL injection issues.
            cmd = new Npgsql.NpgsqlCommand("INSERT INTO p\r\n" +
                "(" + DBField.id + idStrings.names + ", " + DBField.key + ", " + valueStrings.nameOfDbField + ")\r\n" +
                "VALUES (" + id + idStrings.values + ", '" + keyA.Key.PToString + "', " + valueStrings.valueOrParameter + ")", _cn1);

            if (valueStrings.valueOrParameter.StartsWith(":")) {
                if (valueStrings.dbType == null) throw new NullReferenceException(nameof(valueStrings) + "." + nameof(valueStrings.dbType) + ". Must be set when " + nameof(valueStrings) + "." + nameof(valueStrings.valueOrParameter) + ".StartsWith(\":\") (" + valueStrings.valueOrParameter + ")");
                cmd.Parameters.Add(new Npgsql.NpgsqlParameter(valueStrings.valueOrParameter, valueStrings.dbType) {
                    Value = new Func<object>(() => {
                        if (!keyA.Key.A.IsPassword) return value;
                        // salt and hash
                        if (pid == null) throw new ArgumentNullException(nameof(pid) + " must be set for " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.IsPassword));
                        if (!(value is string)) throw new InvalidObjectTypeException(value, typeof(string), "Only string is allowed for " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.IsPassword));
                        return Util.GeneratePasswordHashWithSalt((long)pid, (string)value);
                    })()
                });
            } else {
                if (valueStrings.dbType != null) throw new NotNullReferenceException(nameof(valueStrings) + "." + nameof(valueStrings.dbType) + ". Must not be set when !" + nameof(valueStrings) + "." + nameof(valueStrings.valueOrParameter) + ".StartsWith(\":\") (" + valueStrings.valueOrParameter + ")");
                if (keyA.Key.A.IsPassword) throw new Exception(nameof(AgoRapideAttribute) + "." + nameof(keyA.Key.A.IsPassword) + " only valid for strings, not " + value.GetType());
            }
            var propertiesAffected = ExecuteNonQuery(cmd, expectedRows: 1, doLogging: false);
            result?.SetCount(CoreP.PAffectedCount, propertiesAffected);
            result?.Count(CoreP.PCreatedCount);
            result?.Count(CoreP.PTotalCount);

            return id;
        }

        public void UpdateProperty<T>(long cid, BaseEntityT entity, PropertyKey a, T value, Result result) {
            // Note how we only log when property is created or updated
            if (a.Key.A.IsMany) throw new NotImplementedException(nameof(a.Key.A.IsMany));
            var key = a.Key.CoreP;
            var detailer = new Func<string>(() => nameof(entity) + ": " + entity.Id + ", " + nameof(key) + ": " + key + ", " + nameof(value) + ": " + value + ", " + nameof(cid) + ": " + cid);
            if (entity.Properties == null) throw new NullReferenceException(nameof(entity) + "." + nameof(entity.Properties) + ", " + detailer());

            var creator = new Func<long>(() => CreateProperty(cid, entity.Id, null, a, value, result));

            if (entity.Properties.TryGetValue(key, out var existingProperty)) {
                var existingValue = existingProperty.V<T>();
                if (existingValue.Equals(value)) {
                    // Note how this is not logged
                    OperateOnProperty(cid, existingProperty, PropertyOperation.SetValid, result);
                    result?.Count(CoreP.PUnchangedCount);
                } else { // Vanlig variant
                         // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                         // TODO: At least for presenting DateTime-objects and double in a preferred manner
                    Log(detailer() + ". Property changed from '" + existingValue + "' to '" + value + "'", result);
                    var id = creator();
                    var changedProperty = GetPropertyById(id);
                    result?.Count(CoreP.PChangedCount);
                    entity.Properties[key] = changedProperty;
                }
            } else {
                // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                // TODO: At least for presenting DateTime-objects and double in a preferred manner
                Log(detailer() + ". Property was not known. Initial value: '" + value + "'", result);
                var id = creator();
                var newProperty = GetPropertyById(id);
                /// result?.Count(M(CoreP.PCreatedCount)); Counted by <see cref="CreateProperty"/>
                result?.Count(CoreP.PTotalCount);
                entity.Properties[key] = newProperty;
            }
        }

        /// <summary>
        /// TODO: Should we add checks for AccessRights here? 
        /// See comments for <see cref="CoreP.EntityToRepresent"/>
        /// </summary>
        /// <param name="entity"></param>
        public void SwitchIfHasEntityToRepresent(ref BaseEntityT entity) {
            if (entity.TryGetPV(CoreP.EntityToRepresent.A(), out long representedEntityId)) {
                Log("entityId: " + entity.Id + ", switching to " + representedEntityId);
                var representedByEntity = entity;
                var entityToRepresent = GetEntityById<BaseEntityT>(representedEntityId);
                // TODO: Should we add checks for AccessRights here? 
                /// See comments for <see cref="CoreP.EntityToRepresent"/>
                entityToRepresent.AddProperty(CoreP.RepresentedByEntity.A(), representedByEntity.Id);
                entityToRepresent.RepresentedByEntity = representedByEntity;
                entity = entityToRepresent;
            }
        }

        /// <summary>
        /// TODO: MOVE COMMENT TO INTERFACE DECLARATION INSTEAD!
        /// Note how this is used for both "ordinary" entities and properties that may have children
        /// 
        /// TODO: FIX COMMENT OR CODE:
        /// Bemerk at denne LEGGER IKKE TIL child i selve property-parameteren (bruk AddChildrenToProperty for dette formålet). THIS IS A BIT ARTIFICIAL!
        /// </summary>
        /// <param name="parentProperty"></param>
        /// <returns></returns>
        public Dictionary<CoreP, Property> GetChildProperties(Property parentProperty) {
            Log(nameof(parentProperty.Id) + ": " + parentProperty.Id);
            if (!true.Equals(parentProperty.Key.Key.A.CanHaveChildren)) throw new Exception(
                "!" + nameof(parentProperty.Key.Key.A.CanHaveChildren) + " (" + parentProperty.ToString() + ". " +
                "Explanation: You are not allowed to operate with child properties for " + parentProperty.Key.Key.PExplained + " because there is no [" + nameof(AgoRapideAttribute) + "(" + nameof(AgoRapideAttribute.CanHaveChildren) + " = true)] defined for this enum value");
            var cmd = new Npgsql.NpgsqlCommand(PropertySelect + " WHERE\r\n" +
                // TODO: CHECK IF THIS IS STILL THE CORRECT METHOD
                "(\r\n" +
                "  " + DBField.pid + " = " + parentProperty.Id + " OR\r\n" +
                "  (\r\n" +                                               // pid IS NOT NULl and fid = parent is for limiting to relations
                "    (" + DBField.pid + " IS NOT NULL) AND\r\n" +                        // where this entity is the foreign entity. Note the  
                "    (" + DBField.fid + " = " + parentProperty.Id + ")\r\n" +            // careful construct because we DO NOT WANT root-properties
                                                                                         // TODO: Consider adding AND name LIKE 
                                                                                         // 'Relation%' here as an additional measure
                "  )\r\n" +                                              // where we are indicated as QueryCompoent to be included
                ") AND\r\n" +                                            // (that would result in lots of properties being set no-longer-current)
                DBField.invalid + " IS NULL\r\n" +
                "ORDER BY " + DBField.id + " ASC", _cn1);
            return ReadAllPropertyValuesAndSetNoLongerCurrentForDuplicates(cmd);
        }

        protected long ExecuteScalarLong(Npgsql.NpgsqlCommand cmd, Func<string> detailer) => TryExecuteScalarLong(cmd, out var retval) ? retval : throw new NoResultFromDatabaseException(nameof(cmd) + "." + nameof(cmd.CommandText) + ": " + cmd.CommandText + detailer.Result("\r\nDetails: "));
        protected bool TryExecuteScalarLong(Npgsql.NpgsqlCommand cmd, out long _long) {
            if (cmd.Connection == null) throw new NullReferenceException(nameof(cmd) + "." + nameof(cmd.Connection));
            lock (cmd.Connection) {
                object retval;
                try {
                    retval = cmd.ExecuteScalar();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                if ((retval == null) || (retval is DBNull)) {
                    _long = default(long);
                    return false;
                }
                _long = (retval as long? ?? throw new InvalidObjectTypeException(retval, typeof(long)));
                return true;
            }
        }

        private int ExecuteNonQuery(Npgsql.NpgsqlCommand cmd) => TryExecuteNonQuery(cmd, -1, true, out var retval) ? retval : retval;
        private int ExecuteNonQuery(Npgsql.NpgsqlCommand cmd, bool doLogging) => TryExecuteNonQuery(cmd, -1, doLogging, out var retval) ? retval : retval;
        private int ExecuteNonQuery(Npgsql.NpgsqlCommand cmd, int expectedRows, bool doLogging) => TryExecuteNonQuery(cmd, expectedRows, doLogging, out var retval) ? retval : throw new UnexpectedNumberOfRowsException(retval, expectedRows);
        /// <summary>
        /// Returns false if <paramref name="affectedRows"/> does not correspond to <paramref name="expectedRows"/>
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="expectedRows">Will always be set regardless of returned value</param>
        /// <param name="doLogging"></param>
        /// <param name="affectedRows"></param>
        private bool TryExecuteNonQuery(Npgsql.NpgsqlCommand cmd, int expectedRows, bool doLogging, out int affectedRows) {
            lock (cmd.Connection) {
                try {
                    affectedRows = cmd.ExecuteNonQuery();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, nameof(expectedRows) + ": " + expectedRows, ex);
                }
            }
            if (doLogging) Log(nameof(affectedRows) + ": " + affectedRows);
            return affectedRows == expectedRows;
        }

        public class NoResultFromDatabaseException : ApplicationException {
            public NoResultFromDatabaseException(string message) : base(message) { }
            public NoResultFromDatabaseException(string message, Exception inner) : base(message, inner) { }
        }

        public List<int> ReadAllIds(Npgsql.NpgsqlCommand cmd) {
            // TODO: Decide on logging here. What to log.
            Log("\r\n" + nameof(cmd.CommandText) + ":\r\n" + cmd.CommandText + "\r\n" + nameof(cmd.Parameters) + ".Count: " + cmd.Parameters.Count);
            var retval = new List<int>();
            lock (cmd.Connection) {
                Npgsql.NpgsqlDataReader r;
                try {
                    r = cmd.ExecuteReader();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                while (r.Read()) {
                    retval.Add(r.GetInt32(0));
                }
                r.Close();
            }
            return retval;
        }

        protected List<Property> ReadAllPropertyValues(Npgsql.NpgsqlCommand cmd) {
            var retval = new List<Property>();
            lock (cmd.Connection) {
                Npgsql.NpgsqlDataReader r;
                try {
                    r = cmd.ExecuteReader();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                while (r.Read()) retval.Add(ReadOneProperty(r));
                r.Close();
            }
            return retval;
        }

        /// <summary>
        /// For any duplicates the first property's <see cref="Property.Invalid"/> will be set to <see cref="DateTime.Now"/> 
        /// and that property will be excluded from the returned dictionary. 
        /// (it is therefore important to always read properties in ASCending order from database before calling this method)
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected Dictionary<CoreP, Property> ReadAllPropertyValuesAndSetNoLongerCurrentForDuplicates(Npgsql.NpgsqlCommand cmd) {
            var dict = new Dictionary<CoreP, Property>();
            lock (cmd.Connection) {
                Npgsql.NpgsqlDataReader r;
                try {
                    r = cmd.ExecuteReader();
                } catch (Exception ex) {
                    throw new PostgreSQLDatabaseException(cmd, ex);
                }
                var noLongerCurrent = new List<(Property p, byte id)>();
                while (r.Read()) {
                    var p = ReadOneProperty(r);
                    if (p.Key.Key.CoreP.Equals(CoreP.None)) throw new InvalidPropertyKeyException(p.KeyDB, p.Id);
                    if (p.Key.Index > 0) {
                        var isManyParent = dict.GetOrAddIsManyParent(p.Key.Key);
                        if (isManyParent.Properties.TryGetValue((CoreP)(object)p.Key.Index, out var toBeOverwritten)) {
                            noLongerCurrent.Add((toBeOverwritten, 1)); // #1 when calling GetId
                        }
                        isManyParent.Properties[(CoreP)(object)(int.MaxValue - p.Key.Index)] = p;
                    } else {
                        if (dict.TryGetValue(p.Key.Key.CoreP, out var toBeOverwritten)) {
                            noLongerCurrent.Add((toBeOverwritten, 2)); // #2 when calling GetId
                        }
                        dict[p.Key.Key.CoreP] = p;
                    }
                }
                r.Close();
                if (noLongerCurrent.Count > 0) {
                    Log("Calling " + nameof(OperateOnProperty) + " for " + noLongerCurrent.Count + " properties");
                    var ids = new List<long> { // Distinguish between multiple or not (maybe not really important?)
                        0,
                        GetId(System.Reflection.MethodBase.GetCurrentMethod().Name + "#1"),
                        GetId(System.Reflection.MethodBase.GetCurrentMethod().Name + "#2"),
                    };
                    noLongerCurrent.ForEach(n => {
                        if (n.id < 1 || n.id > 2) throw new Exception("Invalid " + nameof(n.id) + " (" + n.id + ")");
                        OperateOnProperty(operatorId: ids[n.id], property: n.p, operation: PropertyOperation.SetInvalid, result: null);
                    });
                }
            }
            return dict;
        }

        /// <summary>
        /// TODO: Change to use separate lock for each connection
        /// </summary>
        protected static string connectionOpenerLock = "";

        /// <summary>
        /// Tip: In PostgreSQL you can check open connections with SELECT * FROM pg_stat_activity;
        ///
        /// Code is a bit primitive. Check issues regarding connection pooling for instance.
        /// Note that Dispose is recommended to always call
        /// </summary>
        /// <param name="cn"></param>
        protected void OpenConnection(out Npgsql.NpgsqlConnection cn, string id) {
            Log(id);
            lock (connectionOpenerLock) { // TODO: CHECK USE OF LOCKING
                var stage = "";
                try {
                    stage = "new Npgsql.NpgsqlConnection";
                    cn = new Npgsql.NpgsqlConnection(_connectionString + "ApplicationName=" + _applicationType + " PID " + System.Diagnostics.Process.GetCurrentProcess().Id + " thread " + System.Threading.Thread.CurrentThread.ManagedThreadId + " " + id);
                    stage = "Open";
                    cn.Open();
                } catch (Exception ex) {
                    cn = null;
                    var exOuter = new OpenDatabaseConnectionException(System.Reflection.MethodBase.GetCurrentMethod().Name + " for " + id + " failed at stage " + stage + " because of " + ex.GetType().ToString() + " with message " + ex.Message + " (see " + nameof(ex.InnerException) + " for more details)", ex);
                    HandleException(exOuter);
                    throw exOuter;
                }
            }
        }

        public class OpenDatabaseConnectionException : ApplicationException {
            public OpenDatabaseConnectionException(string message) : base(message) { }
            public OpenDatabaseConnectionException(string message, Exception inner) : base(message, inner) { }
        }


        /// <summary>
        /// TODO: Split this into List{string} of SQL-statements (or maybe a Dictionary and some kind of auto-discovery for missing elements in the database). 
        /// TODO: In other words, check for existence of each and every element at startup and run the corresponding SQL code to initialize them.
        /// 
        /// TODO: At startup we could always update the comments? Or not? There is a SQL injection risk here (although coming from our own source code)
        /// TODO: If you choose to always update comments you should create some kind of absolutely SQL-safe string by using white listed characters. (A-Z, a-z, 1-9, _ or similar)
        /// 
        /// Returns SQL for CREATE TABLE command. 
        /// Mapping of data types between PostgreSQL and .NET:
        ///   long     = bigint
        ///   double   = double
        ///   bool     = boolean
        ///   DateTime = timestamp without time zone
        ///   string   = text
        /// </summary>
        public static string SQL_CREATE_TABLE = new Func<string>(() => {
            // makeSQLSafe is a quite quick and dirty implementation. Do not use elsewhere!
            var makeSQLSafe = new Func<string, string>(s => s.Replace(";", ":").Replace("--", "__").Replace("\r", "/").Replace("\n", "/").Replace("'", "`").Replace("\"", "`"));
            return "CREATE TABLE p\r\n(\r\n" +
            string.Join("\r\n", Util.EnumGetValues((DBField)(-1)).Select(f => {
                return "  " + f.ToString() + " " + new Func<string>(() => {
                    var a = f.GetAgoRapideAttributeT();
                    var postfix = new Func<string>(() => {
                        switch (f) {
                            case DBField.id:
                            case DBField.cid: return " NOT NULL,"; // Maybe use AgoRapideAttribute IsStrict for these?
                            case DBField.created: return " DEFAULT now(),"; // TODO: Should we add these as AgoRapideAttributes?
                            default: return ",";
                        }
                    })();
                    if (a.A.Type == null) throw new NullReferenceException(nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " not defined for " + typeof(DBField) + "." + f.ToString());
                    if (a.A.Type.Equals(typeof(long))) return "bigint" + postfix;
                    if (a.A.Type.Equals(typeof(double))) return "double precision" + postfix;
                    if (a.A.Type.Equals(typeof(bool))) return "boolean" + postfix;
                    if (a.A.Type.Equals(typeof(DateTime))) return "timestamp without time zone" + postfix;
                    if (a.A.Type.Equals(typeof(string))) return "text" + postfix;
                    throw new InvalidTypeException(a.A.Type, nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.Type) + " (" + a.A.Type + ") defined for " + typeof(DBField) + "." + f.ToString() + " is not valid");
                })();
            })) + "\r\n" +
            "  CONSTRAINT p_pk PRIMARY KEY (" + DBField.id + ")\r\n" +
        @")
WITH(
  OIDS = FALSE
);

ALTER TABLE p
  OWNER TO agorapide;

COMMENT ON TABLE p IS 'Main property table'; 
" +
            string.Join("\r\n", Util.EnumGetValues((DBField)(-1)).Select(f => f.GetAgoRapideAttributeT()).Select(f =>
                "COMMENT ON COLUMN p." + f.A.Property.ToString() + " IS '" + makeSQLSafe(f.A.Description) + (string.IsNullOrEmpty(f.A.LongDescription) ? "" : (" // " + nameof(f.A.LongDescription) + ": " + f.A.LongDescription)) + "';")) +

        // TODO: As of Jan 2017 we have troubles with newlines in the CREATE SEQUENCE below with the Visual Studio RC 2017 editor.
        @"
CREATE SEQUENCE seq_property_id
INCREMENT 1
MINVALUE 1
MAXVALUE 9223372036854775807
START 1000  -- Starting with 1000 is a trick that makes it possible in API calls to have the range 1 to 999 signify entity specific id's (for instance like node-id's for an IoT gateway (like Z-Wave Node ID))
CACHE 1;
ALTER TABLE seq_property_id
OWNER TO agorapide;
"; // TOOD: Add some configuration value for username here 
        })();

        // 
        public static string PropertySelect =>
            "SELECT " +
            string.Join(", ", Util.EnumGetValues((DBField)(-1)).Select(e => e.ToString())) + " " +
            "FROM p "; // Do not terminate with ";" here. Usually a WHERE-clause will be added

        protected bool IsDisposed = false;
        /// <summary>
        /// Standard AgoRapide principle is to explicit call dispose at the end of a each 
        /// Controller method with BaseController.DBDispose like:
        /// try {
        ///   ...
        /// } finally {
        ///    DBDispose();
        /// }
        /// 
        /// It is important to close connections because we do not close after each query. 
        /// </summary>
        public void Dispose() {
            if (IsDisposed) return;

            Log("");

            if (_cn1 != null) {
                try {
                    _cn1.Close();
                    _cn1 = null;
                    Log("_cn1 closed OK");
                } catch (Exception ex) {
                    Log("_cn1 " + ex.GetType().ToString() + ", " + ex.Message);
                }
            }
            //if (_cn2 != null) {
            //    try {
            //        _cn2.Close();
            //        _cn2 = null;
            //        Log("_cn2 closed OK");
            //    } catch (Exception ex) {
            //        Log("_cn2 " + ex.GetType().ToString() + ", " + ex.Message);
            //    }
            //}
            //if (_cn3 != null) {
            //    try {
            //        _cn3.Close();
            //        _cn3 = null;
            //        Log("_cn3 closed OK");
            //    } catch (Exception ex) {
            //        Log("_cn3 " + ex.GetType().ToString() + ", " + ex.Message);
            //    }
            //}
            try {
                Npgsql.NpgsqlConnection.ClearAllPools();
                Log("Npgsql.NpgsqlConnection.ClearAllPools OK");
            } catch (Exception ex) {
                Log("Npgsql.NpgsqlConnection.ClearAllPools " + ex.GetType().ToString() + ", " + ex.Message);
            }
            IsDisposed = true;
        }

        /// <summary>
        /// Returns id of class + method for use as <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> 
        /// (note that usually you should use the "currentUser".id for this purpose)
        /// </summary>
        /// <param name="caller"></param>
        /// <returns></returns>
        protected long GetId([System.Runtime.CompilerServices.CallerMemberName] string caller = "") => ApplicationPart.GetOrAdd<ClassAndMethod>(GetType(), caller, this).Id;
        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result">May be null</param>
        /// <param name="caller"></param>
        protected void Log(string text, Result result, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            Log(text, caller);
            result?.LogInternal(text, GetType(), caller);
        }

        public class PostgreSQLDatabaseException : ApplicationException {
            public PostgreSQLDatabaseException(Npgsql.NpgsqlCommand command, Exception inner) : this(command, null, inner) { }
            public PostgreSQLDatabaseException(Npgsql.NpgsqlCommand command, string message, Exception inner) : base("Exception " + inner.GetType() + " occurred when executing " + typeof(Npgsql.NpgsqlCommand) + " '" + command.CommandText + "'" + (string.IsNullOrEmpty(message) ? "" : ("Details: " + message)), inner) { }
        }
    }
}
