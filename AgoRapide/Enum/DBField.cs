using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Describes the different fields in the database. 
    /// 
    /// TODO: Add information about for which fields to add indexes
    /// TODO: Possible by adding a field to <see cref="AgoRapideAttribute"/>
    /// 
    /// Mostly used for documentation. Also used for generating database schema. 
    /// 
    /// The integer values are those typically used by a datareader like <see cref="Npgsql.NpgsqlDataReader"/>
    /// 
    /// Note how does not contain None (<see cref="Util.EnumGetValues{T}"/> should therefore be used with caution)
    /// 
    /// Future addition to this table should be limited to data types that the database engine actually understands.
    /// Other data types can be stored as <see cref="strv"/> (especially those supporting <see cref="ITypeDescriber"/>)
    /// </summary>
    public enum DBField {

        /// <summary>
        /// Note how does not contain None (<see cref="Util.EnumGetValues{T}"/> should be used with caution)
        /// </summary>
        [AgoRapide(
            Description ="Primary key in database",
            Type = typeof(long))]
        id = 0,

        [AgoRapide(
            Description = "Timestamp when created in database", 
            Type = typeof(DateTime))]
        created = 1,

        [AgoRapide(
            Description = "Creator id (entity which created this property)", 
            Type = typeof(long))]
        cid = 2,

        [AgoRapide(
            Description = "Parent id (entity which this property belongs to)", 
            LongDescription = "Not relevant for entity root properties",
            Type = typeof(long)
            )]
        pid = 3,

        [AgoRapide(
            Description = "Foreign id (only relevant when this property is a relation", 
            LongDescription = 
                "Only relevant if this property is a relation. " +
                "For relations, one entity is designated by " + nameof(pid) + " and the other by " + nameof(fid) + ". " +
                "There is no set requirements for which entity ends up on which side.",
            Type = typeof(long))]
        fid = 4,

        [AgoRapide(
            Description = "The actual name of the property",
            LongDescription =
                "Correponds to the actual TProperty-enum used (usually an enum called P)" +
                "See also " + nameof(AgoRapideAttribute.IsMany) + "-properties",
            Type = typeof(string))]
        key = 5,

        [AgoRapide(
            Description = "Long value", 
            Type = typeof(long))]
        lngv = 6,

        [AgoRapide(
            Description = "Double Long value", 
            Type = typeof(double))]
        dblv = 7,

        [AgoRapide(
            Description = "Bool value", 
            Type = typeof(bool))]
        blnv = 8,

        [AgoRapide(
            Description = "DateTime value", 
            Type = typeof(DateTime))]
        dtmv = 9,

        [AgoRapide(
            Description = "Geometry value. TODO: Not implemented as of Jan 2017", 
            Type = typeof(string))]
        geov = 10,

        [AgoRapide(
            Description = "String value (also used for enums and for " + nameof(ITypeDescriber) + ")", 
            Type = typeof(string))]
        strv = 11,

        [AgoRapide(
            Description = "Timestamp when last known valid", 
            Type = typeof(DateTime))]
        valid = 12,

        [AgoRapide(
            Description = "Validator id (entity which last validated this property)", 
            Type = typeof(long))]
        vid = 13,

        [AgoRapide(
            Description = "Timestamp when invalidated (NULL if still valid (indicates 'current' properties))", 
            Type = typeof(DateTime))]
        invalid = 14,

        [AgoRapide(
            Description = "Invalidator id (entity which invalidated this property)", 
            Type = typeof(long))]
        iid = 15
    }
}