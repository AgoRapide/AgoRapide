using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// TODO: REMOVE FROM REPOSITORY! NO LONGER IN USE!
    /// 
    /// TODO: Comments and code is a little bit primitive as of Dec 2016
    /// 
    /// Corresponds to one single example in documentation.
    /// 
    /// Note how an ApiSample may contain multiple ApiCall, each with its own asserts,
    /// but in most cases one ApiSample will contain only one ApiCall
    /// 
    /// Note that it is not logical to create ApiSamples with multiple ApiCalls
    /// because the final documentation is built up by syntax (see ApiMethod.GetAllOverloads og ApiMethod.GetApiCallsBySyntax)
    /// In other words, we use the information given per ApiCall not per ApiSample
    /// and therefore there is no point in the system allowing ApiSamples with multiple ApiCalls
    /// 
    /// SampleS = Sample only
    /// SampleU = Unit test only
    /// SampleB = Both sample and unit test (the most common occurence)
    /// 
    /// ------------------------------
    /// SampleX: 
    ///   Api("A....
    ///   AssertB("Defined(....
    ///   Assert...
    ///   Api("B....
    ///   AssertB("Defined(....
    /// ------------------------------
    /// </summary>
    public class ApiSample {

        private Method _method;

        private string _xmlDocumentation;

        /// <summary>
        /// Sample number within method
        /// </summary>
        private long sampleNo;

        /// <summary>
        /// Text after "SampleX: // "
        /// </summary>
        private string helpText;

        public ApiSample(Method method, string xmlDocumentation, long sampleNo) {
            _method = method;
            _xmlDocumentation = xmlDocumentation;
            this.sampleNo = sampleNo;
        }

        private List<ApiCall> _thisSampleApiCalls;
        /// <summary>
        /// Note how this also sets _HTMLSnippet (which in turn makes it legal to call ToHTMLSnippet)
        /// </summary>
        /// <param name="getNextAPICallId">Må være satt første gang vi kalles. Kan ellers være null</param>
        /// <returns></returns>
        public List<ApiCall> GetApiCalls(Func<int> getNextAPICallId) {
            if (_thisSampleApiCalls != null) return _thisSampleApiCalls;
            if (getNextAPICallId == null) throw new Exception("getNextAPICallId == null. GetApiCalls should have been called from ApiMethod.allApiCalls (which supports a getNextAPICallId)");

            _thisSampleApiCalls = new List<ApiCall>();

            var thisSample = new System.Text.StringBuilder();

            var lines = _xmlDocumentation.Split("\r\n");
            var line = lines[0];

            var usage = ApiCall.Usages.Both;
            if (line.StartsWith("SampleS:")) usage = ApiCall.Usages.Sample;
            if (line.StartsWith("SampleU:")) usage = ApiCall.Usages.UnitTest;

            var strHTMLSnippet = new System.Text.StringBuilder();

            helpText = null;
            var pos = line.LastIndexOf("//");
            if (pos > -1) helpText = line.Substring(pos + 2).Trim();
            if (helpText != null) {
                strHTMLSnippet.Append("<h2>Sample " + helpText + "</h2>\r\n");
            } else {
                strHTMLSnippet.Append("<h2>Sample " + sampleNo + "</h2>\r\n");
            }

            for (var i = 1; i < lines.Count; i++) {
                line = lines[i];
                if (string.IsNullOrEmpty(line.Trim())) {
                    // Ignorer
                } else if (line.StartsWith("  Api")) { // Eksempel: Api("Customer/AddProperty?property_name=first_name&value=John") // Sets first_name to 'John'<br>
                    line = line.Trim();
                    if (line.EndsWith("<br>")) line = line.Substring(0, line.Length - 4).Trim();
                    var apiCall = new ApiCall(_method, new FunctionCall("Api", line), usage);
                    _thisSampleApiCalls.Add(apiCall);
                } else if (line.StartsWith("  Assert")) {
                    if (_thisSampleApiCalls.Count == 0) throw new Exception("thisSampleApiCalls.Count == 0, item.value: \r\n" + _xmlDocumentation + ", at single line " + line);
                    _thisSampleApiCalls[_thisSampleApiCalls.Count - 1].Asserts.Add(new FunctionCall("Assert?", line.Trim()));
                } else {
                    throw new Exception("Unknown start of command (must start with '  Api... or '  Assert...) (line " + line + ")");
                }
            }
            if (_thisSampleApiCalls.Count == 0) throw new Exception("thisSampleApiCalls == 0, item.value: " + _xmlDocumentation);

            _thisSampleApiCalls.ForEach(a => {
                a.Initialize(getNextAPICallId);
                strHTMLSnippet.Append(a.HTML.Samples);
            });

            _HTMLSnippet = strHTMLSnippet.ToString();

            return _thisSampleApiCalls;
        }


        private string _HTMLSnippet;
        /// <summary>
        /// Returnerer HTML representasjon egnet til å plassere inne i resten av hjelpeteksten
        /// 
        /// Består av ExamplesStandaloneHTML for alle ApiCalls som vi finner
        /// </summary>
        /// <returns></returns>
        public string ToHTMLSnippet() {
            if (_HTMLSnippet != null) return _HTMLSnippet;
            throw new Exception("You must call GetApiCalls before ToHTMLSnippet (in order to give a getNextAPICallId-instance)"); // TODO: Dette er ikke særlig pent!
            //Document(null);
            //return _HTMLSnippet;
        }
    }
}
