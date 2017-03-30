﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// Generates <see cref="ResponseFormat.JSON"/>-view of results.
    /// </summary>
    public class JSONView : BaseView { 
        public JSONView(Request request) : base(request) { }

        /// <summary>
        /// Only to be used in emergencies. Last resort for getting some useful information back to client in situations where
        /// unable to generate a standard AgoRapide-response through the <see cref="Request.GetResponse"/>-mechanism.
        /// </summary>
        /// <param name="resultCode"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static object GenerateEmergencyResult(ResultCode resultCode, string message) => 
            new System.Web.Mvc.JsonResult { // Without method we can not construct a Request object. Send emergency response as JSON only.
                Data = new {
                    ResultCode = ResultCode.exception_error.ToString(),
                    ResultCodeDescription = ResultCode.exception_error.GetAgoRapideAttributeT().A.Description,
                    Message = System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + message
                }
            };
        
        /// <summary>
        /// Note use of <see cref="JSONView.GenerateEmergencyResult"/> in case of an exception occurring. 
        /// (In other words this method tries to always return some useful information)
        /// </summary>
        /// <returns></returns>
        public override object GenerateResult() {
            try {
                if (Request.Result == null) {
                    dynamic json = new System.Web.Helpers.DynamicJsonObject(new Dictionary<string, object>());
                    json[nameof(ResultCode)] = ResultCode.exception_error.ToString();
                    json[nameof(CoreProperty.Message)] = "ERROR: No result-object available, very unexpected";
                    return new System.Web.Mvc.JsonResult { Data = json };
                } else {
                    return Request.Result.ToJSONDetailed(Request);
                }
            } catch (Exception ex) {
                Util.LogException(ex);
                return GenerateEmergencyResult(ResultCode.exception_error, "An exception of type " + ex.GetType() + " occurred in " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + ". See logfile on server for details"); // Details: " + Util.GetExeptionDetails(ex)); // Careful, do not give out details now
            }
        }
    }
}
