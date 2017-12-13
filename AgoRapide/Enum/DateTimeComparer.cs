using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {
    public enum DateTimeComparer {
        None,
        ThisHour,
        LastHour,
        Today,
        Yesterday,
        ThisWeek,
        LastWeek,
        ThisMonth, 
        LastMonth,
        ThisQuarter,
        LastQuarter,
        ThisYear,
        LastYear,
        Last7Days,
        Last30Days,
        Last90Days,
    }
}
