using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// TODO: MOVE INTO API-FOLDER!
    /// 
    /// Never stored in database. 
    /// 
    /// Note how many of the methods in the AgoRapide-library will log 
    /// extensively to <see cref="BaseEntityWithLogAndCount.LogInternal"/> in <see cref="Result"/>.
    /// (meaning your server logs are not filled up with unnecessary clutter, but the system is still available to give an API client
    /// detailed information about problems). 
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
                AddProperty(CoreP.ResultCodeDescription.A(), ResultCode.GetEnumValueAttribute().Description);
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

        public override string ToHTMLDetailed(Request request) {
            AdjustAccordingToResultCodeAndMethod(request);
            var retval = new StringBuilder();
            if (SingleEntityResult != null) {
                if (SingleEntityResult is Result) throw new InvalidObjectTypeException(SingleEntityResult, "Would have resulted in recursive call in " + System.Reflection.MethodBase.GetCurrentMethod().Name + " if allowed");
                // retval.Append("<p>Single entity</p>");
                retval.Append(SingleEntityResult.ToHTMLDetailed(request));
            } else if (MultipleEntitiesResult != null) {
                if (MultipleEntitiesResult.Count == 0) {
                    retval.AppendLine("<p>No entities resulted from your query</p>");
                } else {
                    var types = MultipleEntitiesResult.Select(e => e.GetType()).Distinct().ToList();
                    if (types.Count > 1) retval.AppendLine("<p>" + MultipleEntitiesResult.Count + " entities in total</p>"); // of type " + MultipleEntitiesResult.First().GetType().ToStringShort() + "</p>");
                    types.ForEach(t => { /// Split up separate tables for each type because <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/> are not compatible between different types
                        // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                        var thisTypeSorted = MultipleEntitiesResult.Where(e => e.GetType().Equals(t)).OrderBy(e => e.IdFriendly).ToList();
                        retval.AppendLine("<p>" + thisTypeSorted.Count + " entities of type " + t.ToStringShort() + "</p>");
                        retval.AppendLine("<table>");
                        retval.AppendLine(thisTypeSorted[0].ToHTMLTableRowHeading(request));
                        retval.AppendLine(string.Join("", thisTypeSorted.Select(e => e.ToHTMLTableRow(request))));
                        retval.AppendLine("</table>");
                    });
                }
            } else if (ResultCode == ResultCode.ok) {
                // Do not bother with explaining. 
                // ToHTMLDetailed will return the actual result
            } else {
                retval.AppendLine("<p>No result from your query</p>");
                // ToHTMLDetailed will return details needed. 
            }

            /// Note how <see cref="BaseEntity.ToHTMLDetailed"/> contains special code for <see cref="Result"/> hiding type and name
            return base.ToHTMLDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
        }

        /// <summary>
        /// Note how <see cref="Result"/> is the only <see cref="BaseEntity"/>-class having a method called <see cref="ToJSONDetailed"/>. 
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

                // This does not work. It will create a table into which we are unable to insert nameof(ResultCode) and similar
                // var json = System.Web.Helpers.Json.Decode(System.Web.Helpers.Json.Encode(retval));
                // Instead we must do like this:
                json = new System.Web.Helpers.DynamicJsonObject(new Dictionary<string, object>());

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
                    json["details"] = System.Web.Helpers.Json.Decode(encoded);
                } catch (ArgumentException ex) {
                    throw new JsonDecodeArgumentException(new List<BaseEntity> { this }, ex);
                }
            }
            return new System.Web.Mvc.JsonResult { Data = json };
        }

        /// <summary>
        /// Handles problem with case-sensitive .NET dictionary keys being incompatible with case-insensitive JSON 
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
                    retval.Append("Possible resolution: Set some properties no-longer-current so you end up with all identical lower case keys\r\n");
                    return retval.ToString();
                }
                return "Unable to understand why " + ex.GetType() + " occurred";
            })(), ex) { }
        }

        public void Include(Result other) {
            if (other.ResultCode > ResultCode) ResultCode = other.ResultCode;
            other.Counts.ForEach(otherCount => SetCount(otherCount.Key, Counts.TryGetValue(otherCount.Key, out var myValue) ? myValue + otherCount.Value : otherCount.Value));
            if (other.LogData.Length > 0) LogData.Append(other.LogData);
        }
    }
}
