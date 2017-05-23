// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(
        AgoRapideEnumType = EnumType.DocumentationOnlyEnum,
        Description = 
            "Categories different types of enum used in AgoRapide. " + 
            "This enum -" + nameof(EnumType) + "- is itself a -" + nameof(EnumType.DocumentationOnlyEnum) + "-"
    )]
    public enum EnumType {

        None,

        [EnumValue(
            Description = "Provides a central repository of explanation of terms that are not present in the C# code.",
            LongDescription = "Corresponding -" + nameof(BaseAttribute) + "- is -" + nameof(EnumAttribute) + "- / -" + nameof(EnumValueAttribute) + "-. "
        )]
        DocumentationOnlyEnum,

        [EnumValue(
            Description = "\"Ordinary\" enums used for indicating range of valid values for a given key",
            LongDescription = "Corresponding -" + nameof(BaseAttribute) + "- is -" + nameof(EnumAttribute) + "- / -" + nameof(EnumValueAttribute) + "-. "
        )]
        EnumValue,

        [EnumValue(
            Description =
                "Constitutes keys for -" + nameof(BaseEntity) + "- -" + nameof(BaseEntity.Properties) + "- collection. " +
                "In AgoRapide library called -" + nameof(CoreP) + "- (often called P in final application)",
            LongDescription = 
                "Corresponding -" + nameof(BaseAttribute) + "- is -" + nameof(EnumAttribute) + "- / -" + nameof(PropertyKeyAttribute) + "-. " +
                "All -" + nameof(PropertyKey) + "- map towards -" + nameof(CoreP) + "- at application startup through -" + nameof(PropertyKeyMapper) + "-")]
        PropertyKey
    }
}