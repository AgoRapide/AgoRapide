using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Rename into 
    /// TODO: Fix meaning of this. Code now looks like this is an Enum-class.
    /// </summary>
    [Class(
        Description = 
            "Represents an Enum with values. " + 
            "Based on -" + nameof(EnumValueAttribute) + "-. ",
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

        ///// <summary>
        ///// TODO: Move into <see cref="AgoRapide.Core.Enum"/>
        ///// </summary>
        ///// <param name="db"></param>
        //public static void RegisterCoreEnumClasses(IDatabase db) {
        //    void Register<T>() where T : struct, IFormattable, IConvertible, IComparable
        //    { // What we really would want is "where T : Enum"
        //        RegisterEnumClass<T>(db);
        //    }
        //    Register<CoreP>(); 
        //    Register<AccessLevel>();
        //    Register<APIMethodOrigin>();
        //    Register<CoreAPIMethod>();
        //    Register<DateTimeFormat>();
        //    Register<DBField>();
        //    Register<Environment>();
        //    Register<HTTPMethod>();
        //    Register<PropertyOperation>();
        //    Register<ResponseFormat>();
        //    Register<ResultCode>();
        //}

        ///// <summary>
        ///// TODO: COMPLETE THIS!
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="enumClass"></param>
        ///// <param name="db"></param>
        //public static void RegisterEnumClass<T>(IDatabase db) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        //    var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);
        //    throw new NotImplementedException();
        //    Util.EnumGetValues<T>().ForEach(e => {
        //        /// TODO: COMPLETE THIS! 
        //        /// TODO: Get information from <see cref="EnumMapper"/> for instance
        //        // db.UpdateProperty()
        //        // ec.AddProperty(M(CoreP.EnumValue), "");
        //    });
        //}

        public override void ConnectWithDatabase(IDatabase db) => GetOrAdd(A, db, enrichAndReturnThisObject: this);
    }
}
