// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide.Core {

    [Class(
        Description = "Building block for drill down functionality and AgoRapide query language"
    )]
    public class Context : ITypeDescriber, IEquatable<Context> {

        public SetOperator SetOperator { get; private set; }
        public Type Type { get; private set; }
        public QueryId QueryId { get; private set; }

        public bool Equals(Context other) => SetOperator == other.SetOperator && Type.Equals(other.Type) && QueryId.Equals(other.QueryId);
        public override bool Equals(object other) {
            if (other == null) return false;
            switch (other) {
                case Context context: return Equals(context);
                default: return false;
            }
        }
        private int? _hashcode;
        public override int GetHashCode() => (int)(_hashcode ?? (_hashcode = (SetOperator + "_" + Type + "_" + QueryId.ToString()).GetHashCode()));

        /// <summary>
        /// TODO: Implement <see cref="Context.Size"/>
        /// TODO: Analyze whole database / all results given from <see cref="BaseSynchronizer"/>, store in <see cref="PropertyKeyAttributeEnriched"/>
        /// TODO: Use that again against <see cref="Context.QueryId"/>
        /// </summary>
        public Percentile Size = new Percentile(50);

        public Context(SetOperator setOperator, Type type, QueryId queryId) {
            SetOperator = setOperator; InvalidEnumException.AssertDefined(SetOperator);
            Type = type ?? throw new ArgumentNullException(nameof(type));
            QueryId = queryId ?? throw new ArgumentNullException(nameof(queryId));
        }

        public static Context Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidContextException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(string value, out Context id) => TryParse(value, out id, out var dummy);
        public static bool TryParse(string value, out Context id, out string errorResponse) {
            value += ";"; // Simplifies parsing
            value = value.Replace("%3B", ";"); /// HACK: TODO: Fix decoding in <see cref="QueryId.TryParse"/> and <see cref="Context.TryParse
            var pos = 0;
            var nextWord = new Func<string>(() => {
                var nextPos = value.IndexOf(';', pos);
                if (nextPos == -1) return null;
                var word = value.Substring(pos, nextPos - pos);
                pos = nextPos + 1;
                return word;
            });
            var strSetOperator = nextWord(); if (string.IsNullOrEmpty(strSetOperator)) { id = null; errorResponse = "No " + nameof(SetOperator) + " found"; return false; }
            if (!Util.EnumTryParse<SetOperator>(strSetOperator, out var setOperator, out errorResponse)) { id = null; return false; };
            var strType = nextWord(); if (string.IsNullOrEmpty(strType)) { id = null; errorResponse = "No type found"; return false; }
            if (!Util.TryGetTypeFromString(strType, out var type)) { id = null; errorResponse = "Invalid type (" + strType + ") found"; return false; }
            // TODO: Improve parser, there may be ; within QueryId
            var strQueryId = nextWord(); if (string.IsNullOrEmpty(strQueryId)) { id = null; errorResponse = "No " + nameof(QueryId) + " found"; return false; }
            if (!QueryId.TryParse(strQueryId, out var queryId, out errorResponse)) { id = null; return false; };
            var strLeftover = nextWord(); if (strLeftover != null) { id = null; errorResponse = nameof(strLeftover) + ": " + strLeftover; return false; }
            id = new Context(setOperator, type, queryId);
            errorResponse = null;
            return true;
        }

        public override string ToString() => SetOperator + ";" + Type.ToStringVeryShort() + ";" + QueryId.ToString();

        /// <summary>
        /// TODO: Reconsider necessity (value) of this method
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [ClassMember(
            Description = "Returns TRUE if given entity is exactly specified within this -" + nameof(Context) + "-.",
            LongDescription = "May return false negatives")]
        public bool SpecifiesEntityExact(BaseEntity entity) {
            if (!Type.IsAssignableFrom(entity.GetType())) return false;
            switch (QueryId) {
                case QueryIdInteger queryIdInteger: if (queryIdInteger.Id.Equals(entity.Id)) return true; break;
                case QueryIdString queryIdString: if (entity.IdString.Equals(queryIdString.ToString())) return true; break; // TODO: Test this variant

                    /// See also <see cref="SpecifiesEntityAsPart"/>
            }
            return false;
        }

        /// <summary>
        /// TODO: Reconsider necessity (value) of this method
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [ClassMember(
            Description = "Returns TRUE if given entity is part of this -" + nameof(Context) + "- (but does not constitue the whole of it).",
            LongDescription = "May return false negatives")]
        public bool SpecifiesEntityAsPart(BaseEntity entity) {
            if (!Type.IsAssignableFrom(entity.GetType())) return false;
            switch (QueryId) {
                case QueryIdKeyOperatorValue queryIdKeyOperatorValue: if (queryIdKeyOperatorValue.IsAll) return true; break;

                    /// See also <see cref="SpecifiesEntityExact(BaseEntity)"/>
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentUser">May be null (in which case empty list will be returned)</param>
        /// <param name="entity">Id may be null (in which case empty list will be returned)</param>
        /// <returns></returns>
        [ClassMember(Description = "Note that returned result may have omissions or contain superfluous items")]
        public static List<GeneralQueryResult> GetPossibleContextOperationsForCurrentUserAndEntity(Request request, BaseEntity entity) {
            var retval = new List<GeneralQueryResult>();
            if (entity is Property) return retval; // This most probably only creates confusion in the API HTML administrative interface.
            if (request.CurrentUser == null) return retval;
            if (entity.Id == 0) return retval;
            var contexts = request.CurrentUser.PV<List<Context>>(CoreP.Context.A(), new List<Context>());

            var adder = new Action<SetOperator>(setOperator => {
                var r = new GeneralQueryResult(); /// TODO: Consider adding constructor for <see cref="GeneralQueryResult"/> with these properties as parameters
                r.AddProperty(
                    CoreP.SuggestedUrl.A(),
                    request.API.CreateAPIUrl(
                        CoreAPIMethod.UpdateProperty,
                        request.CurrentUser.GetType(),
                        new QueryIdInteger(request.CurrentUser.Id),
                        CoreP.Context.A(),
                        new Context(setOperator, entity.GetType(), new QueryIdInteger(entity.Id)).ToString()
                    )
                );
                r.AddProperty(CoreP.Description.A(), nameof(SetOperator) + "." + setOperator);
                retval.Add(r);
            });

            if (contexts.Any(c => c.SpecifiesEntityAsPart(entity) && (c.SetOperator == SetOperator.Union || c.SetOperator == SetOperator.Intersect))) {
                adder(SetOperator.Remove);
            }

            if (!contexts.Any(c => c.SpecifiesEntityAsPart(entity) || c.SpecifiesEntityExact(entity))) {
                adder(SetOperator.Union);
            }

            /// <see cref="PropertyOperation.SetInvalid"/> for context properties which may be removed.
            if (request.CurrentUser.Properties.TryGetValue(CoreP.Context, out var contextParent)) {
                contextParent.Properties.Values.Where(p => p.V<Context>().SpecifiesEntityExact(entity)).ForEach(p => {
                    var r = new GeneralQueryResult(); /// TODO: Consider adding constructor for <see cref="GeneralQueryResult"/> with these properties as parameters
                    r.AddProperty(
                        CoreP.SuggestedUrl.A(),
                        request.API.CreateAPIUrl(
                            CoreAPIMethod.PropertyOperation,
                            typeof(Property),
                            new QueryIdInteger(p.Id),
                            PropertyOperation.SetInvalid
                        )
                    );
                    r.AddProperty(CoreP.Description.A(), nameof(Context) + "." + PropertyOperation.SetInvalid);
                    retval.Add(r);
                });
            }
            return retval;
        }
        public static void EnrichAttribute(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    ParseResult.Create(key, retval) :
                    ParseResult.Create(errorResponse);
            });

        public class InvalidContextException : ApplicationException {
            public InvalidContextException(string message) : base(message) { }
            public InvalidContextException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="contexts">TODO: Consider removal of this parameter</param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static Dictionary<Type, Dictionary<long, BaseEntity>> ExecuteContextsQueries(BaseEntity currentUser, List<Context> contexts, BaseDatabase db) {
            var types = contexts.Select(c => c.Type).Distinct();

            /// Built up dictionary for each type based on <see cref="AgoRapide.SetOperator.Intersect"/> and <see cref="AgoRapide.SetOperator.Union"/>. 
            var retval = types.Select(t => {
                Dictionary<long, BaseEntity> retvalThisType = null;
                /// Start with smallest sized set. TODO: <see cref="Context.Size"/> is not implemented as of June 2017.
                contexts.Where(c => c.Type.Equals(t) && c.SetOperator == SetOperator.Intersect).OrderBy(c => c.Size.Value).Reverse().ToList().ForEach(c => { /// Do all <see cref="AgoRapide.SetOperator.Intersect"/>
                    if (!db.TryGetEntities(currentUser, c.QueryId, AccessType.Read, t, out var temp, out var errorResponse)) return;
                    var tempDict = temp.ToDictionary(e => e.Id);
                    if (retvalThisType == null) {
                        retvalThisType = tempDict;
                    } else {
                        retvalThisType.Where(e => !tempDict.ContainsKey(e.Key)).ToList().ForEach(e => retvalThisType.Remove(e.Key));
                    }
                });
                if (retvalThisType == null) retvalThisType = new Dictionary<long, BaseEntity>();
                contexts.Where(c => c.Type.Equals(t) && c.SetOperator == SetOperator.Union).ForEach(c => { /// Do all <see cref="AgoRapide.SetOperator.Union"/>
                    if (!db.TryGetEntities(currentUser, c.QueryId, AccessType.Read, t, out var temp, out var errorResponse)) return;
                    temp.ForEach(e => retvalThisType[e.Id] = e); // Adds to dictionary (if not already there)
                });
                return (Type: t, Dictiomary: retvalThisType);
            }).ToDictionary(tuple => tuple.Type, tuple => tuple.Dictiomary);

            // Take intersects of all these collections through automatic traversal
            types.ForEach(fromType => {
                var fromEntities = retval.GetValue(fromType, () => nameof(fromType));
                if (fromEntities.Count == 0) return; // Nothing to remove
                types.ForEach(toType => {
                    if (fromType.Equals(toType)) return; // Assumed irrelevant (even though there might hypotetically be valid traversals, like from Person superior to Person subordinate or the other way.
                    if (!TryGetTraversal(fromType, toType, out var traversals)) return; // Disjoint "universes".
                    if (traversals.Count > 1) throw new NotImplementedException("Traversals over multiple levels not supported (from " + fromType + " to " + toType + " via " + string.Join(",", traversals.Select(t => t.ToString())));
                    InvalidCountException.AssertCount(traversals.Count, 1); // TODO: Implement traversing over more than one level.Start with a limit of two levels(do not use recursivity)                    
                    var traversal = traversals[0];
                    traversal.Key.Key.A.AssertNotIsMany(() => "Traversals not yet supported for " + nameof(PropertyKeyAttribute.IsMany)); // TOOD: Implement support for IsMany (easy to implement, just iterate through List instead of single value below)

                    var toEntities = retval.GetValue(fromType, () => nameof(toType));

                    switch (traversal.Direction) {
                        case TraversalDirection.FromForeignKey:
                            InvalidTypeException.AssertAssignable(toType, traversal.Key.Key.A.ForeignKeyOf, () => "Invalid assumption about traversal from " + fromType + ": " + traversal.Key);
                            fromEntities.Values.Where(from => !toEntities.ContainsKey(from.PV<long>(traversal.Key))).ToList().ForEach(e => {
                                fromEntities.Remove(e.Id);
                            }); break;
                        case TraversalDirection.ToForeignKey:
                            InvalidTypeException.AssertAssignable(fromType, traversal.Key.Key.A.ForeignKeyOf, () => "Invalid assumption about traversal from " + fromType + ": " + traversal.Key);
                            fromEntities.Values.Where(from => !toEntities.Values.Any(to => to.PV<long>(traversal.Key, 0) == from.Id)).ToList().ForEach(e => {
                                fromEntities.Remove(e.Id);
                            }); break;
                        default: throw new InvalidEnumException(traversal.Direction);
                    }
                });
            });

            /// Remove all hits for <see cref="AgoRapide.SetOperator.Remove"/>
            types.ForEach(t => {
                var entities = retval.GetValue(t, () => nameof(t));
                if (entities.Count == 0) return; // Nothing to remove
                contexts.Where(c => c.Type.Equals(t) && c.SetOperator == SetOperator.Remove).ForEach(c => { /// Do all <see cref="AgoRapide.SetOperator.Remove"/>
                    if (!db.TryGetEntities(currentUser, c.QueryId, AccessType.Read, t, out var temp, out var errorResponse)) return;
                    temp.ForEach(e => {
                        if (entities.ContainsKey(e.Id)) entities.Remove(e.Id);
                    });
                });
            });

            // Note how some values for retval now may very well be empty

            var traversedValues = new Dictionary<Type, Dictionary<long, BaseEntity>>();

            /// Add entity types connected with the types that we have, but which are not included in contexts given.            
            types.ForEach(fromType => {
                GetPossibleTraversalsFromType(fromType).ForEach(traversals => {
                    // TODO: Inefficient use of Contains since types is just an IEnumerable. Turn types into HashSet or similar.
                    if (types.Contains(traversals.Key)) throw new NotImplementedException("Traversal from " + fromType + " to already known type " + traversals + " not yet implemented"); // TODO. Decide how to handle this, maybe just ignore
                    if (traversedValues.ContainsKey(traversals.Key)) throw new NotImplementedException("Multiple traversals to " + traversals.Key + " (one of them from " + fromType + ") not yet implemented"); // TODO. Decide how to handle this, maybe just add from what already found
                    if (traversals.Value.Count > 1) throw new NotImplementedException("Traversals over multiple levels not supported (from " + fromType + " to " + traversals.Key + " via " + string.Join(",", traversals.Value.Select(t => t.ToString())));

                    var traversal = traversals.Value[0];

                    var fromValues = retval.GetValue(fromType).Values;
                    var toValues = new Dictionary<long, BaseEntity>();

                    switch (traversal.Direction) {
                        case TraversalDirection.FromForeignKey:
                            InvalidTypeException.AssertAssignable(traversals.Key, traversal.Key.Key.A.ForeignKeyOf, () => "Invalid assumption about traversal from " + fromType + ": " + traversal.ToString());
                            fromValues.ForEach(from => {
                                if (!from.Properties.TryGetValue(traversal.Key.Key.CoreP, out var p)) return;
                                var foreignId = p.V<long>();
                                if (toValues.ContainsKey(foreignId)) return;
                                toValues[foreignId] = db.GetEntityById(foreignId, traversals.Key);
                            }); break;
                        case TraversalDirection.ToForeignKey:
                            /// TODO: Ensure that <see cref="InvalidTypeException.AssertAssignable"/> is used correct here (compare with <see cref="TryGetTraversal"/>
                            InvalidTypeException.AssertAssignable(fromType, traversal.Key.Key.A.ForeignKeyOf, () => "Invalid assumption about traversal from " + fromType + ": " + traversal.ToString());
                            fromValues.ForEach(from => {
                                var toEntities = db.GetEntities(currentUser, new QueryIdKeyOperatorValue(traversal.Key.Key, Operator.EQ, from.Id), AccessType.Read, traversals.Key);
                                toEntities.ForEach(to => {
                                    if (toValues.ContainsKey(to.Id)) return;
                                    toValues[to.Id] = to;
                                });
                            }); break;
                    }

                    traversedValues.Add(traversals.Key, toValues);
                });
            });

            traversedValues.ForEach(t => retval.AddValue(t.Key, t.Value));
            return retval;
        }

        public static List<Traversal> GetTraversal(Type fromType, Type toType) => TryGetTraversal(fromType, toType, out var retval) ? retval : throw new InvalidTraversalException(fromType, toType, "No possible traversals found.\r\nPossible resolution: Ensure that " + nameof(PropertyKeyAttribute.ForeignKeyOf) + " = typeof(" + toType.ToStringVeryShort() + ") has been specified for a property of " + fromType.ToStringVeryShort() + ".");
        private static Dictionary<Type, Dictionary<
            Type,
            List<Traversal>> // Note how this is stored as Null (not an empty List) if no traversals exists between the two types
        > _getTraversalCache;
        /// <summary>
        /// Returns route <paramref name="fromType"/> <paramref name="toType"/>
        /// 
        /// Note how throws exception if combination of types is not known. 
        /// 
        /// Not thread-safe for initial call. Therefore first call to this method must be done single threaded at application startup. 
        /// 
        /// TODO: Implement traversing over more than one level. Start with a limit of two levels (do not use recursivity)
        /// </summary>
        /// <param name="fromType">Must be one of <see cref="APIMethod.AllEntityTypes"/></param>
        /// <param name="toType">Must be one of <see cref="APIMethod.AllEntityTypes"/></param>
        /// <param name="traversal">Returned value will be either null or have Count > 0</param>
        /// <returns></returns>
        public static bool TryGetTraversal(Type fromType, Type toType, out List<Traversal> traversal) {
            if (_getTraversalCache == null) {
                _getTraversalCache = new Dictionary<Type, Dictionary<Type, List<Traversal>>>();
                APIMethod.AllEntityTypes.ForEach(from => {
                    var traversalFrom = (_getTraversalCache[from] = new Dictionary<Type, List<Traversal>>());
                    APIMethod.AllEntityTypes.ForEach(to => {
                        var traversalThisCombination = new List<Traversal>();
                        from.GetChildProperties().Values.Where(p => p.Key.A.ForeignKeyOf != null).ForEach(key => { /// <see cref="TraversalDirection.FromForeignKey"/>
                            if (to.IsAssignableFrom(key.Key.A.ForeignKeyOf)) {
                                if (traversalThisCombination.Count > 0) {
                                    throw new InvalidTraversalException(from, to, /// TODO: Add possibility for multiple traversals, like from Person to Person (as superior) and Person (as subordinates)
                                        "Multiple routes found, both\r\n" +
                                        string.Join(",", traversalThisCombination.Select(t => t.ToString())) + "\r\nand\r\n" +
                                        key.Key.PToString);
                                }
                                traversalThisCombination.Add(new Traversal(TraversalDirection.FromForeignKey, key));
                            } else {
                                // TODO: Implement traversing over more than one level. Start with a limit of two levels (do not use recursivity)

                                // TODO: Note how traversals over more than one level fails silently here
                            }
                        });
                        to.GetChildProperties().Values.Where(p => p.Key.A.ForeignKeyOf != null).ForEach(key => { /// <see cref="TraversalDirection.ToForeignKey"/>
                            if (from.IsAssignableFrom(key.Key.A.ForeignKeyOf)) {
                                if (traversalThisCombination.Count > 0) {
                                    throw new InvalidTraversalException(from, to, /// TODO: Add possibility for multiple traversals, like from Person to Person (as superior) and Person (as subordinates)
                                        "Multiple routes found, both\r\n" +
                                        string.Join(",", traversalThisCombination.Select(t => t.ToString())) + "\r\nand\r\n" +
                                        key.Key.PToString);
                                }
                                traversalThisCombination.Add(new Traversal(TraversalDirection.ToForeignKey, key));
                            } else {
                                // TODO: Implement traversing over more than one level. Start with a limit of two levels (do not use recursivity)

                                // TODO: Note how traversals over more than one level fails silently here
                            }
                        });

                        traversalFrom[to] = traversalThisCombination.Count > 0 ? traversalThisCombination : null;
                    });
                });
            }
            if (!_getTraversalCache.TryGetValue(fromType, out var temp)) {
                throw new InvalidTraversalException(fromType, toType, nameof(fromType) + " (" + fromType + ") not found in " + nameof(_getTraversalCache));
            }
            if (!temp.TryGetValue(toType, out traversal)) {
                throw new InvalidTraversalException(fromType, toType, nameof(toType) + " (" + toType + ") not found in " + nameof(temp));
            }
            return traversal != null;
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<Type, List<Traversal>>> _getPossibleTraversalsFromTypeCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<Type, List<Traversal>>>();
        /// <summary>
        /// <see cref="TryGetTraversal"/> must have been called once before this method.
        /// </summary>
        /// <param name="fromType"></param>
        /// <returns></returns>
        public static Dictionary<Type, List<Traversal>> GetPossibleTraversalsFromType(Type fromType) => _getPossibleTraversalsFromTypeCache.GetOrAdd(fromType, t => {
            if (_getTraversalCache == null) {
                // throw new NullReferenceException(nameof(_getTraversalCache) + ". Possible resolution: " + nameof(TryGetTraversal) + " must be called before this method");
                TryGetTraversal(typeof(APIMethod), typeof(ClassMember), out _);
                if (_getTraversalCache == null) throw new NullReferenceException(nameof(_getTraversalCache));
            }
            if (!_getTraversalCache.TryGetValue(fromType, out var temp)) throw new InvalidTraversalException(fromType, fromType, "Unknown " + nameof(fromType));
            return temp.Where(e => e.Value != null).ToDictionary(e => e.Key, e => e.Value);
        });

        public class InvalidTraversalException : ApplicationException {
            public InvalidTraversalException(string message) : base(message) { }
            public InvalidTraversalException(Type fromType, Type toType, string message) : base(
                "Unable to traverse from " + fromType.ToStringVeryShort() + " to " + toType.ToStringVeryShort() + ".\r\n" +
                (typeof(BaseEntity).IsAssignableFrom(fromType) ? "" : "Possible cause: " + nameof(fromType) + " not of type " + nameof(BaseEntity) + ".\r\n") +
                (APIMethod.AllEntityTypes.Contains(fromType) ? "" : "Possible cause: " + nameof(fromType) + " not known by any " + nameof(APIMethod) + ".\r\n") +
                (typeof(BaseEntity).IsAssignableFrom(toType) ? "" : "Possible cause: " + nameof(toType) + " not of type " + nameof(BaseEntity) + ".\r\n") +
                (APIMethod.AllEntityTypes.Contains(toType) ? "" : "Possible cause: " + nameof(toType) + " not known by any " + nameof(APIMethod) + ".\r\n") +
                "Details: " + message) { }
            public InvalidTraversalException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Move to Enum-folder if becomes permanent.
        /// </summary>
        [Enum(
            Description = "Describes direction of entity relationship traversion",
            AgoRapideEnumType = EnumType.EnumValue)]
        public enum TraversalDirection {

            None,

            [EnumValue(
                Description = "From the many-side of the relation towards the one-side",
                LongDescription = "Query will look like Entity/WHERE Id = value")]
            FromForeignKey,

            [EnumValue(
                Description = "From the one-side of the relation towards the many-side",
                LongDescription = "Query will look like Entity/WHERE {ForeignKey} = value"
            )]
            ToForeignKey,
        }

        public class Traversal {
            public TraversalDirection Direction { get; private set; }
            public PropertyKey Key { get; private set; }

            public Traversal(TraversalDirection direction, PropertyKey key) {
                Direction = direction;
                Key = key;
            }

            public override string ToString() => Direction + "_" + Key.Key.PToString;
        }
    }
}