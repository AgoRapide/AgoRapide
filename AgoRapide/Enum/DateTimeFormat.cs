using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {
    public enum DateTimeFormat {

        None,

        /// <summary>
        /// Example: yyyy-MM-dd HH:mm:ss.fff
        /// <see cref="Configuration.DateAndHourMinSecMsFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSecMs"/> 
        /// </summary>
        DateHourMinSecMs,

        /// <summary>
        /// Example: yyyy-MM-dd HH:mm:ss
        /// <see cref="Configuration.DateAndHourMinSecFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSec"/> 
        /// </summary>
        DateHourMinSec,

        /// <summary>
        /// Example: yyyy-MM-dd HH:mm
        /// <see cref="Configuration.DateAndHourMinFormat"/> corresponds to <see cref="DateTimeFormat.DateHourSec"/> 
        /// </summary>
        DateHourMin,

        /// <summary>
        /// Example: yyyy-MM-dd
        /// <see cref="Configuration.DateOnlyFormat"/> corresponds to <see cref="DateTimeFormat.DateOnly"/> 
        /// </summary>
        DateOnly
    }
}
