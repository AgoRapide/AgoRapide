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
    public class EnumClass : ApplicationPart {

        public static void RegisterCoreEnumClasses(IDatabase db, Action<string> noticeLogger) {
            void Register<T>() where T : struct, IFormattable, IConvertible, IComparable
            { // What we really would want is "where T : Enum"
                RegisterEnumClass<T>(db, noticeLogger);
            }
            Register<CoreProperty>(); /// Note how later calls to <see cref="RegisterEnumClass{T}"/> with equivalant names will override properties found now
            Register<AccessLevel>();
            Register<APIMethodOrigin>();
            Register<CoreMethod>();
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
        /// 
        /// TODO: USE THIS FOR POPULATING COREPROPERTYMAPPER
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumClass"></param>
        /// <param name="db"></param>
        /// <param name="noticeLogger">Used for logging notices about mapping process.</param>
        public static void RegisterEnumClass<T>(IDatabase db, Action<string> noticeLogger) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetOrAdd<EnumClass>(typeof(EnumClass), System.Reflection.MethodBase.GetCurrentMethod().Name, db);
            CorePropertyMapper.RegisterEnum<T>(s => noticeLogger(s));
            var ec = GetOrAdd<EnumClass>(typeof(T), "", db);
            Util.EnumGetValues<T>().ForEach(e => {
                // db.UpdateProperty()
                // ec.AddProperty(M(CoreProperty.EnumValue), "");
            });
        }
    }
}
