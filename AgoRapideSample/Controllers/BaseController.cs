using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using AgoRapide;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapideSample {
    public abstract class BaseController : BaseController<P> {

        //protected override string GetConnectionString() =>
        //    "Pooling=false;" +
        //    "CommandTimeout=20;" + // 20 seconds. Corresponds to default value
        //    "Server=127.0.0.1;" +
        //    "Port=5432;" +
        //    "User Id=agorapide;" +
        //    "Password=agorapide;" +
        //    "Database=agorapide";

        public BaseController() {
            LogEvent += LogFinal;
            HandledExceptionEvent += LogException;
        }

        public static IDatabase<P> GetDatabase(Type ownersType) {
            var retval = new PostgreSQLDatabase<P>(
                "Pooling=false;" +
                "CommandTimeout=20;" + // 20 seconds. Corresponds to default value
                "Server=127.0.0.1;" +
                "Port=5432;" +
                "User Id=agorapide;" +   // TODO: Do of course not store details like this in source-code. 
                "Password=agorapide;" +  // TODO: Instead use a configuration file kept outside of your source-code repository (outside of GitHub)
                "Database=agorapide",    // TODO: In this case database is just for sample purposes so nothing sensitive is exposed. 
                ownersType
            );
            retval.LogEvent += LogFinal; // Note how LogEvent and HandledExceptionEvent is deliberately left out of IDatabase[TProperty] so you may implement your own logging mechanism instead
            retval.HandledExceptionEvent += LogException; // TODO: Is that a good idea? Why not have them as standard within the AgoRapide-library?
            return retval;
        }

        /// <summary>
        /// Type of <see cref="_db"/> could without any problem be set to 
        /// <see cref="PostgreSQLDatabase{TProperty}"/> instead of 
        /// <see cref="IDatabase{TProperty}"/>
        /// </summary>
        protected IDatabase<P> _db;
        protected override IDatabase<P> DB => _db ?? (_db = GetDatabase(GetType()));

        protected void DBDispose() {
            if (_db != null) _db.Dispose();
        }

        //protected override string GetHTMLHeading(Request<P> request, string title, ResultCode status) => "<p>" + System.Reflection.MethodBase.GetCurrentMethod().Name + " has not been implemented</p>";
        //protected override string GetHTMLFooter(Request<P> request) => "<p>" + System.Reflection.MethodBase.GetCurrentMethod().Name + " has not been implemented</p>";

        /// <summary>
        /// Insert your preferred logging mechanism in:
        /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
        /// Startup.cs (multiple places)
        /// </summary>
        /// <param name="text"></param>
        private static void LogFinal(string text) => Util.Log(text);

        /// <summary>
        /// Insert your preferred logging mechanism in:
        /// AgoRapideSample.BaseController.LogFinal and AgoRapideSample.BaseController.LogException AND
        /// Startup.cs (multiple places)
        /// </summary>
        /// <param name="ex"></param>
        private static void LogException(Exception ex) => Util.LogException(ex);

        ///// <summary>
        ///// A short name is used since it is repeated frequently in classes inheriting <see cref="BaseController"/>
        ///// </summary>
        //public class M : MethodAttribute {
        //}
    }
}