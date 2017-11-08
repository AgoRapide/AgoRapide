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
    /// TODO: Move to separate file
    /// </summary>
    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum ExpansionType {
        None,

        [EnumValue(Description = "Only year of date, like 2018-09-12 becoming 2018")]
        DateYear,

        [EnumValue(Description = "Only quarter of date, like 2018-09-12 becoming -" + nameof(Quarter.Q4) + "-.")]
        DateQuarter,

        [EnumValue(Description = "Only month of date, like 2018-09-12 becoming 12")]
        DateMonth,

        [EnumValue(Description = "Only weekday of date, like 2018-09-12 becoming -" + nameof(DayOfWeek.Sunday) + "-.")]
        DateWeekday,

        [EnumValue(Description = "Only period of day, like 2018-09-12 09:00 becoming -" + nameof(PeriodOfDay.Morning) + "-.")]
        DatePeriodOfDay,

        [EnumValue(Description = "Only hour of date (0-23), like 2018-09-12 07:52 becoming 7.")]
        DateHour,

        [EnumValue(Description = "Only year and quarter of date, like 2018-09-12 becoming \"2018_Q4\".")]
        DateYearQuarter,

        [EnumValue(Description = "Only year and month of date, like 2018-09-12 becoming \"2018_12\".")]
        DateYearMonth,

        [EnumValue(Description = "Only weekday and period of day, like 2018-09-12 13:00 becoming \"Sunday_Afternoon\".")]
        DateWeekDayPeriodOfDay,

        [EnumValue(Description = "Less than 24 hours is 0 days, less than 48 is 1 day and so on.")]
        DateAgeDays,

        [EnumValue(Description = "Less than 7 days is 0 weeks, less than 14 days is 1 week and so on.")]
        DateAgeWeeks,

        [EnumValue(Description = "Less than 30 days is 0 months, less than 60 days is 1 month and so on (note how months are not calculcated exact as of Sep 2017).")]
        DateAgeMonths,

        [EnumValue(Description = "Less than 365 days is 0 years, less than 730 days is 1 year and so on (note how years are not calculcated exact as of Sep 2017).")]
        DateAgeYears,

        [EnumValue(Description = "Less than 60 minutes is 0 hours, less than 120 is 1 hour and so on.")]
        TimeSpanHours,

        [EnumValue(Description = "Less than 24 hours is 0 days, less than 48 is 1 day and so on.")]
        TimeSpanDays,

        [EnumValue(Description = "Less than 7 days is 0 weeks, less than 14 days is 1 week and so on.")]
        TimeSpanWeeks,

        [EnumValue(Description = "Less than 30 days is 0 months, less than 60 days is 1 month and so on (note how months are not calculcated exact as of Sep 2017).")]
        TimeSpanMonths,

        [EnumValue(Description = "Less than 365 days is 0 years, less than 730 days is 1 year and so on (note how years are not calculcated exact as of Sep 2017).")]
        TimeSpanYears,
    }

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum Quarter {
        None,
        Q1,
        Q2,
        Q3,
        Q4
    }

    [Enum(AgoRapideEnumType = EnumType.EnumValue)]
    public enum PeriodOfDay {
        None,

        [EnumValue(Description = "[00:00 - 06:00>")]
        Night,

        [EnumValue(Description = "[06:00 - 12:00>")]
        Morning,

        [EnumValue(Description = "[12:00 - 18:00>")]
        Afternoon,

        [EnumValue(Description = "[18:00 - 24:00>")]
        Evening,
    }

    public static class ExpansionTypeE {
        public static Type ToExpandedType(this ExpansionType expansionType) {
            switch (expansionType) {
                case ExpansionType.DateYear: return typeof(long);
                case ExpansionType.DateQuarter: return typeof(Quarter);
                case ExpansionType.DateMonth: return typeof(long);
                case ExpansionType.DateWeekday: return typeof(DayOfWeek);
                case ExpansionType.DatePeriodOfDay: return typeof(PeriodOfDay);
                case ExpansionType.DateHour: return typeof(long);
                case ExpansionType.DateYearQuarter: return typeof(string);
                case ExpansionType.DateYearMonth: return typeof(string);
                case ExpansionType.DateWeekDayPeriodOfDay: return typeof(string);
                case ExpansionType.DateAgeDays: return typeof(long);
                case ExpansionType.DateAgeWeeks: return typeof(long);
                case ExpansionType.DateAgeMonths: return typeof(long);
                case ExpansionType.DateAgeYears: return typeof(long);
                case ExpansionType.TimeSpanHours: return typeof(long);
                case ExpansionType.TimeSpanDays: return typeof(long);
                case ExpansionType.TimeSpanWeeks: return typeof(long);
                case ExpansionType.TimeSpanMonths: return typeof(long);
                case ExpansionType.TimeSpanYears: return typeof(long);
                default: throw new InvalidEnumException(expansionType);
            }
        }

        public static bool HasLimitedRange(this ExpansionType expansionType) {
            switch (expansionType) {
                case ExpansionType.DateHour:
                case ExpansionType.DateMonth:
                case ExpansionType.DateYear:       // May have to be removed (or made configurable).
                case ExpansionType.DateAgeYears:   // May have to be removed (or made configurable).
                case ExpansionType.DateWeekday:
                case ExpansionType.DatePeriodOfDay:
                case ExpansionType.DateWeekDayPeriodOfDay:
                case ExpansionType.DateYearQuarter: // May have to be removed (or made configurable).
                case ExpansionType.TimeSpanMonths:  // May have to be removed (or made configurable).
                case ExpansionType.TimeSpanYears:   // May have to be removed (or made configurable).
                    return true;
                default:
                    return false;
            }
        }

        private static Type[] _sourceTypes;
        /// <summary>
        /// Returns either type of <see cref="DateTime"/> or type of <see cref="TimeSpan"/>.
        /// (throws <see cref="InvalidEnumException"/> for <see cref="ExpansionType.None"/>)
        /// </summary>
        /// <param name="expansionType"></param>
        /// <returns></returns>
        public static Type ToSourceType(this ExpansionType expansionType) {
            if (_sourceTypes == null) _sourceTypes = new Func<Type[]>(() => { // NOTE: Method is really a bit over-engineered (but quite performant)
                var expansionTypes = Util.EnumGetValues((ExpansionType)(-1)); // Include .None
                var retval = new Type[expansionTypes.Count];
                expansionTypes.ForEach(e => {
                    var i = (int)e;
                    if (i >= retval.Length) throw new InvalidEnumException(e, "Invalid index (" + i + ") for " + nameof(ExpansionType) + "." + e + ". Possible cause: Integer values specified in enum declaration.");
                    if (e == ExpansionType.None) {
                        retval[i] = null;
                    } else if (e.ToString().StartsWith("Date")) {
                        retval[i] = typeof(DateTime);
                    } else if (e.ToString().StartsWith("TimeSpan")) {
                        retval[i] = typeof(TimeSpan);
                    } else {
                        throw new InvalidEnumException(e);
                    }
                });
                return retval;
            })();
            var _int = (int)expansionType;
            if (_int < 0 || _int >= _sourceTypes.Length) throw new InvalidEnumException(expansionType, "Index (" + _int + ") out of range");
            return _sourceTypes[_int] ?? throw new InvalidEnumException(expansionType); /// Exception would typically happen for <see cref="ExpansionType.None"/>
        }
    }
}