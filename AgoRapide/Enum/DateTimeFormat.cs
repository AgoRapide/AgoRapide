using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Enum(EnumTypeY = EnumType.DataEnum)]
    public enum DateTimeFormat {

        None,

        [EnumMember(Description = "Example: yyyy-MM-dd HH:mm:ss.fff. -" + nameof(ConfigurationAttribute.DateAndHourMinSecMsFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSecMs) + "-.")]
        DateHourMinSecMs,

        [EnumMember(Description = "Example: yyyy-MM-dd HH:mm:ss. -" + nameof(ConfigurationAttribute.DateAndHourMinSecFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSec) + "-.")]
        DateHourMinSec,

        [EnumMember(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateAndHourMinFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMin) + "-.")]
        DateHourMin,

        [EnumMember(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateOnlyFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateOnly) + "-.")]
        DateOnly
    }
}
