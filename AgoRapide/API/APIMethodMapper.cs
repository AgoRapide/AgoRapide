﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using AgoRapide.Core;
using AgoRapide;

namespace AgoRapide.API {

    public static class APIMethodMapper<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        /// <summary>
        /// Maps the given <paramref name="routes"/> through <paramref name="config"/> (<see cref="HttpConfiguration"/>). 
        /// 
        /// First priority goes to HTML routes (everything ending with <see cref="Configuration.HTMLPostfixIndicator"/> ("/HTML")
        /// Last priority is given to the route served by <see cref="CoreMethod.GenericMethod"/>
        /// 
        /// Note how <see cref="Util.Configuration"/>.<see cref="Configuration.ApiPrefix"/> (default "api/") will be prepended to every route, 
        /// expect <see cref="CoreMethod.RootIndex"/>
        /// 
        /// See also <see cref="CoreMethod.GenericMethod"/> (<see cref="BaseController{TProperty}.AgoRapideGenericMethod"/>)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="routes"></param>
        public static void MapHTTPRoutes(
            HttpConfiguration config,
            List<APIMethod<TProperty>> routes) {

            var apiPrefix = Util.Configuration.ApiPrefix;

            // Keep track of name and routeTemplate and catch any duplicates inside this method with friendly error messages
            // (instead of those produced by .NET which are more difficult to debug). 
            var tempNames = new Dictionary<string, APIMethod<TProperty>>();
            var tempTemplates = new Dictionary<string, APIMethod<TProperty>>();
            var tempMappings = new List<Tuple<string, string, object>>();
            var tempMapper = new Action<APIMethod<TProperty>, string, string, object>((method, name, routeTemplate, defaults) => {
                tempNames.AddValue(name, method, () => "\r\nName collision.\r\nNew method: " + method.ToString() + "\r\nExisting method: " + tempNames[name].ToString());
                tempTemplates.AddValue(routeTemplate, method, () => "\r\nRoute template collision.\r\nNew method: " + method.ToString() + "\r\nExisting method: " + tempTemplates[routeTemplate].ToString());
                tempMappings.Add(new Tuple<string, string, object>(name, routeTemplate, defaults));
            });

            var nonCore = routes.Where(r => {
                switch (r.A.A.CoreMethod) {
                    //case CoreMethod.None: 
                    //    // TODO: GET RID OF THESE SPECIAL CASES. Mark MethodAttribute instead (that is, mark RootIndex and GenericMethod with
                    //    // TODO: something). AND DO NOT CARE ABOUT THE REST!
                    //case CoreMethod.EntityIndex:       // There is nothing special with this method routing-wise
                    //case CoreMethod.MethodIndex:       // There is nothing special with this method routing-wise
                    //case CoreMethod.PropertyIndex:     // There is nothing special with this method routing-wise
                    //case CoreMethod.ExceptionDetails:  // There is nothing special with this method routing-wise
                    //case CoreMethod.HTTPStatus:        // There is nothing special with this method routing-wise
                    //    return true;
                    //// TODO: GET RID OF THESE SPECIAL CASES. Mark MethodAttribute instead (that is, mark RootIndex and GenericMethod with
                    //// TODO: something). AND DO NOT CARE ABOUT THE REST!
                    case CoreMethod.RootIndex:
                    case CoreMethod.GenericMethod: return false;
                    default: return true;
                        //// TODO: GET RID OF THESE SPECIAL CASES. Mark MethodAttribute instead (that is, mark RootIndex and GenericMethod with
                        //// TODO: something). AND DO NOT CARE ABOUT THE REST!
                        //default: throw new InvalidEnumException(r.A.A.CoreMethod);
                }
            });
            nonCore.ForEach(r => r.RouteTemplates.ForEach(t => tempMapper(r, t + "_" + Util.Configuration.HTMLPostfixIndicatorWithoutLeadingSlash, (Util.Configuration.ApiPrefix + t + Util.Configuration.HTMLPostfixIndicator).Replace("//", "/"), r.Defaults)));
            nonCore.ForEach(r => r.RouteTemplates.ForEach(t => tempMapper(r, t, Util.Configuration.ApiPrefix + t, r.Defaults)));

            var singleFinder = new Func<CoreMethod, APIMethod<TProperty>>(coreMethod => {
                return routes.Single(r => r.A.A.CoreMethod == coreMethod, () =>
                "Looking for " + typeof(CoreMethod) + "." + CoreMethod.RootIndex + ". " +
                "Possible resolution: " +
                "One of your Controller methods must be marked with [" + nameof(MethodAttribute) + "(" + nameof(MethodAttribute.CoreMethod) + " = " + nameof(CoreMethod) + "." + coreMethod + "... " +
                "(and remember to include that Controller's type in your call to " + nameof(APIMethod<TProperty>) + "." + nameof(APIMethod<TProperty>.CreateSemiAutogeneratedMethods) + ")");
            });

            var rootIndex = singleFinder(CoreMethod.RootIndex);
            tempMapper(rootIndex, nameof(CoreMethod) + "_" + CoreMethod.RootIndex, "", rootIndex.Defaults);
            tempMapper(rootIndex, nameof(CoreMethod) + "_" + CoreMethod.RootIndex + "_" + Util.Configuration.HTMLPostfixIndicatorWithoutLeadingSlash, Util.Configuration.HTMLPostfixIndicatorWithoutLeadingSlash, rootIndex.Defaults);

            var genericMethod = singleFinder(CoreMethod.GenericMethod);
            if (genericMethod.RouteTemplates != null && string.Join(",", genericMethod.RouteTemplates) != Util.Configuration.GenericMethodRouteTemplate) {
                throw new APIMethod<TProperty>.MethodInitialisationException(typeof(CoreMethod) + "." + genericMethod.A.A.CoreMethod + "'s " + nameof(APIMethod<TProperty>.RouteTemplates) + " should have been set to " + nameof(Configuration) + "." + nameof(Configuration.GenericMethodRouteTemplate) + " (" + Util.Configuration.GenericMethodRouteTemplate + "), not " + string.Join(",", genericMethod.RouteTemplates) + ". This is most probably a bug in AgoRapide framework");
            }
            tempMapper(genericMethod, nameof(CoreMethod) + "_" + CoreMethod.GenericMethod, Util.Configuration.GenericMethodRouteTemplate, genericMethod.Defaults);

            // It should now be safe to call config.Routes.MapHttpRoute
            tempMappings.ForEach(m => {
                config.Routes.MapHttpRoute(name: m.Item1, routeTemplate: m.Item2, defaults: m.Item3);
            });
        }
    }
}