﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// See <see cref="Create"/>
    /// 
    /// TODO: Move somewhere else because also referenced from <see cref="AgoRapide.Database.BaseDatabase.TryGetContext"/>
    /// TODO: Most probably it belongs in AgoRapide.Core
    /// </summary>
    [Class(Description =
        "Contains a drill-down suggestion within some -" + nameof(Context) + "-.\r\n" +
        "For example if the context is a list of orders then -" + nameof(Create) + "- could return relevant drill-down suggestions for every specific year " +
        "(Year = 2017, Year = 2018, Year = 2019 and so on).\r\n" +
        "(the context itself is not part of this class.)r\n" +
        "\r\n" +
        "Note how\r\n" +
        "-" + nameof(DrillDownSuggestion.ToAddToContextUrl) + "- returns a -" + nameof(CoreAPIMethod.UpdateProperty) + "- API-command " +
        "for adding the suggestion to a current -" + nameof(Context) + "-\r\n" +
        "while\r\n" +
        "-" + nameof(ToQueryUrl) + "- returns the URL for querying ALL entities satisfying -" + nameof(DrillDownSuggestion.QueryId) + "- (regardless of context).\r\n"
    //"\r\n" +
    //"-" + nameof(DrillDownSuggestion.Count) + "- gives the number of entities remaining within the context after executing -" + nameof(ToAddToContextUrl) + "-, " +
    //"in other words -" + nameof(DrillDownSuggestion.Count) + "- is only relevant for -" + nameof(ToAddToContextUrl) + "-, NOT -" + nameof(ToQueryUrl) + "-).\r\n"
    )]
    public class DrillDownSuggestion {

        public Type EntityType { get; private set; }

        [ClassMember(Description = "Number of entities of type -" + nameof(EntityType) + "- remaining within the context after executing - " + nameof(ToAddToContextUrl) + "-.")]
        public long Count { get; private set; }

        public string Text { get; private set; }
        public QueryIdKeyOperatorValue QueryId { get; private set; }

        /// <summary>
        /// TODO: Explain what is actually the purpose / meaning of this
        /// </summary>
        [Enum(Description = "Introduced in order to avoid some -" + nameof(DrillDownSuggestion) + "- being used in conjunction with -" + nameof(QueryIdFieldIterator) + "-")]
        public enum DrillDownSuggestionType {
            None,
            GlobalQuantile,
            LocalQuantile,
            /// <summary>
            /// TODO: Explain what is actually the purpose / meaning of this
            /// </summary>
            Ordinary,
        }

        public DrillDownSuggestionType Type { get; private set; }

        public DrillDownSuggestion(DrillDownSuggestionType type, Type entityType, long count, string text, QueryIdKeyOperatorValue queryId) {
            Type = type;
            EntityType = entityType;
            Count = count;
            Text = text;
            QueryId = queryId;
        }

        /// TODO: Consider implementing this, in order to make more consistent with <see cref="ToAddToContextUrl"/>
        //public GeneralQueryResult ToQueryUrl(string header) => new GeneralQueryResult(Url, header + ": " + Text);

        /// <summary>
        /// TODO: Find a better name for this method / property
        /// 
        /// TODO: Clean up use of parameters.
        /// </summary>
        /// <param name="setOperator"></param>
        /// <param name="request"></param>
        /// <param name="header"></param>
        /// <param name="useOnlyHeader"></param>
        /// <returns></returns>
        [ClassMember(Description = "Returns an -" + nameof(CoreAPIMethod.UpdateProperty) + "- API-command for adding the suggestion to a current -" + nameof(Context) + "-")]
        public GeneralQueryResult ToAddToContextUrl(SetOperator setOperator, ValidRequest request, string header, bool useOnlyHeader) => new GeneralQueryResult(
            request.API.CreateAPIUrl(
                CoreAPIMethod.UpdateProperty,
                request.CurrentUser.GetType(),
                new QueryIdInteger(request.CurrentUser.Id),
                CoreP.Context.A(),
                new Context(setOperator, EntityType, QueryId).ToString()
            ),
            useOnlyHeader ? header : (header + ": " + Text));

        private Uri _queryUrl;
        [ClassMember(Description = "URL for querying ALL entities satisfying -" + nameof(DrillDownSuggestion.QueryId) + "- (regardless of context).")]
        public Uri ToQueryUrl => _queryUrl ?? (_queryUrl = APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, EntityType, QueryId));

        /// <summary>
        /// TODO: From Nov 2017 this method is also use in combination with <see cref="QueryIdFieldIterator"/> 
        /// TODO: (therefore the introduction of <paramref name="includeAllSuggestions"/>
        /// TOOD: but there may be some subtle bugs remaining which could lead to suggestions being omitted 
        /// TOOD: (this would again lead to a "0"-value in the corresponding <see cref="FieldIterator"/> row / column).
        /// TODO: Apr 2019: Situation has not been improved (see "HACK: Added 22 Apr 2019. Note how this excludes possibility of iterating over individual dates.")
        /// 
        /// Note that result is <see cref="ConcurrentDictionary{TKey, TValue}"/> instead of <see cref="Dictionary{TKey, TValue}"/> because code 
        /// executes in parallell.
        /// 
        /// TODO: Structure of result from <see cref="DrillDownSuggestion.Create"/> is too complicated. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities">Alle objects most be assignable to <paramref name="type"/></param>
        /// <param name="maxSuggestionsPerKey">
        /// Practical method when only some ideas about possible suggestions are needed
        /// (typical when called from <see cref="BaseController.HandleCoreMethodContext"/> (<see cref="CoreAPIMethod.Context"/>).
        /// </param>
        /// <param name="limitToSingleKey">
        /// May be null. 
        /// If null then suggestions for all keys as found by <see cref="Extensions.GetChildProperties"/> will be returned. 
        /// </param>
        /// <param name="excludeQuantileSuggestions">Useful in combination with <see cref="QueryIdFieldIterator"/> for which quantiles will mostly be confusing</param>
        /// <param name="includeAllSuggestions">
        /// If TRUE then drill-down suggestion will be given even if ALL <paramref name="entities"/> satisfy the suggestion. 
        /// Essentially to set to TRUE when used in combination with <see cref="QueryIdFieldIterator"/>. 
        /// Will be ignored for keys of type <see cref="DateTime"/> (see "HACK: Added 22 Apr 2019. Note how this excludes possibility of iterating over individual dates.")
        /// </param>
        /// <returns></returns>
        [ClassMember(Description = "Returns all relevant drill-down suggestions")]
        public static ConcurrentDictionary<
                CoreP,
                Dictionary<
                    Operator,
                    Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >
                >
             > Create(Type type, IEnumerable<BaseEntity> entities, long? maxSuggestionsPerKey = null, PropertyKey limitToSingleKey = null, bool excludeQuantileSuggestions = false, bool includeAllSuggestions = false) {

            var retval = new ConcurrentDictionary<
                CoreP,
                Dictionary<
                    Operator,
                    Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >
                >
             >();

            var totalCount = entities.Count();
            if (totalCount == 0) return retval;

            entities.ForEach(e => InvalidObjectTypeException.AssertAssignable(e, type));

            /// NOTE: The level of parallelization here is quite coarse.
            /// NOTE: If for instance <param name="type"/> has only one child property that is computationaly expensive, then
            /// NOTE: the parallelization here would have no effect.
            Parallel.ForEach(type.GetChildProperties().Values, key => {

                if (limitToSingleKey != null && key.Key.CoreP != limitToSingleKey.Key.CoreP) return;

                var properties = entities.Select(e => e.Properties == null ? null : (e.Properties.TryGetValue(key.Key.CoreP, out var p) ? p : null));
                var propertiesNotNull = properties.Where(p => p != null).ToList(); // Used for Percentile-evaluation

                var percentileDrilldowns = new Dictionary< // How this is inserted into retval is a HACK. TODO: Make prettier
                        string, // The actual values found. 
                        DrillDownSuggestion
                >();

                if (!excludeQuantileSuggestions) {
                    if (key.Key.A.IsSuitableForPercentileCalculation) {
                        // Offer percentile evaluations for these.

                        // NOTE: Hardcoded use of quantile Quintile as of Sep 2017
                        // TODO: Add preferred quantile to PropertyKeyAttribute.

                        // Quintile calculated based on ALL properties within "universe".
                        Util.EnumGetValues<Quintile>().ForEach(q => {
                            var count = propertiesNotNull.Count(p => p.Percentile.AsQuintile == q);
                            if (count == 0) return; // No point in suggesting
                            if (count == totalCount) return; // No point in suggesting
                            var query = new QueryIdKeyOperatorValue(key.Key, Operator.EQ, q);
                            percentileDrilldowns.Add("Global " + q.ToString(), // TODO: Works fine until we have a value Global Quintile1 or similar (will give us key-collision below)
                                new DrillDownSuggestion(DrillDownSuggestionType.GlobalQuantile, type, count, q + " (" + count + ")", query)
                            );
                        });

                        // Lowest and highest quintile based on only properties within selection

                        if (!key.Key.A.OperatorsAsHashSet.Contains(Operator.GT)) {
                            /// LT, LEQ, GEQ, GT considered equivalant. Most probably we could also ask for if Type implements <see cref="IComparable"/>
                        } else if (propertiesNotNull.Count < 10) {
                            // Offering quintiles now considered irrelevant
                        } else {
                            var sortedProperties = propertiesNotNull.OrderBy(p => p.Value).ToList();
                            var oneFifth = sortedProperties.Count / 5;

                            // TODO: Go over algorithm here. May be off by one element!
                            var quintileValue = sortedProperties[oneFifth - 1].Value; /// Calculate value for local <see cref="Quintile.Quintile1"/>
                            // Count as efficient as possible, based on the fact that the list is already sorted.
                            var count = oneFifth; while (count < sortedProperties.Count && sortedProperties[count].Value == quintileValue) count++;
                            var query = new QueryIdKeyOperatorValue(key.Key, Operator.LEQ, quintileValue);
                            // TODO: Find better term than "Local" (confer with "Global above).
                            percentileDrilldowns.Add("Local " + Quintile.Quintile1, // TODO: Works fine until we have a value Local Quintile1 or similar (will give us key-collision below)
                                new DrillDownSuggestion(DrillDownSuggestionType.LocalQuantile, type, count, "Local " + Quintile.Quintile1 + " (" + count + ")", query)
                            );

                            // TODO: Go over algorithm here. May be off by one element!
                            quintileValue = sortedProperties[sortedProperties.Count - oneFifth].Value; /// Calculate value for local <see cref="Quintile.Quintile5"/>
                            // Count as efficient as possible, based on the fact that the list is already sorted.
                            count = oneFifth; while ((sortedProperties.Count - count) >= 0 && sortedProperties[sortedProperties.Count - count].Value == quintileValue) count++;
                            query = new QueryIdKeyOperatorValue(key.Key, Operator.GEQ, quintileValue);
                            // TODO: Find better term than "Local" (confer with "Global above).
                            percentileDrilldowns.Add("Local " + Quintile.Quintile5, // TODO: Works fine until we have a value Local Quintile5 or similar (will give us key-collision below)
                                new DrillDownSuggestion(DrillDownSuggestionType.LocalQuantile, type, count, "Local " + Quintile.Quintile5 + " (" + count + ")", query)
                            );
                        }
                    }
                }

                if (key.Key.A.Operators == null) {
                    if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                        retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                    }
                    return;
                }

                /// Can also be true if <paramref name="includeAllSuggestions"/> is set.
                var mixOfNullsAndNotNullFound = false; // Found either directly (see direct below) or as "side effect" in lambda (see further below)
                List<(object, string)> objStrValues = null; // This are the actual values that we will offer for drill-down, like "Monday", "42", "John", "Smith" or similar.

                var addDateTimeComparer = key.Key.A.Type == typeof(DateTime);
                if (addDateTimeComparer && includeAllSuggestions) {
                    includeAllSuggestions = false; // HACK: Added 22 Apr 2019. Note how this excludes possibility of iterating over individual dates.
                    /// NOTE: If you want to add the possibility of iterating over invividual dates then one possible solution is to add
                    /// NOTE: another field to <see cref="PropertyKeyAttribute"/> called DoNotUseDateTimeComparer for instance.
                }

                /// TODO: Decide about <see cref="Operator.NEQ"/>. As of Sep 2017 we use <see cref="SetOperator.Remove"/> as substitute
                if (key.Key.A.Operators.Length == 1 && key.Key.A.Operators[0] == Operator.EQ && !key.Key.A.HasLimitedRange && !includeAllSuggestions) {
                    // Our only choice is limiting to NULL or NOT NULL values.  
                    if (entities.All(e => e.Properties == null || !e.Properties.TryGetValue(key.Key.CoreP, out _))) { // None are set.
                        if (addDateTimeComparer) {
                            goto calculate_objStrValues;
                        }
                        if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                            retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                        }
                        return; // None are set.
                    }
                    if (!includeAllSuggestions && entities.All(e => e.Properties != null && e.Properties.TryGetValue(key.Key.CoreP, out _))) { // All are set.
                        if (addDateTimeComparer) {
                            goto calculate_objStrValues;
                        }
                        if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER! 
                            retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                        }
                        return; // All are set.
                    }
                    objStrValues = new List<(object, string)> { }; // null, "NULL" will be added below
                    mixOfNullsAndNotNullFound = true;
                } else {
                    // Note how Distinct() is called weakly typed for object, meaning it uses the Equals(object other)-method.
                    var objValues = !key.Key.A.IsMany ?
                        properties.Select(p => p?.Value ?? null).Distinct() :             // Added support for IsMany 24 Nov 2018.
                        properties.Select(p => p?.Value ?? null).SelectMany(o => o == null ? new List<object> { null } : o as List<object> ?? throw new InvalidObjectTypeException(o, typeof(List<object>), "For all IsMany-properties the expected Property Value is List<object>" + Util.BreakpointEnabler)).Distinct();

                    /// Note that the Distinct() operation done above will not work properly of IEquatable is not implemented for the actual type.
                    /// We therefore work around this by collecting together all object-values with the same string-representation
                    /// (Since the resulting API command is string-based this should not present any issues)
                    objStrValues = objValues.
                        Where(v => {
                            if (v == null) mixOfNullsAndNotNullFound = true; // Note "side effect" here.
                            return v != null;
                        }).
                        Select(v => (v, key.Key.ConvertObjectToString(v))).Distinct(new EqualityComparerTupleObjectString()).
                        ToList(); // Important in order for "side effect" to work as intended (we must force execution of the Where-lambda)

                    if (typeof(ITypeDescriber).IsAssignableFrom(key.Key.A.Type)) { /// Enforce IEquatable or similar for these classes anyway as it will improve performance (less calls to <see cref="PropertyKeyAttributeEnriched.ConvertObjectToString"/>)
                        if (objStrValues.Count != objValues.Count()) {
                            var t = typeof(IEquatable<>).MakeGenericType(new Type[] { key.Key.A.Type });
                            throw new InvalidCountException(objStrValues.Count, objValues.Count(),
                                    nameof(PropertyKeyAttributeEnriched.ConvertObjectToString) + " is inconsistent with " + nameof(Enumerable.Distinct) + " for " + key.Key.A.Type + ".\r\n" +
                                "Possible cause: " + // TODO: This explanation is possible wrong. It is the Equals(object other)-method that most probably is missing, and that does not have any connection with IEquatable
                                    (t.IsAssignableFrom(key.Key.A.Type) ?
                                        ("Wrongly implemented " + t.ToStringShort() + " for " + key.Key.A.Type.ToStringShort()) :
                                        ("Missing implementation of " + t.ToStringShort() + " for " + key.Key.A.Type.ToStringShort())
                                    ) + "\r\n" +
                                "Remember to also always implement Equals (both Equals(object other) and Equals(" + key.Key.A.Type.ToStringVeryShort() + " other) and GetHashCode together with " + t.ToStringShort()
                            );
                        }
                    } else {
                        /// We can not enforce IEquatable for other classes. 
                        /// Note that it is of course worth it anyway to implement IEquatable for all your classes (with corresponding Equals and GetHashCode))
                        /// (it can for instance dramatically reduce the size of the objValues-collection, improving the performance here)
                    }

                    if (objStrValues.Count == 0) {
                        mixOfNullsAndNotNullFound = false;
                    }
                }
                if (mixOfNullsAndNotNullFound) {
                    objStrValues.Add((null, "NULL"));
                }

                calculate_objStrValues:

                var r1 = new Dictionary<
                    Operator,
                    Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >
                >();

                Util.EnumGetValues<Operator>().ForEach(o => {
                    if (o != Operator.EQ) return; /// Because not supported by <see cref="QueryIdKeyOperatorValue.IsMatch(BaseEntity)"/>

                    if (!key.Key.A.OperatorsAsHashSet.Contains(o)) return;

                    var objStrValuesForThisOperator = objStrValues;
                    if (o == Operator.EQ && !key.Key.A.HasLimitedRange && !includeAllSuggestions) {
                        /// NOTE: Count > 50 is arbitrarily chosen.
                        /// NOTE: Note that for only <see cref="Operator.EQ"/> this will only happen with Count == 1 because of filtering out above for HasLimitedRange
                        /// NOTE: For <see cref="DateTime"/>, long and similar types that can be compared like <see cref="Operator.LT"/> and similar 
                        /// NOTE: 
                        if (objStrValuesForThisOperator.Count > 50) { // Increased from 10 to 50 29 Nov 2017 because we now have much better HTML-presentation of drill-down suggestions.
                            /// We have too many values for using <see cref="Operator.EQ"/> against all of them, but we can check for null and 0
                            objStrValuesForThisOperator = new List<(object, string)>();
                            if (objStrValues[objStrValues.Count - 1].Item1 == null) {
                                objStrValuesForThisOperator.Add((null, "NULL"));
                            }
                            if (typeof(long).Equals(key.Key.A.Type)) {
                                if (objStrValues.Any(i => i.Item2 == "0")) { // TODO: Any-query IS NOT GOOD PERFORMANCE WISE
                                    objStrValuesForThisOperator.Add(((long)0, "0"));
                                }
                            }
                            // Note, result now may quite well be null suggestions
                        } else if (limitToSingleKey != null) {
                            /// HACK: For situation where <see cref="Database.BaseDatabase.TryGetContext"/> iterates over a column
                            /// HACK: we do not want spurious extra rows due to some columns values giving few row elements 
                            objStrValuesForThisOperator = new List<(object, string)>();
                        }
                    }
                    if (addDateTimeComparer && o == Operator.EQ) {
                        objStrValuesForThisOperator = objStrValuesForThisOperator.ToList(); // IMPORTANT, copy object, in order not to affect original liste (in case we have more than one operator)
                        Util.EnumGetValues<DateTimeComparer>().ForEach(c => objStrValuesForThisOperator.Add((c, c.ToString())));
                    }

                    if (objStrValuesForThisOperator.Count == 0) {
                        return; // Give up altogether
                    }

                    var r2 = new Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >();
                    objStrValuesForThisOperator.ForEach(t => {
                        if (maxSuggestionsPerKey != null && r2.Count >= maxSuggestionsPerKey) return;

                        var query = new QueryIdKeyOperatorValue(key.Key, o, t.Item1); // Now how it is "random" which object value (out of several with identical string-representation) is chosen now. But we assume that all of them have the same predicate effect
                        var count = entities.Where(query.IsMatch).Count();

                        if (count > 0 && (includeAllSuggestions || count != totalCount)) { /// Note how we do not offer drill down if all entities match (except when <paramref name="includeAllSuggestions"/> is set.
                            r2.AddValue(
                                t.Item2,
                                new DrillDownSuggestion(DrillDownSuggestionType.Ordinary, type, count, t.Item2 + " (" + count + ")", query)
                           );
                        }
                    });
                    if (r2.Count > 0) r1.AddValue(o, r2);
                });
                if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                    if (!r1.TryGetValue(Operator.EQ, out var r2)) {
                        r2 = (r1[Operator.EQ] = percentileDrilldowns);
                    } else {
                        percentileDrilldowns.ForEach(e => r2.AddValue(e.Key, e.Value, () => nameof(percentileDrilldowns)));
                    }
                }
                if (r1.Count > 0) retval.Add(key.Key.CoreP, r1);
            });
            return retval;
        }

        /// <summary>
        /// Work around problems with <see cref="Enumerable.Distinct"/> and <see cref="PropertyKeyAttributeEnriched.ConvertObjectToString"/>
        /// </summary>
        public class EqualityComparerTupleObjectString : IEqualityComparer<(object, string)> {
            public bool Equals((object, string) t1, (object, string) t2) => t1.Item2.Equals(t2.Item2);
            public int GetHashCode((object, string) t) => t.Item2.GetHashCode();
        }
    }
}