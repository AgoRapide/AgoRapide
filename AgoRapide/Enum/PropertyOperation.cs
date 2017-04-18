using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(
        Description = "Describes operations allowed on a -" + nameof(Property) + "-",
        EnumTypeY = EnumType.DataEnum
    )]
    public enum PropertyOperation {
        None,

        [EnumMember(Description =
            "Indicates that the property is still known to be valid. " +
            "Sets -" + nameof(DBField.valid) + "- to the current timestamp regardless of its current value. " +
            "Note that you can not undo a -" + nameof(SetInvalid) + "- operation by -" + nameof(SetValid) + "- " +
            "(because the only field affected by -" + nameof(SetValid) + "- is -" + nameof(DBField.valid) + "-)")]
        SetValid,
        
        [EnumMember(Description =
            "Indicates that the property is no longer valid (no longer current). " +
            "This is the closest you come to a \"delete\" operation in AgoRapide. " +
            "If the property is a root-property (for a -" + nameof(BaseEntity) + "- then that whole entity " +
            "will be considered no longer valid / \"deleted\". " +
            "Sets -" + nameof(DBField.invalid) + "- to the current timestamp regardless of its current value. " +
            "Note that you can not undo a -" + nameof(SetInvalid) + "- operation by -" + nameof(SetValid) + "- " +
            "(because the only field affected by -" + nameof(SetValid) + "- is -" + nameof(DBField.valid) + "-)")]
        SetInvalid,
    }
}
