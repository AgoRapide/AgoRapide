using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [AgoRapide(
        Description = "Describes operations allowed on a -" + nameof(Property) + "-",
        EnumType = EnumType.DataEnum
    )]
    public enum PropertyOperation {
        None,

        [AgoRapide(Description =
            "Indicates that the property is still known to be valid. " +
            "Sets -" + nameof(DBField.valid) + "- to the current timestamp regardless of its current value. " +
            "Note that you can not undo a -" + nameof(SetInvalid) + "- operation by -" + nameof(SetValid) + "- " +
            "(because the only field affected by -" + nameof(SetValid) + "- is -" + nameof(DBField.valid) + "-)")]
        SetValid,
        
        [AgoRapide(Description =
            "Indicates that the property is no longer valid (no longer current). " +
            "This is the closest you come to a \"delete\" operation in AgoRapide. " +
            "If the property is a root-property (for a -" + nameof(BaseEntityT) + "- then that whole entity " +
            "will be considered no longer valid / \"deleted\". " +
            "Sets -" + nameof(DBField.invalid) + "- to the current timestamp regardless of its current value. " +
            "Note that you can not undo a -" + nameof(SetInvalid) + "- operation by -" + nameof(SetValid) + "- " +
            "(because the only field affected by -" + nameof(SetValid) + "- is -" + nameof(DBField.valid) + "-)")]
        SetInvalid,

        // This does not fit (it is a read operation, the others are write)
        //[AgoRapide(Description = 
        //    "Asks for history of a given property")]
        //History,
    }
}
