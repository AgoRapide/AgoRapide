﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Add information about for which fields to add indexes
    /// TODO: Possible by adding a field to <see cref="PropertyKeyAttribute"/>
    /// 
    /// The integer values are those typically used by a datareader like <see cref="Npgsql.NpgsqlDataReader"/>
    /// 
    /// Note how does not contain None (<see cref="Util.EnumGetValues{T}"/> should therefore be used with caution)
    /// 
    /// Future addition to this table should be limited to data types that the database engine actually understands.
    /// Other data types can be stored as <see cref="strv"/> (especially those supporting <see cref="ITypeDescriber"/>)
    /// </summary>
    [PropertyKey( 
        Description = "Describes the different fields in the database. Mostly used for documentation. Also used for generating database schema. ",
        EnumType = EnumType.EnumValue)] /// TODO: Consider turning into <see cref="EnumType.PropertyKey"/> (after that has been renamed into KeyEnum)
    public enum DBField {

        /// <summary>
        /// Note how does not contain None (<see cref="Util.EnumGetValues{T}"/> should be used with caution)
        /// </summary>
        [PropertyKey(
            Description ="Primary key in database",
            Type = typeof(long))]
        id = 0,

        [PropertyKey(
            Description = "Timestamp when created in database", 
            Type = typeof(DateTime))]
        created = 1,

        [PropertyKey(
            Description = "Creator id (entity which created this property)", 
            Type = typeof(long))]
        cid = 2,

        [PropertyKey(
            Description = "Parent id (entity which this property belongs to)", 
            LongDescription = "Not relevant for entity root properties",
            Type = typeof(long)
            )]
        pid = 3,

        [PropertyKey(
            Description = "Foreign id (only relevant when this property is a relation", 
            LongDescription = 
                "Only relevant if this property is a relation. " +
                "For relations, one entity is designated by " + nameof(pid) + " and the other by " + nameof(fid) + ". " +
                "There is no set requirements for which entity ends up on which side.",
            Type = typeof(long))]
        fid = 4,

        [PropertyKey(
            Description = "The actual name of the property",
            LongDescription =
                "Correponds to the actual enum used (like -" + nameof(CoreP) + "- or -P-).\r\n" +
                "See also -" + nameof(PropertyKeyAttribute.IsMany) + "--properties",
            Type = typeof(string))]
        key = 5,

        [PropertyKey(
            Description = "Long value", 
            Type = typeof(long))]
        lngv = 6,

        [PropertyKey(
            Description = "Double Long value", 
            Type = typeof(double))]
        dblv = 7,

        [PropertyKey(
            Description = "Bool value", 
            Type = typeof(bool))]
        blnv = 8,

        [PropertyKey(
            Description = "DateTime value", 
            Type = typeof(DateTime))]
        dtmv = 9,

        [PropertyKey(
            Description = "Geometry value. TODO: Not implemented as of Jan 2017", 
            Type = typeof(string))]
        geov = 10,

        [PropertyKey(
            Description = "String value (also used for enums and for " + nameof(ITypeDescriber) + ")", 
            Type = typeof(string))]
        strv = 11,

        [PropertyKey(
            Description = "Timestamp when last known valid", 
            Type = typeof(DateTime))]
        valid = 12,

        [PropertyKey(
            Description = "Validator id (entity which last validated this property)", 
            Type = typeof(long))]
        vid = 13,

        [PropertyKey(
            Description = "Timestamp when invalidated (NULL if still valid (indicates 'current' properties))", 
            Type = typeof(DateTime))]
        invalid = 14,

        [PropertyKey(
            Description = "Invalidator id (entity which invalidated this property)", 
            Type = typeof(long))]
        iid = 15
    }
}