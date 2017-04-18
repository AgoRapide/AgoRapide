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
        Description = "Describes how access is granted within AgoRapide.",
        LongDescription =
            "Restricting access is the responsibility of " +
            "-" + nameof(IDatabase.TryGetEntities) + "- and " +
            "-" + nameof(Extensions.GetChildPropertiesForUser) + "-")]
    public enum AccessLocation {

        None,

        [EnumMember(
            Description = "Access through relation (A relation gives right to an involved entity.  Not implemented as of March 2017)",
            LongDescription =
                "Responsibility of -" + nameof(IDatabase.TryGetEntities) + "- (Not implemented as of March 2017)\r\n" +
                "(note how -" + nameof(Extensions.GetChildPropertiesForUser) + "- also partly implements -" + nameof(AccessLocation.Relation) + "-")]
        Relation,

        [EnumMember(
            Description = "General access to an individual entity (stored in database as -" + nameof(CoreP.AccessLevelRead) + "- and -" + nameof(CoreP.AccessLevelWrite) + ")-",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Entity,

        [EnumMember(
            Description = "Access for a type (through -" + nameof(AgoRapideAttribute) + "- for that type).",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Type,

        [EnumMember(Description =
            "Access for a " + nameof(CoreP) + "- or en enum mapped to -" + nameof(CoreP) + "- (through -" + nameof(AgoRapideAttribute) + "- for that -" + nameof(CoreP) + "-\r\n" +
            "Typical example here would be -" + nameof(APIMethod) + "- with -" + nameof(AccessType.Read) + "- set to -" + nameof(AccessLevel.Anonymous) + "- and -" + nameof(AccessType.Write) + "- set to -" + nameof(AccessLevel.System) + "-.",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Property
    }
}
