using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(
        Description = 
            "Represents an Enum with values. " + 
            "Based on -" + nameof(EnumValueAttribute) + "- / -" + nameof(PropertyKeyAttribute) + "-. ",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class EnumValue : ApplicationPart {

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.GetOrAdd{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public EnumValue() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public EnumValue(EnumValueAttribute attribute) : base(attribute) { }

        /// <summary>
        /// TODO: Move into <see cref="AgoRapide.Core.Enum"/>
        /// </summary>
        /// <param name="db"></param>
        public static void RegisterAndIndexCoreEnumClasses(IDatabase db) {
            void Register<T>() where T : struct, IFormattable, IConvertible, IComparable
            { // What we really would want is "where T : Enum"
                RegisterAndIndexEnumClass<T>(db);
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
        /// TODO: Move into <see cref="AgoRapide.Core.Enum"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumClass"></param>
        /// <param name="db"></param>
        public static void RegisterAndIndexEnumClass<T>(IDatabase db) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);
            var enumType = typeof(T).GetEnumAttribute().AgoRapideEnumType;
            Util.EnumGetValues<T>().ForEach(e => {
                var enumValue = new EnumValue(e.GetEnumValueAttribute());
                enumValue.ConnectWithDatabase(db);
                Documentator.IndexEntity(enumValue);
            });
        }

        public override void ConnectWithDatabase(IDatabase db) => GetOrAdd(A, db, enrichAndReturnThisObject: this);
    }
}
