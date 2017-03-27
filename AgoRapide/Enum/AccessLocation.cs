using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    /// <summary>
    /// As of March 2017 this enum is not used in code, only in documentation. 
    /// 
    /// TODO: ELABORATE MORE ON THE DOCUMENTATION!
    /// </summary>
    [AgoRapide(
        Description = "Describes how access is granted within AgoRapide.",
        LongDescription =
            "Restricting access is the responsibility of " +
            "-" + nameof(IDatabase.TryGetEntities) + "- and " +
            "-" + nameof(Extensions.GetChildPropertiesForUser) + "-")]
    public enum AccessLocation {

        None,

        [AgoRapide(
            Description = "Access through relation (A relation gives right to an involved entity.  Not implemented as of March 2017)",
            LongDescription = 
                "Responsibility of -" + nameof(IDatabase.TryGetEntities) + "- (Not implemented as of March 2017)\r\n" +
                "(note how -" + nameof(Extensions.GetChildPropertiesForUser) + "- also partly implements -" + nameof(AccessLocation.Relation) + "-")]
        Relation,

        [AgoRapide(
            Description = "General access to an individual entity (stored in database as -" + nameof(CoreProperty.AccessLevelRead) + "- and -" + nameof(CoreProperty.AccessLevelWrite) + ")-",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Entity,

        [AgoRapide(
            Description = "For a type (through -" + nameof(AgoRapideAttribute) + "- for that type).",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Type,

        [AgoRapide(Description =
            "For a TProperty / -" + nameof(CoreProperty) + "- (through -" + nameof(AgoRapideAttribute) + "- for that TProperty / -" + nameof(CoreProperty) + "-\r\n" +
            "Typical example here would be -" + nameof(APIMethod) + "- with -" + nameof(AccessType.Read) + "- set to -" + nameof(AccessLevel.Anonymous) + "- and -" + nameof(AccessType.Write) + "- set to -" + nameof(AccessLevel.System) + "-.",
            LongDescription = "Responsibility of -" + nameof(Extensions.GetChildPropertiesForUser) + "-.")]
        Property
    }
}
