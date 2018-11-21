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
                                if ((iteration++) > (max * 3)) break; // Give up, there are too many collisions. Most probably max is quite close to actual count, meaning it is "difficult" to draw new random entities for each iteration.
                                var i = r.Next(thisTypeSorted.Count);
                                var key = thisTypeSorted[i].Id > 0 ? thisTypeSorted[i].Id.ToString() : thisTypeSorted[i].IdFriendly; // Use Id if exists (unique database id), if not use IdFriendly (IdFriendly is not good enough in itself)
                                if (dict.ContainsKey(key)) continue;
                                dict.Add(key, thisTypeSorted[i]);
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
                        retval.Append("<table id=\"sorttable" + tableId + "\">\r\n"); // TOOD: Verify that multiple tables really are supported this way?                                        


                        var aggregationTypes = Util.EnumGetValues<AggregationType>();
                        var heading = thisTypeSorted[0].ToHTMLTableRowHeading(request, aggregationTypes);
                        // Insert aggregates as relevant
                        aggregationTypes.ForEach(a => {
                            thisTypeSorted[0].ToHTMLTableColumns(request).ForEach(key => {
                                if (key.Key.A.AggregationTypes.Contains(a)) {
                                    heading = heading.Replace("<!--" + key.Key.PToString + "_" + a + "-->", new Func<string>(() => {
                                        var aggregate = PropertyKeyAggregate.CalculateSingleValue(a, key, thisTypeSorted);
                                        if (aggregate == null) return "&nbsp;";
                                        return aggregate.ToString() + (a == AggregationType.Percent ? "%" : "");
                                    })());
                                }
                            });
                        });

                        /// Note somewhat similar code in <see cref="Result.ToHTMLDetailed"/> and <see cref="BaseController.HandleCoreMethodContext"/> for presenting drill-down URLs
                        /// TOOD: Consider using <see cref="GeneralQueryResult"/> in order to communicate drill down URLs
                        /// TODO: Add CreateDrillDownUrls also to <see cref="ToJSONDetailed"/> (in addition to <see cref="ToHTMLDetailed"/>)
                        var drillDownUrls = DrillDownSuggestion.Create(t, thisTypeSorted);
                        /// TODO: Structure of result from <see cref="DrillDownSuggestion.Create"/> is too complicated. 

                        // Insert Context information as relevant
                        // if (request.CurrentUser != null && request.CurrentUser.TryGetPV<List<Context>>(CoreP.Context.A(), out var contexts)) {
                        if (request.CurrentUser != null && request.CurrentUser.Properties.TryGetValue(CoreP.Context, out var context)) {
                            var contexts = context.Properties.Values.Select(p => (Id: p.Id, Context: p.V<Context>()));
                            var contextsKeyOperatorValue = contexts.Where(c => c.Context.QueryId is QueryIdKeyOperatorValue);
                            thisTypeSorted[0].ToHTMLTableColumns(request).ForEach(key => {
                                var replacementThisKey = new StringBuilder();

                                var part = new StringBuilder();
                                // ===================================
                                // Context part 1, suggest removal of existing contexts
                                // ===================================
                                var contextsThisKey = contextsKeyOperatorValue.Where(c => ((QueryIdKeyOperatorValue)c.Context.QueryId).Key.PToString.Equals(key.Key.PToString)).ToList();
                                if (contextsThisKey.Count > 0) { // Add links for removing these contexts.
                                    part.Append(string.Join("<br>", contextsThisKey.Select(c => {
                                        var queryId = (QueryIdKeyOperatorValue)c.Context.QueryId;
                                        // TODO: Fix compile error here!
                                        return request.API.CreateAPILink(request.API.CreateAPICommand(CoreAPIMethod.PropertyOperation, typeof(Property), new QueryIdInteger(c.Id), PropertyOperation.SetInvalid), c.Context.SetOperator + " " + queryId.Operator + " " + queryId.Value + " => " + PropertyOperation.SetInvalid);
                                    })));
                                }
                                if (part.Length > 0) replacementThisKey.Append(part.ToString());

                                part = new StringBuilder();
                                // ===================================
                                // Context part 2, suggest new contexts
                                // ===================================
                                var foundNonQuintileSuggestion = false;
                                if (drillDownUrls.TryGetValue(key.Key.CoreP, out var operators)) {
                                    // NOTE: Note that code here is almost a dupliate of code below
                                    operators.ForEach(_operator => {
                                        // Note how ordering by negative value should be more efficient then ordering and then reversing
                                        // _operator.Value.OrderBy(s => s.Value.Count).Reverse().ForEach(suggestion => {
                                        var ordered = _operator.Value.OrderBy(s => -s.Value.Count);
                                        new List<string> { "", "Local Quintile", "Quintile" }.ForEach(prefix => {
                                            ordered.ForEach(suggestion => {
                                                if (string.IsNullOrEmpty(prefix)) {
                                                    if (suggestion.Value.Text.StartsWith("Local Quintile") || suggestion.Value.Text.StartsWith("Quintile")) return;
                                                    foundNonQuintileSuggestion = true;
                                                } else {
                                                    if (!suggestion.Value.Text.StartsWith(prefix)) return;
                                                }
                                                part.Append("<br><br>");
                                                // Suggest both 
                                                // 1) adding to context
                                                new List<SetOperator> { SetOperator.Intersect, SetOperator.Remove, SetOperator.Union }.ForEach(s => /// Note how <see cref="SetOperator.Union"/> is a bit weird. It will only have effect if some context properties are later removed (see suggestions below).
                                                part.Append("&nbsp;" + request.API.CreateAPILink(
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
                                                part.Append("&nbsp;<a href=\"" + suggestion.Value.ToQueryUrl + "\">(All)<a>");
                                            });
                                        });
                                    });
                                    drillDownUrls.Remove(key.Key.CoreP); // Removed in order to see any left overs (we may have drill-down for fields that are not shown)
                                }
                                // TODO: Add span with click for visible / invisible here
                                if (part.Length > 0) replacementThisKey.Append("D".HTMLEncloseWithInVisibilityToggle(part.ToString()));

                                part = new StringBuilder();
                                // ===================================
                                /// Context part 3, suggest iterations (<see cref="QueryIdFieldIterator"/>, resulting in a <see cref="FieldIterator"/>-result)
                                // ===================================
                                if (!foundNonQuintileSuggestion) {
                                    /// (NOTE: This part is only suggested if there where drill-down suggestions from last part)
                                } else {
                                    var contextTypes =
                                        request.TypesInvolvedInCurrentContext ??  // Preferred value. TODO: Consider throwing exception if not found TODO: Value not implemented as of 23 Nov 2017.
                                        contexts.Select(c => c.Context.Type).Distinct().ToList(); /// Not preferred value. We now only know about types already in use in context, not those found by <see cref="Context.TraverseToAllEntities"/>. 
                                    types.ForEach(columnType => {
                                        columnType.GetChildProperties().Values.ForEach(columnKey => {
                                            if (columnType.Equals(t) && columnKey.Key.PToString.Equals(key.Key.PToString)) return; // Do not iterate on key itself.
                                            part.Append("<br>" + APICommandCreator.HTMLInstance.CreateAPILink(
                                                CoreAPIMethod.EntityIndex, (columnType.Equals(t) ? "" : (columnType.ToStringVeryShort() + ".")) + columnKey.Key.PToString, t,
                                                new QueryIdFieldIterator(key, columnType, columnKey, AggregationType.Count, aggregationKey: null)));
                                        });
                                    });
                                }
                                // TODO: Add span with click for visible / invisible here
                                if (part.Length > 0) replacementThisKey.Append("RC".HTMLEncloseWithInVisibilityToggle(part.ToString()));

                                if (replacementThisKey.Length > 0) heading = heading.Replace("<!--" + key.Key.PToString + "_Context-->", replacementThisKey.ToString());
                            });
                        }

                        retval.AppendLine(heading);

                        retval.AppendLine("<tbody>");
                        retval.AppendLine(string.Join("", entitiesToShowAsHTML.Select(e => e.ToHTMLTableRow(request))));
                        retval.AppendLine("</tbody>");
                        retval.AppendLine("</table>");
                        retval.Append("<script>new Tablesort(document.getElementById('sorttable" + tableId + "'));</script>\r\n");
                        // retval.Append("<script>new Tablesort(document.getElementById('\"sort_" + tableId + "\"'));</script>\r\n");                                                

                        // Add any remaining suggestions (we may have drill-down for fields that are not shown, and therefore not taken in code above)
                        // TODO: Include aggregations here.

                        // NOTE: Note that code below is almost a dupliate of code above
                        drillDownUrls.OrderBy(k => k.Key.A().Key.PToString).ForEach(e => { // k => k.Key.A().Key.PToString is somewhat inefficient                                                        

                            // TODO: CONSIDER FACTORING OUT COMMON ELEMENTS IN DUPLIATE CODE HERE AND ABOVE
                            // TODO: Note how aggregations are not included here.
                            var key = e.Key.A();
                            retval.Append("<p><b>" + key.Key.PToString.HTMLEncloseWithinTooltip(key.Key.A.WholeDescription) + "</b>: ");
                            e.Value.ForEach(_operator => {
                                // Note how ordering by negative value should be more efficient then ordering and then reversing
                                // _operator.Value.OrderBy(s => s.Value.Count).Reverse().ForEach(suggestion => {
                                _operator.Value.OrderBy(s => -s.Value.Count).ForEach(suggestion => {
                                    if (request.CurrentUser == null) {
                                        // Only suggest general query
                                        retval.Append("<a href=\"" + suggestion.Value.ToQueryUrl + "\">" + suggestion.Value.Text.HTMLEncode() + "<a>&nbsp;");
                                    } else {
                                        // TODO: CONSIDER FACTORING OUT COMMON ELEMENTS IN DUPLIATE CODE HERE AND ABOVE
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
                                        retval.Append("&nbsp;<a href=\"" + suggestion.Value.ToQueryUrl + "\">(All)<a>&nbsp;");
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
        /// There are three levels of packaging PDF information.
        /// <see cref="PDFView.GenerateResult"/>
        ///   <see cref="PDFView.GetPDFStart"/>
        ///   <see cref="Result.ToPDFDetailed"/>
        ///     <see cref="BaseEntity.ToPDFDetailed"/> (called from <see cref="Result.ToPDFDetailed"/>)
        ///     <see cref="Result.ToPDFDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToPDFDetailed"/>). 
        ///     <see cref="BaseEntity.ToPDFDetailed"/> (called from <see cref="Result.ToPDFDetailed"/>)
        ///   <see cref="PDFView.GetPDFEnd"/>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ClassMember(Description = "Uses the base method -" + nameof(BaseEntity.ToPDFDetailed) + "- for actual \"packaging\" of information")]
        public override string ToPDFDetailed(Request request) {
            AdjustAccordingToResultCodeAndMethod(request);
            var retval = new StringBuilder();
            if (SingleEntityResult != null) {
                if (SingleEntityResult is Result) throw new InvalidObjectTypeException(SingleEntityResult, "Would have resulted in recursive call in " + System.Reflection.MethodBase.GetCurrentMethod().Name + " if allowed");
                retval.Append(SingleEntityResult.ToPDFDetailed(request).Replace("<!--DELIMITER-->",""));
            } else if (MultipleEntitiesResult != null) {
                if (MultipleEntitiesResult.Count == 0) {
                    retval.AppendLine("No entities resulted from your query");
                } else {

                    if (request.URL.ToString().Contains("/CurrentContext/") && request.CurrentUser != null) { // URL as shown in header is not sufficient to explain where data comes from.
                        retval.AppendLine();
                        request.CurrentUser.PV<List<Context>>(CoreP.Context.A()).ForEach(c => retval.AppendLine(c.ToString()));
                    }

                    var types = MultipleEntitiesResult.Select(e => e.GetType()).Distinct().ToList();
                    if (types.Count > 1) retval.AppendLine("Total entities" + request.PDFFieldSeparator + MultipleEntitiesResult.Count);
                    types.ForEach(t => { /// Split up separate tables for each type because <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/> are not compatible between different types
                        // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                        var thisTypeSorted = MultipleEntitiesResult.Where(e => e.GetType().Equals(t)).OrderBy(e => e.IdFriendly).ToList();
                        retval.AppendLine();
                        retval.AppendLine("Entities of type " + t.ToStringVeryShort() + request.PDFFieldSeparator + thisTypeSorted.Count);
                        retval.AppendLine();
                        /// Note how PDF views are always supposed to be a detailed view
                        /// (we are not really using <see cref="BaseEntity.ToPDFTableRowHeading"/> or <see cref="BaseEntity.ToPDFTableRow"/>
                        /// In other words, the following is not relevant:
                        //retval.AppendLine(thisTypeSorted[0].ToPDFTableRowHeading(request));
                        //retval.AppendLine(string.Join("", thisTypeSorted.Select(e => e.ToPDFTableRow(request))));

                        retval.AppendLine(string.Join("\r\n---------\r\n", thisTypeSorted.Select(e => e.ToPDFDetailed(request).Replace("<!--DELIMITER-->", ""))));
                    });
                }
            } else if (ResultCode == ResultCode.ok) {
                /// Do not bother with explaining. 
                /// Our base method <see cref="BaseEntity.ToPDFDetailed"/> will return the actual result (see below).
            } else {
                retval.AppendLine("\r\nNo result from your query\r\n");
                /// Our base method <see cref="BaseEntity.ToPDFDetailed"/> will return details needed (see below).
            }
            // TODO: Push all necessary TeX encodings like this down to the lowest possible level (where the TeX-output is actually generated)
            retval.Replace("_", "\\_").Replace("&","\\&").Replace("#","\\#");

            // TOOD: Remove this. Usually bug in non-related class related to scraping of HTML-pages
            retval.Replace("Ã¥", "å");

            /// Note how <see cref="BaseEntity.ToPDFDetailed"/> contains special code for <see cref="Result"/> hiding type and name
            return base.ToPDFDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
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
                retval.AppendLine("\r\nNo result from your query\r\n");
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