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
    /// Represents one function call in the documentation (Api / AssertB, AssertU, AssertS)
    /// 
    /// Se also ApiCall som representerer et helt eksempel (Sample)
    /// 
    /// TODO: Dec 2016. Split this class since Api og Assert+ is two fundamentally different things
    /// 
    /// Example 1 of function call:
    /// ----------------
    /// 
    /// Api("Person?id=42") // Ask for person 42
    /// which will be parsed like:
    ///   functionName: "Api"
    ///   Parameter: "Person?id=42"
    ///   comment: "Ask for person 42"
    ///   
    /// Note that only // is not allowed as comment since we may have // inside of API-call like
    /// Api("Person//Property...") for instance
    /// TODO: Create a better parser in order to avoid such situations (Dec 2016)
    /// 
    /// 
    /// Example 2 of function call:
    /// ----------------
    /// 
    /// AsssertB("com.agorapide.bapi.assertDefined(data.Data.person.Properties,'data.Data.person.Properties')")
    /// 
    /// assertDefined is a standard bapi.js Javascript function that takes two parameters, the expression to
    /// evaluate and a string explanation for the corresponding eror messages
    /// 
    /// It is possible to shorten the above code in the documentation down to:
    /// AsssertB("defined(data.Data.person.Properties,'data.Data.person.Properties')")   /// 
    /// and even:
    /// AsssertB("defined(component.Properties)")
    /// 
    /// The last one will be turned into the following:
    /// AsssertB("com.agorapide.bapi.assertDefined(data.Data.person,'data.Data.person')")
    /// AsssertB("com.agorapide.bapi.assertDefined(data.Data.person.Properties,'data.Data.person.Properties')")
    /// 
    /// In other words, you do not have to built up level by level of asserts in the documentation
    /// </summary>
    public class FunctionCall {

        /// <summary>
        /// Api, AssertB, AssertS, AssertU
        /// </summary>
        public string functionName;

        /// <summary>
        /// For functionName Api this would be the URL itself, for Assert+ it is a Javascript funksjon call (AssertValue or AssertDefined)
        /// </summary>
        public string parameter;

        /// <summary>
        /// Tekst that follows // plus space
        /// </summary>
        public string comment;

        /// <summary>
        /// Date to asserte. Example: "data.person.id".
        /// 
        /// This is a hack. Value is read after after call to JavascriptAsserts
        /// (TODO: MAKE MUCH NICER! But must then fix _A LOT_ IN THE parser for JavascriptAsserts
        /// 
        /// Used to document what kind of data an API-method returns.
        /// 
        /// Empty or null if this instance of FunctionCall does not represent a call to 
        /// com.agorapide.bapi.assertDefined
        /// 
        /// Remember to read the comment field
        /// </summary>
        public string dataAsserted;

        public FunctionCall(string functionNameRequired, string functionCall) {

            var pos = functionCall.LastIndexOf(" // ");
            if (pos > -1) {
                comment = functionCall.Substring(pos + 4).Trim();
                 comment = Util.Configuration.InsertWellKnownIds(comment);
                functionCall = functionCall.Substring(0, pos).Trim();
            }
            if ("Assert?".Equals(functionNameRequired)) {
                if (functionCall.StartsWith("AssertB(\"")) {
                    functionNameRequired = "AssertB";
                } else if (functionCall.StartsWith("AssertS(\"")) {
                    functionNameRequired = "AssertS";
                } else if (functionCall.StartsWith("AssertU(\"")) {
                    functionNameRequired = "AssertU";
                } else {
                    throw new Exception("Illegal assert (" + functionCall + "). Not one of AssertB(\", AssertS(\" or AssertU(\". Did you remember the quotation marks?");
                }
            }

            functionName = functionNameRequired;
            if (!functionCall.StartsWith(functionNameRequired + "(\"")) throw new Exception("!functionCall.StartsWith(functionName) (" + functionNameRequired + ") (" + functionCall + ")");
            parameter = functionCall.Substring((functionNameRequired + "(\"").Length);
            if (parameter.EndsWith("\");")) {
                parameter = parameter.Substring(0, parameter.Length - 3);
            } else if (parameter.EndsWith("\")")) {
                parameter = parameter.Substring(0, parameter.Length - 2);
            } else {
                throw new Exception("Unknown end of parameter, expected \") (" + parameter + ") (" + functionNameRequired + ") (" + functionCall + ")");
            }

            if (functionNameRequired.StartsWith("Assert")) {
                if (parameter.StartsWith("assertDefined(") || parameter.StartsWith("AssertDefined(")) {
                    parameter = "com.agorapide.bapi.assertDefined(" + parameter.Substring("assertDefined(".Length);
                } else if (parameter.StartsWith("defined(") || parameter.StartsWith("Defined(")) {
                    parameter = "com.agorapide.bapi.assertDefined(" + parameter.Substring("Defined(".Length);
                } else if (parameter.StartsWith("assertValue(") || parameter.StartsWith("AssertValue(")) {
                    parameter = "com.agorapide.bapi.assertValue(" + parameter.Substring("assertValue(".Length);
                } else if (parameter.StartsWith("value(") || parameter.StartsWith("Value(")) {
                    parameter = "com.agorapide.bapi.assertValue(" + parameter.Substring("Value(".Length);
                }
            }

            parameter = Util.Configuration.InsertWellKnownIds(parameter);
        }


        /// <summary>
        /// Returns complete Javascript code for asserts
        /// 
        /// If only one assert then will return:
        /// function(data) { return com.diggerin.dapi.assertDefined(data.Data.person,'data.Data.person'); } // Comment
        /// 
        /// If multiple then will return:
        /// function(data) { 
        ///     if (!com.agorapide.bapi.assertDefined(data.Data.person,'data.Data.person')) return false; 
        ///     if (!com.agorapide.bapi.assertDefined(data.Data.person.Properties,'data.Data.person.Properties')) return false; 
        ///     return true;
        /// } // Comment
        /// 
        /// </summary>
        /// <param name="alreadyAsserted">
        /// Does not return anything if already in this HashSet
        /// </param>
        /// <returns></returns>
        public string JavascriptAsserts(ApiCall.Usages usage, HashSet<string> alreadyAsserted) {
            if (!functionName.StartsWith("Assert")) {
                throw new Exception("Illegal functionName (" + functionName + "). Must start with AssertB/U/S");
            }

            switch (usage) {
                case ApiCall.Usages.Sample:
                    if (!(functionName.StartsWith("AssertB") || functionName.StartsWith("AssertS"))) return "";
                    break;
                case ApiCall.Usages.UnitTest:
                    if (!(functionName.StartsWith("AssertB") || functionName.StartsWith("AssertU"))) return "";
                    break;
                default:
                    throw new Exception("Illegal usage (" + usage.ToString() + ")");
            }

            if (!parameter.StartsWith("com.agorapide.bapi.assertDefined")) {
                // Use as is, this is not assertDefined function
                return "        function(data) { return " + parameter + "; } ";
            }

            if (parameter.StartsWith("com.agorapide.bapi.assertDefined(data.Data.")) {
                // Use as is, this is an already expanded assertDefined function
                dataAsserted = parameter.Substring("com.agorapide.bapi.assertDefined(".Length, parameter.Length - "com.agorapide.bapi.assertDefined(".Length - 1);
                return "        function(data) { return " + parameter + "; } ";
            }

            // It is only specified Defined(person) or Defined(person.Properties)
            // or similar. Insert multiple asserts as necessary:
            if (!parameter.StartsWith("com.agorapide.bapi.assertDefined(")) throw new Exception("!parameter.StartsWith(\"com.agorapide.bapi.assertDefined(\", " + parameter + " (functionName: " + functionName + ")");
            if (!parameter.EndsWith(")")) throw new Exception("!parameter.EndsWith(\")\"), " + parameter + " (functionName: " + functionName + ")");

            if (parameter.Contains("'")) throw new Exception("parameter.Contains(\"'\") (short-hand form should not include explanation of what is being asserted, the explanation will be inserted automatically) " + parameter + " (functionName: " + functionName + ")");

            // Find all asserts that are not already done:
            var lstAsserts = new List<string>();
            var property = new System.Text.StringBuilder();
            property.Append("data.Data");
            foreach (var assert in parameter.Substring("com.agorapide.bapi.assertDefined(".Length, parameter.Length - "com.agorapide.bapi.assertDefined(".Length - 1).Trim().Split('.')) {
                property.Append("." + assert);
                if (alreadyAsserted.Contains(property.ToString())) continue;
                lstAsserts.Add(property.ToString());
                alreadyAsserted.Add(property.ToString());
            }
            dataAsserted = property.ToString();

            switch (lstAsserts.Count) {
                case 0: return ""; // Litt spesielt men normalt nok.
                case 1: return "        function(data) { return com.agorapide.bapi.assertDefined(" + lstAsserts[0] + ", '" + lstAsserts[0] + "'); } ";
                default:
                    var retval = new System.Text.StringBuilder();
                    retval.Append("        function(data) {\r\n");
                    lstAsserts.ForEach(a => retval.Append("          if (!com.agorapide.bapi.assertDefined(" + a + ", '" + a + "')) return false;\r\n"));
                    retval.Append(
                        "          return true\r\n" +
                        "        }");
                    return retval.ToString();
            }
        }

    }
}
