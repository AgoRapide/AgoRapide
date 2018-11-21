// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide;

namespace AgoRapide.API {

    /// <summary>
    /// See also sub-class <see cref="ValidRequest"/>
    /// </summary>
    [Class(
        Description =
            "Container class with information about a given REST API request.\r\n" +
            "Super class -" + nameof(Request) + "- is a minimal version used when request from client was not fully understood " +
            "(invalid parameters for instance or because some exceptions occurred at parsing of request).\r\n" +
            "Sub class -" + nameof(ValidRequest) + "- is the more exact version with actual validated -" + nameof(ValidRequest.Parameters) + "-"
    )]
    public class Request {

        public System.Net.Http.HttpRequestMessage HttpRequestMessage { get; private set; }
        public APIMethod Method { get; private set; }

        /// <summary>
        /// May be null (for instance for anonymous requests)
        /// </summary>
        public BaseEntity CurrentUser { get; set; }

        /// <summary>
        /// HACK: Try to find a better way to initialize this value, and a better place to store this value. 
        /// HACK: Now it is set by <see cref="BaseController.HandleCoreMethodEntityIndex"/>
        /// </summary>
        [ClassMember(
            Description =
                "Used to adjust detail of information returned by -" + nameof(BaseEntity.ToHTMLTableColumns) + "-, -" + nameof(BaseEntity.ToHTMLTableRowHeading) + "- and -" + nameof(BaseEntity.ToHTMLTableRow) + "-.\r\n" +
                "(ignored by -" + nameof(BaseEntity.ToCSVTableColumns) + "-, -" + nameof(BaseEntity.ToCSVTableRowHeading) + "- and -" + nameof(BaseEntity.ToCSVTableRow) + "-.)",
            LongDescription =
                "Only relevant for -" + nameof(CoreAPIMethod.EntityIndex) + "- when distinguishing between browsing like\r\n" +
                "   api/Person/CurrentContext/HTML (see -" + nameof(QueryIdContext) + "-)\r\n" +
                "and\r\n" +
                "   api/Person/WHERE department = 'Manufacturing'\r\n" +
                "The former is considered a report request where detailed information is desired while " +
                "the latter is considered more like a search query where only -" + nameof(PriorityOrder.Important) + "-information is needed."
        )]
        public PriorityOrder PriorityOrderLimit { get; set; } = PriorityOrder.Important;

        /// <summary>
        /// HACK: 
        /// </summary>
        [ClassMember(Description =
            "Communicates the different types involved in a current context. " +
            "Found by -" + nameof(Context.TryExecuteContextsQueries) + "-). " +
            "Used by -" + nameof(Result.ToHTMLDetailed) + "-.")]
        public List<Type> TypesInvolvedInCurrentContext { get; set; }

        /// <summary>   
        /// TODO: This is just a preparation for a more dynamic approach.
        /// TODO: Move into <see cref="CSVView"/>?
        /// 
        /// TODO: Implement this through a setting, either in <see cref="Configuration"/> or for <see cref="CurrentUser"/>
        /// </summary>
        public string CSVFieldSeparator = ";";

        /// <summary>   
        /// TODO: This is just a preparation for a more dynamic approach.
        /// TODO: Move into <see cref="PDFView"/>?
        /// 
        /// TODO: Implement this through a setting, either in <see cref="Configuration"/> or for <see cref="CurrentUser"/>
        /// </summary>
        public string PDFFieldSeparator = ": ";

        public Result Result { get; } = new Result();

        /// <summary>
        /// Initializes minimum version of Request. Will be marked <see cref="Request.IsIncomplete"/>. 
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="method"></param>
        /// <param name="currentUser">May be null</param>
        /// <param name="exceptionHasOccurred">
        /// <see cref="ExceptionHasOccurred"/>
        /// </param>
        public Request(System.Net.Http.HttpRequestMessage httpRequestMessage, APIMethod method, BaseEntity currentUser, bool exceptionHasOccurred) {
            HttpRequestMessage = httpRequestMessage ?? throw new ArgumentNullException(nameof(httpRequestMessage));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            CurrentUser = currentUser;
            ExceptionHasOccurred = exceptionHasOccurred;

            URL = httpRequestMessage.RequestUri;
            ResponseFormat = GetResponseFormatFromURL(URL);

            if (ExceptionHasOccurred) {
                // Do not bother which check below. Will most probably fail anyway.
            } else {
                if (Method != null && Method.RequiresAuthorization != (currentUser != null)) throw new InvalidRequestInitializationException(nameof(Method) + "." + nameof(Method.RequiresAuthorization) + " (" + Method.RequiresAuthorization + ") != (" + nameof(currentUser) + " != null) (" + (currentUser != null) + "). " + nameof(httpRequestMessage) + ": " + httpRequestMessage.RequestUri.ToString() + ", " + nameof(method) + ": " + method.ToString());
            }
        }

        public object GetOKResponseAsEntityId(Type entityType, long id) => GetOKResponseAsEntityId(entityType, id, message: null);
        /// <summary>
        /// </summary>
        /// <param name="entityType">Necessary for creating <see cref="CoreP.SuggestedUrl"/></param>
        /// <param name="id"></param>
        /// <param name="message">May be null or empty. <paramref name="id"/> will be added.</param>
        /// <returns></returns>
        public object GetOKResponseAsEntityId(Type entityType, long id, string message) {
            if (!string.IsNullOrEmpty(message)) message += ". ";
            message += "Id: " + id;
            if (!string.IsNullOrEmpty(Method.MA.Description)) message += ". The following was executed: " + Method.MA.Description;
            Result.ResultCode = ResultCode.ok;
            Result.AddProperty(CoreP.DBId.A(), id);
            Result.AddProperty(CoreP.SuggestedUrl.A(), API.CreateAPIUrl(entityType, id));
            Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        public object GetOKResponseAsText(string value, string message) {
            Result.ResultCode = ResultCode.ok;
            Result.AddProperty(PropertyP.PropertyValue.A(), value);
            Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        /// <summary>
        /// Calls either <see cref="GetOKResponseAsSingleEntity"/> or <see cref="GetOKResponseAsMultipleEntities"/> according to 
        /// <see cref="QueryId.IsSingle"/>, that is gives a response according to how it must be assumed the client expects it.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entities"></param>
        /// <returns></returns>
        public object GetOKResponseAsSingleEntityOrMultipleEntities(QueryId id, List<BaseEntity> entities, string message = null) {
            if (id.IsSingle) {
                if (entities.Count != 1) throw new InvalidCountException(nameof(id.IsSingle) + " && Count != 1 (" + entities.Count + ")");
                return GetOKResponseAsSingleEntity(entities[0], message);
            } else {
                return GetOKResponseAsMultipleEntities(entities, message);
            }
        }

        public object GetOKResponseAsSingleEntity(BaseEntity entity, string message = null) {
            Result.ResultCode = ResultCode.ok;
            Result.SingleEntityResult = entity ?? throw new ArgumentNullException(nameof(entity));
            if (!string.IsNullOrEmpty(message)) Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        public object GetOKResponseAsMultipleEntities(List<BaseEntity> entities, string message = null) {
            Result.ResultCode = ResultCode.ok;
            Result.MultipleEntitiesResult = entities ?? throw new ArgumentNullException(nameof(entities));
            if (!string.IsNullOrEmpty(message)) Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        public object GetErrorResponse(ErrorResponse errorResponse, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => GetErrorResponse(errorResponse.ResultCode, errorResponse.Message, caller);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode">Must be one ending with '_error', except <see cref="ResultCode.access_error"/> for which <see cref="GetAccessDeniedResponse"/> must be used</param>
        /// <param name="message">May be null</param>
        /// <param name="caller">May be null</param>
        /// <returns></returns>
        public object GetErrorResponse(ResultCode resultCode, string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            switch (resultCode) {
                case ResultCode.ok: throw new InvalidEnumException(resultCode);
                case ResultCode.access_error: throw new InvalidEnumException(resultCode, "Use method " + nameof(GetAccessDeniedResponse) + " instead");
                default: break; // OK;
            }
            Result.ResultCode = resultCode;
            if (!string.IsNullOrEmpty(caller)) Result.AddProperty(CoreP.Caller.A(), caller);
            if (!string.IsNullOrEmpty(message)) Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        /// <summary>
        /// </summary>
        /// <param name="message">May be null</param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public object GetAccessDeniedResponse(string message, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            if (!string.IsNullOrEmpty(message)) message += ". ";
            message += "You do not have access to the method " + Method.IdFriendly;
            if (CurrentUser != null) {
                message += " (user identify recognized was " + CurrentUser.IdFriendly + " (id: " + CurrentUser.Id + ")";
            }
            message += ". Last .NET method involved: " + caller; // TODO: Better text here! TODO: Add type / class within which caller resides!
            Result.ResultCode = ResultCode.access_error;
            Result.AddProperty(CoreP.Message.A(), message);
            return GetResponse();
        }

        /// <summary>
        /// Returns response to client according to contents of <see cref="Result"/>. 
        /// 
        /// The caller is responsible for <see cref="Result"/> being properly setup. 
        /// 
        /// Note that if possible then it is strongly preferred to use one of the more specific 
        /// <see cref="GetOKResponseAsEntityId"/>, <see cref="GetAccessDeniedResponse"/>, <see cref="GetErrorResponse"/> or similar 
        /// which again will call this method. 
        /// 
        /// Note how <see cref="Request.GetResponse"/> removes <see cref="CoreP.Log"/> from <see cref="ResultCode.ok"/> result if not <see cref="APIMethodAttribute.ShowDetailedResult"/>
        /// </summary>
        /// <returns></returns>
        public object GetResponse() {
            if (Result == null) throw new NullReferenceException(nameof(Result) + "\r\nDetails: " + ToString());
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return new JSONView(this).GenerateResult();
                case ResponseFormat.HTML: return new HTMLView(this).GenerateResult();
                case ResponseFormat.PDF: return new PDFView(this).GenerateResult();
                case ResponseFormat.CSV: return new CSVView(this).GenerateResult();
                default: throw new InvalidEnumException(ResponseFormat);
            }
        }

        public override string ToString() => "Url: " + URL + ", Method: " + (Method?.ToString() ?? "[NULL]") + ", CurrentUser: " + (CurrentUser?.ToString() ?? "[NULL]");

        /// <summary>
        public class InvalidRequestInitializationException : ApplicationException {
            public InvalidRequestInitializationException(string message) : base(message) { }
            public InvalidRequestInitializationException(string message, Exception inner) : base(message, inner) { }
        }

        public ResponseFormat ResponseFormat { get; private set; }
        /// <summary>
        /// Usually used for <see cref="CoreAPIMethod.RootIndex"/> when JSON / CSV is most probably not needed.
        /// </summary>
        public void ForceHTMLResponse() => ResponseFormat = ResponseFormat.HTML;

        /// <summary>
        /// Signifies that as little as possible of consistency checking / assertions should be done when handling this request.
        /// 
        /// TODO: <see cref="Request.ExceptionHasOccurred"/> should also be used in <see cref="Result"/> and
        /// TODO: <see cref="HTMLView"/> / <see cref="JSONView"/>
        /// </summary>
        public bool ExceptionHasOccurred { get; private set; }

        /// <summary>
        /// TODO: Change into <see cref="Uri"/>
        /// </summary>
        public Uri URL { get; private set; }

        private Uri _JSONUrl;
        /// <summary>
        /// Gives corresponding URL for <see cref="ResponseFormat.JSON"/>
        /// </summary>
        public Uri JSONUrl => _JSONUrl ?? (_JSONUrl = new Func<Uri>(() => {
            var strUrl = URL.ToString();
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return URL;
                case ResponseFormat.HTML: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.HTMLPostfixIndicator.Length));
                case ResponseFormat.PDF: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.PDFPostfixIndicator.Length));
                case ResponseFormat.CSV: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.CSVPostfixIndicator.Length));
                default: throw new InvalidEnumException(ResponseFormat);
            }
        })());

        private Uri _HTMLUrl;
        /// <summary>
        /// Gives corresponding URL for <see cref="ResponseFormat.HTML"/>
        /// </summary>
        public Uri HTMLUrl => _HTMLUrl ?? (_HTMLUrl = new Func<Uri>(() => {
            var strUrl = URL.ToString();
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return new Uri(strUrl + Util.Configuration.C.HTMLPostfixIndicator);
                case ResponseFormat.HTML: return URL;
                case ResponseFormat.PDF: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.PDFPostfixIndicator.Length) + Util.Configuration.C.HTMLPostfixIndicator);
                case ResponseFormat.CSV: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.CSVPostfixIndicator.Length) + Util.Configuration.C.HTMLPostfixIndicator);
                default: throw new InvalidEnumException(ResponseFormat);
            }
        })());

        private Uri _PDFUrl;
        /// <summary>
        /// Gives corresponding URL for <see cref="ResponseFormat.PDF"/>
        /// </summary>
        public Uri PDFUrl => _PDFUrl ?? (_PDFUrl = new Func<Uri>(() => {
            var strUrl = URL.ToString();
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return new Uri(strUrl + Util.Configuration.C.PDFPostfixIndicator);
                case ResponseFormat.HTML: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.HTMLPostfixIndicator.Length) + Util.Configuration.C.PDFPostfixIndicator);
                case ResponseFormat.PDF: return URL;
                case ResponseFormat.CSV: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.CSVPostfixIndicator.Length) + Util.Configuration.C.PDFPostfixIndicator);
                default: throw new InvalidEnumException(ResponseFormat);
            }
        })());

        private Uri _CSVUrl;
        /// <summary>
        /// Gives corresponding URL for <see cref="ResponseFormat.CSV"/>
        /// </summary>
        public Uri CSVUrl => _CSVUrl ?? (_CSVUrl = new Func<Uri>(() => {
            var strUrl = URL.ToString();
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return new Uri(strUrl + Util.Configuration.C.CSVPostfixIndicator);
                case ResponseFormat.HTML: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.HTMLPostfixIndicator.Length) + Util.Configuration.C.CSVPostfixIndicator);
                case ResponseFormat.PDF: return new Uri(strUrl.Substring(0, strUrl.Length - Util.Configuration.C.PDFPostfixIndicator.Length) + Util.Configuration.C.CSVPostfixIndicator);
                case ResponseFormat.CSV: return URL;
                default: throw new InvalidEnumException(ResponseFormat);
            }
        })());

        private APICommandCreator _API;
        public APICommandCreator API => _API ?? (_API = new Func<APICommandCreator>(() => {
            switch (ResponseFormat) {
                case ResponseFormat.JSON: return APICommandCreator.JSONInstance;
                case ResponseFormat.HTML: return APICommandCreator.HTMLInstance;
                case ResponseFormat.PDF: return APICommandCreator.PDFInstance;
                case ResponseFormat.CSV: return APICommandCreator.CSVInstance;
                default: throw new InvalidEnumException(ResponseFormat);
            }
        })());

        /// <summary>
        /// Matches <paramref name="request"/> with <see cref="APIMethod"/>
        /// 
        /// Returns either 
        /// <paramref name="exactMatch"/> or
        /// <paramref name="candidateMatches"/> or
        /// <paramref name="maybeIntended"/>
        /// 
        /// TODO: Method has some potential for improvement, especially after support for <see cref="HTTPMethod.POST"/> was added March 2017
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="responseFormat"></param>
        /// <param name="exactMatch">
        /// Item1 is the method to execute
        /// Item2 contains parameters
        ///       (note that the actual parameter VALUES are not validated at this stage. 
        ///       It is only validated that the parameters are given (they may even be empty))
        /// </param>
        /// <param name="candidateMatches">
        /// Item1 is the candidate list of <see cref="APIMethod"/> for which the URL / list of parameters is incomplete. 
        ///       Will always contain at least one method. 
        ///       Typically used to give a <see cref="ResultCode.missing_parameter_error"/>. 
        /// Item2 is "lastMatchingSegmentNo" (not relevant for <see cref="HTTPMethod.POST"/>)
        ///       "lastMatchingSegmentNo" is a zero based count indicating last of <see cref="APIMethod.RouteSegments"/> that matched for <paramref name="candidateMatches"/>. 
        ///       Indicates from where to construct information about missing parameter (used by <see cref="APIMethodCandidate"/> constructor)
        /// Item3 is debug-information. 
        ///       Its purpose is to help the client with understanding what data ended up at the server
        /// </param>
        /// <param name="maybeIntended">
        /// Item1 is list of <see cref="APIMethod"/> which maybe was intended by caller. 
        /// Item2 is debug-information. 
        ///       Its purpose is to help the client with understanding what data ended up at the server
        /// </param>
        /// <returns></returns>
        public static void GetMethodsMatchingRequest(
            System.Net.Http.HttpRequestMessage request,
            ResponseFormat responseFormat,
            out (APIMethod method, List<string> parameters)? exactMatch,
            out (List<APIMethod> methods, int lastMatchingSegmentNo, string explanation)? candidateMatches,
            out (List<APIMethod> methods, string explanation)? maybeIntended) {

            var url = request.RequestUri.ToString();
            var urlSegments = url.Split('/').ToList();
            if (responseFormat == ResponseFormat.HTML) {
                urlSegments.RemoveAt(urlSegments.Count - 1); /// Corresponds to <see cref="Configuration.HTMLPostfixIndicator"/>.
            } else if (responseFormat == ResponseFormat.PDF) {
                urlSegments.RemoveAt(urlSegments.Count - 1); /// Corresponds to <see cref="Configuration.PDFPostfixIndicator"/>.
            } else if (responseFormat == ResponseFormat.CSV) {
                urlSegments.RemoveAt(urlSegments.Count - 1); /// Corresponds to <see cref="Configuration.CSVPostfixIndicator"/>.
            }

            if (!string.IsNullOrEmpty(Util.Configuration.C.APIPrefix) && Util.Configuration.C.APIPrefix.Length > 1) { // In principle length is guaranteed to be more than one when not empty
                var prefix = Util.Configuration.C.ApiPrefixToLower;
                var prefixWithoutTrailingSlash = prefix.Substring(0, prefix.Length - 1);
                while (!urlSegments[0].ToLower().Equals(prefixWithoutTrailingSlash)) {
                    urlSegments.RemoveAt(0);
                    if (urlSegments.Count == 0) throw new MethodMatchingException(nameof(ConfigurationAttribute) + "." + nameof(ConfigurationAttribute.APIPrefix) + ": '" + prefixWithoutTrailingSlash + "' not present in " + nameof(urlSegments) + " but '" + prefix + "' was found in " + url);
                }
                urlSegments.RemoveAt(0);
            } else {
                var count = Math.Min(3, urlSegments.Count); // Like http://domain/, three items must be removed.
                for (var i = 1; i <= count; i++) urlSegments.RemoveAt(0);
            }
            var parametersFoundInRequestURLOrContentBody = new List<string>();

            var isPOST = request.Method.ToString().Equals(HTTPMethod.POST.ToString());
            var content = request.Content.ReadAsStringAsync().Result;

            // Note: Use of "var" not possible here, because then we will loose naming of tuples
            List<(string key, string value)> postParameters = !isPOST ? new List<(string key, string value)>() : content.Split("&").Select(s => {
                var pair = s.Split("=");
                if (pair.Count != 2) throw new MethodMatchingException("'" + s + "' is not valid. Expected exact 1 '=' character, not " + (pair.Count - 1) + ".\r\nDetails: " + nameof(content) + ": " + content + ", " + nameof(url) + ": " + url);
                return (pair[0], System.Web.HttpUtility.UrlDecode(pair[1]));
            }).ToList();

            var getDebugInformation = new Func<string>(() =>
                    nameof(System.Net.Http.HttpMethod) + ": " + request.Method.ToString() + ",\r\n" +
                    nameof(urlSegments) + ".Count: " + urlSegments.Count + ",\r\n" +
                    nameof(urlSegments) + ": " + string.Join("/", urlSegments) + ",\r\n" +
                    nameof(content) + ": " + content + ",\r\n" +
                    nameof(isPOST) + ": " + isPOST + ",\r\n" +
                    (!isPOST ? "" : (
                        nameof(postParameters) + ":\r\n" +
                        string.Join("\r\n", postParameters.Select(p => {
                            if (!PropertyKeyMapper.TryGetA(p.key, out var key)) return p.key + " [Not recognized] = '" + p.value + "'";
                            return p.key + (key.Key.CoreP == CoreP.None ? (" [Not recognized]" + ")") : "") + " = " + (key.Key.A.IsPassword ? "[WITHHELD]" : ("'" + p.value + "'"));
                        })))
                    )
            );

            // Find potential methods step by step. When has zero potential methods then go back to last list, and suggest those, while at
            // the same time explaning what is needed to use them. 

            // TODO: Future version: If ends with one method for which all parameters are OK then that would be an autogenerated method. 
            // (since not then would have been picked up by the ordinary ASP .NET routing mechanism. 

            var lastList = APIMethod.AllMethods.Where(m => {
                switch (m.MA.CoreMethod) {
                    case CoreAPIMethod.GenericMethod:
                    case CoreAPIMethod.RootIndex: return false;
                    default: return true;
                }
            }).ToList();
            var urlSegmentNo = 0;
            var postParameterNo = 0;
            while ( /// TODO: Code has some potential for improvement, 
                urlSegmentNo + postParameterNo <= (urlSegments.Count + postParameters.Count - 1)
                ) {
                //if (urlSegmentNo >= urlSegments.Count) throw new MethodMatchingException(
                //    nameof(urlSegmentNo) + " (" + urlSegmentNo + ") > " + nameof(urlSegments) + ".Count (" + urlSegments.Count + ")\r\n" +
                //    nameof(postParameterNo) + ": " + postParameterNo + "\r\n" +
                //    nameof(lastList) + " (count " + lastList.Count + "):\r\n   " + string.Join("\r\n   ", lastList.Select(s => s.ToString())) + "\r\n" +
                //    "Details: " + getDebugInformation());

                var strSegmentToLower = urlSegments.Count <= urlSegmentNo ? "" : urlSegments[urlSegmentNo].ToLower(); /// Do ToLower only once
                // Log("Looking for method candidates, segment: " + strSegment);
                var increasePostParameterNo = false;
                var newList = lastList.Where(m => {
                    if (m.RouteSegments.Count <= (urlSegmentNo + postParameterNo)) return false;
                    if (isPOST && m.RouteSegments[urlSegmentNo + postParameterNo].Parameter != null) { // Match against POST content, not against URL segment                        
                        if (postParameters.Count <= postParameterNo) return false;
                        var r = /// Note how both name of parameter and parameter must match (or rather, only name of parameter because <see cref="RouteSegmentClass.MatchesURLSegment"/> will now always return true.
                            m.RouteSegments[urlSegmentNo + postParameterNo].SegmentName.Equals(postParameters[postParameterNo].key) &&
                            m.RouteSegments[urlSegmentNo + postParameterNo].MatchesURLSegment(
                                postParameters[postParameterNo].value,
                                postParameters[postParameterNo].value); // Strictly this last parameter should be the ToLower representation but it does not matter. We could even set it to null now.
                        if (r) {
                            // At least one method found where parameter matches, increase for next iteration in while-loop
                            increasePostParameterNo = true; // TODO: Could we remove this side-effect?
                        }
                        return r;
                    } else {
                        if (urlSegments.Count <= urlSegmentNo) return false;
                        return m.RouteSegments[urlSegmentNo + postParameterNo].MatchesURLSegment(urlSegments[urlSegmentNo], strSegmentToLower);
                    }
                }).ToList();
                // Log(nameof(newList) + ".Count: " + newList.Count);
                if (newList.Count == 0) {
                    exactMatch = null;
                    candidateMatches = null;
                    maybeIntended = (lastList, getDebugInformation());
                    return;
                }
                lastList = newList;
                if (increasePostParameterNo) {
                    postParameterNo++;
                } else {
                    urlSegmentNo++;
                }
            }

            if ((urlSegmentNo + postParameterNo) == 0) {
                exactMatch = null;
                candidateMatches = null;
                maybeIntended = (lastList, getDebugInformation());
                return;
            }

            if (lastList.Count == 0) throw new MethodMatchingException(nameof(lastList) + ".Count == 0");
            var exactMatches = lastList.Where(m => m.RouteSegments.Count == (urlSegmentNo + postParameterNo)).ToList();

            // Returns parameters (either from urlSegments or from postParameters
            // TODO: Has potential for improvement
            var getParameters = new Func<APIMethod, List<string>>(method => {
                List<string> retval;
                if (isPOST) { // Add from postParameters
                    retval = postParameters.Select(s => s.value).ToList();
                } else {
                    if (method.RouteSegments.Count != urlSegments.Count) throw new MethodMatchingException(nameof(method.RouteSegments) + ".Count (" + method.RouteSegments.Count + ") != " + nameof(urlSegments) + ".Count: " + urlSegments.Count + "\r\nDetails: " + nameof(url) + ": " + url + ", " + nameof(content) + ": " + content + ", " + nameof(method) + ": " + method.ToString());
                    retval = new List<string>();
                    // TODO: We could have property in APIMethod called IndexesOfParameters or similar, or just
                    // TODO: give list urlSegments as parameter to APIMethod and get parameter values back.
                    for (var i = 0; i < method.RouteSegments.Count; i++) { // Add from urlSegments, where applicable
                        if (method.RouteSegments[i].Parameter != null) retval.Add(urlSegments[i]);
                    }
                }
                if (retval.Count != method.Parameters.Count) throw new MethodMatchingException(nameof(retval) + ".Count: " + retval.Count + ", " + nameof(method.Parameters) + ".Count: " + method.Parameters.Count + "\r\nDetails: " + nameof(url) + ": " + url + ", " + nameof(content) + ": " + content + ", " + nameof(method) + ": " + method.ToString());
                return retval;
            });

            switch (exactMatches.Count) {
                case 0:
                    exactMatch = null;
                    candidateMatches = (lastList, urlSegmentNo - 1 + postParameterNo, getDebugInformation());
                    maybeIntended = null;
                    return;
                case 1:
                    exactMatch = (exactMatches[0], getParameters(exactMatches[0]));
                    candidateMatches = null;
                    maybeIntended = null;
                    return;
                default:
                    /// We have multiple matches but try again if maybe only one is relevant anyway
                    /// 
                    /// Typical example would be
                    ///   Autogenerated, Person/{QueryIdInteger}/History, CoreMethod.History
                    /// and
                    ///   Autogenerated, Person/{QueryId}/{PropertyOperation}, CoreMethod.PropertyOperation
                    /// The first should be the one chosen, since literal string "History" goes before {PropertyOperation}
                    /// 
                    /// (We could have reduced the number of matches by checking the validity of each parameter, BUT, 
                    /// that would lead to bad performance since the validation would be done twice.)
                    /// 
                    /// So, instead we just order by number of parameters, and keep the method with the least number of parameters.
                    /// (assumed there is only one such method)
                    var exactMatchesOrdered = exactMatches.OrderBy(m => m.Parameters.Count).ToList();
                    if (exactMatchesOrdered[0].Parameters.Count < exactMatchesOrdered[1].Parameters.Count) {
                        exactMatch = (exactMatches[0], getParameters(exactMatches[0]));
                        candidateMatches = null;
                        maybeIntended = null;
                        return;
                    } else {
                        throw new MethodMatchingException(nameof(exactMatches) + ".Count: " + exactMatches.Count + ". " +
                            "Only one such match should be possible. The actual methods are:\r\n" + string.Join("\r\n", exactMatches.Select(m => m.ToString())));
                    }
            }
        }

        public class MethodMatchingException : ApplicationException {
            public MethodMatchingException(string message) : base(message) { }
            public MethodMatchingException(string message, Exception inner) : base(message, inner) { }
        }

        public static ResponseFormat GetResponseFormatFromURL(Uri url) {
            var urlToLower = url.ToString().ToLower();
            if (urlToLower.EndsWith(Util.Configuration.C.HTMLPostfixIndicatorToLower)) return ResponseFormat.HTML;
            if (urlToLower.EndsWith(Util.Configuration.C.PDFPostfixIndicatorToLower)) return ResponseFormat.PDF;
            if (urlToLower.EndsWith(Util.Configuration.C.CSVPostfixIndicatorToLower)) return ResponseFormat.CSV;
            return ResponseFormat.JSON;
        }
    }

    [Class(Description =
        "Contains actual -" + nameof(Parameters) + "- of request.\r\n" +
        "See super class -" + nameof(Request) + "- for documentation."
    )]
    public class ValidRequest : Request {

        public Parameters Parameters { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="method"></param>
        /// <param name="currentUser"></param>
        /// <param name="parameters">
        /// </param>
        public ValidRequest(System.Net.Http.HttpRequestMessage httpRequestMessage, APIMethod method, BaseEntity currentUser, Parameters parameters) : base(httpRequestMessage, method, currentUser, exceptionHasOccurred: false) => Parameters = parameters;
    }
}