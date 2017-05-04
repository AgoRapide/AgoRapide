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
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public EnumValue() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public EnumValue(EnumValueAttribute attribute) : base(attribute) => EVA = attribute;

        ///// <summary>
        ///// TODO: This overload may be removed by a general relation mechanism for parent-child
        ///// TODO: (marking with attributes what the parent is)
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //public override string ToHTMLDetailed(Request request) {
        //    var retval = new StringBuilder();
        //    var ea = EVA.EnumValue.GetType().GetEnumAttribute();
        //    retval.Append("<p>" + request.API.CreateAPILink(CoreAPIMethod.EntityIndex, "Enum " + ea.EnumType.ToStringVeryShort(), typeof(Enum), ea.Id.IdString) + "</p>");
        //    return base.ToHTMLDetailed(request).ReplaceWithAssert("<!--DELIMITER-->", retval.ToString());
        //}

        public override void ConnectWithDatabase(IDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
