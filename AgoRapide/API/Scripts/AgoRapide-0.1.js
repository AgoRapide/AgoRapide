/*
    AgoRapide JavaScript Library version 0.1
    http://AgoRapide.com/
    Author: Bjørn Erling Fløtten

    MIT License
    Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

var com;
if (!com) {
    com = {};
} else if (typeof com != 'object') {
    throw new Error("com already exists and is not an object.");
}
if (!com.AgoRapide) {
    com.AgoRapide = {};
} else if (typeof com.AgoRapide != 'object') {
    throw new Error("com.AgoRapide already exists and is not an object.");
}

com.AgoRapide.AgoRapide = function comAgoRapideAgoRapide() {
    log("Initializing AgoRapide Library");
    var updateIndicator = {};
    var namespace = "com.AgoRapide.AgoRapide";
    var doLogging = true;

    /* 
        -----------------------------
        Internal variables and functions
        -----------------------------
    */
    var baseUrl = "http://" + window.location.host + "/api/";
    log("default baseUrl: " + baseUrl);
    log("window.location.host: " + window.location.host);

    var errorReporter = undefined;
    log("errorReporter has been set to " + errorReporter);


    function log(text) {
        if (!doLogging) return;
        if (text == undefined) text = "";
        var name = "";
        var end = "";
        if (arguments.callee.caller != null) {
            name = arguments.callee.caller.name + "(" + name;
            end += ")";
            if (arguments.callee.caller.caller != null) {
                name = arguments.callee.caller.caller.name + "(" + name;
                end += ")";
                if (arguments.callee.caller.caller.caller != null) {
                    name = arguments.callee.caller.caller.caller.name + "(" + name;
                    end += ")";
                    if (arguments.callee.caller.caller.caller.caller != null) {
                        name = arguments.callee.caller.caller.caller.caller.name + "(" + name;
                        end += ")";
                        if (arguments.callee.caller.caller.caller.caller.caller != null) {
                            name = arguments.callee.caller.caller.caller.caller.caller.name + "(" + name;
                            end += ")";
                            if (arguments.callee.caller.caller.caller.caller.caller.caller != null) {
                                name = arguments.callee.caller.caller.caller.caller.caller.caller.name + "(" + name;
                                end += ")";
                            }
                        }
                    }
                }
            }
        }
        name += end;
        console.log(namespace + " " + name + ": " + text);
    }

    function logAndThrow(text) {
        log("__ERROR__: " + text);
        log("Will now throw");
        throw text;
    }

    function logIfDefined(text, explanation) {
        if (text == undefined) return;
        if (explanation == undefined) {
            log(text);
        } else {
            log(explanation + ": " + text);
        }
    }

    function explainException(exception) {
        log("Exception occurred in " + namespace); // TODO: Remove logging of namespace if this is to be called externally also
        exception = exception.toString();
        log("Exception: " + exception);
    }

    // Returns a short description of what kind of data was returned
    // TODO: EXPAND ON THIS
    function explainData(data) {
        if (data == undefined) return "data is undefined, unable to explain";
        if (data.Data.scalar_result != undefined) return "scalar_result: " + data.Data.scalar_result;
        if (data.Data.agorapide_array != undefined) return "agorapide_array: " + data.Data.AgoRapideArray.length + " elements";
        return "Unable to explain type of data that was returned";
    }

    function assertDefinedThrowIfNot(value, description) {
        if (value != undefined) return;
        var msg = "";
        if (description == undefined) {
            msg = "AssertDefined failed, undefined value. Parameter 'description' is missing, unable to explain describe value";
        } else {
            msg = "AssertDefined failed, The value '" + description + "' was not defined";
        }
        log(msg);
        log("Will now throw");
        throw msg;
    }

    function assertDefined(value, description) {
        if (value != undefined) return true;
        if (description == undefined) {
            log("FAIL! Undefined value. Parameter 'description' is missing, unable to explain which value was undefined");
            return false;
        } else {
            log("FAIL! Value '" + description + "' was not defined");
            return false;
        }
    }

    function assertValue(foundValue, requiredValue, description) {
        if (foundValue != requiredValue) {
            log("AssertValue failed. The found value (" + foundValue + ") does not correspond to the required value (" + requiredValue + ")");
            log("Description: " + description);
            return false;
        }
        // log("Assert succeeded for " + description);
        return true;
    }

    /*
      Convenience method that logs information about call gone wrong
      Calls errorReporter (with one string-parameter) if that function is defined.
      Calls parameters.error (parameterless) if that function is defined.
    */
    function callError(reason, parameters) {
        log("parameters.url: " + parameters.url);
        if (parameters.data != undefined) log("parameters.data: " + parameters.data);
        if (parameters.type != undefined) log("parameters.type: " + parameters.type);
        log("Error because of: " + reason);
        if (parameters.error == undefined) {
            log("parameters.error == undefined (no function to call)");
        } else {
            log("Calling " + parameters.error.name + "()");
            parameters.error(reason);
        }
        if (errorReporter != undefined) {
            log("Calling errorReporter " + errorReporter.name + "()");
            errorReporter(reason);
        }
    }

    /*
      See externally callable function for documentation
    */
    function call(parameters) {

        assertDefinedThrowIfNot(parameters.url, "parameters.url");
        assertDefinedThrowIfNot(parameters.success, "parameters.success");

        if (parameters.log == true) {
            logIfDefined(parameters.url, 'parameters.url');
            logIfDefined(parameters.method, 'parameters.method');

            if (parameters.data != undefined) {
                log("typeof(parameters.data): " + typeof (parameters.data));
            }
            logIfDefined(parameters.data, 'parameters.data');
        }

        var addBaseUrl = false;

        if (parameters.url.length < 7) {
            addBaseUrl = true;
        } else if (!((parameters.url.substring(0, 7).toLowerCase() == "http://") || (parameters.url.substring(0, 8).toLowerCase() == "https://"))) {
            addBaseUrl = true;
        }

        if (addBaseUrl) {
            if (baseUrl == undefined) {
                callError(
                    "parameters.url (" + parameters.url + ") does not start with 'http' or 'https' and baseUrl was not defined. " +
                    "Either set parameters.url to a complete URL or define baseUrl like com.AgoRapide.AgoRapide.setBaseUrl('http://AgoRapide.AgoRapide.com/api/')",
                    parameters);
                return;
            }
            parameters.url = baseUrl + parameters.url;
        }

        $.ajax({
            dataType: "json",
            url: parameters.url,
            type: parameters.type,
            data: parameters.data,
            async: parameters.async,

            // Authentication (Not optimal, will most probably wait for an 401 with WWW-Authenticate header BASIC)
            username: parameters.username,
            password: parameters.password,

            success: function callSuccess(data) {
                if (data.Data == undefined) {
                    callError("data.Data == undefined", parameters);
                } else if (data.Data.ResultCode == undefined) {
                    callError("data.Data.ResultCode == undefined", parameters);
                } else if (data.Data.ResultCode == "ok") {
                    parameters.success(data);
                } else {
                    var additionalText = "";
                    // Get additional details through Details.Properties["Message"].Value 
                    if (data.Data.Details == undefined) {
                        // Give up
                    } else if (data.Data.Details.Properties == undefined) {
                        // Give up
                    } else if (data.Data.Details.Properties["Message"] == undefined) {
                        // Give up
                    } else {
                        additionalText = data.Data.Details.Properties["Message"].Value;
                    }
                    // Note that Details.Properties["ResultCodeDescription"].Value is not available for JSON
                    callError(data.Data.ResultCode + "\r\nDetails:\r\n" + additionalText, parameters) // 
                }
            },
            error: function callError2(jqXHR, textStatus, errorThrown) { // Must be named callError2!!!. callError is already in use.
                if (jqXHR.status == 0) {
                    log("jqXHR.status " + jqXHR.status + ". Possible cause: Cross-site scripting problem. For instance, are you making API-calls to another domain than the domain from which this HTML-page originates? Check Access-Control-Allow-Origin header.");
                    log("TIP: If you are testing with _script_testing.html, do not load it as file:/// into the browser but as http://localhost/...");
                    log("TIP: If you are developing, ensure that you remembered to start the AgoRapide-project (the IIS Application)");
                }
                log("jqXHR.status: " + jqXHR.status);
                log("jqXHR.statusText: " + jqXHR.statusText);
                log("textStatus: " + textStatus);
                log("errorThrown: " + errorThrown);
                log("jqXHR.responseText: " + jqXHR.responseText);
                callError("[See already logged information, press F12 for Console]", parameters);
            }
        });
    }

    /*
      See externally callable function for documentation
    */
    var lastValidGeneralQueryCriteria = "";
    function generalQuery(parameters) {
        var statusHtml = function (html) {
            if (parameters.statusHtml == undefined) return;
            parameters.statusHtml(html);
        };
        if (lastValidGeneralQueryCriteria != parameters.generalQueryId) { // User has most probably pressed an additional key since "our" search was set up
            log('Not executing general query for ' + parameters.generalQueryId);
            statusHtml('Not executing general query for ' + parameters.generalQueryId); return;
        }

        if (parameters.generalQueryId == '') {
            statusHtml(''); return;
        }

        log("Executing general query for '" + parameters.generalQueryId + "', lastValidPersonGeneralQueryId: '" + lastValidGeneralQueryCriteria + "'");
        statusHtml('generalQueryId: ' + parameters.generalQueryId);

        com.AgoRapide.AgoRapide.call({
            url: 'GeneralQuery/' + encodeURIComponent(parameters.generalQueryId),
            success: function (data) {
                if (lastValidGeneralQueryCriteria != parameters.generalQueryId) { // User has most probably pressed an additional key since "our" general query was set up
                    log('Ignoring success for ' + parameters.generalQueryId); return;
                }
                statusHtml('');
                if (data.Data.MultipleEntities == undefined) {
                    statusHtml('Unknown type of result');
                    return;
                }
                var result = '';
                for (var i = 0; i < data.Data.MultipleEntities.length; i++) {
                    var e = data.Data.MultipleEntities[i]['Properties'];
                    var url = e['SuggestedUrl'].Value;
                    var description = e['Description'].Value;
                    result += '<a href="' + url + '/HTML">' + description + '</a>&nbsp;&nbsp;';
                }

                statusHtml(result);
            },
            error: function () {
                if (lastValidGeneralQueryCriteria != parameters.generalQueryId) { // User has most probably pressed an additional key since "our" general query was set up
                    log('Ignoring error for ' + parameters.generalQueryId); return;
                }
                com.AgoRapide.AgoRapide.log('Error when executing general query for ' + parameters.generalQueryId);
                statusHtml('');
            }
        });
    }

    /* 
        -----------------------------
        External callable functions
        -----------------------------
    */
    return {
        enableLogging: function () {
            doLogging = true;
        },

        disableLogging: function () {
            doLogging = false;
        },

        /*          
          The errorReporter is a client defined function with one string-parameter (detailing the error-condition) that will be called whenever AgoRapide-calls fail with code other than OK
          (Normally for com.AgoRapide.AgoRapide-methods the parameters.error callback function is parameterless in order to simplify the code, setting a global callback function makes
          it possible to get more detailed information about what went wrong)
        */
        setErrorReporter: function setErrorReporter(reporter) {
            errorReporter = reporter;
            log("errorReporter has been set (by external call) to " + reporter.name);
        },

        /*
          baseUrl is something like http://AgoRapide.AgoRapide.com/api/
          When baseUrl is defined you may call the API like "Person/42" instead of "http://AgoRapide.AgoRapide.com/api/Person/42"

          You may also want to set base-URL to avoid same-origin policy restrictions for scripting.
        */
        setBaseUrl: function setBaseUrl(url) {
            baseUrl = url;
            log("baseUrl has been set (by external call) to " + baseUrl);
        },

        getBaseUrl: function () { return baseUrl; }, // You will most probably not need this information

        assertValue: function (foundValue, requiredValue, description) {
            return assertValue(foundValue, requiredValue, description);
        },

        /*
          Used mostly for unit-testing functions. Logs failures and returns false if not defined.
          See assertDefinedAndThrow if you need execution to be terminated if assert fails.
        */
        assertDefined: function (value, description) {
            return assertDefined(value, description);
        },

        /*
          Used mostly for AgoRapide internal functions. Throws a string describing problem
          See assertDefined if you do _not want execution to be terminated if assert fails.
        */
        assertDefinedThrowIfNot: function (value, description) {
            return assertDefinedThrowIfNot(value, description);
        },

        log: function (text) {
            return log(text);
        },

        logAndThrow: function (text) {
            return logAndThrow(text);
        },

        explainData: function (data) {
            return explainData(data);
        },

        logExplanation: function (data) {
            return log(explainData(data));
        },

        /*
           Executes jQuery call ($.ajax) with extensive validation and logging suited for AgoRapide.
                   
           parameters.url:
              URL to call
    
           parameters.success:
              Function with one parameter to call when success (ResultCode OK).

           If the parameter to the function parameters.success is called "data" you 
           will find ResultCode as data.Data.ResultCode.

           Likewise the result itself will be found as one or more of
               data.Data.MultipleEntities,
               data.Data.SingleEntity,
               data.Data.Details
           or similar.
        
           ResultCode is checked for OK and problems are logged in detail
           (this is the main advantage of this function compared to calling $.ajax direct)
    
           Optional parameters:
           ========================

           parameters.username
           parameters.password:
              If not set and code is run from a web browser then the browser
              will typically prompt for credentials when it gets HTTP response code 401 with WWW-Authenticate header BASIC-
              Optional. 
           
           parameters.error:
              function with one parameter 'text' that will be called if any problem occurs. Optional.

           parameters.log:
              Boolean. If true then detailed logging will be made. Optional, default = false.
    
           parameters.type:
              String. "GET", "POST", "PUT", or "DELETE". Optional, default value =  GET.
     .
           parameters.data:
              Query-field values as string with format
              "?parameter1=value1&parameter2=value2" or as object with the format 
              { parameter1: "value1", parameter2: "value2" } 
              In the first case you should url-encode the values with encodeURIComponent or similar.
              (set parameters.data when data is not included in url or when parameters.type is something other than GET).
              Optional. 
    
           parameters.async:
              Boolean. Set to false if you want a synchronous request. Optional, default value = false.
        */
        call: function (parameters) {
            call(parameters);
        },

        /* 
          Helper function for general query. Used by AgoRapide HTML admin-interface. Wrapper around api-method GeneralQuery/{GeneralQueryId}

          Every query is time-delayed by 700ms. If this function is called again within 700ms then the last query will not be executed.
          This function may therefore be called immediately for every keypress by the user without any performance hits. 

          parameters.generalQueryId: If identical to the last query id used then a new query will not be initiated.

          parameters.htmlStatus: Function receiving a string-parameter containing HTML-formatted result of search. 
          This function will be called repeatedly. 
          If the query was successful then the HTML result will contain complete links like to a api-method
          (typically like Person/{id}/UpdateProperty/EntityToRepresent/{queryResult}, these links are application dependent)
        */
        generalQuery: function (parameters) {
            if (lastValidGeneralQueryCriteria == parameters.generalQueryId) return;
            lastValidGeneralQueryCriteria = parameters.generalQueryId;
            var r = setTimeout(function () { generalQuery(parameters) }, 700);
        }
    }
} ();
