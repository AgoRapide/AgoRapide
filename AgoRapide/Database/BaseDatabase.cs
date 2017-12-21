// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide.Database {
    /// <summary>
    /// TOOD: Move functionality from <see cref="PostgreSQLDatabase"/> into <see cref="BaseDatabase"/>
    /// TODO: Abstract the basic <see cref="Npgsql.NpgsqlCommand"/> and similar, in order to support multiple databases
    /// TODO: without implementing full sub classes of <see cref="BaseDatabase"/>.
    /// 
    /// TODO: Add TryGetEntityIds and GetEntityIds with <see cref="QueryId"/> as parameter just like done with 
    /// <see cref="GetEntities{T}"/> and <see cref="TryGetEntities{T}"/>
    /// </summary>
    [Class(Description = "Provides the fundamentals for reading and writing towards a database")]
    public abstract class BaseDatabase : BaseCore, IDisposable {

        [ClassMember(Description = "Name of main table in database. Usually 'p'. Also used for naming other objects in database like SEQUENCE sequence_p_id and CONSTRAINT p_pk.")]
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

        //public void UseInMemoryCache<T>(BaseSynchronizer synchronizer) {
        //}

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

        public BaseEntity GetEntity(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, Type requiredType) => TryGetEntity(currentUser, id, accessTypeRequired, requiredType, out var retval, out var errorResponse) ? retval : throw new InvalidCountException(id + ". Details: " + errorResponse.ResultCode + ", " + errorResponse.Message);

        /// <summary>
        /// Convenience method, easier alternative to <see cref="TryGetEntities"/>
        /// 
        /// Only use this method for <see cref="QueryId.IsSingle"/> 
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="id"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="requiredType"></param>
        /// <param name="entity"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public bool TryGetEntity(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, Type requiredType, out BaseEntity entity, out ErrorResponse errorResponse) {
            id.AssertIsSingle();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, requiredType, out List<BaseEntity> temp, out errorResponse)) {
                entity = null;
                return false;
            }
            temp.AssertExactOne(() => nameof(id) + ": " + id.ToString());
            entity = temp[0];
            return true;
        }

        public T GetEntity<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired) where T : BaseEntity, new() => TryGetEntity<T>(currentUser, id, accessTypeRequired, out var retval, out var errorResponse) ? retval : throw new InvalidCountException(id + ". Details: " + errorResponse.ResultCode + ", " + errorResponse.Message);
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
        public bool TryGetEntity<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, out T entity, out ErrorResponse errorResponse) where T : BaseEntity, new() {
            id.AssertIsSingle();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, out List<T> temp, out errorResponse)) {
                entity = null;
                return false;
            }
            temp.AssertExactOne(() => nameof(id) + ": " + id.ToString());
            entity = temp[0];
            return true;
        }


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
        public List<T> GetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired) where T : BaseEntity, new() {
            id.AssertIsMultiple();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, out List<T> entities, out var errorResponse)) throw new InvalidCountException(id + ". Details: " + errorResponse.ResultCode + ", " + errorResponse.Message);
            return entities;
        }

        public List<BaseEntity> GetEntities(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, Type requiredType) {
            id.AssertIsMultiple();
            if (!TryGetEntities(currentUser, id, accessTypeRequired, requiredType, out var entities, out var errorResponse)) throw new InvalidCountException(id + ". Details: " + errorResponse.ResultCode + ", " + errorResponse.Message);
            return entities;
        }

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
        public bool TryGetEntities<T>(BaseEntity currentUser, QueryId id, AccessType accessTypeRequired, out List<T> entities, out ErrorResponse errorResponse) where T : BaseEntity, new() {
            if (!TryGetEntities(currentUser, id, accessTypeRequired, typeof(T), out var temp, out errorResponse)) {
                entities = null;
                return false;
            }
            entities = temp.Select(e => (T)e).ToList();
            return true;
        }


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
        /// Returns result of first querying against <paramref name="contexts"/> and then more specific <paramref name="id"/>
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="id">Must be either <see cref="QueryIdContext"/> or <see cref="QueryIdFieldIterator"/></param>
        /// <param name="requiredType"></param>
        /// <param name="contexts"></param>
        /// <param name="entities"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public bool TryGetContext(BaseEntity currentUser, QueryId id, Type requiredType, List<Context> contexts, out List<BaseEntity> entities, out ErrorResponse errorResponse) {
            // Note that "result" is in general discarded.
            // TODO: Communicate "result" somehow since it may contain useful information when the call above took a long time (because a new synchronization had to be made).
            var result = new Result();

            if (!Context.TryExecuteContextsQueries(currentUser, contexts, this, result, out var contextEntities, out errorResponse)) {
                entities = null;
                return false;
            }
            if (!contextEntities.TryGetValue(requiredType, out var requiredTypeEntities) || requiredTypeEntities.Count == 0) {
                entities = null;
                errorResponse = new ErrorResponse(ResultCode.data_error, "No entities of type " + requiredType + " contained within current context for " + currentUser.GetType().ToStringVeryShort() + " " + currentUser.IdFriendly);
                return false;
            }

            switch (id) {
                case QueryIdContext q:
                    entities = requiredTypeEntities.Values.ToList();
                    errorResponse = null;
                    return true;
                case QueryIdFieldIterator q:
                    /// TODO: Consider moving this code to <see cref="DrillDownSuggestion"/> 
                    var aggregateTotalExpected = q.AggregationType == AggregationType.Count ?
                        requiredTypeEntities.Values.Count :
                        PropertyKeyAggregate.CalculateSingleValue(q.AggregationType, q.AggregationKey, requiredTypeEntities.Values.ToList()) ?? 0L;

                    /// Note how <param name="requiredType"/> now is really ignored, as <see cref="FieldIterator"/> will be returned instead.
                    if (!contextEntities.TryGetValue(q.ColumnType, out var columnTypeEntities) || columnTypeEntities.Count == 0) {
                        entities = null;
                        errorResponse = new ErrorResponse(ResultCode.data_error, "No entities of type " + q.ColumnType + " contained within current context for " + currentUser.GetType().ToStringVeryShort() + " " + currentUser.IdFriendly);
                        return false;
                    }

                    /// For each suggested drill-down for <see cref="QueryIdFieldIterator.ColumnKey"/>, 
                    /// add that one to context, 
                    /// calculate the new context and 
                    /// ask for drill-down suggestions against <param name="requiredType"/> / <see cref="QueryIdFieldIterator.RowKey"/> 
                    /// (each such iteration will give us a new column in the final result)
                    /// Collect all drill-down suggestions thus found before creating <see cref="FieldIterator"/>-instances
                    /// (because <see cref="FieldIterator"/>-instances are row-based)
                    /// Note how <param name="requiredType"/> now is really ignored, as <see cref="FieldIterator"/> will be returned instead.
                    entities = new List<BaseEntity>();
                    var nextCoreP = int.MaxValue;
                    var allColumns = new List<(
                        PropertyKey PropertyKey,  /// Dynamically generated here based on <see cref="DrillDownSuggestion.Text"/> for <see cref="QueryIdFieldIterator.ColumnKey"/>)
                        Dictionary<
                            string,  /// Key is left-most column in table (<see cref="FieldIterator.LeftmostColumn"/> (the <see cref="DrillDownSuggestion.Text"/> for / <see cref="QueryIdFieldIterator.RowKey"/>)
                            long     /// The actual count for the given combination
                        > Values
                    )>();

                    var aggregateTotalAllColums = 0L;

                    var foundDateTimeComparerAmongColumns = false; // TRUE means that sums across columns are not relevant because they would probably be meaningless (adding ThisYear to ThisMonth for instance)
                    var foundDateTimeComparerAmongRows = false;  // TRUE means that sums across rows are not relevant because they would probably be meaningless (adding ThisYear to ThisMonth for instance)

                    DrillDownSuggestion.Create(q.ColumnType, columnTypeEntities.Values, limitToSingleKey: q.ColumnKey, excludeQuantileSuggestions: true,
                        includeAllSuggestions: true /// Extremely important, if not given then some data will be left out.
                    ).Values.ForEach(operatorsColumn => {
                        operatorsColumn.ForEach(operatorColumn => { /// TODO: Structure of result from <see cref="DrillDownSuggestion.Create"/> is too complicated. 
                            operatorColumn.Value.
                                Where(s => s.Value.Type == DrillDownSuggestion.DrillDownSuggestionType.Ordinary). /// It is in principle unnecessary to filter after introduction of excludeQuantileSuggestions above.
                                ForEach(suggestionColumn => {
                                    Log(suggestionColumn.Value.QueryId.ToString());
                                    if (suggestionColumn.Value.QueryId.Value is DateTimeComparer) foundDateTimeComparerAmongColumns = true;
                                    var thisColumn = new Dictionary<string, long>();
                                    var sumThisColumn = 0L; // TODO: Expand this to Min, Max and so on.

                                    var newContext = contexts.ToList();
                                    newContext.Add(new Context(SetOperator.Intersect, q.ColumnType, suggestionColumn.Value.QueryId));

                                    newContext.                                     // Remove all corresponding Union with the same key
                                        Where(c =>                                  // because Intersect in combination with Union would be meaningless.
                                            c.SetOperator == SetOperator.Union &&   // TODO: Is this a HACK? Should it be improved somehow?Æ
                                            c.Type == q.ColumnType &&
                                            c.QueryId is QueryIdKeyOperatorValue &&
                                            ((QueryIdKeyOperatorValue)c.QueryId).Key.PToString == suggestionColumn.Value.QueryId.Key.PToString).
                                        ToList().
                                        ForEach(c => {
                                            newContext.Remove(c);
                                        });

                                    result = new Result();
                                    if (!Context.TryExecuteContextsQueries(currentUser, newContext, this, result, out var newContextEntities, out var newErrorResponse)) {
                                        // We do not expect the call to fail here
                                        throw new Exception(nameof(Context.TryExecuteContextsQueries) + " failed (very unexpectedly) for " + q.ColumnType + " " + suggestionColumn.Value.QueryId + ". Details: " + nameof(newErrorResponse) + ": " + newErrorResponse);
                                    }
                                    // Note that "result" is now discarded.
                                    // TODO: Communicate "result" somehow since it may contain useful information when the call above took a long time (because a new synchronization had to be made).
                                    if (!newContextEntities.TryGetValue(requiredType, out var newRequiredTypeEntities) || newRequiredTypeEntities.Count == 0) {
                                        // Consider "normal". Corresponding column in final result will just be blank.
                                        return;
                                    }

                                    DrillDownSuggestion.Create(requiredType, newRequiredTypeEntities.Values, limitToSingleKey: q.RowKey, excludeQuantileSuggestions: true,
                                        includeAllSuggestions: true /// Extremely important, if not some data will be left out.
                                    ).Values.ForEach(operatorsRow => {
                                        operatorsRow.ForEach(operatorRow => { /// TODO: Structure of result from <see cref="DrillDownSuggestion.Create"/> is too complicated. 
                                            operatorRow.Value.
                                                Where(s => s.Value.Type == DrillDownSuggestion.DrillDownSuggestionType.Ordinary). /// It is in principle unnecessary to filter after introduction of excludeQuantileSuggestions above.
                                            ForEach(suggestionRow => {
                                                if (suggestionRow.Value.QueryId.Value is DateTimeComparer) foundDateTimeComparerAmongRows = true;
                                                if (q.AggregationType == AggregationType.Count) {
                                                    // 1) The simple case, we alredy know the answer since the mechanismk giving the drill-down suggestions has already counted for us
                                                    thisColumn.Add(suggestionRow.Value.QueryId.ToString(), suggestionRow.Value.Count);
                                                    sumThisColumn += suggestionRow.Value.Count;
                                                } else {
                                                    // 2) A more complicated case, we have to execute the actual context, and do the aggregate calculation
                                                    var aggregateContext = newContext.ToList();
                                                    aggregateContext.Add(new Context(SetOperator.Intersect, requiredType, suggestionRow.Value.QueryId));

                                                    if (!Context.TryExecuteContextsQueries(currentUser, aggregateContext, this, result, out var aggregateContextEntities, out var aggregateErrorResponse)) {
                                                        // We do not expect the call to fail here
                                                        throw new Exception(nameof(Context.TryExecuteContextsQueries) + " failed (very unexpectedly) for " + requiredType + " " + suggestionRow.Value.QueryId + ". Details: " + nameof(aggregateErrorResponse) + ": " + aggregateErrorResponse);
                                                    }
                                                    // Note that "result" is now discarded.
                                                    // TODO: Communicate "result" somehow since it may contain useful information when the call above took a long time (because a new synchronization had to be made).
                                                    if (!aggregateContextEntities.TryGetValue(requiredType, out var aggregateRequiredTypeEntities) || aggregateRequiredTypeEntities.Count == 0) {
                                                        // Consider "normal". Corresponding column in final result will just be blank.
                                                        return;
                                                    }
                                                    // Aggregate values
                                                    var value = PropertyKeyAggregate.CalculateSingleValue(q.AggregationType, q.AggregationKey, aggregateRequiredTypeEntities.Values.ToList());
                                                    if (value != null) {
                                                        thisColumn.Add(suggestionRow.Value.QueryId.ToString(), (long)value);
                                                        switch (q.AggregationType) {
                                                            case AggregationType.Count:
                                                                throw new InvalidEnumException(q.AggregationType); // Already checked above
                                                            case AggregationType.Sum:
                                                                sumThisColumn += (long)value; break;
                                                            default:
                                                                break; // Other types are difficult to aggregate
                                                        }
                                                    }
                                                }
                                            });
                                        });
                                    });

                                    if (foundDateTimeComparerAmongRows) {
                                        // Sum would probably be meaningless (adding ThisYear to ThisMonth for instance)
                                    } else {

                                        if (sumThisColumn != 0) {
                                            thisColumn.Add("SUM", sumThisColumn);
                                            aggregateTotalAllColums += (long)sumThisColumn;
                                        }
                                    }

                                    var pk = new PropertyKey(new PropertyKeyAttributeEnrichedDyn(new PropertyKeyAttribute( // Note how this is a "throw-away" instance only meant to be used within the context of the current API request.

                                            // Replaced text with only value 27 Nov 2017.
                                            // property: suggestionColumn.Value.Text, // TOOD: REMOVE CHARACTERS OTHER THAN A-Z, 0-9, _ here
                                            property: suggestionColumn.Value.QueryId.Value?.ToString() ?? "[NULL]",
                                            description: "Drill-down suggestion for " + q.ColumnType.ToStringVeryShort() + "/" + suggestionColumn.Value.QueryId.ToString(), //  "." + q.ColumnKey.Key.PToString + ": " + suggestionColumn.Value.Text,
                                            longDescription: suggestionColumn.Value.QueryId.Key.A.WholeDescription,
                                            isMany: false
                                        ) {
                                        Type = typeof(long),
                                        /// TODO: Consider adding <see cref="AggregationType.Count"/> and <see cref="AggregationType.Percent"/> here.
                                        /// 
                                        // TOOD: PUT BACK AGGREGATION TYPES HERE WHEN SUPPORTING THIS IN XXX
                                        // AggregationTypes = new AggregationType[] { AggregationType.Sum, AggregationType.Min, AggregationType.Max, AggregationType.Average, AggregationType.Median }
                                    },
                                        (CoreP)(nextCoreP--))); // Note how this is a "throw-away" instance only meant to be used within the context of the current API request.
                                    pk.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate();
                                    allColumns.Add((
                                        pk,
                                        thisColumn
                                    ));
                                });
                        });
                    });
                    if (aggregateTotalAllColums != aggregateTotalExpected) {
                        if (foundDateTimeComparerAmongColumns || foundDateTimeComparerAmongRows) {
                            // Normal situation
                        } else {
                            throw new InvalidCountException(aggregateTotalAllColums, aggregateTotalExpected, nameof(aggregateTotalAllColums));
                        }
                    }

                    allColumns.Sort((a, b) => a.PropertyKey.Key.PToString.CompareTo(b.PropertyKey.Key.PToString)); // Order columns alfabetically
                    var uniqueRows = allColumns.SelectMany(e => e.Item2.Keys).Distinct(); // Find all unique rows
                    if (foundDateTimeComparerAmongColumns) {
                        // Sum would probably be meaningless (adding ThisYear to ThisMonth for instance)
                    } else {
                        switch (q.AggregationType) {
                            case AggregationType.Count:
                            case AggregationType.Sum:
                                // Sum all rows.
                                var pk = new PropertyKey(new PropertyKeyAttributeEnrichedDyn(new PropertyKeyAttribute( // Note how this is a "throw-away" instance only meant to be used within the context of the current API request.

                                        // Replaced text with only value 27 Nov 2017.
                                        // property: suggestionColumn.Value.Text, // TOOD: REMOVE CHARACTERS OTHER THAN A-Z, 0-9, _ here
                                        property: "Sum",
                                        description: "Column sum",
                                        longDescription: null,
                                        isMany: false
                                    ) {
                                    Type = typeof(long),
                                },
                                    (CoreP)(nextCoreP--))); // Note how this is a "throw-away" instance only meant to be used within the context of the current API request.
                                pk.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate();
                                //var rowSums = new Dictionary<string, long>();
                                //uniqueRows.ForEach(r => {
                                //    rowSums.Add(r, allColumns.Select(c => c.Values.TryGetValue(r, out var v) ? (long?)v : null).Where(v => v != null).Aggregate(0L, (s, v) => s + (long)v));
                                //});
                                var newColumn = uniqueRows.Select(r => (r, allColumns.Select(c => c.Values.TryGetValue(r, out var v) ? (long?)v : null).Where(v => v != null).Aggregate(0L, (s, v) => s + (long)v))).ToDictionary(e => e.Item1, e => e.Item2);
                                var aggregateTotalAllRows = newColumn.Aggregate(0L, (s, v) => "SUM".Equals(v.Key) ? s : s + v.Value); // Careful not sum SUM calculated above
                                if (aggregateTotalAllRows != aggregateTotalExpected) {
                                    if (foundDateTimeComparerAmongColumns || foundDateTimeComparerAmongRows) {
                                        // Normal situation
                                    } else {
                                        throw new InvalidCountException(aggregateTotalAllRows, aggregateTotalExpected, nameof(aggregateTotalAllRows));
                                    }
                                }
                                allColumns.Add((pk, newColumn));
                                break;
                        }
                    }

                    entities = uniqueRows.
                        OrderBy(s => "SUM".Equals(s) ? "ZZZ" : s). // Order rows alfabetically
                        Select(r => (BaseEntity)(new FieldIterator(r, allColumns.Select(c => (Property)(c.Values.TryGetValue(r, out var v) ?
                                 new PropertyT<long>(c.PropertyKey.PropertyKeyWithIndex, v) :
                                             /// null) // This does not work because methods like <see cref="FieldIterator.ToHTMLTableRowHeading"/> will not have enough information
                                             new PropertyT<long>(c.PropertyKey.PropertyKeyWithIndex, 0) /// TODO: An alternative would be to use null as value here (and typeof(long?)) as type above, but then we must watch out for methods like <see cref="PropertyKeyAggregate.CalculateSingleValue"/>
                                           )
                            ).ToList()))
                        ).ToList();
                    errorResponse = null;
                    return true;
                default:
                    throw new InvalidObjectTypeException(id);
            }
        }

        /// <summary>
        /// See <see cref="CoreAPIMethod.History"/>. 
        /// Implementator should return results with ORDER BY <see cref="DBField.id"/> DESC
        /// 
        /// TODO: Implement some LIMIT statement or throw exception if too many, or add an explanatory message
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract List<Property> GetEntityHistory(BaseEntity entity);

        protected void AssertAccess(BaseEntity currentUser, BaseEntity entity, AccessType accessTypeRequired) {
            if (!TryVerifyAccess(currentUser, entity, accessTypeRequired, out var errorResponse)) throw new AccessViolationException(nameof(currentUser) + " " + currentUser.Id + " " + nameof(currentUser.AccessLevelGiven) + " " + currentUser.AccessLevelGiven + " insufficent for " + entity.Id + " (" + nameof(accessTypeRequired) + ": " + accessTypeRequired + "). Details: " + errorResponse);
        }

        /// <summary>
        /// TODO: NOT YET IMPLEMENTED IN IMPLEMENTATION
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="entity"></param>
        /// <param name="accessTypeRequired"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public abstract bool TryVerifyAccess(BaseEntity currentUser, BaseEntity entity, AccessType accessTypeRequired, out string errorResponse);

        public T GetEntityById<T>(long id) where T : BaseEntity, new() => TryGetEntityById(id, typeof(T), out var retval) ? (T)(object)retval : throw new ExactOneEntityNotFoundException(id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requiredType">May be null</param>
        /// <returns></returns>
        public BaseEntity GetEntityById(long id, Type requiredType) => TryGetEntityById(id, requiredType, out var retval) ? retval : throw new ExactOneEntityNotFoundException(id);

        public bool TryGetEntityById<T>(long id, out T entity) where T : BaseEntity, new() {
            if (!TryGetEntityById(id, typeof(T), out var retval)) {
                entity = null;
                return false;
            }
            entity = (T)retval;
            return true;
        }

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

        /// <summary>
        /// Note how this is used for both "ordinary" entities and properties that may have children
        /// 
        /// Returns null if !<paramref name="parentProperty"/>.<see cref="PropertyKeyAttribute.CanHaveChildren"/>
        /// </summary>
        /// <param name="parentProperty"></param>
        /// <returns></returns>
        public abstract ConcurrentDictionary<CoreP, Property> GetChildProperties(Property parentProperty);

        public Property GetPropertyById(long id) => TryGetPropertyById(id, out var retval) ? retval : throw new PropertyNotFoundException(id);
        public abstract bool TryGetPropertyById(long id, out Property property);

        /// <summary>
        /// Gets all root properties of a given type. 
        /// The implementation must return result in increasing order.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract List<long> GetRootPropertyIds(Type type);

        public abstract List<Property> GetRootProperties(Type type);

        /// <summary>
        /// TODO: NOT IN USE AS OF SEP 2017.
        /// TODO: If we are going to use this it has to be made as efficient as <see cref="GetAllEntities(Type)"/>
        /// 
        /// Gets all entities of type <typeparamref name="T"/>. 
        /// TODO: Add overload which can be limited to source / workspace or similar. 
        /// TODO: Or use <see cref="TryGetEntities"/> for that purpose. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract List<T> GetAllEntities<T>() where T : BaseEntity, new();
        /// <summary>
        /// Gets all entities of type <paramref name="type"/>
        /// 
        /// TODO: Add overload which can be limited to source / workspace or similar. 
        /// TODO: Or use <see cref="TryGetEntities"/> for that purpose. 
        /// 
        /// NOTE: Implementation is expected to do something more efficient than just 
        /// NOTE:   GetRootPropertyIds(type).Select(id => GetEntityById(id, type)).ToList();
        /// NOTE: because that will result in too many queries against the database. 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract List<BaseEntity> GetAllEntities(Type type); // Added 9 Sep 2017

        public long CreateEntity<T>(long cid, Result result) => CreateEntity(cid, typeof(T), properties: (IEnumerable<(PropertyKeyWithIndex key, object value)>)null, result: result);
        public long CreateEntity(long cid, Type entityType, Result result) => CreateEntity(cid, entityType, properties: (IEnumerable<(PropertyKeyWithIndex key, object value)>)null, result: result);
        public long CreateEntity<T>(long cid, Parameters properties, Result result) => CreateEntity(cid, typeof(T), properties.Properties.Values.Select(p => (p.Key, p.Value)), result);
        public long CreateEntity(long cid, Type entityType, Parameters properties, Result result) => CreateEntity(cid, entityType, properties.Properties.Values.Select(p => (p.Key, p.Value)), result);
        public long CreateEntity<T>(long cid, IEnumerable<(PropertyKeyWithIndex key, object value)> properties, Result result) => CreateEntity(cid, typeof(T), properties, result);
        public long CreateEntity<T>(long cid, Dictionary<CoreP, Property> properties, Result result) => CreateEntity(cid, typeof(T), properties.Values.Select(p => (p.Key, p.Value)), result);
        public long CreateEntity(long cid, Type entityType, Dictionary<CoreP, Property> properties, Result result) => CreateEntity(cid, entityType, properties.Values.Select(p => (p.Key, p.Value)), result);
        /// <summary>
        /// Returns <see cref="DBField.id"/>
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="entityType"></param>
        /// <param name="properties">May be null or empty. Turn this into an Properties collection? Or just a BaseEntity template or similar?</param>
        /// <param name="result">May be null</param>
        /// <returns></returns>
        public long CreateEntity(long cid, Type entityType, IEnumerable<(PropertyKeyWithIndex key, object value)> properties, Result result) {
            Log(nameof(cid) + ": " + cid + ", " + nameof(entityType) + ": " + entityType.ToStringShort() + ", " + nameof(properties) + ": " + (properties?.Count().ToString() ?? "[NULL]"));
            InvalidTypeException.AssertAssignable(entityType, typeof(BaseEntity), detailer: null);
            var retval = new Result();
            var pid = CreateProperty(cid, null, null, CoreP.RootProperty.A().PropertyKeyWithIndex, entityType, result);
            if (properties != null) {
                foreach (var v in properties) {
                    CreateProperty(cid, pid, null, v.key, v.value, result);
                }
            }
            return pid;
        }

        public void AssertUniqueness(PropertyKeyWithIndex key, object value) {
            if (!TryAssertUniqueness(key, value, out var existing, out var errorResponse)) throw new UniquenessException(errorResponse + "\r\nDetails: " + existing.ToString());
        }
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
        /// null is allowed only when <see cref="Util.CurrentlyStartingUp"/>. 
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
        public abstract long CreateProperty(long? cid, long? pid, long? fid, PropertyKeyWithIndex key, object value, Result result = null);

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
        public abstract void OperateOnProperty(long? operatorId, Property property, PropertyOperation operation, Result result = null);

        public abstract void Dispose();

        /// <summary>
        /// <see cref="BaseEntity.Properties"/>
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="noLongerCurrent">
        /// Will always be set. 
        /// <see cref="OperateOnProperty"/> should be called with <see cref="PropertyOperation.SetInvalid"/> for these by calling method. 
        /// </param>
        /// <returns></returns>
        protected ConcurrentDictionary<CoreP, Property> OrderIntoIntoBaseEntityPropertiesCollection(List<Property> properties, out List<Property> noLongerCurrent) {
            noLongerCurrent = new List<Property>();
            var retval = new ConcurrentDictionary<CoreP, Property>();
            foreach (var p in properties) {
                var test = p.Key; /// Check that <see cref="Property.KeyDB"/> parses correctly. 
                if (p.Key.Key.A.IsMany) {
                    var isManyParent = retval.GetOrAddIsManyParent(p.Key);
                    if (isManyParent.Properties.TryGetValue(p.Key.IndexAsCoreP, out var toBeOverwritten)) {
                        noLongerCurrent.Add(toBeOverwritten);
                    }
                    isManyParent.Properties[p.Key.IndexAsCoreP] = p;
                } else {
                    if (retval.TryGetValue(p.Key.Key.CoreP, out var toBeOverwritten)) {
                        noLongerCurrent.Add(toBeOverwritten);
                    }
                    retval[p.Key.Key.CoreP] = p;
                }
            };
            return retval;
        }

        protected void SetNoLongerCurrent(List<Property> noLongerCurrent) {
            if (noLongerCurrent.Count > 0) {
                Log("Calling " + nameof(OperateOnProperty) + " for " + noLongerCurrent.Count + " properties");
                var id = GetIdNonStrict(MethodBase.GetCurrentMethod());
                noLongerCurrent.ForEach(p => { /// Note use of <see cref="GetIdNonStrict"/> because we might have <see cref="ApplicationPart.GetFromDatabaseInProgress"/>
                    OperateOnProperty(operatorId: id, property: p, operation: PropertyOperation.SetInvalid, result: null);
                });
            }
        }

        /// <summary>
        /// Creates and populates an instance of <see cref="BaseEntity"/> based on <paramref name="root"/> and <paramref name="properties"/>. 
        /// (note that <paramref name="root"/> itself may also be returned if <paramref name="requiredType"/> indicates that 
        /// <see cref="Property"/> is desired).
        /// </summary>
        /// <param name="requiredType"></param>
        /// <param name="root"></param>
        /// <param name="properties">May be null (typical for <paramref name="requiredType"/> = <see cref="Property"/>)</param>
        /// <returns></returns>
        protected BaseEntity CreateEntityInMemory(Type requiredType, Property root, ConcurrentDictionary<CoreP, Property> properties) {
            BaseEntity retval = null;
            var addProperties = new Action(() => {
                if (properties == null) return;
                retval.Properties = properties;
                retval.Properties.Values.ForEach(p => {
                    p.Parent = retval;
                    if (p.Properties != null) { /// Typical case for <see cref="Property.IsIsManyParent"/>
                        p.Properties.Values.ForEach(p2 => {
                            p2.Parent = retval;
                        });
                    }
                });
            });
            if (root.Key.Key.CoreP.Equals(CoreP.RootProperty)) {
                /// This is the "normal" variant, that <param name="root"/> is root-property of the BaseEntity-object
                /// being asked for.                
                var rootType = root.V<Type>();
                if (requiredType != null) {
                    InvalidTypeException.AssertAssignable(requiredType, typeof(BaseEntity), () => "Regards parameter " + nameof(requiredType));
                    InvalidTypeException.AssertAssignable(rootType, requiredType, () => nameof(requiredType) + " (" + requiredType + ") does not match " + nameof(rootType) + " (" + rootType + " (as found in database as " + root.V<string>() + "))");
                }

                /// TODO: 
                /// TODO: Decide how to use <see cref="InMemoryCache"/> if <param name="requiredType"/> was null
                /// TODO: 

                if (rootType.IsAbstract) throw new InvalidTypeException(rootType, nameof(rootType) + " (as found in database as " + root.V<string>() + "). Details: " + root.ToString());
                // Log("Activator.CreateInstance(requiredType) (" + rootType.ToStringShort() + ")");
                // Note how "where T: new()" constraint helps to ensure that we have a parameter less constructor now
                // We could of course also check with rootType.GetConstructor first.
                retval = Activator.CreateInstance(rootType) as BaseEntity ?? throw new InvalidTypeException(rootType, "Very unexpected since was just asserted OK");

                retval.Id = root.Id;
                retval.RootProperty = root;
                retval.Created = root.Created;

                addProperties();

                retval.Properties.AddValue(CoreP.RootProperty, root);
                retval.AddProperty(CoreP.DBId.A(), root.Id);
                switch (retval) {
                    case IStaticProperties s: s.GetStaticProperties().ForEach(p => retval.Properties.AddValue(p.Key, p.Value)); break;
                }
                return retval;
            } else {
                // This is only allowed if what is asked for is a property-object, or if it can be assumed that returning a property-object is OK.
                if (requiredType != null) {
                    if (requiredType.Equals(typeof(Property))) { /// <param name="root"/> is excplicit what is asked for, OK. 
                        retval = root;
                        addProperties();
                        return retval;
                    }
                    if (requiredType.Equals(typeof(BaseEntity))) { // Assume that root (as Property) is what is asked for. 
                        retval = root;
                        addProperties();
                        return retval;
                    }
                }
                throw new InvalidEnumException(root.Key.Key.CoreP, "Expected " + PropertyKeyMapper.GetA(CoreP.RootProperty).Key.A.EnumValueExplained + " but got " + nameof(root.KeyDB) + ": " + root.KeyDB + ".\r\n" +
                    (requiredType == null ?
                        ("Possible cause (1): Method " + MethodBase.GetCurrentMethod().Name + " was called without " + nameof(requiredType) + " and a redirect to " + nameof(TryGetPropertyById) + " was therefore not possible.\r\n") :
                        ("Possible cause (2): " + nameof(root.Id) + " does not point to an 'entity root property' (" + nameof(CoreP.RootProperty) + ").")
                    )
                );
            }
        }

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
        /// <param name="SkipSetValid">
        /// May be null.
        /// If set then (for cases when existing value equals <paramref name="value"/>) 
        /// <see cref="PropertyOperation.SetValid"/> will not be performed 
        /// if age of last <see cref="Property.Valid"/> is less than the given value.
        /// In other words, this is a performance increasing feature in cases where you do not constantly want / need to 
        /// update <see cref="DBField.valid"/> (typicall would be API method information at startup for instance). 
        /// </param>
        /// <returns></returns>
        public void UpdateProperty<T>(long cid, BaseEntity entity, PropertyKey key, T value, Result result = null, TimeSpan? SkipSetValid = null) {
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
                        } else if (SkipSetValid != null && existingProperty.Valid != null && existingProperty.Valid.Value.Add(SkipSetValid.Value) > DateTime.Now) {
                            // Skip this, recently updated.
                        } else {
                            OperateOnProperty(cid, existingProperty, PropertyOperation.SetValid, result);
                        }
                        result?.Count(typeof(Property), CountP.CountStillValid);
                    } else { // Vanlig variant
                             // TODO: Use of default value.ToString() here is not optimal
                             // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                             // TODO: At least for presenting DateTime-objects and double in a preferred manner
                        Log(detailer() + ". Property changed from '" + existingValue + "' to '" + valueToUpdate + "'", result);
                        var changedProperty = creator(keyToUse, valueToUpdate);
                        result?.Count(typeof(Property), CountP.CountChanged);
                        entityOrIsManyParent.Properties[keyAsCoreP] = changedProperty; // TOOD: result-Property from creator has not been initialized properly now (with Parent for instance)
                    }
                } else {
                    // TODO: Use of default value.ToString() here is not optimal
                    // TODO: Implement AgoRapide extension method for ToString-representation of generic value?
                    // TODO: At least for presenting DateTime-objects and double in a preferred manner
                    Log(detailer() + ". Property was not known. Initial value: '" + valueToUpdate + "'", result);
                    var newProperty = creator(keyToUse, valueToUpdate);
                    entityOrIsManyParent.Properties[keyAsCoreP] = newProperty; // TOOD: result-Property from creator has not been initialized properly now (with Parent for instance)
                    result?.Count(typeof(Property), CountP.CountTotal);
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
                                isManyParent.Properties.ForEach(e => { /// Note that entityOrIsManyParentUpdater can not be used because it relies on <typeparam name="T"/>
                                    var existingProperty = e.Value;
                                    var s = existingProperty.V<string>();
                                    if (remainingToAdd.Contains(s)) { /// Note how we compare with string while entityOrIsManyParentUpdater compares directly against <typeparam name="T"/>
                                        if (existingProperty.Id <= 0) {
                                            // Skip this, property is in-memory only 
                                        } else if (SkipSetValid != null && existingProperty.Valid != null && existingProperty.Valid.Value.Add(SkipSetValid.Value) > DateTime.Now) {
                                            // Skip this, recently updated.
                                        } else {
                                            OperateOnProperty(cid, existingProperty, PropertyOperation.SetValid, result);
                                        }
                                        remainingToAdd.Remove(s);
                                    } else {
                                        if (existingProperty.Id <= 0) {
                                            // Skip this, property is in-memory only 
                                        } else {
                                            OperateOnProperty(cid, existingProperty, PropertyOperation.SetInvalid, result);
                                        }
                                        // result?.Count(CoreP.PSetInvalidCount); // TODO: Correct statistics here
                                        toBeDeleted.Add(e.Key);
                                    }
                                });
                                toBeDeleted.ForEach(c => isManyParent.Properties.TryRemove(c, out _)); /// May already have been removed by <see cref="OperateOnProperty"/>
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

        public class OpenDatabaseConnectionException : ApplicationException {
            public OpenDatabaseConnectionException(string message) : base(message) { }
            public OpenDatabaseConnectionException(string message, Exception inner) : base(message, inner) { }
        }

        public class NoResultFromDatabaseException : ApplicationException {
            public NoResultFromDatabaseException(string message) : base(message) { }
            public NoResultFromDatabaseException(string message, Exception inner) : base(message, inner) { }
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

        /// <summary>
        /// TODO: Move into <see cref="BaseDatabase"/>
        /// </summary>
        public class UniquenessException : ApplicationException {
            public UniquenessException(string message) : base(message) { }
            public UniquenessException(string message, Exception inner) : base(message, inner) { }
        }

        public class InvalidPasswordException<T> : ApplicationException where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            public InvalidPasswordException(T property) : this(property, null, null) { }
            public InvalidPasswordException(T property, string message) : this(property, message, null) { }
            public InvalidPasswordException(T property, string message, Exception inner) : base(property.GetEnumValueAttribute().EnumValueExplained + (string.IsNullOrEmpty(message) ? "" : (". Details: " + message)), inner) { }
        }

        public class PropertyNotFoundException : ApplicationException {
            public PropertyNotFoundException(long id) : base("Property with id '" + id + "' not found") { }
        }
    }
}