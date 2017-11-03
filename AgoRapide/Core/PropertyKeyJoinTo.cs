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
        "Copying of properties from one entity into another based on -" + nameof(PropertyKeyAttribute.JoinTo) + "-.\r\n" +
        "The concept can be considered similar to using SQL JOINs. " +
        "For instance for Customer.CustomerName JoinTo could be set to type of Order. " +
        "This would result in every Order also showing CustomerName.")]
    public class PropertyKeyJoinTo : PropertyKeyInjected {
        /// <summary>
        /// Must be specified since <see cref="SourceProperty"/> may have multiple values in <see cref="PropertyKeyAttribute.Parents"/>
        /// </summary>
        public Type SourceEntityType { get; private set; }

        public PropertyKey SourceProperty { get; private set; }

        [ClassMember(Description = "The -" + nameof(PropertyKeyAttribute.ForeignKeyOf) + "--property of the aggregation source entity (linking the given entity and the source entity together).")]
        public PropertyKey ForeignKeyProperty { get; private set; }

        /// <summary>
        /// Should only be called at application startup through <see cref="PropertyKeyMapper"/>
        /// </summary>
        /// <param name="aggregationType"></param>
        /// <param name="sourceEntityType"></param>
        /// <param name="sourceProperty"></param>
        /// <param name="key"></param>
        public PropertyKeyJoinTo(Type sourceEntityType, PropertyKey foreignKeyProperty, PropertyKey sourceProperty, PropertyKeyAttributeEnriched key) : base(key) {
            Util.AssertCurrentlyStartingUp();
            SourceEntityType = sourceEntityType;
            ForeignKeyProperty = foreignKeyProperty;
            SourceProperty = sourceProperty;
        }

        /// <summary>
        /// TOOD: OCT 2017. REMOVE COMMENT BELOW:
        /// Note that will also set <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate. 
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities"></param>
        [ClassMember(Description = "Calculates (or rather copies) properties based on keys returned by -" + nameof(GetKeys) + "-.")]
        public static void CalculateValues(Type type, List<BaseEntity> entities) =>
            // Introduced Parallel.ForEach 3 Nov 2017
            Parallel.ForEach(type.GetChildProperties().Values.Select(key => key as PropertyKeyJoinTo).Where(key => key != null), key => {
                entities.ForEach(e => {
                    InvalidObjectTypeException.AssertAssignable(e, type);
                    if (!e.TryGetPV<long>(key.ForeignKeyProperty, out var foreignKey)) {
                        return; // Foreign key does not exist for this entity (for instance like Customer does not exist for Order). Ignore.
                    }
                    var sourceEntity = InMemoryCache.EntityCache.GetValue(foreignKey, () => "Attempting to find " + key.Key.PToString + " for " + e.ToString());
                    if (sourceEntity.TryGetPV(key.SourceProperty, out string value)) {  // NOTE HOW WE USE string ALWAYS AS TYPE HERE
                        e.AddProperty(key, value);                                      // NOTE HOW WE USE string ALWAYS AS TYPE HERE
                    }
                });
            });

        /// <summary>
        /// Called from <see cref="PropertyKeyMapper.MapEnumFinalize"/>
        /// 
        /// Actual values are later calculated by <see cref="CalculateValues"/> 
        /// 
        /// TODO: REMOVE COMMENT:
        /// (note how that one also sets <see cref="PropertyKeyAttribute.HasLimitedRange"/> as appropriate). 
        /// <param name="keys"></param>
        /// <returns></returns>
        public static List<PropertyKeyJoinTo> GetKeys(List<PropertyKey> keys) {
            Util.AssertCurrentlyStartingUp();
            var retval = new List<PropertyKeyJoinTo>();

            // TODO: Oct 2017: As is now, code will silently ignore JoinTo's that are improperly given.
            // TODO: (like JoinTo's to entities without a foreign key pointing back to from where the JoinTo originates)
            // TODO: Fix by starting with all JoinTo's and find a corresponding ForeignKey pointing towards that entity. Throw exception if none found. 
            keys.Where(k => k.Key.A.ForeignKeyOf != null).ForEach(k => {
                k.Key.A.Parents.ForEach(p => { /// Note how multiple parents may share same foreign key. 
                                               /// IMPORTANT: DO NOT CALL p.GetChildProperties (That is, <see cref="Extensions.GetChildProperties"/> as value will be cached, making changes done her invisible)
                    keys.
                        Where(key => key.Key.HasParentOfType(k.Key.A.ForeignKeyOf) && key.Key.A.IsJoinToFor(p)).
                        ForEach(jp => { // Aggregate for all properties that are possible to aggregate over. 

                            // REMOVED 26 OCT 2017. 
                            // InvalidTypeException.AssertEquals(jp.Key.A.Type, typeof(string), () => "NOTE: Somewhat arbitrary restriction. If only the string value is needed for -" + nameof(CalculateValues) + "- then this limitation may be lifted. Details: " + jp.ToString());
                            // TODO: ALLOW ALL TYPES, SEE HARDCODED typeof(string) BELOW
                            // TOOD: ALLOW ALL TYPES. START WITH ALLOWING ENUMS AND ITypeDescriber

                            var joinToKey = new PropertyKeyJoinTo(
                                p,
                                k,
                                jp,
                                new PropertyKeyAttributeEnrichedDyn(
                                    new PropertyKeyAttribute(
                                            property: GetKeyName(p, jp),
                                            description: "-" + jp.Key.PToString + "- copied from -" + k.Key.A.ForeignKeyOf.ToStringVeryShort() + "- to -" + p.ToStringVeryShort() + "-.)",
                                            longDescription: "",
                                            isMany: false
                                        ) {
                                        Parents = new Type[] { p },
                                        // Type = jp.Key.A.Type,
                                        Type = typeof(string), // TODO: ALLOW ALL TYPES. START WITH ALLOWING ENUMS AND ITypeDescriber
                                        HasLimitedRange = jp.Key.A.HasLimitedRange,

                                        /// TODO: Note how <see cref="BaseEntity.ToHTMLTableRowHeading"/> / <see cref="BaseEntity.ToHTMLTableRow"/> uses
                                        /// TODO: <see cref="Extensions.GetChildPropertiesByPriority(Type, PriorityOrder)"/> which as of Sep 2017
                                        /// TODO: will not take into count access level as set here.
                                        /// TOOD: (while <see cref="BaseEntity.ToHTMLDetailed"/> uses <see cref="Extensions.GetChildPropertiesForUser"/>
                                        AccessLevelRead = AccessLevel.Relation // Important, make visible to user
                                    },
                                    (CoreP)PropertyKeyMapper.GetNextCorePId()
                                )
                            );
                            joinToKey.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate(); // HACK!    
                            retval.Add(joinToKey);
                        });
                });
            });
            return retval;
        }

        public static string GetKeyName(Type joinedToParent, PropertyKey joinToProperty) =>
            (joinedToParent.ToStringVeryShort() + "_" + joinToProperty.Key.PToString). // Note "." would be better than "_" for delimiting entity type with field name because that would be more SQL-like
            Replace(joinedToParent.ToStringVeryShort() + "_" + joinedToParent.ToStringVeryShort(), joinedToParent.ToStringVeryShort() + "_"); // This replace will turn for instance Customer_CustomerName into CustomerName
    }
}