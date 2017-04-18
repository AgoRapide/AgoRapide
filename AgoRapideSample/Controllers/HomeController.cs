﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.ComponentModel;
using AgoRapide;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapideSample {

    /// <summary>
    /// Contains methods that "must" always be implemented in your application. 
    /// </summary>
    public class HomeController : BaseController {

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [APIMethod(CoreMethod = CoreMethod.RootIndex)]
        public object RootIndex() {
            try {
                if (!TryGetRequest(out var request, out var completeErrorResponse)) return completeErrorResponse;
                request.ForceHTMLResponse(); // It is much more user friendly to have HTML respons always here. If JSON is needed it can always be obtained by querying api/Method/All or similar.
                // TODO: Replace this with dictionary with links
                // TODO: Like AllMethods, AllClassAndMethod, AllEnumClass
                return request.GetOKResponseAsMultipleEntities(APIMethod.AllMethods.Select(m => (BaseEntity)m).ToList());
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// Note how this method must be implemented in the application itself since it by nature is application specific. 
        /// In other words it can not be moved into <see cref="BaseController"/>. 
        /// 
        /// 
        /// </summary>
        /// <param name="GeneralQueryId"></param>
        /// <returns></returns>
        [HttpGet]
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.User)]
        [APIMethod(
            Description = "Returns all persons where one of -" + nameof(P.FirstName) + "-, -" + nameof(P.LastName) + "- or -" + nameof(P.Email) + "- matches {" + nameof(CoreP.GeneralQueryId) + "}",
            S1 = nameof(GeneralQuery), S2 = CoreP.GeneralQueryId, CoreMethod = CoreMethod.GeneralQuery)]
        public object GeneralQuery(string GeneralQueryId) {
            try {
                if (!TryGetRequest(GeneralQueryId, out var request, out var completeErrorResponse)) return completeErrorResponse;

                /// TODO: PostgreSQL specific? Where do we want to add this?
                /// TODO: Should we add a WILDCARD-parameter to <see cref="QueryIdKeyOperatorValue"/>.
                if (!GeneralQueryId.EndsWith("%")) GeneralQueryId += "%";

                QueryId queryId = new QueryIdKeyOperatorValue(new List<AgoRapideAttributeEnriched> {
                    P.FirstName.A().Key,  // Add all keys that you consider
                    P.LastName.A().Key,   // relevant for a general query here
                    P.Email.A().Key       // (remember to optimize database correspondingly, like using partial indexes in PostgreSQL)
                }, Operator.ILIKE, GeneralQueryId);
                /// TODO: Add a LIMIT parameter to <see cref="QueryIdKeyOperatorValue"/>.
                /// Note relatively expensive reading of whole <see cref="Person"/>-objects now. 
                if (!DB.TryGetEntities(                    
                    request.CurrentUser.RepresentedByEntity ?? request.CurrentUser, /// Note how search will always be done viewed from <see cref="BaseEntity.RepresentedByEntity"/>
                    queryId,
                    AccessType.Read, useCache: true,
                    entities: out List<Person> persons,
                    errorResponse: out var tplErrorResponse)) return request.GetErrorResponse(tplErrorResponse);
                // Note, you can search for other types of entities here also, and add the corresponding persons to the
                // persons collection found now. 
                if (persons.Count == 0) return request.GetErrorResponse(ResultCode.data_error, "No persons found for query '" + GeneralQueryId + "'");
                return request.GetOKResponseAsMultipleEntities(persons.Select(p => {
                    var r = new GeneralQueryResult();
                    r.AddProperty(CoreP.AccessLevelRead.A(), AccessLevel.Anonymous); /// Since <see cref="AgoRapideAttribute.Parents"/> are specified for properties belonging to <see cref="GeneralQueryResult"/> we must also set general access right for each and every such entity.
                    r.AddProperty(
                        CoreP.SuggestedUrl.A(), 
                        request.CreateAPIUrl(
                            CoreMethod.UpdateProperty, 
                            typeof(Person),  /// Note important point here, do NOT set <see cref="CoreP.EntityToRepresent"/> for <see cref="CoreP.EntityToRepresent"/>!
                            new QueryIdInteger(request.CurrentUser.RepresentedByEntity?.Id ?? request.CurrentUser.Id), CoreP.EntityToRepresent, p.Id.ToString()
                        )
                    );
                    r.AddProperty(CoreP.Description.A(), p.Name);
                    return (BaseEntity)r;
                }).ToList());
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: AND MAKE A CORE METHOD OUT OF THIS!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// TODO: (maybe not possible since email / password, type of entity (Person) and so on is quite specific for the actual application.
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        [APIMethod(
            S1 = nameof(AddFirstAdminUser), S2 = P.Email, S3 = P.Password, Description =
            "Adds the first administrative user to the system (with -" + nameof(P) + "." + nameof(P.AccessLevelGiven) + "- = -" + nameof(AccessLevel) + "." + nameof(AccessLevel.Admin) + "-. " +
            "Only allowed if no entities of type -" + nameof(Person) + "- exists",
            ShowDetailedResult = true)]
        public object AddFirstAdminUser(string Email, string Password) {
            try {
                if (!TryGetRequest(Email, Password, out var request, out var completeErrorResponse)) return completeErrorResponse;

                // Check that the only person in the database at the moment is the anonymous user created by Startup.cs
                // ------------------------------
                // TODO: CHECK IN Startup if PERSONS EXISTS AND DISABLE THIS METHOD IF IT DOES (less costly, less risk of DDOS against this method)
                // (it not disabled we can still make a check, reading all entities)
                var persons = DB.GetRootPropertyIds(typeof(Person));  // TODO: This is costly! Check in a less costly manner!
                // TODO: If client has forgot admin credentials, give instructions for recovery. Like sending e-mail, deleting
                // TODO: password from database or deleting all properties for admin-user.
                var au = Util.Configuration.A.AnonymousUser;
                if (au == null) throw new Exception(nameof(Util.Configuration.A.AnonymousUser) + " not set up correctly (null)");
                if (au.Id <= 0) throw new Exception(nameof(Util.Configuration.A.AnonymousUser) + " not set up correctly (" + nameof(au.Id) + ": " + au.Id + ")");
                if (!au.GetType().Equals(typeof(Person))) throw new Exception(nameof(Util.Configuration.A.AnonymousUser) + " not set up correctly (type: " + au.GetType() + ")");
                // Check above is in order for Count > 1 below to work out
                if (persons.Count > 1) return request.GetErrorResponse(ResultCode.data_error, "Admin user already exists. There is no need for calling this method.");
                if (persons[0] != au.Id) throw new Exception(nameof(Util.Configuration.A.AnonymousUser) + " not set up correctly (" + nameof(au.Id) + " " + au.Id + " does not correspond to " + nameof(DB.GetRootPropertyIds) + " result which was " + persons[0] + ")");
                // ------------------------------
                request.Parameters.AddProperty(CoreP.AccessLevelGiven.A(), AccessLevel.Admin);
                request.Result.LogInternal("Note how this API-method gives you a high level of details in the generated result because -" + nameof(APIMethodAttribute) + "." + nameof(APIMethodAttribute.ShowDetailedResult) + "- = true", GetType());
                return request.GetOKResponseAsEntityId(typeof(Person), DB.CreateEntity<Person>(GetId(), request.Parameters, request.Result), null);
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.User)] // Stricter access like administrative access will be considered further downstream (by AgoRapideGenericMethod)
        [APIMethod(CoreMethod = CoreMethod.GenericMethod)]
        public object GenericMethod() {
            try {
                var method = GetMethod();
                return AgoRapideGenericMethod(method, CurrentUser(method));
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.Admin)]
        [HttpGet]
        [APIMethod(CoreMethod = CoreMethod.ExceptionDetails, S1 = nameof(ExceptionDetails), AccessLevelUse = AccessLevel.Admin)]
        public object ExceptionDetails() {
            try {
                return HandleCoreMethodExceptionDetails(GetMethod());
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }
    }
}