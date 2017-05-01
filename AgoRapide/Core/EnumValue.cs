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
            "Based on -" + nameof(EnumValueAttribute) + "- / -" + nameof(PropertyKeyAttribute) + "-.",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class EnumValue : ApplicationPart {

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public EnumValue() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public EnumValue(EnumValueAttribute attribute) : base(attribute) { }

        public override void ConnectWithDatabase(IDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
