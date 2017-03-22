using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Note increasing level of access making it possible to compare with greater than / lesser than in code
    /// 
    /// See corresponding <see cref="CoreProperty.AccessLevelGiven"/>, <see cref="CoreProperty.AccessLevelUse"/>, <see cref="CoreProperty.AccessLevelRead"/> and <see cref="CoreProperty.AccessLevelWrite"/>
    /// 
    /// See also <see cref="AccessType"/> and <see cref="AccessLocation"/>
    /// </summary>
    [AgoRapide(
        Description = 
            "Describes level of access, from -" + nameof(Anonymous) + "- to -" + nameof(System) + "-.",
        LongDescription =
            "See -" + nameof(AccessLocation) + "- for how -" + nameof(AccessLevel) + "- " +
            "(for -" + nameof(AccessType.Read) + "- / -" + nameof(AccessType.Write) + "-) may be specified.")]
    public enum AccessLevel {
        None,

        Anonymous,

        User,

        /// <summary>
        /// TODO: Elaborate on this. Is this needed in addition to <see cref="AccessLocation.Relation"/>???
        /// </summary>
        [AgoRapide(Description = "A relation has to exist between the current user and the entity in question.")]
        Relation,

        Admin,

        System
    }
}
