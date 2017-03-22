using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [AgoRapide(
        Description = "Represents an Enum with values",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class EnumClass<TProperty> : ApplicationPart<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        public static void RegisterCoreEnumClasses(IDatabase<TProperty> db) {
            RegisterEnumClass<AccessLevel>(db);
            RegisterEnumClass<APIMethodOrigin>(db);
            RegisterEnumClass<CoreMethod>(db);
            RegisterEnumClass<CoreProperty>(db);
            RegisterEnumClass<DateTimeFormat>(db);
            RegisterEnumClass<DBField>(db);
            RegisterEnumClass<Environment>(db);
            RegisterEnumClass<HTTPMethod>(db);
            RegisterEnumClass<PropertyOperation>(db);
            RegisterEnumClass<ResponseFormat>(db);
            RegisterEnumClass<ResultCode>(db);
            // RegisterEnumClass<RouteSegment>(db);
        }

        /// <summary>
        /// TODO: COMPLETE THIS!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumClass"></param>
        /// <param name="db"></param>
        public static void RegisterEnumClass<T>(IDatabase<TProperty> db) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetOrAdd<EnumClass<TProperty>>(typeof(EnumClass<TProperty>),System.Reflection.MethodBase.GetCurrentMethod().Name, db);
            // 
            var ec = GetOrAdd<EnumClass<TProperty>>(typeof(T), "", db);
            Util.EnumGetValues<T>().ForEach(e => {
                // db.UpdateProperty()
                // ec.AddProperty(M(CoreProperty.EnumValue), "");
            });
        }
    }
}
