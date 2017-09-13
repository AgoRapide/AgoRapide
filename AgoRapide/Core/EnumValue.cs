// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide.Core {

    [Class(
        Description = 
            "Represents an Enum with values. " + 
            "Based on -" + nameof(EnumValueAttribute) + "- / -" + nameof(PropertyKeyAttribute) + "-.",
        ParentType = typeof(AgoRapide.Core.Enum),
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class EnumValue : ApplicationPart {

        public EnumValueAttribute EVA { get; private set; }
        /// <summary>
        /// Dummy constructor for use by <see cref="BaseDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public EnumValue() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public EnumValue(EnumValueAttribute attribute, BaseDatabase db) : base(attribute) {
            EVA = attribute;
            ConnectWithDatabase(db);
        }

        protected override void ConnectWithDatabase(BaseDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
