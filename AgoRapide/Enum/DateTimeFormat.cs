// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum DateTimeFormat {

        None,

        [EnumValue(Description = "Example: yyyy-MM-dd HH:mm:ss.fff. -" + nameof(ConfigurationAttribute.DateAndHourMinSecMsFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSecMs) + "-.")]
        DateHourMinSecMs,

        [EnumValue(Description = "Example: yyyy-MM-dd HH:mm:ss. -" + nameof(ConfigurationAttribute.DateAndHourMinSecFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSec) + "-.")]
        DateHourMinSec,

        [EnumValue(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateAndHourMinFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMin) + "-.")]
        DateHourMin,

        [EnumValue(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateOnlyFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateOnly) + "-.")]
        DateOnly
    }
}
