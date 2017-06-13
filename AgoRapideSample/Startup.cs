﻿
using System;
using System.Linq;
using System.Collections.Generic;
// Due to some perceived confusion about origin of some methods, we deliberately omit some using-statements here
// and instead use the full namespace (likewise extension methods are called "manually").

/// <summary>
/// This file (Startup.cs) contains the logic for startup and authentication.
/// Classes defined here are: 
/// Startup, 
/// WebAPIConfig, 
/// MethodP, 
/// BasicAuthenticationAttribute
/// </summary>
namespace AgoRapideSample {

    /// <summary>
    /// This class sets up the project to use OWIN / Katana.
    /// 
    /// See <see cref="Configuration"/> for information about adjustments that you must make according to Controllers and types used in your project.
    /// 
    /// Note that as of Dec 2016 it works better to launch AgoRapideSample from Visual Studio with F5 (Start debugging) instead of
    /// CTRL-F5  (Start without debugging). 
    /// </summary>
    public class Startup {

        /// <summary>
        /// Note that there are many different methods to obtain the environment under which we are running. 
        /// 
        /// The default used here is the most simple of all, looking for the existence of a given folder
        /// on disk. 
        /// 
        /// More traditional would be to use the web.config file and the configuration class.
        /// 
        /// The advantages of the method shown here is that it dispenses with the need for separate
        /// (and different) configuration files.
        /// </summary>
        /// <returns></returns>
        private (Uri uri, AgoRapide.Environment environment) GetEnvironment() {
            if (System.IO.Directory.Exists(@"c:\git\AgoRapide")) {
                return (new Uri("http://localhost:52668/"), AgoRapide.Environment.Development);
            } else if (System.IO.Directory.Exists(@"D:\p\wwwRootAgoRapideSample")) {
                return (new Uri("http://sample.agorapide.com"), AgoRapide.Environment.Production);
            } else {
                throw new UnknownEnvironmentException("Unable to recognize environment that application is running under.");
            }
        }

        /// <summary>
        /// IMPORTANT: Please note that: 
        /// 
        /// 1) Each Controller in your project should be added here as a parameter in the call to 
        ///    <see cref="AgoRapide.APIMethod.CreateSemiAutogeneratedMethods"/>
        ///    
        /// 2) Each <see cref="AgoRapide.BaseEntity"/>-class in your project should be added here as a parameter in the call to
        ///    <see cref="AgoRapide.APIMethod.CreateAutogeneratedMethods"/>
        ///    
        /// 3) Each enum in your project should be included here in a call to 
        ///    <see cref="AgoRapide.EnumValue.RegisterEnumClass"/>
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(Owin.IAppBuilder appBuilder) {
            try {
                var logPath = @"c:\p\Logfiles\AgoRapideSample\AgoRapideLogX_[DATE_HOUR].txt";

                // Note how we set AgoRapide.Core.Util.Configuration twice, first in order to be able to log, second in order to set rootUrl and rootPath
                AgoRapide.Core.Util.Configuration = new AgoRapide.Core.Configuration(new AgoRapide.Core.ConfigurationAttribute(
                    logPath: logPath,
                    rootUrl: AgoRapide.Core.Util.Configuration.C.RootUrl
                ));

                Log("");
                var environment = GetEnvironment();

                // TODO: We expect to be able to do this:
                // var rootUrl = AgoRapide.Extensions.Use(System.Web.HttpContext.Current.Request.Url, u => u.Scheme + "://" + u.Host + (u.Port != 80 ? (":" + u.Port.ToString()) : "") + "/");
                // but we do not get the correct URL in that manner at this stage (maybe because there is not defined any "real" HttpRequest yet?)
                // Therefore we use the GetEnvironment method:
                var rootUrl = environment.uri.ToString();
                Log("rootUrl: " + rootUrl);
                // rootPath works as expected
                var rootPath = System.Web.HttpContext.Current.Server.MapPath("") + @"\";
                /// TODO: REMOVE USE OF RootPath now that documentation if offered through general API mechanism
                Log("rootPath: " + rootPath);

                Log("environment: " + environment.environment);

                // Note how we set AgoRapide.Core.Util.Configuration twice, first in order to be able to log, second in order to set rootUrl and rootPath
                AgoRapide.Core.Util.Configuration = new AgoRapide.Core.Configuration(new AgoRapide.Core.ConfigurationAttribute(
                    logPath: logPath,
                    rootUrl: rootUrl
                ) {
                    // Change to different version of JQuery by adding this line:
                    // ScriptRelativePaths = new List<string> { "Scripts/AgoRapide-0.1.js", "Scripts/jquery-3.1.1.min.js" },
                    Environment = environment.environment,
                    SuperfluousStackTraceStrings = new List<string>() {
                        @"c:\git\AgoRapide",
                        @"C:\AgoRapide2\trunk\"
                    }
                    // ...
                    // Note how may change a lot of other configuration parameters here, as needed
                    // ...
                });

                /// Mapping must be now because of a lot of static properties which calls one of <see cref="AgoRapide.Core.Extensions.A"/>
                void mapper1<T>() where T : struct, IFormattable, IConvertible, IComparable => AgoRapide.Core.PropertyKeyMapper.MapEnum<T>(s => {
                    // return; // TODO: ENABLE LOGGIN HERE. We have a serious chicken and egg problem now.
                    Log(nameof(AgoRapide.Core.PropertyKeyMapper.MapEnum) + ": " + s);
                }); // What we really would want is "where T : Enum"
                mapper1<AgoRapide.DBField>(); /// This is a quasi <see cref="AgoRapide.Core.PropertyKeyAttribute"/>
                mapper1<AgoRapide.CoreP>();
                mapper1<AgoRapide.Core.ConfigurationAttribute.ConfigurationP>();
                mapper1<AgoRapide.API.APIMethodP>();
                mapper1<AgoRapide.API.ResultP>();
                mapper1<P>();
                /// Add all your <see cref="AgoRapide.EnumType.PropertyKey"/> at bottom of list, 
                /// that is in order of going outwards from inner AgoRapide library towards your final application

                AgoRapide.Core.PropertyKeyMapper.MapEnumFinalize(s => Log(nameof(AgoRapide.Core.PropertyKeyMapper.MapEnumFinalize) + ": " + s));
                Log(nameof(AgoRapide.Core.PropertyKeyMapper.AllCoreP) + ":\r\n\r\n" + string.Join("\r\n", AgoRapide.Core.PropertyKeyMapper.AllCoreP.Select(c => c.Key.A.EnumValueExplained)) + "\r\n");

                var systemUser = new Person();
                systemUser.AddProperty(AgoRapide.Core.Extensions.A(AgoRapide.CoreP.AccessLevelGiven), AgoRapide.AccessLevel.System);
                AgoRapide.Core.Util.Configuration.C.SystemUser = systemUser;

                Log("\r\n\r\n" +
                    "Asserting mapping towards " + typeof(AgoRapide.CoreP) + " in order to expose any issues at once\r\n" +
                    "(note mapping to " + (((int)(object)AgoRapide.Core.Util.EnumGetValues<AgoRapide.CoreP>().Max()) + 1) + " and onwards)");
                void mapper2<T>() where T : struct, IFormattable, IConvertible, IComparable // What we really would want is "where T : Enum"
                {
                    Log(
                        typeof(T) + " to " + typeof(AgoRapide.CoreP) + ":\r\n" +
                        string.Join("\r\n", AgoRapide.Core.Util.EnumGetValues<T>().Select(p => nameof(T) + "." + p + " => " + AgoRapide.Core.PropertyKeyMapper.GetA(p).Key.CoreP)) + "\r\n");
                }
                mapper2<AgoRapide.Core.ConfigurationAttribute.ConfigurationP>();
                mapper2<P>();

                string mapper3<T>() => typeof(T) + " => " + AgoRapide.Core.Util.MapTToCoreP<T>().Key.A.EnumValueExplained + "\r\n";
                Log("\r\n\r\n" +
                    "Testing " + nameof(AgoRapide.Core.Util.MapTToCoreP) + " for a few enums\r\n\r\n" +
                    mapper3<AgoRapide.ResultCode>() + /// Maps to <see cref="AgoRapide.CoreP.ResultCode"/>
                    mapper3<AgoRapide.APIMethodOrigin>()  /// Maps to <see cref="AgoRapide.CoreP.APIMethodOrigin"/>
                );

                Log("Miscellaneous testing");
                AgoRapideSample.ExtensionsP.A(AgoRapideSample.P.Password).Key.A.AssertIsPassword(null);

                Log("(See corresponding code in Startup.cs for above. Add for more types as you develop your application)");

                Log(nameof(AgoRapide.Database.PostgreSQLDatabase.SQL_CREATE_TABLE) + ":\r\n\r\n" + new AgoRapide.Database.PostgreSQLDatabase(BaseController.DATABASE_OBJECTS_OWNER, null, BaseController.DATABASE_TABLE_NAME, typeof(Startup)).SQL_CREATE_TABLE + "\r\n");
                Log("Attempting to connect to database. If connection fails check that the database has been created according to above logged SQL-code");
                var db = BaseController.GetDatabase(GetType());

                Log("Reading all " + typeof(AgoRapide.Core.ClassMember)); // Important before we ask for startupAsApplicationPart
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.Core.ClassMember>(db, text => Log("(by " + typeof(AgoRapide.Core.ApplicationPart) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                var startupAsApplicationPart = AgoRapide.Core.ApplicationPart.GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);
                db.UpdateProperty(startupAsApplicationPart.Id, startupAsApplicationPart, key: AgoRapide.Core.Extensions.A(AgoRapide.CoreP.Log), value: "Initiating startup", result: null);

                // ---------------------

                Log("Reading all " + typeof(AgoRapide.Core.Class));
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.Core.Class>(db, text => Log("(by " + typeof(AgoRapide.Core.ApplicationPart) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                Log("Reading all " + typeof(AgoRapide.Core.Enum));
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.Core.Enum>(db, text => Log("(by " + typeof(AgoRapide.Core.ApplicationPart) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                Log("Reading all " + typeof(AgoRapide.Core.EnumValue));
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.Core.EnumValue>(db, text => Log("(by " + typeof(AgoRapide.Core.ApplicationPart) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                // ---------------------

                Log("Looking for " + AgoRapide.CoreP.IsAnonymous + " persons");
                var queryId = new AgoRapide.Core.QueryIdKeyOperatorValue(AgoRapide.Core.Extensions.A(AgoRapide.CoreP.IsAnonymous).Key, AgoRapide.Operator.EQ, true);
                if (!db.TryGetEntity(AgoRapide.Core.Util.Configuration.C.SystemUser, queryId, AgoRapide.AccessType.Read, entity: out Person anonymousUser, errorResponse: out var errorResponse)) {
                    Log(AgoRapide.CoreP.IsAnonymous + " person not found, creating one");
                    AgoRapide.Core.Util.Configuration.C.AnonymousUser = db.GetEntityById<Person>(db.CreateEntity<Person>(
                        cid: startupAsApplicationPart.Id,
                        properties: new Dictionary<AgoRapide.CoreP, object> {
                            { AgoRapide.CoreP.IdFriendly, "anonymous"},
                            { AgoRapide.CoreP.IsAnonymous, true },
                            { AgoRapide.CoreP.AccessLevelRead, AgoRapide.AccessLevel.Anonymous },
                            { AgoRapide.CoreP.AccessLevelWrite, AgoRapide.AccessLevel.System }
                        }.Select(e => (AgoRapide.Core.Extensions.A(e.Key).PropertyKeyWithIndex, e.Value)).ToList(),
                        result: null));
                } else {
                    AgoRapide.Core.Util.Configuration.C.AnonymousUser = anonymousUser;
                }

                // ---------------------

                Log("Reading all (or rather the single) " + typeof(AgoRapide.Core.Configuration));
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.Core.Configuration>(db, text => Log("(by " + typeof(AgoRapide.Core.Configuration) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                Log("Writing configuration to database");
                AgoRapide.Core.Util.Configuration.ConnectWithDatabasePublicAccess(db);

                /// --------------------- <see cref="AgoRapide.API.APIMethod"/>

                Log("Reading all " + typeof(AgoRapide.API.APIMethod));
                AgoRapide.Core.ApplicationPart.GetFromDatabase<AgoRapide.API.APIMethod>(db, text => Log("(by " + typeof(AgoRapide.Core.ApplicationPart) + "." + nameof(AgoRapide.Core.ApplicationPart.GetFromDatabase) + ") " + text)); // TODO: Fix better logging mechanism here

                Log("Calling " + nameof(AgoRapide.API.APIMethod.CreateSemiAutogeneratedMethods));
                AgoRapide.API.APIMethod.CreateSemiAutogeneratedMethods(
                    controllers: new List<Type> {
                        typeof(HomeController),
                        typeof(AnotherController)  // Add to this list each Controller in your project
                    },
                    attributeClassesSignifyingRequiresAuthorization: new List<Type> {
                        typeof(System.Web.Http.AuthorizeAttribute),
                        typeof(BasicAuthenticationAttribute)
                    },
                    db: db
                );

                Log("Calling " + nameof(AgoRapide.API.APIMethod.CreateAutogeneratedMethods));
                AgoRapide.API.APIMethod.CreateAutogeneratedMethods(
                    types: new List<Type> {
                        typeof(AgoRapide.Core.Configuration),
                        typeof(AgoRapide.Core.Class),
                        typeof(AgoRapide.Core.ClassMember),
                        typeof(AgoRapide.Core.Enum),
                        typeof(AgoRapide.Core.EnumValue),
                        typeof(AgoRapide.API.APIMethod),
                        typeof(AgoRapide.BaseEntity),
                        typeof(AgoRapide.Property),
                        typeof(Person),
                        typeof(Car)
                        /// Add to this list each <see cref="AgoRapide.BaseEntity"/>-derived class in your project 
                        /// for which you want to automatically implement common API-methods like 
                        /// <see cref="AgoRapide.CoreAPIMethod.AddEntity"/> and so on.
                    },
                    db: db
                );

                Log("The following methods where found by " +
                    nameof(AgoRapide.API.APIMethod) + "." + nameof(AgoRapide.API.APIMethod.CreateSemiAutogeneratedMethods) + " and " +
                    nameof(AgoRapide.API.APIMethod) + "." + nameof(AgoRapide.API.APIMethod.CreateAutogeneratedMethods) + ":\r\n\r\n" +
                    string.Join("\r\n", AgoRapide.API.APIMethod.AllMethods.Select(m => m.ToString())) + "\r\n");

                if (AgoRapide.API.APIMethod.IgnoredMethods.Count > 0) {
                    /// Note that we do not delete from the database in cases like this
                    /// (in general as of Feb 2017 we do not have deletion of <see cref="AgoRapide.ApplicationPart"/> no longer in the C# code)
                    Log("In addition the following methods are present in the C# code but where ignored because the " + nameof(AgoRapide.Environment) + " does not match the current one (" + AgoRapide.Core.Util.Configuration.C.Environment + "):\r\n\r\n" +
                        string.Join("\r\n", AgoRapide.API.APIMethod.IgnoredMethods.Select(m => m.ToString() + " (" + nameof(m.MA.Environment) + ": " + m.MA.Environment + ")")) + "\r\n");
                }

                /// --------------------- <see cref="AgoRapide.Core.Documentator"/>

                Log("Calling " + nameof(AgoRapide.Core.Documentator) + "." + nameof(AgoRapide.Core.Documentator.IndexKnowEntities));
                AgoRapide.Core.Documentator.IndexKnowEntities(db); // TODO: Make more logical choice of methdod names and placing of logic

                /// Note how <see cref="AgoRapide.Core.Documentator.IndexKnowEntities"/> is not able to index 
                /// entities residing outside its own assembly. We must therefore index "our own" entities by ourselves now.
                Log(nameof(AgoRapide.Core.Enum.RegisterAndIndexEnum));
                typeof(P).Assembly.GetTypes().Where(t => t.IsEnum).ToList().ForEach(t => { /// Going through <see cref="P"/> ensures we get a reference to the AgoRapideSample assembly (or you could use System.Reflection.Assembly.GetExecutingAssembly)
                    Log("Calling " + nameof(AgoRapide.Core.Enum) + "." + nameof(AgoRapide.Core.Enum.RegisterAndIndexEnum) + " for " + t.ToString());
                    AgoRapide.Core.Enum.RegisterAndIndexEnum(t, db);
                });

                Log(nameof(AgoRapide.Core.Class.RegisterAndIndexClass));
                typeof(HomeController).Assembly.GetTypes().Where(t => !t.IsEnum).ToList().ForEach(t => { /// Going through <see cref="HomeController"/> ensures we get a reference to the AgoRapideSample assembly (or you could use System.Reflection.Assembly.GetExecutingAssembly)
                    Log("Calling " + nameof(AgoRapide.Core.Class) + "." + nameof(AgoRapide.Core.Class.RegisterAndIndexClass) + " for " + t.ToString());
                    AgoRapide.Core.Class.RegisterAndIndexClass(t, db);
                });

                Log("Calling " + nameof(AgoRapide.Core.Documentator.IndexFinalize));
                var replacements = AgoRapide.Core.Documentator.IndexFinalize();
                Log("The following documentation keys where found:\r\n\r\n" +
                    string.Join("\r\n\r\n", replacements.OrderBy(e => e.Key).Select(e => e.Key + "\r\n" + e.Value)) +
                    "\r\n\r\n");

                Log("Calling " + nameof(AgoRapide.API.APIMethod) + "." + nameof(AgoRapide.API.APIMethod.SetImplementatorId));
                AgoRapide.API.APIMethod.SetImplementatorId(db);
                // ---------------------

                var httpConfiguration = new System.Web.Http.HttpConfiguration();
                WebApiConfig.Register(httpConfiguration);
                Log("Calling Owin.WebApiAppBuilderExtensions.UseWebApi");
                Owin.WebApiAppBuilderExtensions.UseWebApi(appBuilder, httpConfiguration);

                db.UpdateProperty(startupAsApplicationPart.Id, startupAsApplicationPart, key: AgoRapide.Core.Extensions.A(AgoRapide.CoreP.Log), value: "Completed startup", result: null);
                Log("Completed");
            } catch (Exception ex) {
                /// Insert your preferred logging mechanism in:
                /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
                /// Startup.cs (multiple places)
                AgoRapide.Core.Util.LogException(ex);
            }
        }

        /// <summary>
        /// Insert your preferred logging mechanism in:
        /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
        /// Startup.cs (multiple places)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caller"></param>
        private void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => AgoRapide.Core.Util.Log(GetType().ToString() + "." + caller + ": " + text);
    }

    public class WebApiConfig {
        public static void Register(System.Web.Http.HttpConfiguration httpConfiguration) {
            Log("Calling " + nameof(AgoRapide.API.APIMethodMapper) + "." + nameof(AgoRapide.API.APIMethodMapper.MapHTTPRoutes));
            AgoRapide.API.APIMethodMapper.MapHTTPRoutes(httpConfiguration, AgoRapide.API.APIMethod.AllMethods.Where(m => m.Origin != AgoRapide.APIMethodOrigin.Autogenerated).ToList());

            Log("Removing XmlFormatter");
            httpConfiguration.Formatters.Remove(httpConfiguration.Formatters.XmlFormatter);
            Log("Adding JSONFormatter");
            httpConfiguration.Formatters.JsonFormatter.MediaTypeMappings.Add(new System.Net.Http.Formatting.QueryStringMapping("json", "true", "application/json"));
            Log("Completed");
        }

        /// <summary>
        /// Insert your preferred logging mechanism in:
        /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
        /// Startup.cs (multiple places)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caller"></param>
        private static void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => AgoRapide.Core.Util.Log(typeof(WebApiConfig).ToString() + "." + caller + ": " + text);
    }

    public class UnknownEnvironmentException : ApplicationException {
        public UnknownEnvironmentException(string message) : base(message) { }
        public UnknownEnvironmentException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Basic Authentication is used in AgoRapideSample for ease-of-getting-started purposes only. 
    /// Do not use Basic Authentication in production! Use OAuth 2.0 or similar.
    /// 
    /// Code is copied from http://stackoverflow.com/questions/28352998/using-both-oauth-and-basic-auth-in-asp-net-web-api-with-owin
    /// </summary>
    public class BasicAuthenticationAttribute : Attribute, System.Web.Http.Filters.IAuthenticationFilter {

        public AgoRapide.AccessLevel AccessLevelUse { get; set; }

        public System.Threading.Tasks.Task AuthenticateAsync(System.Web.Http.Filters.HttpAuthenticationContext context, System.Threading.CancellationToken cancellationToken) {
            var errorResultGenerator = new Func<System.Web.Http.Results.UnauthorizedResult>(() => new System.Web.Http.Results.UnauthorizedResult(new System.Net.Http.Headers.AuthenticationHeaderValue[0], context.Request));
            try {
                var database = BaseController.GetDatabase(GetType());

                var generatePrincipal = new Action<AgoRapide.BaseEntity>(currentUser => {
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

                    AgoRapide.API.Request.GetMethodsMatchingRequest(context.Request, AgoRapide.API.Request.GetResponseFormatFromURL(context.Request.RequestUri.ToString()), out var exactMatch, out var candidateMatches, out _);
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
                        generatePrincipal((Person)AgoRapide.Core.Util.Configuration.C.AnonymousUser); // Careful with casting here. Must match creation of anonymous user in Startup.Configuration
                    }
                } else {
                    var credArray = System.Text.Encoding.GetEncoding("UTF-8").GetString(Convert.FromBase64String(headers.Authorization.Parameter)).Split(':');
                    if (credArray.Length != 2) {
                        context.ErrorResult = errorResultGenerator();
                    } else {
                        if (!database.TryVerifyCredentials(credArray[0], credArray[1], out var currentUser)) {
                            context.ErrorResult = errorResultGenerator();
                        } else {
                            generatePrincipal(currentUser);
                        }
                    }
                }
                return System.Threading.Tasks.Task.FromResult(0);
            } catch (Exception ex) {
                /// Insert your preferred logging mechanism in:
                /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
                /// Startup.cs (multiple places)
                AgoRapide.Core.Util.LogException(ex);

                // TODO: Add missing parameters here and return ExceptionResult instead
                // TODO: context.ErrorResult = new System.Web.Http.Results.ExceptionResult(ex, false, null, request, null);
                // TODO: instead of returning UnauthorizedResult like below:
                context.ErrorResult = errorResultGenerator();
                return System.Threading.Tasks.Task.FromResult(0);
            }
        }

        public System.Threading.Tasks.Task ChallengeAsync(System.Web.Http.Filters.HttpAuthenticationChallengeContext context, System.Threading.CancellationToken cancellationToken) {
            context.Result = new ResultWithChallenge(context.Result);
            return System.Threading.Tasks.Task.FromResult(0);
        }

        public class ResultWithChallenge : System.Web.Http.IHttpActionResult {
            private readonly System.Web.Http.IHttpActionResult next;
            public ResultWithChallenge(System.Web.Http.IHttpActionResult next) => this.next = next;
            public async System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(System.Threading.CancellationToken cancellationToken) {
                var response = await next.ExecuteAsync(cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                    response.Headers.WwwAuthenticate.Add(new System.Net.Http.Headers.AuthenticationHeaderValue("Basic"));
                }
                return response;
            }
        }

        public bool AllowMultiple => false;
        private static void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => AgoRapide.Core.Util.Log(typeof(BasicAuthenticationAttribute).ToString() + "." + caller + ": " + text);
    }
}