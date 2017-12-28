// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Linq;
using System.Collections.Generic;
// Due to some perceived confusion about origin of some methods, we deliberately omit some using-statements here
// and instead use the full namespace (likewise extension methods are called "manually").

namespace AgoRapideSample {

    /// <summary>
    /// This class sets up the project to use OWIN / Katana.
    /// 
    /// Note that as of Dec 2016 it works better to launch the project from Visual Studio with F5 (Start debugging) instead of
    /// CTRL-F5  (Start without debugging). 
    /// </summary>
    public class Startup {

        /// <summary>
        /// Note that there are many different methods to obtain the environment under which we are running. 
        /// 
        /// The default used here is the most simple of all, looking for the existence of a given folder
        /// on disk. 
        /// 
        /// More traditional would be to use the Web.config file and the <see cref="System.Configuration.Configuration"/> 
        /// 
        /// The advantages of the method shown here is that it dispenses with the need for separate
        /// (and different) configuration files.
        /// </summary>
        /// <returns></returns>
        private (Uri rootUrl, AgoRapide.Environment environment) GetEnvironment() {
            if (System.IO.Directory.Exists(@"c:\git\AgoRapide")) {
                return (new Uri("http://localhost:52668/"), AgoRapide.Environment.Development);
            } else if (System.IO.Directory.Exists(@"D:\p\wwwRootAgoRapideSample")) {
                return (new Uri("http://sample.agorapide.com"), AgoRapide.Environment.Production);
            } else {
                throw new AgoRapide.Core.UnknownEnvironmentException("Unable to recognize environment that application is running under.");
            }
        }

        /// <summary>
        /// IMPORTANT: Please note that each enum in your project 
        /// IMPORTANT: should be included below in a call to <see cref="AgoRapide.Core.PropertyKeyMapper.MapEnum"/> (See mapper1)
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(Owin.IAppBuilder appBuilder) {
            try {
                var logPath = @"c:\p\Logfiles\AgoRapideSample\AgoRapideLog_[DATE_HOUR].txt";

                /// Note how we set AgoRapide.Core.Util.Configuration twice, 
                /// First in order to set <see cref="AgoRapide.Core.ConfigurationAttribute.LogPath"/> (so we can call <see cref="AgoRapide.Core.Util.Log"/>), 
                /// Second in order to set <see cref="AgoRapide.Core.ConfigurationAttribute.RootUrl"/> 
                AgoRapide.Core.Util.Configuration = new AgoRapide.Core.Configuration(new AgoRapide.Core.ConfigurationAttribute(
                    logPath: logPath,
                    rootUrl: AgoRapide.Core.Util.Configuration.C.RootUrl,
                    databaseGetter: type => throw new NullReferenceException(nameof(AgoRapide.Core.ConfigurationAttribute.DatabaseGetter) + " not yet set")
                ));

                Log(null, "");
                var environment = GetEnvironment();
                Log(null, "rootUrl: " + environment.rootUrl); // Necessary because System.Web.HttpContext.Current.Request.Url not available now. 
                Log(null, "environment: " + environment.environment);

                // Note how we set AgoRapide.Core.Util.Configuration twice, first in order to be able to log, second in order to set rootUrl, rootPath and databaseGetter
                AgoRapide.Core.Util.Configuration = new AgoRapide.Core.Configuration(new AgoRapide.Core.ConfigurationAttribute(
                    logPath: logPath,
                    rootUrl: environment.rootUrl,
                    databaseGetter: ownersType => BaseController.GetDatabase(ownersType)
                ) {
                    // Change to different version of JQuery by adding this line:
                    // ScriptRelativePaths = new List<string> { "Scripts/AgoRapide-0.1.js", "Scripts/jquery-3.1.1.min.js" },

                    Environment = environment.environment,
                    SuperfluousStackTraceStrings = new List<string>() {
                        @"c:\git\AgoRapide",
                        @"C:\git\AgoRapide\",
                        @"C:\AgoRapide2\trunk\",
                        @"C:\diggerout\trunk\DAPI\DAPI"
                    },
                });

                /// Include all assemblies in which your controllers and <see cref="AgoRapide.BaseEntity"/>-classes resides.
                var assemblies = new List<System.Reflection.Assembly> { typeof(HomeController).Assembly };
                AgoRapide.API.APIMethod.SetEntityTypes(assemblies, new List<Type>()); /// Exclude <see cref="AgoRapide.Person"/> now if you do not want to use that class. 

                AgoRapide.Core.PropertyKeyMapper.MapKnownEnums(s => Log("MapEnums", nameof(AgoRapide.Core.PropertyKeyMapper.MapKnownEnums) + ": " + s)); /// TODO: Move into <see cref="AgoRapide.Core.Startup"/> somehow

                /// Mapping must be done now because of a lot of static properties which calls one of <see cref="AgoRapide.Core.Extensions.A"/>
                void mapper1<T>() where T : struct, IFormattable, IConvertible, IComparable => AgoRapide.Core.PropertyKeyMapper.MapEnum<T>(s => Log("MapEnums", nameof(AgoRapide.Core.PropertyKeyMapper.MapEnum) + ": " + s)); // What we really would want is "where T : Enum"
                mapper1<CarP>();        /// TODO: Automate this somehow by using information in current assembly
                mapper1<P>();           /// TODO: Automate this somehow by using information in current assembly
                                        /// Add all your <see cref="AgoRapide.EnumType.PropertyKey"/> at bottom of list, in order of inheritance (if any)

                AgoRapide.Core.PropertyKeyMapper.MapEnumFinalize(s => Log("MapEnums", nameof(AgoRapide.Core.PropertyKeyMapper.MapEnumFinalize) + ": " + s));

                Log("MapEnums", "\r\n\r\n" +
                    "Asserting mapping of all -" + AgoRapide.EnumType.PropertyKey + "- enums towards " + typeof(AgoRapide.CoreP) + " in order to expose any issues at once\r\n" +
                    "(note mapping to " + (((int)(object)AgoRapide.Core.Util.EnumGetValues<AgoRapide.CoreP>().Max()) + 1) + " and onwards)");
                void mapper2<T>() where T : struct, IFormattable, IConvertible, IComparable // What we really would want is "where T : Enum"
                {
                    Log("MapEnums",
                        typeof(T) + " to " + typeof(AgoRapide.CoreP) + ":\r\n" +
                        string.Join("\r\n", AgoRapide.Core.Util.EnumGetValues<T>().Select(p => typeof(T) + "." + p + " => " + AgoRapide.Core.PropertyKeyMapper.GetA(p).Key.CoreP)) + "\r\n");
                }
                mapper2<AgoRapide.Core.ConfigurationAttribute.ConfigurationP>(); /// TODO: Move into <see cref="AgoRapide.Core.Startup"/> somehow
                mapper2<AgoRapide.API.ResultP>();                                /// TODO: Move into <see cref="AgoRapide.Core.Startup"/> somehow
                mapper2<AgoRapide.API.APIMethodP>();                             /// TODO: Move into <see cref="AgoRapide.Core.Startup"/> somehow
                mapper2<AgoRapide.PersonP>();                                    /// TODO: Move into <see cref="AgoRapide.Core.Startup"/> somehow
                mapper2<P>();                                                    /// TODO: Automate this somehow by using information in current assembly           
                mapper2<CarP>();                                                 /// TODO: Automate this somehow by using information in current assembly

                Log(null, "Miscellaneous testing");
                AgoRapide.ExtensionsPersonP.A(AgoRapide.PersonP.Password).Key.A.AssertIsPassword(null);

                Log(nameof(AgoRapide.Database.PostgreSQLDatabase.SQL_CREATE_TABLE), nameof(AgoRapide.Database.PostgreSQLDatabase.SQL_CREATE_TABLE) + ":\r\n\r\n" + new AgoRapide.Database.PostgreSQLDatabase(BaseController.DATABASE_OBJECTS_OWNER, null, BaseController.DATABASE_TABLE_NAME, typeof(Startup)).SQL_CREATE_TABLE + "\r\n");

                var httpConfiguration = new AgoRapide.Core.CoreStartup().Initialize<AgoRapide.Person>(
                    attributeClassesSignifyingRequiresAuthorization: new List<Type> {
                        typeof(System.Web.Http.AuthorizeAttribute),
                        typeof(AgoRapide.API.BasicAuthenticationAttribute)
                    },
                    clientAssemblies: assemblies,
                    Log: (category, text) => Log(category, text) // Note the special logging mechanism utilizing different category files. 
                );

                Log(null, "Calling Owin.WebApiAppBuilderExtensions.UseWebApi");
                Owin.WebApiAppBuilderExtensions.UseWebApi(appBuilder, httpConfiguration);

                Log(null, "Completed");
            } catch (Exception ex) {
                BaseController.LogException(ex);
            }
        }

        /// <summary>
        /// Insert your preferred logging mechanism in <see cref="BaseController.LogFinal"/>, <see cref="BaseController.LogException"/>, <see cref="Startup.Log"/>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caller"></param>
        private void Log(string category, string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => 
            AgoRapide.Core.Util.Log(category==null ? null : "Startup_" + category, GetType().ToString() + "." + caller + ": " + text);        
    }
}