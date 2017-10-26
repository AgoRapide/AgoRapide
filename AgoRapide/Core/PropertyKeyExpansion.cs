// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {
    [Class(Description = 
        "Describes properties in detail based on -" + nameof(PropertyKeyAttribute.ExpansionTypes) + "- like expanding\r\n" +
        "  OrderDate = 2018-12-09 to OrderDate_Quarter = 2018_Q4 (see -" + nameof(ExpansionType.DateYearQuarter) + "-)\r\n" +
        "or expanding\r\n" +
        "  RegisteredDate = 2013-12-09 to RegisteredDate_AgeYears = 5 (see -" + nameof(ExpansionType.DateAgeYears) + "-)\r\n" +
        "or expanding\r\n" +
        "  SwitchOn = 2018-06-03 08:13 to SwitchOn_PeriodOfDay = Morning (see -" + nameof(ExpansionType.DatePeriodOfDay) + "- / -" + nameof(PeriodOfDay.Morning) + "-)\r\n" +
        "\r\n" +
        "Pre-calculating values like this will simplify further querying."
    )]
    public class PropertyKeyExpansion : PropertyKeyInjected {

        [ClassMember(Description = "Standard collection of expansion.")] // TODO: Are these needed? It is possibly better to just the few expansions actually needed. 
        public static ExpansionType[] ExpansionAllDate = new ExpansionType[] {
            ExpansionType.DateYear, ExpansionType.DateQuarter, ExpansionType.DateMonth, ExpansionType.DateWeekday, ExpansionType.DateYearQuarter, ExpansionType.DateYearMonth,
            ExpansionType.DateAgeDays, ExpansionType.DateAgeWeeks, ExpansionType.DateAgeMonths, ExpansionType.DateAgeYears };

        [ClassMember(Description = "Standard collection of expansion.")] // TODO: Are these needed? It is possibly better to just the few expansions actually needed. 
        public static ExpansionType[] ExpansionYearMonthQuarter = new ExpansionType[] {
            ExpansionType.DateYear, ExpansionType.DateQuarter, ExpansionType.DateMonth, ExpansionType.DateWeekday, ExpansionType.DateYearQuarter, ExpansionType.DateYearMonth };

        [ClassMember(Description = "Standard collection of expansion.")] // TODO: Are these needed? It is possibly better to just the few expansions actually needed. 
        public static ExpansionType[] ExpansionAge = new ExpansionType[] {
            ExpansionType.DateAgeDays, ExpansionType.DateAgeWeeks, ExpansionType.DateAgeMonths, ExpansionType.DateAgeYears };

        public ExpansionType ExpansionType { get; private set; }

        [ClassMember(Description = "This will typically be a DateTime-property.")]
        public PropertyKey SourceProperty { get; private set; }

        public PropertyKeyExpansion(ExpansionType expansionType, PropertyKey sourceProperty, PropertyKeyAttributeEnriched key) : base(key) {
            ExpansionType = expansionType;
            SourceProperty = sourceProperty;
        }

        /// <summary>
        /// Note that will also set <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate. 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities"></param>
        [ClassMember(
            Description = "Calculates the actual aggregates based on keys returned by -" + nameof(GetKeys) + "-.",
            LongDescription = "Example: If we have Persons and Projects and every Project has a foreign key LeaderPersonId, then this method will aggregate Count_ProjectLeaderPersonid for every Person.")]
        public static void CalculateValues(Type type, List<BaseEntity> entities) => type.GetChildProperties().Values.Select(key => key as PropertyKeyExpansion).Where(key => key != null).ForEach(key => {
            var hasLimitedRange = true; var valuesFound = new HashSet<string>();

            var now = DateTime.Now;
            entities.ForEach(e => { /// Note that we could possible have entities as outer loop instead, but that would loose similary with <see cref="CalculateForeignKeyAggregates"/>
                InvalidObjectTypeException.AssertAssignable(e, type);

                if (e.Properties.TryGetValue(key.SourceProperty.Key.CoreP, out var sourceProperty)) {
                    string strValue;

                    if (typeof(DateTime).Equals(key.ExpansionType.ToSourceType())) {
                        var dtmValue = sourceProperty.V<DateTime>();
                        switch (key.ExpansionType) { /// Note how AddProperty generic type now chosen must correspond to <see cref="ExpansionTypeE.ToExpandedType"/>
                            case ExpansionType.DateYear: { var v = (long)dtmValue.Year; strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateQuarter: { var v = (Quarter)(((dtmValue.Month - 1) / 3) + 1); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateMonth: { var v = (long)dtmValue.Month; strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateWeekday: { var v = dtmValue.DayOfWeek; strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DatePeriodOfDay: { var v = (PeriodOfDay)((dtmValue.Hour / 6) + 1); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateHour: { var v = (long)dtmValue.Hour; strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateYearQuarter: { var v = dtmValue.Year + "_" + (Quarter)(((dtmValue.Month - 1) / 3) + 1); strValue = v; e.AddProperty(key, v); break; }
                            case ExpansionType.DateYearMonth: { var v = dtmValue.Year + "_" + dtmValue.Month.ToString("00"); strValue = v; e.AddProperty(key, v); break; }
                            case ExpansionType.DateWeekDayPeriodOfDay: { var v = dtmValue.DayOfWeek.ToString() + ((PeriodOfDay)((dtmValue.Hour / 6) + 1)).ToString(); strValue = v; e.AddProperty(key, v); break; }
                            case ExpansionType.DateAgeDays: { var v = (long)(now.Subtract(dtmValue).TotalDays); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateAgeWeeks: { var v = (long)(now.Subtract(dtmValue).TotalDays / 7); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateAgeMonths: { var v = (long)(now.Subtract(dtmValue).TotalDays / 30); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.DateAgeYears: { var v = (long)(now.Subtract(dtmValue).TotalDays / 365); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            default: throw new InvalidEnumException(key.ExpansionType);
                        }
                    } else if (typeof(TimeSpan).Equals(key.ExpansionType.ToSourceType())) {
                        var tspValue = sourceProperty.V<TimeSpan>();
                        switch (key.ExpansionType) { /// Note how AddProperty generic type now chosen must correspond to <see cref="ExpansionTypeE.ToExpandedType"/>
                            case ExpansionType.TimeSpanHours: { var v = (long)(tspValue.TotalHours + .5); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.TimeSpanDays: { var v = (long)(tspValue.TotalDays + .5); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.TimeSpanWeeks: { var v = (long)((tspValue.TotalDays / 7) + .5); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.TimeSpanMonths: { var v = (long)((tspValue.TotalDays / 30) + .5); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            case ExpansionType.TimeSpanYears: { var v = (long)((tspValue.TotalDays / 365) + .5); strValue = v.ToString(); e.AddProperty(key, v); break; }
                            default: throw new InvalidEnumException(key.ExpansionType);
                        }
                    } else {
                        throw new InvalidTypeException(key.ExpansionType.ToSourceType(), nameof(key.ExpansionType) + ": " + key.ExpansionType);
                    }
                    if (!valuesFound.Contains(strValue)) {
                        // TOOD: TURN LIMIT OF 20 INTO A CONFIGURATION-PARAMETER
                        if (valuesFound.Count >= 20) { // Note how we allow up to 20 DIFFERENT values, instead of values up to 20. This means that a distribution like 1,2,3,4,5,125,238,1048 still counts as limited.
                            hasLimitedRange = false;
                        } else {
                            valuesFound.Add(strValue);
                        }
                    }
                }
            });
            if (!key.Key.A.HasLimitedRangeIsSet) { // Note that we do only set this once
                // TOOD: Improve on this situation. First call to this method decides. For instance if set to TRUE here, then it will never change back to FALSE afterwards
                key.Key.A.HasLimitedRange = hasLimitedRange; /// If TRUE then important discovery making it possible for <see cref="Result.CreateDrillDownUrls"/> to make more suggestions.
            }
        });

        /// <summary>
        /// Called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>
        /// 
        /// Actual values are later calculated by <see cref="CalculateValues"/> (note how that one also sets <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate). 
        /// <param name="keys"></param>
        /// <returns></returns>
        [ClassMember(Description =
            "Generates -" + nameof(PropertyKeyExpansion) + "- based on -" + nameof(PropertyKeyAttribute.ExpansionTypes) + "-.")]
        public static List<PropertyKeyExpansion> GetKeys(List<PropertyKey> keys) {
            Util.AssertCurrentlyStartingUp();
            var retval = new List<PropertyKeyExpansion>();

            keys.Where(k => k.Key.A.ExpansionTypes != null && k.Key.A.ExpansionTypes.Length > 0).ForEach(k => {
                k.Key.A.ExpansionTypes.ForEach(e => {
                    InvalidTypeException.AssertEquals(k.Key.A.Type, e.ToSourceType(), () => // TODO: Change to AssertAssignable, only think it through first!
                        "Illegal " + nameof(PropertyKeyAttribute) + "." + nameof(PropertyKeyAttribute.ExpansionTypes) + " (" + e + ") specified for " + k.ToString() + ".\r\n" +
                        "The specified value has a source type of " + e.ToSourceType().ToStringVeryShort() + " while the key is of type " + k.Key.A.Type.ToStringVeryShort() + ".\r\n" +
                        "Possible resolution: Delete " + nameof(PropertyKeyAttribute.ExpansionTypes) + " specified for " + k.ToString() + ".");

                    var expansionKey = new PropertyKeyExpansion(
                        e,
                        k,
                        new PropertyKeyAttributeEnrichedDyn(
                            new PropertyKeyAttribute(
                                    property: k.Key.PToString + "_" + e,
                                    description: "-" + e + "- for -" + k.Key.PToString + "-." + e.GetEnumValueAttribute().Description,
                                    longDescription: "",
                                    isMany: false
                                    ) {
                                Parents = k.Key.A.Parents,
                                PriorityOrder = k.Key.A.PriorityOrder, // Added 13 Oct 2017
                                Type = e.ToExpandedType(),
                                HasLimitedRange = e.HasLimitedRange(),

                                /// TODO: Note how <see cref="BaseEntity.ToHTMLTableRowHeading"/> / <see cref="BaseEntity.ToHTMLTableRow"/> uses
                                /// TODO: <see cref="Extensions.GetChildPropertiesByPriority(Type, PriorityOrder)"/> which as of Sep 2017
                                /// TODO: will not take into count access level as set here.
                                /// TOOD: (while <see cref="BaseEntity.ToHTMLDetailed"/> uses <see cref="Extensions.GetChildPropertiesForUser"/>
                                AccessLevelRead = AccessLevel.Relation // Important, make visible to user
                            },
                                (CoreP)PropertyKeyMapper.GetNextCorePId()
                            )
                        );
                    expansionKey.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate(); // HACK!    
                    retval.Add(expansionKey);
                });
            });
            return retval;
        }
    }
}