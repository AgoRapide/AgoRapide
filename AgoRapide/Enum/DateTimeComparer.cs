using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Enum <see cref="DateTimeComparer"/> is candidate for expansion. 
    /// TODO: For instance as a separate concept called DisjointQuery or similar.
    /// TODO: Also note how <see cref="DateTimeComparer"/> and <see cref="ExpansionType"/> attack the same type of
    /// TODO: problem from two opposite directions. 
    /// TODO: The former changes automatically with time while the latter changes data statically (the point of reference is different).
    /// TODO: The former is considered more much more efficient (we could add elements like "Last30Days" for instance).
    /// 
    /// Note how the naming convention used strives to group elements together.
    /// </summary>
    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum DateTimeComparer {
        None,
        HourThis,
        HourLast,
        DayToday,
        Day_Yesterday, // Underscore ensures logical alphabetical sorting.
        Day2DaysAgo,
        //ThisWeek,
        //LastWeek,
        MonthThisMonth,
        MonthThisLastYear,
        MonthThis2YearsYearAgo,
        MonthLastMonth,
        MonthLastLastYear,
        MonthLast2YearsAgo,
        QuarterThisQuarter,
        QuarterThisLastYear,
        QuarterThis2YearsAgo,
        QuarterLastQuarter,
        QuarterLastLastYear,
        QuarterLast2YearsAgo,
        YearThis,
        YearLast,
        Year2YearsAgo,
        //Last7Days,
        //Last30Days,
        //Last90Days,
    }
}
