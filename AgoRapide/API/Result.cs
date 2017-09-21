// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
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
                        retval.AppendLine("<table>");
                        retval.AppendLine(thisTypeSorted[0].ToHTMLTableRowHeading(request));
                        retval.AppendLine(string.Join("", thisTypeSorted.Select(e => e.ToHTMLTableRow(request))));
                        retval.AppendLine("</table>");

                        /// Note somewhat similar code in <see cref="Result.ToHTMLDetailed"/> and <see cref="BaseController.HandleCoreMethodContext"/> for presenting drill-down URLs
                        /// TOOD: Consider using <see cref="GeneralQueryResult"/> in order to communicate drill down URLs
                        CreateDrillDownUrls(thisTypeSorted).OrderBy(k => k.Key.A().Key.PToString).ForEach(e => { // k => k.Key.A().Key.PToString is somewhat inefficient                                                        
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
                                        )));
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
        /// TOOD: Consider using <see cref="GeneralQueryResult"/> in order to communicate drill down URLs
        /// </summary>
        /// <param name="entities">Alle objects are required to be of an identical type</param>
        /// <returns></returns>
        public static Dictionary<
                CoreP,
                Dictionary<
                    Operator,
                    Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >
                >
             > CreateDrillDownUrls(IEnumerable<BaseEntity> entities) {

            var retval = new Dictionary<
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

            var type = entities.First().GetType();
            /// Note how this code (in <see cref="Result.CreateDrillDownUrls"/>) only gives suggestions for existing values.
            /// If we want to implement some kind of surveillance (for what-if scenarios) we would also need to know all possible values in advance. 
            type.GetChildProperties().Values.ForEach(key => { // Note how only properties explicitly defined for this type of entity are considered. 
                if (key.Key.A.IsMany) return; /// These are not supported by <see cref="Property.Value"/>

                if (key.Key.A.Operators == null) return;
                if (key.Key.A.Operators.Length == 1 && key.Key.A.Operators[0] == Operator.EQ && !key.Key.A.HasLimitedRange) return;

                // Note how Distinct() is called weakly typed for object, meaning it uses the Equals(object other)-method.
                var objValues = entities.Select(e => e.Properties == null ? null : (e.Properties.TryGetValue(key.Key.CoreP, out var p) ? p.Value : null)).Distinct(); /// TODO: Add support in <see cref="QueryIdKeyOperatorValue"/> for value null.

                /// Note that the Distinct() operation done above will not work properly of IEquatable is not implemented for the actual type.
                /// We therefore work around this by collecting together all object-values with the same string-representation
                /// (Since the resulting API command is string-based this should not present any issues)
                var objStrValues = objValues.Where(v => v != null).Select(v => (v, key.Key.ConvertObjectToString(v))).Distinct(new EqualityComparerTupleObjectString());

                if (typeof(ITypeDescriber).IsAssignableFrom(key.Key.A.Type)) { /// Enforce IEquatable or similar for these classes anyway as it will improve performance (less calls to <see cref="PropertyKeyAttributeEnriched.ConvertObjectToString"/>)
                    if (objStrValues.Count() != objValues.Count()) {
                        var t = typeof(IEquatable<>).MakeGenericType(new Type[] { key.Key.A.Type });
                        throw new InvalidCountException(objStrValues.Count(), objValues.Count(),
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
                    if (o == Operator.EQ && !key.Key.A.HasLimitedRange) return;

                    var r2 = new Dictionary<
                        string, // The actual values found. 
                        DrillDownSuggestion
                    >();
                    objStrValues.ForEach(t => {
                        var query = new QueryIdKeyOperatorValue(key.Key, o, t.Item1); // Now how it is "random" which object value (out of several with identical string-representation) is chosen now. But we assume that all of them have the same predicate effect
                        var count = entities.Where(query.IsMatch).Count();

                        if (count > 0 && count != totalCount) { // Note how we do not offer drill down if all entities match
                            r2.AddValue(
                                t.Item2,
                                new DrillDownSuggestion { // TODO: Implement constructor forcing parameters here
                                    EntityType = type,
                                    Count = count,
                                    Url = APICommandCreator.HTMLInstance.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, query),
                                    Text = t.Item2 + " (" + count + ")",
                                    QueryId = query
                                }
                           );
                        }
                    });
                    if (r2.Count > 0) r1.AddValue(o, r2);
                });
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
            public Type EntityType; // TODO: Implement constructor forcing parameters here
            /// <summary>
            /// Number of entities resulting if querying according to this suggestion
            /// </summary>
            public long Count;
            public Uri Url;
            public string Text;
            public QueryIdKeyOperatorValue QueryId;

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