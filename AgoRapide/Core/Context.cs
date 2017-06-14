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
    public class Context : ITypeDescriber {

        public SetOperator SetOperator { get; private set; }
        public Type Type { get; private set; }
        public QueryId QueryId { get; private set; }

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
            if (!APIMethod.TryGetTypeFromVeryShortString(strType, out var type)) { id = null; errorResponse = "Invalid type (" + strType + ") found"; return false; }
            // TODO: Improve parser, there may be ; within QueryId
            var strQueryId = nextWord(); if (string.IsNullOrEmpty(strQueryId)) { id = null; errorResponse = "No " + nameof(QueryId) + " found"; return false; }
            if (!QueryId.TryParse(strQueryId, out var queryId, out errorResponse)) { id = null; return false; };
            var strLeftover = nextWord(); if (strLeftover != null) { id = null; errorResponse = nameof(strLeftover) + ": " + strLeftover; return false; }
            id = new Context(setOperator, type, queryId);
            errorResponse = null;
            return true;
        }

        public override string ToString() => SetOperator + ";" + Type.ToStringVeryShort() + ";" + QueryId.ToString();

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
                    if (!TryGetTraversal(fromType, toType, out var traversal)) return; // Disjoint "universes".
                    InvalidCountException.AssertCount(traversal.Count, 1); // TODO: Implement traversing over more than one level.Start with a limit of two levels(do not use recursivity)                    
                    traversal[0].Key.A.AssertNotIsMany(() => "Traversals not yet supported"); // TOOD: Implement support for IsMany (easy to implement, just iterate through List instead of single value below)
                    var toEntities = retval.GetValue(fromType, () => nameof(toType));
                    fromEntities.Values.Where(e => !toEntities.ContainsKey(e.PV<long>(traversal[0]))).ToList().ForEach(e => fromEntities.Remove(e.Id));
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

            /// TODO: Add entity types connected with the types that we have, but which are not included in contexts given.
            
            return retval;
        }

        public static List<PropertyKey> GetTraversal(Type fromType, Type toType) => TryGetTraversal(fromType, toType, out var retval) ? retval : throw new InvalidTraversalException(fromType, toType, "No possible traversals found");
        private static Dictionary<Type, Dictionary<Type, List<PropertyKey>>> _getTraversalCache;
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
        public static bool TryGetTraversal(Type fromType, Type toType, out List<PropertyKey> traversal) {
            if (_getTraversalCache == null) {
                _getTraversalCache = new Dictionary<Type, Dictionary<Type, List<PropertyKey>>>();
                APIMethod.AllEntityTypes.ForEach(from => {
                    var traversalFrom = (_getTraversalCache[from] = new Dictionary<Type, List<PropertyKey>>());
                    APIMethod.AllEntityTypes.ForEach(to => {
                        var traversalThisCombination = new List<PropertyKey>();
                        from.GetChildProperties().Values.Where(p => p.Key.A.ForeignKeyOf != null).ForEach(key => {
                            if (to.IsAssignableFrom(key.Key.A.ForeignKeyOf)) {
                                if (traversalThisCombination.Count > 0) {
                                    throw new InvalidTraversalException(from, to, /// TODO: Add possibility for multiple traversals, like from Person to Person (as superior) and Person (as subordinates)
                                        "Multiple routes found, both\r\n" +
                                        string.Join(",", traversalThisCombination.Select(t => t.Key.PToString)) + "\r\nand\r\n" +
                                        key.Key.PToString);
                                }
                                traversalThisCombination.Add(key);
                            } else {
                                // TODO: Implement traversing over more than one level. Start with a limit of two levels (do not use recursivity)
                            }
                        });
                        traversalFrom[to] = traversalThisCombination.Count > 0 ? traversalThisCombination : null;
                    });
                });
            }
            if (!_getTraversalCache.TryGetValue(fromType, out var temp)) {
                throw new InvalidTraversalException(fromType, toType, nameof(fromType) + " (" + fromType + ") not found in " + nameof(_getTraversalCache));
            }
            if (!temp.TryGetValue(fromType, out traversal)) {
                throw new InvalidTraversalException(fromType, toType, nameof(toType) + " (" + toType + ") not found in " + nameof(temp));
            }
            return traversal != null;
        }

        /// <summary>
        /// <see cref="TryGetTraversal"/> must have been called once before this method.
        /// </summary>
        /// <param name="fromType"></param>
        /// <returns></returns>
        public static Dictionary<Type, List<PropertyKey>> GetPossibleTraversalsFromType(Type fromType) {
            if (_getTraversalCache == null) throw new NullReferenceException(nameof(_getTraversalCache) + ". Possible resolution: " + nameof(TryGetTraversal) + " must be called before this method");
            return _getTraversalCache.TryGetValue(fromType, out var retval) ? retval : throw new InvalidTraversalException(fromType, fromType, "Unknown " + nameof(fromType));
        }

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
    }
}