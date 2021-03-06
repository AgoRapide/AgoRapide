﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {
    /// <summary>
    /// Basic Authentication is used in AgoRapide for ease-of-getting-started purposes only. 
    /// 
    /// Do not use Basic Authentication in production! Use OAuth 2.0 or similar.
    /// 
    /// Code is copied from http://stackoverflow.com/questions/28352998/using-both-oauth-and-basic-auth-in-asp-net-web-api-with-owin
    /// </summary>
    public class BasicAuthenticationAttribute : Attribute, System.Web.Http.Filters.IAuthenticationFilter {

        public AccessLevel AccessLevelUse { get; set; }

        private static IAdditionalCredentialsVerifier _additionalCredentialsVerifier;
        [ClassMember(Description = "If given then -" + nameof(IAdditionalCredentialsVerifier.TryVerifyCredentials) + "- will be called before -" + nameof(Database.BaseDatabase.TryVerifyCredentials) + "-.")]
        public static IAdditionalCredentialsVerifier AdditionalCredentialsVerifier { get => _additionalCredentialsVerifier; set { Util.AssertCurrentlyStartingUp(); _additionalCredentialsVerifier = value; } }

        public Task AuthenticateAsync(System.Web.Http.Filters.HttpAuthenticationContext context, System.Threading.CancellationToken cancellationToken) {
            var errorResultGenerator = new Func<System.Web.Http.Results.UnauthorizedResult>(() => new System.Web.Http.Results.UnauthorizedResult(new System.Net.Http.Headers.AuthenticationHeaderValue[0], context.Request));
            Database.BaseDatabase database = null;
            try {
                database = Util.Configuration.C.DatabaseGetter(GetType());

                var generatePrincipal = new Action<BaseEntity>(currentUser => {
                    context.Principal = new System.Security.Claims.ClaimsPrincipal(new[] {
                        new System.Security.Claims.ClaimsIdentity(
                            claims: new List<System.Security.Claims.Claim> {
                                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, currentUser.Id.ToString())
                            }, // Note that we use Id and not credArray[0]) here
                            authenticationType: "Basic")
                    });
                    context.Request.Properties["AgoRapideCurrentUser"] = currentUser; // Used by AgoRapide.API.BaseController[TProperty].TryGetCurrentUser.
                                                                                      // TODO: This is not utilized as of Jan 2017
                    context.Request.Properties["AgoRapideDatabase"] = database;
                });

                var headers = context.Request.Headers;
                if (headers.Authorization == null || !"basic".Equals(headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase)) {
                    // No authorization information given by client. 

                    // Accept as anonymous user if none of the candidate methods require authorization. 
                    // This ensure a more user friendly API and also makes it possible to have AutoGenerated methods without RequiresAuthorization

                    // NOTE: Do not use this for anything security critial.
                    // NOTE: Simply do only
                    // NOTE:   context.ErrorResult = errorResultGenerator();
                    // NOTE: here if security is important. 

                    Request.GetMethodsMatchingRequest(context.Request, Request.GetResponseFormatFromURL(context.Request.RequestUri), out var exactMatch, out var candidateMatches, out _);
                    if (
                        (exactMatch != null && exactMatch.Value.method.RequiresAuthorization) ||
                        (candidateMatches != null && candidateMatches.Value.methods.Any(m => m.RequiresAuthorization))
                        ) {
                        context.ErrorResult = errorResultGenerator();
                    } else {
                        if (exactMatch != null && exactMatch.Value.method.Origin != AgoRapide.APIMethodOrigin.Autogenerated) throw new AgoRapide.Core.InvalidEnumException(exactMatch.Value.method.Origin,
                            "Found " + nameof(exactMatch) + " for " + exactMatch.Value.method.IdFriendly + " " +
                            "with " + nameof(exactMatch.Value.method.Origin) + " " + exactMatch.Value.method.Origin + " " +
                            "and " + nameof(exactMatch.Value.method.RequiresAuthorization) + " " + exactMatch.Value.method.RequiresAuthorization + ". " +
                            "This is not logical, as such an URL should not result in " + System.Reflection.MethodBase.GetCurrentMethod().Name + " being called");
                        generatePrincipal(Util.Configuration.C.AnonymousUser);
                    }
                } else {
                    if (!Util.Configuration.C.TryAssertHTTPSAsRelevant(context.Request.RequestUri, out _)) {
                        /// Note how we are unable to communicate the explanatory message generated by <see cref="ConfigurationAttribute.TryAssertHTTPSAsRelevant"/> here. 
                        context.ErrorResult = errorResultGenerator();
                    } else {
                        var credArray = Encoding.GetEncoding("UTF-8").GetString(Convert.FromBase64String(headers.Authorization.Parameter)).Split(':');
                        if (credArray.Length != 2) {
                            context.ErrorResult = errorResultGenerator();
                        } else if (
                            (AdditionalCredentialsVerifier != null && AdditionalCredentialsVerifier.TryVerifyCredentials(database, credArray[0], credArray[1], out var currentUser)) ||
                            database.TryVerifyCredentials(credArray[0], credArray[1], out currentUser)
                          ) {
                            generatePrincipal(currentUser);
                        } else {
                            context.ErrorResult = errorResultGenerator();
                        }
                    }
                }
                return Task.FromResult(0);
            } catch (Exception ex) {
                /// Insert your preferred logging mechanism in:
                /// DAPI.BaseController.LogFinal and DAPI.BaseController.LogException AND
                /// Startup.cs (multiple places)
                Util.LogException(ex);

                // TODO: Add missing parameters here and return ExceptionResult instead
                // TODO: context.ErrorResult = new System.Web.Http.Results.ExceptionResult(ex, false, null, request, null);
                // TODO: instead of returning UnauthorizedResult like below:
                context.ErrorResult = errorResultGenerator();
                return Task.FromResult(0);
            } finally {
                database?.Dispose();
            }
        }

        public Task ChallengeAsync(System.Web.Http.Filters.HttpAuthenticationChallengeContext context, System.Threading.CancellationToken cancellationToken) {
            context.Result = new ResultWithChallenge(context.Result);
            return Task.FromResult(0);
        }

        public class ResultWithChallenge : System.Web.Http.IHttpActionResult {
            private readonly System.Web.Http.IHttpActionResult next;
            public ResultWithChallenge(System.Web.Http.IHttpActionResult next) => this.next = next;
            public async Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(System.Threading.CancellationToken cancellationToken) {
                var response = await next.ExecuteAsync(cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                    response.Headers.WwwAuthenticate.Add(new System.Net.Http.Headers.AuthenticationHeaderValue("Basic"));
                }
                return response;
            }
        }

        [Class(Description =
            "Useful when using -" + nameof(Database.BaseSynchronizer) + "- and it is desired to reuse the credentials from the system that is synchronized from, " +
            "that is, when it is desired not to administer an additional set of administrative user profiles in the AgoRapide-based system.")]
        public interface IAdditionalCredentialsVerifier {
            bool TryVerifyCredentials(Database.BaseDatabase database, string username, string password, out BaseEntity currentUser);
        }

        public bool AllowMultiple => false;
    }
}
