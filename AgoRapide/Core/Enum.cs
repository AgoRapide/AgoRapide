// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(
        Description =
            "Represents an Enum. " +
            "Based on -" + nameof(EnumAttribute) + "-.",
        ChildrenType =typeof(EnumValue), 
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class Enum : ApplicationPart {

        /// <summary>
        /// Dummy constructor for use by <see cref="BaseDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public Enum() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public Enum(EnumAttribute attribute) : base(attribute) { }

        public static void RegisterAndIndexCoreEnum(BaseDatabase db) =>
            typeof(CoreP).Assembly.GetTypes().Where(t => t.IsEnum).ForEach(t => RegisterAndIndexEnum(t, db)); /// Going through <see cref="CoreP"/> ensures we get a reference to the AgoRapide assembly

            //void Register<T>() where T : struct, IFormattable, IConvertible, IComparable
            //{ // What we really would want is "where T : Enum"
            //    RegisterAndIndexEnum<T>(db);
            //}

            //// TODO: Completeness verified April 2017
            //Register<AccessLevel>();
            //Register<AccessLocation>();
            //Register<AccessType>();
            //Register<APIMethodOrigin>();
            //Register<CoreAPIMethod>();

            //Register<CoreP>(); /// Note that based on <see cref="PropertyKeyAttribute"/>
            //Register<ConfigurationAttribute.ConfigurationKey>(); /// Note that based on <see cref="PropertyKeyAttribute"/>

            //Register<DateTimeFormat>();
            //Register<DBField>();
            //Register<EnumType>();
            //Register<Environment>();
            //Register<HTTPMethod>();
            //Register<Operator>();
            //Register<PriorityOrder>();
            //Register<PropertyOperation>();
            //Register<ResponseFormat>();
            //Register<ResultCode>();
        // }

        public static void RegisterAndIndexEnum(Type type, BaseDatabase db) { // where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);

            // Note how all Enums are registered, even those without any attributes.
            var _enum = new Enum(type.GetEnumAttribute());
            _enum.ConnectWithDatabase(db);
            Documentator.IndexEntity(_enum);

            Util.EnumGetValues(type).ForEach(e => {
                var enumValue = new EnumValue(e.GetEnumValueAttribute());
                enumValue.ConnectWithDatabase(db);
                Documentator.IndexEntity(enumValue);
            });
        }

        public override void ConnectWithDatabase(BaseDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
