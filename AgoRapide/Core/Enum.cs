// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
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
        public Enum(EnumAttribute attribute, BaseDatabase db) : base(attribute) => ConnectWithDatabase(db);

        public static void RegisterAndIndexCoreEnum(BaseDatabase db) =>
            typeof(CoreP).Assembly.GetTypes().Where(t => t.IsEnum).ForEach(t => RegisterAndIndexEnum(t, db)); /// Going through <see cref="CoreP"/> ensures we get a reference to the AgoRapide assembly

        public static void RegisterAndIndexEnum(Type type, BaseDatabase db) { // where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);            
            Documentator.IndexEntity(new Enum(type.GetEnumAttribute(), db)); // Note how all Enums are registered, even those without any attributes.
            Util.EnumGetValues(type).ForEach(e => Documentator.IndexEntity(new EnumValue(e.GetEnumValueAttribute(), db)));            
        }

        protected override void ConnectWithDatabase(BaseDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
