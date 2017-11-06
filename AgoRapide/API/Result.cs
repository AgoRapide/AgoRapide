// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// Never stored in database. 
    /// 
    /// Communicates result of API method back to client. 
    /// 
    /// See also <see cref="APIMethodAttribute.ShowDetailedResult"/>
    /// 
    /// Usually available as <see cref="ValidRequest.Result"/>
    /// </summary>  
    [Class(Description = "Communicates results of an API command back to client")]
    public class Result : BaseEntityWithLogAndCount {

        public ResultCode ResultCode {
            get => PVM<ResultCode>();
            set => AddPropertyM(value);
        }

        /// <summary>
        /// For not-<see cref="ResultCode.ok"/> will set <see cref="CoreP.ResultCodeDescription"/> and <see cref="CoreP.APIDocumentationUrl"/>. 
        /// For <see cref="ResultCode.exception_error"/> will set <see cref="CoreP.ExceptionDetailsUrl"/>. 
        /// </summary>
        /// <param name="request"></param>
        private void AdjustAccordingToResultCodeAndMethod(Request request) {
            if (ResultCode == ResultCode.ok && !request.Method.MA.ShowDetailedResult) {
                if (Properties != null && Properties.ContainsKey(CoreP.Log)) Properties.Remove(CoreP.Log);
            }
            if (ResultCode != ResultCode.ok) {
                AddProperty(ResultP.ResultCodeDescription.A(), ResultCode.GetEnumValueAttribute().Description);
                if (!Properties.ContainsKey(CoreP.APIDocumentationUrl)) AddProperty(CoreP.APIDocumentationUrl.A(), request.API.CreateAPIUrl(request.Method)); // Note how APIDocumentationUrl in some cases may have already been added (typical by AgoRapideGenericMethod when no method found)
            }
            if (ResultCode == ResultCode.exception_error) {
                AddProperty(CoreP.ExceptionDetailsUrl.A(), request.API.CreateAPIUrl(CoreAPIMethod.ExceptionDetails));
            }
        }

        public BaseEntity SingleEntityResult;
        public List<BaseEntity> MultipleEntitiesResult;

        // public override string Name => "Result summary of API call: " + ResultCode;
        public override string IdFriendly => ResultCode.ToString();

        /// <summary>
        /// There are three levels of packaging HTML information:
        /// <see cref="HTMLView.GenerateResult"/>
        ///   <see cref="HTMLView.GetHTMLStart"/>
        ///   <see cref="Result.ToHTMLDetailed"/>
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///     <see cref="Result.ToHTMLDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToHTMLDetailed"/>). 
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///   <see cref="HTMLView.GetHTMLEnd"/>
        ///   
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ClassMember(Description = "Uses the base method -" + nameof(BaseEntity.ToHTMLDetailed) + "- for actual \"packaging\" of information")]
        public override string ToHTMLDetailed(Request request) {
            AdjustAccordingToResultCodeAndMethod(request);
            var retval = new StringBuilder();
            if (SingleEntityResult != null) {
                if (SingleEntityResult is Result) throw new InvalidObjectTypeException(SingleEntityResult, "Would have resulted in recursive call in " + System.Reflection.MethodBase.GetCurrentMethod().Name + " if allowed");
                retval.Append(SingleEntityResult.ToHTMLDetailed(request));
            } else if (MultipleEntitiesResult != null) {
                if (MultipleEntitiesResult.Count == 0) {
                    retval.AppendLine("<p>No entities resulted from your query</p>");
                } else {
                    var types = MultipleEntitiesResult.Select(e => e.GetType()).Distinct().ToList();
                    if (types.Count > 1) retval.AppendLine("<p>" + MultipleEntitiesResult.Count + " entities in total</p>");
                    types.ForEach(t => { /// Split up separate tables for each type because <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/> are not compatible between different types
                        // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                        var thisTypeSorted = MultipleEntitiesResult.Where(e => e.GetType().Equals(t)).OrderBy(e => e.IdFriendly).ToList();
                        retval.AppendLine("<p>" + thisTypeSorted.Count + " entities of type " + t.ToStringShort() + "</p>");

                        var entitiesToShowAsHTML = thisTypeSorted;
                        var max = request.CurrentUser == null ? 1000 : request.CurrentUser.PV<long>(PersonP.ConfigHTMLMaxCount.A(), 1000);
                        if (entitiesToShowAsHTML.Count > max) { // TODO: Create better algoritm here. Draw randomly between 0 and total count, until have 1000 entities. Look out for situation with close to 1000 entities.
                            var originalCount = entitiesToShowAsHTML.Count;
                            // TODO: Google what is most efficient. Sorting when adding as done here (probably not) or sorting afterwards (probably yes)
                            var dict = new SortedDictionary<string, BaseEntity>();
                            var r = new Random((int)(DateTime.Now.Ticks % int.MaxValue));
                            var iteration = 0;
                            while (dict.Count < max) {
                                var i = r.Next(thisTypeSorted.Count);
                                if (dict.ContainsKey(thisTypeSorted[i].IdFriendly)) continue;
                                dict.Add(thisTypeSorted[i].IdFriendly, thisTypeSorted[i]);
                                if ((iteration++) > (max * 2)) break; // Give up, there are too many collisions. Most probably max is quite close to actual count, meaning it is "difficult" to draw new random entities for each iteration.
                            }
                            // TODO: Google what is most efficient. Sorting when adding as done here (probably not) or sorting afterwards (probably yes)
                            entitiesToShowAsHTML = dict.Values.ToList(); // dict.Values.OrderBy(e => e.IdFriendly).ToList();

                            // Old approach before 5 Oct 2017. Not optimal because would leave out entities at the end.
                            //var step = (thisTypeSorted.Count / max) * 2;
                            //var i = 0; var lastI = 0; while (i < thisTypeSorted.Count && entitiesToShowAsHTML.Count < max) {
                            //    entitiesToShowAsHTML.Add(thisTypeSorted[i]);
                            //    lastI = i;
                            //    i += r.Next((int)step) + 1;
                            //}
                            retval.AppendLine("<p " +
                                "style=\"color:red\"" +  // It is very important to emphasize this
                                ">" + "NOTE: Limited selection shown.".HTMLEncloseWithinTooltip(
                                    "Too many entities for HTML-view (" + originalCount + "), " +
                                    "showing approximately " + max + " entities (" + entitiesToShowAsHTML.Count + "), randomly chosen.\r\n" +
                                    (request.CurrentUser == null ? "" : ("(the value of " + max + " may be changed through property -" + nameof(PersonP.ConfigHTMLMaxCount) + "- for " + request.CurrentUser.IdFriendly + ".)\r\n")) +
                                    "Any sorting directly on HTML-page will only sort within limited selection, not from total result.\r\n" +
                                    "Drill down suggestions and CSV / JSON are based on complete dataset though.") +
                                "</p>");
                        }
                        var tableId = t.ToStringVeryShort();
                        retval.Append("<table id=\"sorttable" + tableId + "\">\r\n"); // Unsure if multiple tables are supported this way?                                        
                        retval.AppendLine(entitiesToShowAsHTML[0].ToHTMLTableRowHeading(request));
                        retval.AppendLine("<tbody>");
                        retval.AppendLine(string.Join("", entitiesToShowAsHTML.Select(e => e.ToHTMLTableRow(request))));
                        retval.AppendLine("</tbody>");
                        retval.AppendLine("</table>");
                        retval.Append("<script>new Tablesort(document.getElementById('sorttable" + tableId + "'));</script>\r\n");
                        // retval.Append("<script>new Tablesort(document.getElementById('\"sort_" + tableId + "\"'));</script>\r\n");                                                
                        /// Note somewhat similar code in <see cref="Result.ToHTMLDetailed"/> and <see cref="BaseController.HandleCoreMethodContext"/> for presenting drill-down URLs
                        /// TOOD: Consider using <see cref="GeneralQueryResult"/> in order to communicate drill down URLs
                        /// TODO: Add CreateDrillDownUrls also to <see cref="ToJSONDetailed"/> (in addition to <see cref="ToHTMLDetailed"/>)
                        CreateDrillDownUrls(t, thisTypeSorted).OrderBy(k => k.Key.A().Key.PToString).ForEach(e => { // k => k.Key.A().Key.PToString is somewhat inefficient                                                        
                            var key = e.Key.A();
                            retval.Append("<p><b>" + key.Key.PToString.HTMLEncloseWithinTooltip(key.Key.A.Description) + "</b>: ");
                            e.Value.ForEach(_operator => {
                                // Note how ordering by negative value should be more efficient then ordering and then reversing
                                // _operator.Value.OrderBy(s => s.Value.Count).Reverse().ForEach(suggestion => {
                                _operator.Value.OrderBy(s => -s.Value.Count).ForEach(suggestion => {
                                    if (request.CurrentUser == null) {
                                        // Only suggest general query
                                        retval.Append("<a href=\"" + suggestion.Value.Url + "\">" + suggestion.Value.Text.HTMLEncode() + "<a>&nbsp;");
                                    } else {
                                        // Suggest both 
                                        // 1) adding to context
                                        new List<SetOperator> { SetOperator.Intersect, SetOperator.Remove, SetOperator.Union }.ForEach(s => /// Note how <see cref="SetOperator.Union"/> is a bit weird. It will only have effect if some context properties are later removed (see suggestions below).
                                            retval.Append("&nbsp;" + request.API.CreateAPILink(
                                                 CoreAPIMethod.UpdateProperty,
                                                 s == SetOperator.Intersect ? suggestion.Value.Text : s.ToString().Substring(0, 1),
                                                 request.CurrentUser.GetType(),
                                                 new QueryIdInteger(request.CurrentUser.Id),
                                                 CoreP.Context.A(),
                                                 new Context(s, t, suggestion.Value.QueryId).ToString()
                                             ))
                                        );
                                        // and 
                                        // 2) Showing all with this value (general query)
                                        retval.Append("&nbsp;<a href=\"" + suggestion.Value.Url + "\">(All)<a>&nbsp;");
                                    }
                                });
                            });
                            retval.Append("</p>");
                        });
                    });
                }
            } else if (ResultCode == ResultCode.ok) {
                /// Do not bother with explaining. 
                /// Our base method <see cref="BaseEntity.ToHTMLDetailed"/> will return the actual result (see below).
            } else {
                retval.AppendLine("<p>No result from your query</p>");
                /// Our base method <see cref="BaseEntity.ToHTMLDetailed"/> will return details needed (see below).
            }

            /// Note how <see cref="BaseEntity.ToHTMLDetailed"/> contains special code for <see cref="Result"/> hiding type and name
            return base.ToHTMLDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
        }

        /// <summary>
        /// There are three levels of packaging CSV information.
        /// <see cref="CSVView.GenerateResult"/>
        ///   <see cref="CSVView.GetCSVStart"/>
        ///   <see cref="Result.ToCSVDetailed"/>
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///     <see cref="Result.ToCSVDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToCSVDetailed"/>). 
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///   <see cref="CSVView.GetCSVEnd"/>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ClassMember(Description = "Uses the base method -" + nameof(BaseEntity.ToCSVDetailed) + "- for actual \"packaging\" of information")]
        public override string ToCSVDetailed(Request request) {
            AdjustAccordingToResultCodeAndMethod(request);
            var retval = new StringBuilder();
            if (SingleEntityResult != null) {
                if (SingleEntityResult is Result) throw new InvalidObjectTypeException(SingleEntityResult, "Would have resulted in recursive call in " + System.Reflection.MethodBase.GetCurrentMethod().Name + " if allowed");
                retval.Append(SingleEntityResult.ToCSVDetailed(request));
            } else if (MultipleEntitiesResult != null) {
                if (MultipleEntitiesResult.Count == 0) {
                    retval.AppendLine("No entities resulted from your query");
                } else {

                    if (request.URL.ToString().Contains("/CurrentContext/") && request.CurrentUser != null) { // URL as shown in header is not sufficient to explain where data comes from.
                        retval.AppendLine();
                        request.CurrentUser.PV<List<Context>>(CoreP.Context.A()).ForEach(c => retval.AppendLine(c.ToString()));
                    }

                    var types = MultipleEntitiesResult.Select(e => e.GetType()).Distinct().ToList();
                    if (types.Count > 1) retval.AppendLine("Total entities" + request.CSVFieldSeparator + MultipleEntitiesResult.Count);
                    types.ForEach(t => { /// Split up separate tables for each type because <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/> are not compatible between different types
                        // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                        var thisTypeSorted = MultipleEntitiesResult.Where(e => e.GetType().Equals(t)).OrderBy(e => e.IdFriendly).ToList();
                        retval.AppendLine();
                        retval.AppendLine("Entities of type " + t.ToStringVeryShort() + request.CSVFieldSeparator + thisTypeSorted.Count);
                        retval.AppendLine();
                        retval.AppendLine(thisTypeSorted[0].ToCSVTableRowHeading(request));
                        retval.AppendLine(string.Join("", thisTypeSorted.Select(e => e.ToCSVTableRow(request))));
                    });
                }
            } else if (ResultCode == ResultCode.ok) {
                /// Do not bother with explaining. 
                /// Our base method <see cref="BaseEntity.ToCSVDetailed"/> will return the actual result (see below).
            } else {
                retval.AppendLine("<p>No result from your query</p>");
                /// Our base method <see cref="BaseEntity.ToCSVDetailed"/> will return details needed (see below).
            }

            /// Note how <see cref="BaseEntity.ToCSVDetailed"/> contains special code for <see cref="Result"/> hiding type and name
            return base.ToCSVDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
        }

        /// <summary>
        /// Note how <see cref="Result"/> is the only <see cref="BaseEntity"/>-class (as of June 2017) having a method called <see cref="ToJSONDetailed"/>. 
        /// (while all <see cref="BaseEntity"/>-classes implement <see cref="BaseEntity.ToJSONEntity"/>)
        /// 
        /// TODO: Add CreateDrillDownUrls also to <see cref="ToJSONDetailed"/> (in addition to <see cref="ToHTMLDetailed"/>)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public dynamic ToJSONDetailed(Request request) {
            AdjustAccordingToResultCodeAndMethod(request);
            dynamic json = new System.Web.Helpers.DynamicJsonObject(new Dictionary<string, object>());
            if (SingleEntityResult != null) {
                // TODO: This is old "working" code that definitely can be improved upon somehow...
                var encoded = System.Web.Helpers.Json.Encode(SingleEntityResult.ToJSONEntity(request));
                try {
                    json["SingleEntity"] = System.Web.Helpers.Json.Decode(encoded);
                } catch (ArgumentException ex) {
                    throw new JsonDecodeArgumentException(new List<BaseEntity> { SingleEntityResult }, ex);
                }
            } else if (MultipleEntitiesResult != null) {
                // TODO: This is old "working" code that definitely can be improved upon somehow...
                var retvalList = new List<dynamic>();
                MultipleEntitiesResult.ForEach(e => retvalList.Add(e.ToJSONEntity(request)));

                /// TODO: Add CreateDrillDownUrls also to <see cref="ToJSONDetailed"/> (in addition to <see cref="ToHTMLDetailed"/>)
                /// 
                // TODO: This is old "working" code that definitely can be improved upon somehow...

                // This does not work. It will create a table into which we are unable to insert nameof(ResultCode) and similar
                // var json = System.Web.Helpers.Json.Decode(System.Web.Helpers.Json.Encode(retval));
                // Instead we must do like this:
                json = new System.Web.Helpers.DynamicJsonObject(new Dictionary<string, object>());

                // TODO: This is old "working" code that definitely can be improved upon somehow...

                // New method, workaround when big results. We can not use json["dapi_array"] = System.Web.Helpers.Json.Decode(System.Web.Helpers.Json.Encode(retvalList));
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 1024 * 1024 * 64 };
                var serialized = serializer.Serialize(retvalList);
                try {
                    json["MultipleEntities"] = serializer.DeserializeObject(serialized);
                } catch (ArgumentException ex) {
                    throw new JsonDecodeArgumentException(MultipleEntitiesResult, ex);
                }
            } else {
                json = new System.Web.Helpers.DynamicJsonObject(new Dictionary<string, object>());
                /// Note how actual result will be returned by <see cref="BaseEntityWithLogAndCount.ToJSONEntity"/> 
            }

            /// Inserting <see cref="ResultCode"/> at "top" of JSON hierarchy makes for easier parsing. 
            json[nameof(ResultCode)] = ResultCode.ToString();

            {
                var encoded = System.Web.Helpers.Json.Encode(ToJSONEntity(request));
                try {
                    json["Details"] = System.Web.Helpers.Json.Decode(encoded);
                } catch (ArgumentException ex) {
                    throw new JsonDecodeArgumentException(new List<BaseEntity> { this }, ex);
                }
            }
            return new System.Web.Mvc.JsonResult { Data = json };
        }

        /// <summary>
        /// Handles problem with case-sensitive .NET dictionary keys being incompatible with case-insensitive JSON
        /// 
        /// TODO: IRRELEVANT AS TOJSONDETAILED IS UNNECESSARY COMPLEX!
        /// TODO: REMOVE THIS METHOD (SEP 2017)
        /// 
        /// </summary>
        public class JsonDecodeArgumentException : ApplicationException {
            public JsonDecodeArgumentException(IEnumerable<BaseEntity> entities, ArgumentException ex) : base(new Func<string>(() => {
                var retval = new StringBuilder();
                entities.ForEach(entity => {
                    entity.Properties.ForEach(p1 => {
                        var lowerCase = p1.Key.ToString().ToLower();
                        var identical = entity.Properties.Where(p2 => p2.Key.ToString().ToLower().Equals(lowerCase)).ToList();
                        if (identical.Count > 1) {
                            retval.Append("\r\n\r\n");
                            retval.Append("For " + entity.GetType() + " " + entity.Id + " (" + entity.IdFriendly + ") the key " + p1.Key + " is not unique in lowerCase (" + lowerCase + "). The following properties share the same key in lowerCase:\r\n");
                            identical.ForEach(i => {
                                retval.Append(i.Key + ": " + i.Value.V<string>() + "\r\n");
                            });
                            retval.Append("\r\n\r\n");
                        }
                    });
                });
                if (retval.Length > 0) { // We have an explanation for the ArgumentException
                                         // TODO: Create better links here. Link to method for set no-longer-current for instance
                                         // TODO: Also create link to HTML version
                    retval.Append("The properties listed above are assumed to result in an " + ex.GetType() + " when attempting to call System.Web.Helpers.Json.Decode\r\n");
                    retval.Append("(the exception will most probably not occur if you ask for HTML-format instead of JSON-format in returned data)\r\n");
                    retval.Append("Possible resolution: Do some " + nameof(PropertyOperation) + "." + nameof(PropertyOperation.SetInvalid) + " so you end up with all identical lower case keys\r\n");
                    return retval.ToString();
                }
                return "Unable to understand why " + ex.GetType() + " occurred";
            })(), ex) { }
        }

        /// <summary>
        /// Extracts all distinct values 
        /// 
        /// Result is <see cref="ConcurrentDictionary{TKey, TValue}"/> instead of <see cref="Dictionary{TKey, TValue}"/> because code 
        /// executes in parallell.
        /// 
        /// TOOD: Consider using <see cref="GeneralQueryResult"/> in order to communicate drill down URLs
        /// </summary>
        /// <param name="entities">Alle objects are required to be of an identical type</param>
        /// <returns></returns>
        public static ConcurrentDictionary<
                CoreP,
                Dictionary<
                    Operator,
                    Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >
                >
             > CreateDrillDownUrls(Type type, IEnumerable<BaseEntity> entities) {

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

            /// Note how this code (in <see cref="Result.CreateDrillDownUrls"/>) only gives suggestions for existing values.
            /// If we want to implement some kind of surveillance (for what-if scenarios) we would also need to know all possible values in advance. 

            //// For some information about parallellism see:
            ///// http://download.microsoft.com/download/B/C/F/BCFD4868-1354-45E3-B71B-B851CD78733D/WhenToUseParallelForEachOrPLINQ.pdf

            // NOTE: Inefficient / not working attempt at parallel execution.
            //type.GetChildProperties().Values.
            //    // AsParallel(). // NOTE: This was a naïve attempt of parallel exection, it has no effect.
            //    ForEach(key => {

            // NOTE: The level of parallelization here is quite coarse.
            // NOTE: If for instance a given type has only one child property that is computationaly expensive, then
            // NOTE: the parallelization here would have no little effect.
            Parallel.ForEach(type.GetChildProperties().Values, key => { // Added use of Parallel.ForEach 3 Nov 2017.

                if (key.Key.A.IsMany) return; /// These are not supported by <see cref="Property.Value"/>

                List<(object, string)> objStrValues;

                var properties = entities.Select(e => e.Properties == null ? null : (e.Properties.TryGetValue(key.Key.CoreP, out var p) ? p : null));
                var propertiesNotNull = properties.Where(p => p != null).ToList(); // Used for Percentile-evaluation

                var percentileDrilldowns = new Dictionary< // How this is inserted into retval is a HACK. TODO: Make prettier
                        string, // The actual values found. 
                        DrillDownSuggestion
                >();

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
                            new DrillDownSuggestion(type, count, APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, query), q + " (" + count + ")", query)
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
                            new DrillDownSuggestion(type, count, APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, query), "Local " + Quintile.Quintile1 + " (" + count + ")", query)
                        );

                        // TODO: Go over algorithm here. May be off by one element!
                        quintileValue = sortedProperties[sortedProperties.Count - oneFifth].Value; /// Calculate value for local <see cref="Quintile.Quintile5"/>
                        // Count as efficient as possible, based on the fact that the list is already sorted.
                        count = oneFifth; while ((sortedProperties.Count - count) >= 0 && sortedProperties[sortedProperties.Count - count].Value == quintileValue) count++;
                        query = new QueryIdKeyOperatorValue(key.Key, Operator.GEQ, quintileValue);
                        // TODO: Find better term than "Local" (confer with "Global above).
                        percentileDrilldowns.Add("Local " + Quintile.Quintile5, // TODO: Works fine until we have a value Local Quintile5 or similar (will give us key-collision below)
                            new DrillDownSuggestion(type, count, APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, query), "Local " + Quintile.Quintile5 + " (" + count + ")", query)
                        );
                    }
                }

                if (key.Key.A.Operators == null) {
                    if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                        retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                    }
                    return;
                }

                var mixOfNullsAndNotNullFound = false; // Found either directly (see direct below) or as "side effect" in lambda (see further below)

                /// TODO: Decide about <see cref="Operator.NEQ"/>. As of Sep 2017 we use <see cref="SetOperator.Remove"/> as substitute
                if (key.Key.A.Operators.Length == 1 && key.Key.A.Operators[0] == Operator.EQ && !key.Key.A.HasLimitedRange) {
                    // Our only choice is limiting to NULL or NOT NULL values.  
                    if (entities.All(e => e.Properties == null || !e.Properties.TryGetValue(key.Key.CoreP, out _))) {
                        if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                            retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                        }
                        return; // None are set.
                    }
                    if (entities.All(e => e.Properties != null && e.Properties.TryGetValue(key.Key.CoreP, out _))) {
                        if (percentileDrilldowns.Count > 0) { // HACK, TODO: MAKE PRETTIER!
                            retval.Add(key.Key.CoreP, new Dictionary<Operator, Dictionary<string, DrillDownSuggestion>> { { Operator.EQ, percentileDrilldowns } });
                        }
                        return; // All are set.
                    }
                    objStrValues = new List<(object, string)> { }; // null, "NULL" will be added below
                    mixOfNullsAndNotNullFound = true;
                } else {

                    // Note how Distinct() is called weakly typed for object, meaning it uses the Equals(object other)-method.
                    var objValues = properties.Select(p => p?.Value ?? null).Distinct();

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
                    if (o == Operator.EQ && !key.Key.A.HasLimitedRange) {
                        /// NOTE: Count > 10 is arbitrarily chosen.
                        /// NOTE: Note that for only <see cref="Operator.EQ"/> this will only happen with Count == 1 because of filtering out above for HasLimitedRange
                        /// NOTE: For <see cref="DateTime"/>, long and similar types that can be compared like <see cref="Operator.LT"/> and similar 
                        /// NOTE: 
                        if (objStrValues.Count > 10) {
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

                            if (objStrValuesForThisOperator.Count == 0) {
                                return; // Give up altogether
                            }
                        }
                    }

                    var r2 = new Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >();
                    objStrValuesForThisOperator.ForEach(t => {
                        var query = new QueryIdKeyOperatorValue(key.Key, o, t.Item1); // Now how it is "random" which object value (out of several with identical string-representation) is chosen now. But we assume that all of them have the same predicate effect
                        var count = entities.Where(query.IsMatch).Count();

                        if (count > 0 && count != totalCount) { // Note how we do not offer drill down if all entities match
                            r2.AddValue(
                                t.Item2,
                                new DrillDownSuggestion(type, count, APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, query), t.Item2 + " (" + count + ")", query)
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

        /// <summary>
        /// TODO: Make immutable
        /// </summary>
        public class DrillDownSuggestion {
            public Type EntityType { get; private set; }
            /// <summary>
            /// Number of entities resulting if querying according to this suggestion
            /// </summary>
            public long Count { get; private set; }
            public Uri Url { get; private set; }
            public string Text { get; private set; }
            public QueryIdKeyOperatorValue QueryId { get; private set; }

            public DrillDownSuggestion(Type entityType, long count, Uri url, string text, QueryIdKeyOperatorValue queryId) {
                EntityType = entityType;
                Count = count;
                Url = url;
                Text = text;
                QueryId = queryId;
            }

            /// <summary>
            /// TODO: Find a better name for this method / property
            /// </summary>
            /// <param name="header"></param>
            /// <returns></returns>
            public GeneralQueryResult ToQuery(string header) => new GeneralQueryResult(Url, header + ": " + Text);
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
            public GeneralQueryResult ToContext(SetOperator setOperator, ValidRequest request, string header, bool useOnlyHeader) => new GeneralQueryResult(
                request.API.CreateAPIUrl(
                    CoreAPIMethod.UpdateProperty,
                    request.CurrentUser.GetType(),
                    new QueryIdInteger(request.CurrentUser.Id),
                    CoreP.Context.A(),
                    new Context(setOperator, EntityType, QueryId).ToString()
                ),
                useOnlyHeader ? header : (header + ": " + Text));
        }
    }

    [EnumAttribute(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum ResultP {
        None,

        [PropertyKey(Type = typeof(ResultCode), Parents = new Type[] { typeof(Result) })]
        ResultCode,

        /// <summary>
        /// The <see cref="PropertyKeyAttribute.Description"/>-attribute of <see cref="ResultCode"/>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [PropertyKey(Type = typeof(string), Parents = new Type[] { typeof(Result) })]
        ResultCodeDescription,
    }

    public static class ResultPExtensions {
        public static PropertyKey A(this ResultP p) => PropertyKeyMapper.GetA(p);
    }
}