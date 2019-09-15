// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// TODO: Currently (Sep 2019) properties are stored with individual fields for information about valid and invalid 
    /// TODO: (<see cref="DBField.valid"/> / <see cref="DBField.vid"/> and <see cref="DBField.invalid"/> / <see cref="DBField.iid"/>)
    /// TODO: with the concept of <see cref="PropertyOperation"/> for setting these values.
    /// TODO: A possible change in database structure could be having properties 
    /// TODO: "acting" on other properties, like SetValid / SetInvalid being stored
    /// TODO: as separate properties in the databasen. This would get rid of four <see cref="DBField"/>-values.
    /// TODO: In addition it would also enable true immutable storage of properties in the database, with
    /// TODO: corresponding possibilities for, among others things, blockchain validation.
    [Enum(
        Description = 
            "Describes operations allowed on a -" + nameof(Property) + "-.\r\n" +
            "In general properties should be immutable but AgoRapide makes an exception for the " +
            "concept of confirming that a property is still valid, and the concept of it having been invalidated",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum PropertyOperation {
        None,

        [EnumValue(Description =
            "Indicates that the property is still known to be valid. " +
            "Sets -" + nameof(DBField.valid) + "- to the current timestamp regardless of its current value. " +
            "-" + nameof(DBField.vid) + "- (validator id) will usually be set at the same time.\r\n" +
            "Note that you can not undo a -" + nameof(SetInvalid) + "- operation by -" + nameof(SetValid) + "- " +
            "(because the only field affected by -" + nameof(SetValid) + "- is -" + nameof(DBField.valid) + "-)")]
        SetValid,
        
        [EnumValue(Description =
            "Indicates that the property is no longer valid (no longer current). " +
            "This is the closest you come to a \"delete\" operation in AgoRapide. " +
            "If the property is a root-property (for a -" + nameof(BaseEntity) + "- then that whole entity " +
            "will be considered no longer valid / \"deleted\". " +
            "Sets -" + nameof(DBField.invalid) + "- to the current timestamp regardless of its current value. " +
            "-" + nameof(DBField.vid) + "- (validator id) will usually be set at the same time.\r\n" +
            "Note that you can not undo a -" + nameof(SetValid) + "- operation by -" + nameof(SetInvalid) + "- " +
            "(because the only field affected by -" + nameof(SetInvalid) + "- is -" + nameof(DBField.invalid) + "-)")]
        SetInvalid,
    }
}
