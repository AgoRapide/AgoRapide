// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
    /// See corresponding <see cref="CoreP.AccessLevelGiven"/>, <see cref="CoreP.AccessLevelUse"/>, <see cref="CoreP.AccessLevelRead"/> and <see cref="CoreP.AccessLevelWrite"/>
    /// 
    /// See also <see cref="AccessType"/> and <see cref="AccessLocation"/>
    /// 
    /// TODO: Consider adding something "past" <see cref="System"/> like Denied or similar (so that even the system itself will not try to make changes)
    /// </summary>
    [Enum(
        Description =
            "Describes level of access, from -" + nameof(Anonymous) + "- to -" + nameof(System) + "-.",
        LongDescription =
            "See -" + nameof(AccessLocation) + "- for how -" + nameof(AccessLevel) + "- " +
            "(for -" + nameof(AccessType.Read) + "- / -" + nameof(AccessType.Write) + "-) may be specified.",
        AgoRapideEnumType = EnumType.EnumValue
    )]
    public enum AccessLevel {
        None,

        Anonymous,

        [EnumValue(
            Description = "Access is given for to all entities that are registered as users in the system.",
            LongDescription = "Will in practise correspond to -" + nameof(Anonymous) + "- except that the system can log who accessed what data."
            )]
        User,

        /// <summary>
        /// TODO: Elaborate on this. Is this needed in addition to <see cref="AccessLocation.Relation"/>???
        /// </summary>
        [EnumValue(Description = "A relation has to exist between the current user and the entity in question.",
            LongDescription = "Either the current user IS the entity or there is some kind of relation giving access (like a parent-child relationship for instance).")]
        Relation,

        Admin,

        System
    }
}
