using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// TODO: REMOVE FROM REPOSITORY! NO LONGER IN USE!
    /// 
    /// TODO: Comments and code is a little bit primitive as of Dec 2016
    /// 
    /// One single documentation example of an API call
    /// 
    /// Note how an ApiSample may contain multiple ApiCall, each with its own asserts,
    /// but in most cases one ApiSample will contain only one ApiCall
    /// 
    /// Note how the call "itself" is represented by the class FunctionCall, in other words, 
    /// the line Api("xxx") in the documentation is represented by an instance of the class FunctionCall
    /// 
    /// Note how the documentation for each overload is duplicated since there may be multiple 
    /// ApiCalls for each overload
    /// 
    /// Example of syntax1, GET REST-form: api/Person/1007/Property/first_name
    /// Example of syntax2, GET "old" query-string form: api/Person//Property?id=1007&property=first_name
    /// 
    /// Offers 
    /// 1) URLs
    /// 2) HTML kode (for dokumentasjon, unit-testing and bulk-usage), 
    /// 3) iOS / Android kode (Not quite updated as of Dec 2016)
    /// 4) Unit-tests (HTML / Javascript)
    /// </summary>
    public class ApiCall {

        /// <summary>
        /// WebAPI Route that this example belongs to
        /// 
        /// TODO: Make private and set through constructor only
        /// </summary>
        public Method Method;

        /// <summary>
        /// På formen Notification/Add/{email}/{name}
        /// </summary>
        public string syntax1;

        public static long syntax1FilenameSafeCount = 1;
        private string _syntax1FilenameSafe = null;
        public string Syntax1FilenameSafe {
            get {
                if (_syntax1FilenameSafe != null) return _syntax1FilenameSafe;
                if (syntax1.Length > 100) {
                    _syntax1FilenameSafe = syntax1FilenameSafeCount++.ToString();
                } else {
                    _syntax1FilenameSafe = syntax1.Replace("{", "(").Replace("}", ")").Replace("/", "_");
                    if (_syntax1FilenameSafe.EndsWith("_")) _syntax1FilenameSafe = _syntax1FilenameSafe.Substring(0, _syntax1FilenameSafe.Length - 1);
                }
                return _syntax1FilenameSafe;
            }
        }
        /// <summary>
        /// På formen Notification/Add?email={email}&amp;name={name}
        /// </summary>
        public string syntax2;

        private long _idUniqueWithinApiMethod = -1;
        /// <summary>
        /// Settes av Initialize-metoden men selve id'en kommer fra ApiMethod
        /// 
        /// Bemerk hvordan denne id'en kun er unik innenfor en spesifikk APIMethod.
        /// Dette er gjort med hensikt slik at URL'ene for hvert enkelt ApiCall i dokumentasjonen holdes noenlunde stabile 
        /// (tidligere hadde vi én global id-teller)
        /// Brukes typisk av HTTPClass og PathClass sine metoder GetBulkUsageStandaloneUrl og GetExampleStandaloneUrl
        /// </summary>
        public long IdUniqueWithinApiMethod {
            get {
                if (_idUniqueWithinApiMethod == -1) throw new Exception("_idUniqueWithinApiMethod == -1");
                return _idUniqueWithinApiMethod;
            }
            private set {
                if (_idUniqueWithinApiMethod != -1) throw new Exception("_idUniqueWithinApiMethod != -1");
                _idUniqueWithinApiMethod = value;
            }
        }

        private long _idUniqueGlobal = -1;
        /// <summary>
        /// Settes av Initialize-metoden 
        /// 
        /// Global unik id. Bruk denne når du for eksempel skal generere en enkelt HTML-"side" med flere APICall fra ulike metoder
        /// (da er ikke IdUniqueWithinApiMethod egnet fordi den er kun unik innenfor metode)
        /// 
        /// Brukes typisk av CodeClass sine metoder nameOfUnitTestFunctionGET1, nameOfUnitTestFunctionGET2 og nameOfUnitTestFunctionPOST
        /// og når det genereres tilsvarende referanser andre steder.
        /// </summary>
        public long IdUniqueGlobal {
            get {
                if (_idUniqueGlobal == -1) throw new Exception("_idUniqueGlobal == -1");
                return _idUniqueGlobal;
            }
            private set {
                if (_idUniqueGlobal != -1) throw new Exception("_idUniqueGlobal != -1");
                _idUniqueGlobal = value;
            }
        }

        private Usages Usage;

        /// <summary>
        /// Describes how ApiCall may be used, for sample only, unit testing only or both.
        /// </summary>
        public enum Usages {
            /// <summary>
            /// Use both for sample and for unit testing. Corresponds to SampleB:
            /// </summary>
            Both,

            /// <summary>
            /// Use for sample only. Corresponds to SampleS:
            /// </summary>
            Sample,

            /// <summary>
            /// Use for unit-testing only. Corresponds to SampleU:
            /// </summary>
            UnitTest
        }

        public FunctionCall ApiFunctionCall { get; private set; }

        /// <summary>
        /// Must be set from outside before call to Initialize
        /// 
        /// Code like
        /// AssertB("Javascript-expression") // Eventual comment
        /// AssertS("Javascript-expression") 
        /// AssertU("Javascript-expression") 
        /// </summary>
        public List<FunctionCall> Asserts { get; private set; }

        /// <summary>
        /// Name of API-method, without parameter. Eksempel: Person/Add
        /// 
        /// Normally the same as Method.Name, but note that theoretically could be antother method as there
        /// is nothing stopping you from giving a sample belonging to other API methods when documentation
        /// a specific API method.
        /// 
        /// MEN, this is mostly hypotetical since we uses Metod to ascertain what HTTP verbs are allowed
        /// like GET and POST. So we could have removed strMethod altogether here since there is little meaning
        /// in setting it to something other than Method.Name
        /// </summary>
        public string StrMethod { get; private set; }

        public List<Parameter> Parameters { get; private set; }

        public PathClass Path { get; private set; }
        public HTTPClass HTTP { get; private set; }
        public HTMLClass HTML { get; private set; }
        public CodeClass Code { get; private set; }

        /// <summary>
        /// Brukes for å sikre unik id til Javascript metoder.
        /// </summary>
        public ApiCall(Method method, FunctionCall apiFunctionCall, Usages usage) {
            Asserts = new List<FunctionCall>();
            Method = method;
            ApiFunctionCall = apiFunctionCall;
            Usage = usage;
        }

        public override string ToString() {
            if (HTTP == null) return "[Not initialized]";
            return syntax1 + " (" + HTTP.exampleGET1 + ")"; // syntax1 en beskriver overloadet versjon, 
        }

        private static long IdUniqueGlobalLastUsed = 0;
        /// <summary>
        /// Will parse the code. 
        /// Separate method since the parser in ApiSample adds Asserts after having craeted instance of ApiCall
        /// TODO: Dec 2016. If we make ApiSample smarter then we could leave out this method (Initialize) and
        /// instead put this code into the constructor.
        /// </summary>
        public void Initialize(Func<int> getNextAPICallId) {
            IdUniqueWithinApiMethod = getNextAPICallId();
            IdUniqueGlobal = ++IdUniqueGlobalLastUsed;

            Parameters = new List<Parameter>();
            Path = new PathClass(this);
            HTTP = new HTTPClass(this);
            HTML = new HTMLClass(this);
            Code = new CodeClass(this);

            try {
                var methodURLWithParameters = ApiFunctionCall.parameter;

                var urlGET1 = "";
                var urlGET2 = "";

                // If POST is not allowed (typically for nested URLs) then we use GET with syntax 1
                // This is not optimal as it limits what we can add when unit-testing
                // Is bult up inside of the URL-string, with the actual values that we find in FORM
                // The problem is nested API-urls that the WebAPI does not allow
                // (TODO: Problem in 2013. Maybe solved in 2017?)
                // Note that is built with use of encodeURIComponent 
                var URLJavascriptGet1 = "";

                var q = methodURLWithParameters.IndexOf("?");
                if (q == -1) {
                    // Vi har kun en kommando. Eksemplene blir lik kommandoen
                    StrMethod = methodURLWithParameters;
                    syntax1 = StrMethod;
                    syntax2 = StrMethod;
                    urlGET1 = StrMethod + "/";
                    urlGET2 = StrMethod;
                    URLJavascriptGet1 = StrMethod;
                } else {
                    // Find all parameters
                    // For syntax1, insert first all places where we find //, after that insert at end
                    // TODO: Write better comment...
                    foreach (var parameter in methodURLWithParameters.Substring(q + 1).Split('&')) {
                        var arrTuple = parameter.Split('=');
                        if (arrTuple.Length != 2) throw new Exception("arrTuple.Length!=2, parameter = '" + parameter + "', methodURLWithParameters = '" + methodURLWithParameters + "'");
                        Parameters.Add(new Parameter { Name = arrTuple[0], Value = Util.Configuration.InsertWellKnownIds(arrTuple[1]) });
                    }
                    StrMethod = methodURLWithParameters.Substring(0, q); // Ta bort alle parameterne
                    syntax1 = StrMethod;
                    syntax2 = StrMethod;
                    urlGET1 = StrMethod;
                    urlGET2 = StrMethod;
                    URLJavascriptGet1 = StrMethod;

                    // Insert parameters
                    if (Parameters.Count == 0) throw new Exception("parameters.Count == 0");
                    var i = 0;

                    while (i < Parameters.Count) {
                        var parameter = Parameters[i];
                        var s = ((i == 0) ? "?" : "&");

                        var pos = syntax1.IndexOf("//");
                        if (pos > -1) {
                            // Insert into string
                            syntax1 = syntax1.Substring(0, pos + 1) + "{" + parameter.Name + "}" + syntax1.Substring(pos + 1);
                        } else {
                            // Insert at end
                            syntax1 += "/{" + parameter.Name + "}";
                        }
                        syntax2 += (s + parameter.Name + "={" + parameter.Name + "}");
                        
                        pos = urlGET1.IndexOf("//");
                        if (pos > -1) {
                            // Insert into string
                            urlGET1 = urlGET1.Substring(0, pos + 1) + parameter.ValueWithUniqeness + urlGET1.Substring(pos + 1);
                        } else {
                            // Insert at end
                            urlGET1 += "/" + parameter.ValueWithUniqeness;
                        }

                        pos = URLJavascriptGet1.IndexOf("//");
                        var encodedValue =
                            "' + encodeURIComponent($('#" + parameter.Name + "_GET1_" + IdUniqueGlobal + "').val()." +
                            @"replace(/:/g, '_COLON_')." + // Nødvendig, hvis ikke gir IIS 400-respons: A potentially dangerous Request.Path value was detected from the client
                            @"replace(/\\/g, '_SLASH_')." + // Nødvendig, hvis ikke gir klarer ikke IIS å map'e URL (ender med {*url} match, siste match)
                            @"replace(/[/]/g,'_SLASH_')." +// required to correctly map URL string. URL will be split on '/' if not encoded
                            @"replace(/%/g,'_PERCENT_')" +// required to prevent error: "potentially dangerous Request.path"
                            ") + '";
                        if (pos > -1) {
                            // Insert into string
                            URLJavascriptGet1 = URLJavascriptGet1.Substring(0, pos + 1) +
                            encodedValue +
                            URLJavascriptGet1.Substring(pos + 1);
                        } else {
                            // Insert at end
                            URLJavascriptGet1 += "/" + encodedValue;
                        }

                        urlGET2 += (s + parameter.Name + "=" + parameter.ValueWithUniqeness);
                        i++;
                    }

                    syntax1 += "/";      // With slash at end we have a greated range of characters that are allowed in GET queries
                    urlGET1 += "/";           // In addition we expect that ends with slash for later adding of HTML at end
                    URLJavascriptGet1 += "/"; //
                }

                if (Method.HttpMethods.Contains(HTTPMethod.GET)) {
                    HTTP.exampleGET1 = urlGET1;
                    HTTP.exampleGET2 = urlGET2;
                    HTML.exampleGET1CompleteHTMLLink = "<a href=\"" + Util.Configuration.BaseUrl + urlGET1 + Util.Configuration.HTMLPostfixIndicatorWithoutLeadingSlash + "\">" + urlGET1.HTMLEncode() + "</a>";
                    HTML.exampleGET2CompleteHTMLLink = "<a href=\"" + Util.Configuration.BaseUrl + urlGET2 + "\">" + urlGET2.HTMLEncode() + "</a>"; // Å legge til /HTML går ikke nå
                }

                var sbParametersBPAPILogging = new System.Text.StringBuilder();
                Parameters.ForEach(p => sbParametersBPAPILogging.Append("    com.connome.bpapi.log('Parameter " + p.Name + ": ' + $('#" + p.Name + "_" + IdUniqueGlobal + "').val());\r\n"));
                // Alternatively $('#" + p.name + "_" + uniquePageId + "').attr('value')

                var sbSamplesHTML = new System.Text.StringBuilder();
                var sbUnitTestsHTML = new System.Text.StringBuilder();

                sbSamplesHTML.Append("<br>");
                sbSamplesHTML.Append("<p>GET, syntax 1 (preferred REST-format): <br>" + HTML.exampleGET1CompleteHTMLLink + "</p><br>");
                sbSamplesHTML.Append("<p>GET, syntax 2 (traditional query-string): <br>" + HTML.exampleGET2CompleteHTMLLink + "</p><br>");

                if (Method.HttpMethods.Contains(HTTPMethod.POST)) {
                    sbSamplesHTML.Append(
                        "<p>POST, traditional</p>" +
                        "<form action=\"" + Util.Configuration.BaseUrl + StrMethod + Util.Configuration.HTMLPostfixIndicator + "\" method=\"POST\">\r\n" +
                        CreateParametersEditable(Parameters, "") + // Trenger ikke spesifisere syntax nå
                        "<input type=\"submit\" value=\"POST\">\r\n</form><br><br><br>\r\n");
                }

                // Javascript. Create three variants, GET1, GET2 og POST
                for (var i = 0; i < 3; i++) {
                    var testId = "";
                    var buttonText = "";
                    var url = "";
                    var typeAndData = "";
                    var syntax = "";
                    switch (i) {
                        case 0:
                            if (!Method.HttpMethods.Contains(HTTPMethod.POST)) continue;
                            testId = "_POST_" + IdUniqueGlobal; buttonText = "Javascript POST";
                            url = Util.Configuration.BaseUrl + StrMethod;
                            typeAndData =
                                "      type: 'POST',\r\n" +
                                "      data: $('#form" + testId + "').serialize(),\r\n";
                            syntax = "POST";
                            break;
                        case 1:
                            if (!Method.HttpMethods.Contains(HTTPMethod.GET)) continue;
                            testId = "_GET1_" + IdUniqueGlobal; buttonText = "Javascript GET1";
                            url = Util.Configuration.BaseUrl + URLJavascriptGet1;
                            typeAndData = "      type: 'GET',\r\n";
                            syntax = "GET1";
                            break;
                        case 2:
                            if (!Method.HttpMethods.Contains(HTTPMethod.GET)) continue;
                            testId = "_GET2_" + IdUniqueGlobal; buttonText = "Javascript GET2";
                            url = Util.Configuration.BaseUrl + StrMethod;
                            typeAndData =
                                "      type: 'GET',\r\n" +
                                "      data: $('#form" + testId + "').serialize(),\r\n";
                            syntax = "GET2";
                            break;
                        default: throw new Exception("Unknown i (" + i + ")");
                    }

                    var codeSamplesHTML =
                        "<form id = \"form" + testId + "\">\r\n" +
                        CreateParametersEditable(Parameters, syntax) +
                        "<input type=\"submit\" value=\"" + buttonText + "\">\r\n" +
                        "</form>\r\n" +
                        "<label id=\"Result" + testId + "\"></label>\r\n" +
                        "<script>\r\n" +
                        "$(document).ready(function () {\r\n" +
                        "    $('#form" + testId + "').submit( function() {\r\n" +
                        "\r\n" +
                        sbParametersBPAPILogging.ToString() +
                        "    var url = '" + url + "';\r\n" +
                        "    $('#Result" + testId + "').text('Contacting server ' + url + '...');\r\n" +
                        "    com.connome.bpapi.log('Contacting server ' +url);\r\n" +
                        "\r\n" +
                        "    com.connome.bpapi.call({\r\n" +
                        "      url: url, \r\n" +
                        typeAndData +
                        Code.GetJavascriptAsserts(Usages.Sample) +
                        "      success: function(data) {\r\n" +
                        // TODO: <br> blir escapet til &lt;br&gt, vi oppnår altså ikke det vi ønsker.
                        "         $('#Result" + testId + "').text('Success ' + url + '<br>' + $.param(data).replace(/%5B/g,'[').replace(/%5D/g,']'));\r\n" +
                        "         com.connome.bpapi.log('Success ' + com.connome.bpapi.explainData(data));\r\n" +
                        "      },\r\n" +
                        "      error: function() {\r\n" +
                        "         $('#Result" + testId + "').text('Error ' + url);\r\n" +
                        "         com.connome.bpapi.log('Error ' + url);\r\n" +
                        "      }\r\n" +
                        "    });\r\n" +
                        "    return false;\r\n" +
                        "  });\r\n" +
                        "});\r\n</script>\r\n<br><br>\r\n";

                    var codeUnitTests =
                        Usage == Usages.Sample ? "" : (
                         "<form id = \"form" + testId + "\">\r\n" +
                         CreateParametersHidden(Parameters, syntax) +
                         "</form>\r\n" +
                         "<script>\r\n" +
                         "function APITest" + testId + "() {\r\n" +
                         "    $('#Result" + testId + "').text('');\r\n" +
                         // parametersBPAPILogging + Ingen vits i å logge når del av mange tester. Blir for mye logging.
                         "    var url = '" + url + "';\r\n" +
                         "    $('#Result" + testId + "').text('Contacting server ' + url + '...');\r\n" +
                         "    com.connome.bpapi.log('Contacting server ' + url);\r\n" +
                         "\r\n" +
                         "    com.connome.bpapi.call({\r\n" +
                         "      url: url, \r\n" +
                         typeAndData +
                         Code.GetJavascriptAsserts(Usages.UnitTest).ToString() +
                         "      success: function(data) {\r\n" +
                         "         $('#Result" + testId + "').text('Success');\r\n" +
                         "         com.connome.bpapi.logExplanation(data);\r\n" +
                         "      },\r\n" +
                         "      error: function() {\r\n" +
                         "         $('#Result" + testId + "').text('Error');\r\n" +
                         "         com.connome.bpapi.log('Error ' + url);\r\n" +
                         "      }\r\n" +
                         "    });\r\n" +
                         "}\r\n</script>\r\n\r\n"); // ikke <br> her, ender bare opp med mange blanke linjer i ferdig HTML-side

                    sbSamplesHTML.Append(codeSamplesHTML);
                    sbUnitTestsHTML.Append(codeUnitTests);
                }

                HTML.Samples = sbSamplesHTML.ToString();
                HTML.UnitTests = sbUnitTestsHTML.ToString();

            } catch (Exception ex) {
                throw new Exception("Unable to parse '" + ApiFunctionCall.parameter + "' due to " + ex.GetType().ToString() + " " + ex.Message, ex);
            }
        }

        private string CreateParametersAsQueryString(List<Parameter> parameters) {
            var retval = new System.Text.StringBuilder();
            // parameters.ForEach(p => retval.Append((retval.Length == 0 ? "?" : "&") + p.name + "=" + p.valueUrlEncoded));
            parameters.ForEach(p => retval.Append((retval.Length == 0 ? "?" : "&") + p.Name + "=" + p.ValueWithUniqeness));
            return retval.ToString();
        }

        private string CreateParametersHidden(List<Parameter> parameters, string syntax) {
            var retval = new System.Text.StringBuilder();
            // Vi trenger ikke UrlEncode nå. Det antas at klienten bruker jQuery "form.serialize" som gjør URL encoding for oss
            parameters.ForEach(p => retval.Append("<input hidden=\"true\" type=\"text\" id =\"" + p.Name + "_" + syntax + "_" + IdUniqueGlobal + "\" name=\"" + p.Name + "\" value=\"" + p.ValueHtmlEncoded + "\"></input>\r\n"));
            return retval.ToString();
        }

        private string CreateParametersEditable(List<Parameter> parameters, string syntax) {
            var retval = new System.Text.StringBuilder();
            retval.Append("<table>");
            parameters.ForEach(p => retval.Append(
                    "<tr>" +
                    "<td><label>" + p.Name + ": </label></td>\r\n" +
                    "<td><input size = \"100\" type=\"text\" id =\"" + p.Name + "_" + syntax + "_" + IdUniqueGlobal + "\" name=\"" + p.Name + "\" value=\"" + p.ValueHtmlEncoded + "\"></input></td>" +
                    "</tr>\r\n"));
            retval.Append("</table>\r\n");
            return retval.ToString();
        }

        public class PathClass {

            private ApiCall _apiCall;

            /// <summary>
            /// Returnerer altså _filnavn_, ikke fullstendig sti
            /// </summary>
            /// <returns></returns>
            public string GetBulkUsageStandaloneFilename() =>_apiCall.Method.DocumentationFilename.Replace(".html", "") + "_" + _apiCall.Syntax1FilenameSafe + "_BulkUsage_" + _apiCall.IdUniqueWithinApiMethod + ".html"; // Trenger vi id-feltet her?
            
            /// <summary>
            /// Returnerer altså _filnavn_, ikke fullstendig sti
            /// </summary>
            /// <returns></returns>
            public string GetExampleStandaloneFilename() => _apiCall.Method.DocumentationFilename.Replace(".html", "") + "_" + _apiCall.Syntax1FilenameSafe + "_Example_" + _apiCall.IdUniqueWithinApiMethod + ".html"; // Trenger vi id-feltet her?
           
            public PathClass(ApiCall apicall) => _apiCall = apicall;            
        }

        /// <summary>
        /// HTTP (URL) relatert informasjon
        /// </summary>
        public class HTTPClass {

            private ApiCall _apiCall;

            /// <summary>
            /// URL på formen Alarm/Add/bjorn@sikomconnect.com/Bjørn
            /// </summary>
            public string exampleGET1;

            /// <summary>
            /// URL på formen Alarm/Add?email=bjorn@sikomconnect.com&name=Bjørn
            /// </summary>
            public string exampleGET2;

            public string GetBulkUsageStandaloneUrl() => _apiCall.Method.DocumentationUrl.Replace(".html", "") + "_" + _apiCall.Syntax1FilenameSafe + "_BulkUsage_" + _apiCall.IdUniqueWithinApiMethod + ".html";           
            public string GetExampleStandaloneUrl() => _apiCall.Method.DocumentationUrl.Replace(".html", "") + "_" + _apiCall.Syntax1FilenameSafe + "_Example_" + _apiCall.IdUniqueWithinApiMethod + ".html";
           
            public HTTPClass(ApiCall apicall) => _apiCall = apicall;
            
        }

        /// <summary>
        /// HTML relatert informasjon
        /// </summary>
        public class HTMLClass {

            private ApiCall _apiCall;

            /// <summary>
            /// "Komplett" hyperlenke (a href osv), på formen Alarm/Add/bjorn@sikomconnect.com/Bjørn
            /// </summary>
            public string exampleGET1CompleteHTMLLink;

            /// <summary>
            /// "Komplett" hyperlenke (a href osv), på formen Alarm/Add?email=bjorn@sikomconnect.com&name=Bjørn
            /// </summary>
            public string exampleGET2CompleteHTMLLink;

            private string _helpTextHTML;
            public string HelpTextHTML {
                get {
                    if (_helpTextHTML != null) return _helpTextHTML;
                    if (string.IsNullOrEmpty(_apiCall.ApiFunctionCall.comment)) {
                        _helpTextHTML = "";
                    } else {
                        _helpTextHTML = Method.InsertDocumentationHTMLLinks("<p>" + _apiCall.ApiFunctionCall.comment + "</p>");
                    }
                    return _helpTextHTML;
                }
            }

            /// <summary>
            /// Informasjon om data som returneres. Fullverdig HTML (gjerne tabell)
            /// 
            /// Bygges opp fra Asserts i dokumentasjonen. 
            /// </summary>
            private string _dataReturnedHTML;
            public string DataReturnedHTML {
                get {
                    if (_dataReturnedHTML != null) return _dataReturnedHTML;
                    var sbDataReturnedHTML = new System.Text.StringBuilder();
                    _apiCall.Asserts.ForEach(a => {
                        if (!string.IsNullOrEmpty(a.dataAsserted)) {
                            sbDataReturnedHTML.Append("<tr><td>" + a.dataAsserted + "</td><td>" + (string.IsNullOrEmpty(a.comment) ? "&nbsp;" : a.comment) + "</td></tr>");
                        }
                    });
                    if (sbDataReturnedHTML.Length > 0) {
                        sbDataReturnedHTML.Insert(0, "<table><tr><th>Data</th><th>Comment</th></tr>");
                        // TOOD: Helst vil vi ha <p> her, men vi er vel inne i en <p> fra før av?
                        // UANSETT: Kan godt gjøres mye PENERE
                        sbDataReturnedHTML.Insert(0, "<br>Examples of data returned<br><br>");
                        sbDataReturnedHTML.Append("<table>");
                    }
                    _dataReturnedHTML = sbDataReturnedHTML.ToString(); // Sett inn lenker til andre API-kall der hvor mulig
                    _dataReturnedHTML = Method.InsertDocumentationHTMLLinks(_dataReturnedHTML);
                    return _dataReturnedHTML;
                }
            }

            /// <summary>
            /// Eksempler som HTML form-kode med POST submit-buttons og Javascript GET buttons (i tillegg til lenker for exampleGET1 og exampleGET2)
            /// 
            /// Genereres av Initialize for ApiCall
            /// 
            /// For plassering inne i body-tag (inneholder altså ikke HTML / BODY tags selv)
            /// 
            /// Lenkes gjerne fra hovedsiden for hver API-metode med lenken "Detailed examples".
            /// 
            /// Bruker jQuery og BPAPI.js. 
            /// 
            /// Inneholder submit-button for følgende scenarier:
            /// Tradisjonell POST initiert av browser.
            /// POST gjennom Javascript (jQuery, BPAPI.js)
            /// GET gjennom Javascript (jQuery, BPAPI.js) (syntaks 1)
            /// GET gjennom Javascript (jQuery, BPAPI.js) (syntaks 2)
            /// </summary>
            public string Samples;

            private string _bulkUsage;
            /// <summary>
            /// HTML form-kode med submit-buttons for masseoppdatering. Bruker Javascript med POST
            /// Poenget her er ikke å teste systemet, men å tilby administrasjonsgrensesnitt
            /// 
            /// For plassering inne i body-tag (inneholder altså ikke HTML / BODY tags selv)
            /// 
            /// Lenkes gjerne fra hovedsiden for hver API-metode med lenken "Bulk usage".
            /// 
            /// Bruker jQuery og BPAPI.js. 
            /// </summary>
            public string BulkUsage {
                get {
                    if (!_apiCall.Method.HttpMethods.Contains(HTTPMethod.POST)) return null;
                    if (_bulkUsage != null) return _bulkUsage;
                    // I tillegg lager vi Javascriptkode for bulkoppdatering
                    _bulkUsage = "\r\n" +
                        "<form id=\"bulkUpdate\">\r\n" +
                        "<textarea id=\"bulkInput\" cols=\"120\" rows=\"15\">Input in CSV or similar format with the following fields:\r\n" + CreateListOfParameterNames(_apiCall.Parameters) + "</textarea><br />\r\n" +
                        "<textarea id=\"bulkOutput\" cols=\"120\" rows=\"5\">Output goes here (with more details visible in the Console log shown by pressing F12 in your web-browser)</textarea><br />\r\n" +
                        "\r\n" +
                        "<label>Field separation character</label>\r\n" +
                        "<select id=\"fieldSeparator\">\r\n" +
                        "    <option value=\";\">Semicolon ;</option>\r\n" +
                        "    <option value=\",\">Comma ,</option>\r\n" +
                        "    <option value=\" \">Space (ASCII 32)</option>\r\n" +
                        "    <option value=\"&#9;\">Tab (ASCII 9)</option>\r\n" +
                        "</select>\r\n" +
                        "<br />\r\n" +
                        "\r\n" +
                        "<input type=\"checkbox\" id=\"verifyOnly\" checked=\"checked\" />\r\n" +
                        "Verify only (do not actually execute method)<br />\r\n" +
                        "<input type=\"submit\" value=\"Call bulk\" /><br />\r\n" +
                        "\r\n" +
                        "</form>\r\n";

                    _bulkUsage = _bulkUsage +
                        "    <script>\r\n" +
                        "        $(document).ready(function () {\r\n" +
                        "            $('#bulkUpdate').submit(function () {\r\n" +
                        "                try {\r\n" +
                        "                    com.connome.bpapi.log('');\r\n" +
                        "                    com.connome.bpapi.log('');\r\n" +
                        "                    com.connome.bpapi.log('VerifyOnly: ' + $('#verifyOnly').is(':checked'));\r\n" +
                        "                    com.connome.bpapi.log('fieldSeparator: \\'' + $('#fieldSeparator').val() + '\\'');\r\n" +
                        "\r\n" +
                        "                    $('#bulkOutput').val(\r\n" +
                        "                        'Initializing...\\n' +\r\n" +
                        "                        'VerifyOnly: ' + $('#verifyOnly').is(':checked') + '\\n' +\r\n" +
                        "                        'fieldSeparator: \\'' + $('#fieldSeparator').val() + '\\''\r\n" +
                        "                        );\r\n" +
                        "\r\n" +
                        "                    com.connome.bpapi.callBulk({\r\n" +
                        "                        url: '" + Util.Configuration.BaseUrl + _apiCall.StrMethod + "',\r\n" +
                        "                        type: 'POST', // Alltid POST, ignoreres egentlig\r\n" +
                        "                        parameterNames: " + CreateJavascriptArrayWithParameterNames(_apiCall.Parameters) + ",\r\n" +
                        "                        data: $('#bulkInput').val(),\r\n" +
                        "                        verifyOnly: $('#verifyOnly').is(':checked'),\r\n" +
                        "                        fieldSeparator: $('#fieldSeparator').val(),\r\n" +
                        "                        success: function (data) {\r\n" +
                        "                            com.connome.bpapi.log('Success ' + com.connome.bpapi.explainData(data));\r\n" +
                        "                            $('#bulkOutput').val('Success ' + com.connome.bpapi.explainData(data));\r\n" +
                        "                        },\r\n" +
                        "                        error: function () {\r\n" +
                        "                            $('#bulkOutput').val('Error, see Console log for details (press F12 if Console not shown)');\r\n" +
                        "                        }\r\n" +
                        "                    });\r\n" +
                        "                } catch (e) {\r\n" +
                        "                    com.connome.bpapi.log('Exception occurred: ' + e);\r\n" +
                        "                    $('#bulkOutput').val('Exception occurred: ' + e);\r\n" +
                        "                } finally {\r\n" +
                        "                    return false;\r\n" +
                        "                }\r\n" +
                        "            });\r\n" +
                        "        });\r\n" +
                        "    </script>\r\n";

                    return _bulkUsage;
                }
            }

            public string GetBulkUsageStandaloneLinkWithLinkText() => "<a href=\"" + _apiCall.HTTP.GetBulkUsageStandaloneUrl() + "\">Bulk usage interface</a>";           
            public string GetExampleStandaloneLinkWithLinkText() => "<a href=\"" + _apiCall.HTTP.GetExampleStandaloneUrl() + "\">Details</a>";
            
            /// <summary>
            /// form-kode med skjulte input-felter. 
            /// 
            /// Logger status til labels definert "utenpå" denne koden. Labels har id'er slik som
            /// Result_GET1_{id}
            /// Result_GET2_{id}
            /// Result_POST_{id}
            /// 
            /// Kalles fra "utsiden". 
            /// Må plasseres på side som inkluderer jQuery og bpapi.js på siden.
            /// 
            /// Bruk 
            /// nameOfUnitTestFunctionGET1
            /// nameOfUnitTestFunctionGET2
            /// nameOfUnitTestFunctionPOST
            /// som inngangspunkt for å starte testfunksjoner.
            /// </summary>
            public string UnitTests;

            public HTMLClass(ApiCall apicall) => _apiCall = apicall;
           
            /// <summary>
            /// Benyttes for alle POST-eksempler (hidden / editable / HTML / Javascript)
            /// Returnerer streng med komplett lenke, og lagrer samtidig dokument med ferdig selvstendig HTML-side
            /// 
            /// Må kalles ETTER at _apiCall.Initialize er kalt
            /// </summary>
            /// <param name="apiCall"></param>
            /// <param name="exampleHtml"></param>
            /// <param name="detailsFolder"></param>
            /// <returns></returns>
            public void GenerateExamplesHtmlStandAlonePage(string documentationDetailsFolder) {
                if (string.IsNullOrEmpty(Samples)) return;

                var retval = new System.Text.StringBuilder();
                retval.Append(
                    "<html>\r\n<head>\r\n" +
                    "<link rel=\"stylesheet\" type=\"text/css\" href=\"/Content/site.css\">\r\n" +
                    "<script src=\"../scripts/bpapi-0.2.js\"></script>\r\n" +
                    "<script src=\"../scripts/jquery-1.8.2.min.js\"></script>\r\n" +
                    "<title>BPAPI method " + _apiCall.Method.Name + " test</title>\r\n" +
                    "</head>\r\n" +
                    "<body bgcolor=\"" + Util.Configuration.BackgroundColourHTML + "\">\r\n" +
                    "<p><a href=\"" + Util.Configuration.RootUrl + "\">BPAPI</a> <a href=\"" + Util.Configuration.RootUrl + "documentation/APIMethods.html\">Methods</a></p>" +
                    "<h1>Example details for BPAPI method " + _apiCall.Method.Name + "</h1>\r\n");

                // Lenke tilbake til hovedsiden for dette kallet.
                retval.Append("<p>Example details for " + _apiCall.Method.DocumentationLink + "</p>\r\n");
                retval.Append("<p>Syntax variant 1 (preferred REST-format): <b>" + _apiCall.syntax1 + "</b></p>\r\n");
                retval.Append("<p>Syntax variant 2 (traditional query-string): " + _apiCall.syntax2 + "</p>\r\n");
                retval.Append("<p>" + DataReturnedHTML + "</p>");

                if (!string.IsNullOrEmpty(_apiCall.ApiFunctionCall.comment)) retval.Append("<p>" + _apiCall.ApiFunctionCall.comment + "</p>\r\n"); // helpText gjelder KALLET, ikke SYNTAKSEN!

                retval.Append(Samples);

                if (_apiCall.Method.HttpMethods.Contains(HTTPMethod.POST)) retval.Append("<br>\r\n" + GetBulkUsageStandaloneLinkWithLinkText());

                retval.Append("<p>Documentation and tests automatically generated from source-code " + DateTime.Now.ToString(DateTimeResolution.DateHourMin) + "</p>\r\n");
                retval.Append("</body>\r\n</html>\r\n");

                System.IO.File.WriteAllText(documentationDetailsFolder + _apiCall.Path.GetExampleStandaloneFilename(), retval.ToString());
            }

            /// <summary>
            /// Bulk update
            /// Returnerer streng med komplett lenke, og lagrer samtidig dokument med ferdig selvstendig HTML-side
            /// 
            /// </summary>
            /// <param name="apiCall"></param>
            /// <param name="exampleHtml"></param>
            /// <param name="detailsFolder"></param>
            /// <returns></returns>
            public void GenerateBulkUsageStandaloneHTMLStandAlonePage(string documentationDetailsFolder) {
                if (!_apiCall.Method.HttpMethods.Contains(HTTPMethod.POST)) return;

                if (string.IsNullOrEmpty(BulkUsage)) throw new Exception("string.IsNullOrEmpty(BulkUsageStandaloneHTML) for " + _apiCall.Method.Name);

                var retval = new System.Text.StringBuilder();
                retval.Append(
                    "<html>\r\n<head>\r\n" +
                    "<link rel=\"stylesheet\" type=\"text/css\" href=\"/Content/site.css\">\r\n" +
                    "<script src=\"../scripts/bpapi-0.2.js\"></script>\r\n" +
                    "<script src=\"../scripts/jquery-1.8.2.min.js\"></script>\r\n" +
                    "<title>BPAPI method " + _apiCall.Method.Name + " bulk usage</title>\r\n" +
                    "</head>\r\n" +
                    "<body bgcolor=\"" + Util.Configuration.BackgroundColourHTML + "\">\r\n" +
                    "<p><a href=\"" + Util.Configuration.RootUrl + "\">BPAPI</a> <a href=\"" + Util.Configuration.RootUrl + "documentation/APIMethods.html\">Methods</a></p>" +
                    "<h1>Bulk usage for BPAPI method " + _apiCall.Method.Name + "</h1>\r\n");

                // Lenke tilbake til hovedsiden for dette kallet.
                retval.Append("<p>Bulk usage for " + _apiCall.Method.Name + "</p>\r\n");
                retval.Append("<p>Syntax: " + _apiCall.syntax1 + "</p>\r\n");

                retval.Append(BulkUsage);

                retval.Append("<p>Documentation and tests automatically generated from source-code " + DateTime.Now.ToString(DateTimeResolution.DateHourMin) + "</p>\r\n");
                retval.Append("</body>\r\n</html>\r\n");

                System.IO.File.WriteAllText(documentationDetailsFolder + _apiCall.Path.GetBulkUsageStandaloneFilename(), retval.ToString());
            }

            private string CreateListOfParameterNames(List<Parameter> parameters) {
                var retval = new System.Text.StringBuilder();
                parameters.ForEach(p => {
                    if (retval.Length > 0) retval.Append(", ");
                    retval.Append(p.Name);
                });
                return retval.ToString();
            }

            private string CreateJavascriptArrayWithParameterNames(List<Parameter> parameters) {
                var retval = new System.Text.StringBuilder();
                parameters.ForEach(p => {
                    if (retval.Length > 0) retval.Append(", ");
                    retval.Append("'" + p.Name + "'");
                });
                return "[" + retval.ToString() + "]";
            }
        }

        public class CodeClass {

            private ApiCall _apiCall;

            private string _dataReturnedJavaDocComment;
            /// <summary>
            /// Informasjon om data som returneres. JavaDoc format (begynner med * og slutter med linjeskift)
            /// 
            /// Bygges opp fra Asserts i dokumentasjonen. 
            /// </summary>
            public string DataReturnedJavaDocComment {
                get {
                    if (_dataReturnedJavaDocComment != null) return _dataReturnedJavaDocComment;
                    var sbDataReturnedJavaDocComment = new System.Text.StringBuilder();
                    _apiCall.Asserts.ForEach(a => {
                        if (!string.IsNullOrEmpty(a.dataAsserted)) {
                            sbDataReturnedJavaDocComment.Append("* " + a.dataAsserted + (string.IsNullOrEmpty(a.comment) ? "" : (":" + a.comment)) + "\r\n");
                        }
                    });
                    if (sbDataReturnedJavaDocComment.Length > 0) {
                        sbDataReturnedJavaDocComment.Insert(0, "\r\n* Examples of data returned:\r\n");
                        sbDataReturnedJavaDocComment.Append("\r\n");
                    }
                    _dataReturnedJavaDocComment = sbDataReturnedJavaDocComment.ToString();
                    return _dataReturnedJavaDocComment;
                }
            }

            /// <summary>
            /// Navn på funksjon som kalles utenfra for å kjøre unit-tester.
            /// </summary>
            public string NameOfUnitTestFunctionGET1 => "APITest_GET1_" + _apiCall.IdUniqueGlobal;
            public string NameOfUnitTestFunctionGET2 => "APITest_GET2_" + _apiCall.IdUniqueGlobal;
            public string NameOfUnitTestFunctionPOST => "APITest_POST_" + _apiCall.IdUniqueGlobal;

            public long CountOfUnitTests {
                get {
                    var retval = 0;
                    if (_apiCall.Method.HttpMethods.Contains(HTTPMethod.GET)) retval += 2; // Syntax1 og Syntax2 (GET1 / GET2)
                    if (_apiCall.Method.HttpMethods.Contains(HTTPMethod.POST)) retval += 1;

                    return retval;
                }
            }

            private Dictionary<Usages, string> _javascriptAsserts = new Dictionary<Usages, string>();
            /// <summary>
            /// Returnerer Javascript kode med asserts (funksjonskall pluss kommentar)
            /// </summary>
            /// <param name="usage"></param>
            /// <returns></returns>
            public string GetJavascriptAsserts(Usages usage) {                
                if (_javascriptAsserts.TryGetValue(usage, out var retval)) return retval;
                var list = new List<Tuple<string, string>>();
                var alreadyAsserted = new HashSet<string>();
                _apiCall.Asserts.ForEach(a => { // Bygge opp liste først
                    var assert = a.JavascriptAsserts(usage, alreadyAsserted);
                    if (!string.IsNullOrEmpty(assert)) list.Add(new Tuple<string, string>(assert, a.comment));
                });
                var sbAsserts = new StringBuilder();
                if (list.Count > 0) {
                    sbAsserts.Append("      asserts: [\r\n");
                    for (var i = 0; i < list.Count; i++) {
                        var function = list[i].Item1;
                        var comment = list[i].Item2;
                        sbAsserts.Append(function);
                        if ((i + 1) < list.Count) {
                            sbAsserts.Append(", ");
                        }
                        sbAsserts.Append((string.IsNullOrEmpty(comment) ? "" : " // " + comment) + "\r\n");
                    };
                    sbAsserts.Append("\r\n      ], \r\n");
                }
                _javascriptAsserts[usage] = sbAsserts.ToString();
                return sbAsserts.ToString();
            }

            public CodeClass(ApiCall apicall) => _apiCall = apicall;
           
            /// <summary>
            /// Denne benyttes når vi ønsker at [ADD_UNIQUENESS] skal bestå
            /// Bemerk at gir heller ikke UrlEncoding for parameterne.
            /// 
            /// Brukes pr des 2013 kun i HTML-siden for unit-testing
            /// </summary>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public string CreateParametersAsQueryStringNoUniqueness() {
                var retval = new System.Text.StringBuilder();
                _apiCall.Parameters.ForEach(p => retval.Append((retval.Length == 0 ? "?" : "&") + p.Name + "=" + p.Value));
                return retval.ToString();
            }
        }

        public class Parameter {
            public string Name { get; set; }
            public string Value { get; set; }

            /// <summary>
            /// Benyttes av egenskapen ValueWithUniqeness som øker uniqenessCounter hver gang den støter på parameter value [ADD_UNIQUENESS] 
            /// </summary>
            private static long uniqenessCounter = 0;

            /// <summary>
            /// Returnerer verdi som med all overveiende grad av sannsynlighet vil være unik i den aktuelle situasjonen
            /// 
            /// I forbindelse med testing er det viktig å kunne for eksempel sette inne en ny kunde med unik e-post addresse
            /// Legger vil uniqueness i form av sekunder siden 1.1.2013 + løpende teller modulo 100.
            /// (sistnevnte sikrer at når vi genererer flere tester "rett etter hverandre" så får hver av dem
            /// unike verdier)
            /// </summary>
            public string ValueWithUniqeness {
                get {
                    if (Value.Contains("[ADD_UNIQUENESS]")) {
                        uniqenessCounter++;
                        return Value.Replace("[ADD_UNIQUENESS]",
                            ((int)DateTime.Now.Subtract(new DateTime(2013, 1, 1)).TotalSeconds).ToString() + "_" +
                            (uniqenessCounter % 100).ToString("00"));
                    } else {
                        return Value;
                    }
                }
            }

            public string ValueUrlDecoded => Util.UrlDecodeAdditional(System.Web.HttpUtility.UrlDecode(ValueWithUniqeness));
                
            /// <summary>
            /// Bemerk at kaller selv valueUrlDecoded først. 
            /// </summary>
            public string ValueHtmlEncoded => ValueUrlDecoded.HTMLEncode();
        }
    }
}