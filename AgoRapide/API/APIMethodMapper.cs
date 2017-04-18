using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using AgoRapide.Core;
using AgoRapide;

namespace AgoRapide.API {

    public static class APIMethodMapper { 
        /// <summary>
        /// Maps the given <paramref name="routes"/> through <paramref name="config"/> (<see cref="HttpConfiguration"/>). 
        /// 
        /// First priority goes to HTML routes (everything ending with <see cref="ConfigurationAttribute.HTMLPostfixIndicator"/> ("/HTML")
        /// Last priority is given to the route served by <see cref="CoreMethod.GenericMethod"/>
        /// 
        /// Note how <see cref="Util.Configuration"/>.<see cref="ConfigurationAttribute.ApiPrefix"/> (default "api/") will be prepended to every route, 
        /// expect <see cref="CoreMethod.RootIndex"/>
        /// 
        /// See also <see cref="CoreMethod.GenericMethod"/> (<see cref="BaseController.AgoRapideGenericMethod"/>)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="routes"></param>
        public static void MapHTTPRoutes(
            HttpConfiguration config,
            List<APIMethod> routes) {

            var apiPrefix = Util.Configuration.A.ApiPrefix;

            // Keep track of name and routeTemplate and catch any duplicates inside this method with friendly error messages
            // (instead of those produced by .NET which are more difficult to debug). 
            var tempNames = new Dictionary<string, APIMethod>();
            var tempTemplates = new Dictionary<string, APIMethod>();
            var tempMappings = new List<(string name, string routeTemplate, object defaults)>();

            var tempMapper = new Action<APIMethod, string, string, object>((method, name, routeTemplate, defaults) => {
                tempNames.AddValue(name, method, () => "\r\nName collision.\r\nNew method: " + method.ToString() + "\r\nExisting method: " + tempNames[name].ToString());
                tempTemplates.AddValue(routeTemplate, method, () => "\r\nRoute template collision.\r\nNew method: " + method.ToString() + "\r\nExisting method: " + tempTemplates[routeTemplate].ToString());
                tempMappings.Add((name, routeTemplate, defaults));
            });

            var nonCore = routes.Where(r => {
                switch (r.A.CoreMethod) {
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
            nonCore.ForEach(r => r.RouteTemplates.ForEach(t => tempMapper(r, t + "_" + Util.Configuration.A.HTMLPostfixIndicatorWithoutLeadingSlash, (Util.Configuration.A.ApiPrefix + t + Util.Configuration.A.HTMLPostfixIndicator).Replace("//", "/"), r.Defaults)));
            nonCore.ForEach(r => r.RouteTemplates.ForEach(t => tempMapper(r, t, Util.Configuration.A.ApiPrefix + t, r.Defaults)));

            var singleFinder = new Func<CoreMethod, APIMethod>(coreMethod => {
                return routes.Single(r => r.A.CoreMethod == coreMethod, () =>
                "Looking for " + typeof(CoreMethod) + "." + CoreMethod.RootIndex + ". " +
                "Possible resolution: " +
                "One of your Controller methods must be marked with [" + nameof(APIMethodAttribute) + "(" + nameof(APIMethodAttribute.CoreMethod) + " = " + nameof(CoreMethod) + "." + coreMethod + "... " +
                "(and remember to include that Controller's type in your call to " + nameof(APIMethod) + "." + nameof(APIMethod.CreateSemiAutogeneratedMethods) + ")");
            });

            var rootIndex = singleFinder(CoreMethod.RootIndex);
            tempMapper(rootIndex, nameof(CoreMethod) + "_" + CoreMethod.RootIndex, "", rootIndex.Defaults);
            tempMapper(rootIndex, nameof(CoreMethod) + "_" + CoreMethod.RootIndex + "_" + Util.Configuration.A.HTMLPostfixIndicatorWithoutLeadingSlash, Util.Configuration.A.HTMLPostfixIndicatorWithoutLeadingSlash, rootIndex.Defaults);

            var genericMethod = singleFinder(CoreMethod.GenericMethod);
            if (genericMethod.RouteTemplates != null && string.Join(",", genericMethod.RouteTemplates) != Util.Configuration.A.GenericMethodRouteTemplate) {
                throw new APIMethod.MethodInitialisationException(typeof(CoreMethod) + "." + genericMethod.A.CoreMethod + "'s " + nameof(APIMethod.RouteTemplates) + " should have been set to " + nameof(ConfigurationAttribute) + "." + nameof(ConfigurationAttribute.GenericMethodRouteTemplate) + " (" + Util.Configuration.A.GenericMethodRouteTemplate + "), not " + string.Join(",", genericMethod.RouteTemplates) + ". This is most probably a bug in AgoRapide framework");
            }
            tempMapper(genericMethod, nameof(CoreMethod) + "_" + CoreMethod.GenericMethod, Util.Configuration.A.GenericMethodRouteTemplate, genericMethod.Defaults);

            // It should now be safe to call config.Routes.MapHttpRoute
            tempMappings.ForEach(m => {
                config.Routes.MapHttpRoute(name: m.name, routeTemplate: m.routeTemplate, defaults: m.defaults);
            });
        }
    }
}
