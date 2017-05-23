// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(
        Description =
            "Used for general sorting. " +
            "A lower value (think like 1'st order of priority, 2'nd order of priority) will put object higher up (make more visible) in a typical AgoRapide sorted list",
        LongDescription =
            "Recommended values are:\r\n" +
            nameof(Important) + " (-1) for important,\r\n" +
            nameof(Neutral) + " (0) (default) for 'ordinary' and\r\n" +
            nameof(NotImportant) + " (1) for not important.\r\n" +
            "In this manner it will be relatively easy to emphasize or deemphasize single properties without having to give values for all the other properties.\r\n" +
            "Eventually expand to -2, -3 or 2, 3 as needed " +
            "(there is no need for expanding the enum -" + nameof(PriorityOrder) + "- itself since any integer is a valid enum value)",
        AgoRapideEnumType =EnumType.EnumValue
    )]
    public enum PriorityOrder {
        Important = -1,
        Neutral = 0,
        NotImportant = 1
    }
}