using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    //public class Enum {
    //    public static string A => "B";
    //}

        /// <summary>
        /// TODO: DELETE THIS. Replace with Key (other EnumClass goes into <see cref="ClassAndMethod"/>)
        /// </summary>
    [PropertyKey(
        Description = "Represents an Enum with values",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class EnumClass : ApplicationPart {

        public static void RegisterCoreEnumClasses(IDatabase db) {
            void Register<T>() where T : struct, IFormattable, IConvertible, IComparable
            { // What we really would want is "where T : Enum"
                RegisterEnumClass<T>(db);
            }
            Register<CoreP>(); 
            Register<AccessLevel>();
            Register<APIMethodOrigin>();
            Register<CoreAPIMethod>();
            Register<DateTimeFormat>();
            Register<DBField>();
            Register<Environment>();
            Register<HTTPMethod>();
            Register<PropertyOperation>();
            Register<ResponseFormat>();
            Register<ResultCode>();
        }

        /// <summary>
        /// TODO: COMPLETE THIS!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumClass"></param>
        /// <param name="db"></param>
        public static void RegisterEnumClass<T>(IDatabase db) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetOrAdd<EnumClass>(typeof(EnumClass), System.Reflection.MethodBase.GetCurrentMethod().Name, db);
            var ec = GetOrAdd<EnumClass>(typeof(T), "", db);
            Util.EnumGetValues<T>().ForEach(e => {
                /// TODO: COMPLETE THIS! 
                /// TODO: Get information from <see cref="EnumMapper"/> for instance
                // db.UpdateProperty()
                // ec.AddProperty(M(CoreP.EnumValue), "");
            });
        }
    }
}
