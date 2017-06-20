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
    public abstract class BaseController : AgoRapide.API.BaseController {

        public const string DATABASE_OBJECTS_OWNER = "agorapide";
        public const string DATABASE_TABLE_NAME = "p";

        public BaseController() {
            LogEvent += LogFinal;
            HandledExceptionEvent += LogException;
        }

        public static BaseDatabase GetDatabase(Type ownersType) {
            var retval = new PostgreSQLDatabase(
                objectsOwner: DATABASE_OBJECTS_OWNER,
                connectionString:
                    "Pooling=false;" +
                    "CommandTimeout=20;" + // 20 seconds. Corresponds to default value
                    "Server=127.0.0.1;" +
                    "Port=5432;" +
                    "User Id=agorapide;" +   // TODO: Do of course not store details like this in source-code. 
                    "Password=agorapide;" +  // TODO: Instead use a configuration file kept outside of your source-code repository (outside of GitHub)
                    "Database=agorapide",    // TODO: In this case database is just for sample purposes so nothing sensitive is exposed. 
                tableName: DATABASE_TABLE_NAME,
                applicationType: ownersType
            );
            retval.LogEvent += LogFinal; // Note how LogEvent and HandledExceptionEvent is deliberately left out of IDatabase[TProperty] so you may implement your own logging mechanism instead
            retval.HandledExceptionEvent += LogException; // TODO: Is that a good idea? Why not have them as standard within the AgoRapide-library?
            return retval;
        }

        /// <summary>
        /// Type of <see cref="_db"/> could without any problem be set to 
        /// <see cref="PostgreSQLDatabase"/> instead of 
        /// <see cref="IDatabase"/>
        /// </summary>
        protected BaseDatabase _db;
        protected override BaseDatabase DB => _db ?? (_db = GetDatabase(GetType()));

        protected void DBDispose() {
            if (_db != null) _db.Dispose();
        }

        /// <summary>
        /// Insert your preferred logging mechanism in <see cref="BaseController.LogFinal"/>, <see cref="BaseController.LogException"/>, <see cref="Startup.Log"/>, 
        /// </summary>
        /// <param name="text"></param>
        private static void LogFinal(string text) => Util.Log(text);

        /// <summary>
        /// Insert your preferred logging mechanism in <see cref="BaseController.LogFinal"/>, <see cref="BaseController.LogException"/>, <see cref="Startup.Log"/>, 
        /// </summary>
        /// <param name="ex"></param>
        public static void LogException(Exception ex) => Util.LogException(ex);
    }
}