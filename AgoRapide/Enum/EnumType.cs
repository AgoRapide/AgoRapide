using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    [Enum(
        EnumTypeY = EnumType.DocumentationOnlyEnum,
        Description = 
            "Categories different types of enum used in AgoRapide. " + 
            "This enum -" + nameof(EnumType) + "- is itself a -" + nameof(EnumType.DocumentationOnlyEnum) + "-"
    )]
    public enum EnumType {

        None,

        [EnumMember(Description = "Provides a central repository of explanation of terms that are not present in the C# code.")]
        DocumentationOnlyEnum,

        /// <summary>
        /// TODO: Rename into KeyEnum
        /// </summary>
        [EnumMember(
            Description =
                "Constitutes keys for -" + nameof(BaseEntity) + "- -" + nameof(BaseEntity.Properties) + "- collection. " +
                "In AgoRapide library called -" + nameof(CoreP) + "- (often called P in final application)",
            LongDescription = "All -" + nameof(EntityPropertyEnum) + "- map towards -" + nameof(CoreP) + "- at application startup through -" + nameof(EnumMapper) + "-")]
        EntityPropertyEnum,

        /// <summary>
        /// TODO: Rename into ValueEnum. 
        /// </summary>
        [EnumMember(
            Description = "\"Ordinary\" enums used for indicating range of valid values for a given key")]
        DataEnum
    }
}