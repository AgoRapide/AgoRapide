// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapideSample {
    [Class(
        Description = "A car is a personal transportation device.",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.Anonymous
    )]
    public class Car : APIDataObject {
        public override string IdFriendly => "The " + PV<Colour>(CarP.Colour.A()) + " car";
    }

    [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum CarP {
        None,

        [PropertyKey(
            Type = typeof(Colour),
            Parents = new Type[] { typeof(Car) },
            IsObligatory = true,
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        Colour,

        /// <summary>
        /// TODO: To be removed
        /// </summary>
        [PropertyKey(
            Type = typeof(Colour),
            Parents = new Type[] { typeof(Car) },
            IsMany = true,
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        Colour2,

        /// <summary>
        /// TODO: To be removed
        /// </summary>
        [PropertyKey(
            Type = typeof(Colour),
            Parents = new Type[] { typeof(Car) },
            IsMany = true,
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        Colour3,

        [PropertyKey(
            Parents = new Type[] { typeof(Car) },
            ForeignKeyOf = typeof(Person),
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        CarOwner,
    }

    public static class ExtensionsCarP {
        public static PropertyKey A(this CarP p) => PropertyKeyMapper.GetA(p);
    }

}