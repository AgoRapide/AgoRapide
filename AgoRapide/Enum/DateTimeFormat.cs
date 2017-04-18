using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [AgoRapide(EnumType = EnumType.DataEnum)]
    public enum DateTimeFormat {

        None,

        [AgoRapide(Description = "Example: yyyy-MM-dd HH:mm:ss.fff. -" + nameof(ConfigurationAttribute.DateAndHourMinSecMsFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSecMs) + "-.")]
        DateHourMinSecMs,

        [AgoRapide(Description = "Example: yyyy-MM-dd HH:mm:ss. -" + nameof(ConfigurationAttribute.DateAndHourMinSecFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMinSec) + "-.")]
        DateHourMinSec,

        [AgoRapide(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateAndHourMinFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateHourMin) + "-.")]
        DateHourMin,

        [AgoRapide(Description = "Example: yyyy-MM-dd HH:mm. -" + nameof(ConfigurationAttribute.DateOnlyFormat) + "- corresponds to -" + nameof(DateTimeFormat.DateOnly) + "-.")]
        DateOnly
    }
}
