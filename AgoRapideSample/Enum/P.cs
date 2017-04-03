﻿using System;
using System.ComponentModel;
using AgoRapide;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide.API;

/// <summary>
/// TODO: Clean up this file. There are too many classes in it.
/// </summary>
namespace AgoRapideSample {

    [AgoRapide(
        Description = "The central vision of AgoRapide is to put as much as possible of your application logic into this enum (together with -" + nameof(CoreP) + "-)",
        LongDescription =
            "A lot of API-functionality is autogenerated based on this enum:\r\n" +
            "1) Autogenerating of -" + nameof(APIMethod) + "- like -" + nameof(CoreMethod.AddEntity) + "-\r\n" +
            "2) Validation of input parameters in -TryGetRequest-\r\n" + // This is not allowed: nameof(BaseController.TryGetRequest) + 
            "3) Documentation with sample parameters.\r\n" +
            "4) Unit testing.\r\n" +
            "and so on and so on",
        EnumType = EnumType.EntityPropertyEnum
    )]
    public enum P {
        None,

        /// <summary>
        /// NOTE: This way of storing names (<see cref="FirstName"/> / <see cref="LastName"/>) is valid for only some cultures. See 
        /// NOTE: https://www.w3.org/International/questions/qa-personal-names
        /// NOTE: for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [AgoRapide(Group = typeof(PersonPropertiesDescriber), Type = typeof(string))]
        FirstName,

        /// <summary>
        /// NOTE: This way of storing names (<see cref="FirstName"/> / <see cref="LastName"/>) is valid for only some cultures. See 
        /// NOTE: https://www.w3.org/International/questions/qa-personal-names
        /// NOTE: for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [AgoRapide(Group = typeof(PersonPropertiesDescriber), Type = typeof(string))]
        LastName,

        [AgoRapide(Group = typeof(PersonPropertiesDescriber), Type = typeof(DateTime), DateTimeFormat = DateTimeFormat.DateOnly)]
        DateOfBirth,

        /// <summary>
        /// Note how <see cref="NorwegianPostalCode"/> implements <see cref="ITypeDescriber"/>. 
        /// </summary>
        [AgoRapide(Group = typeof(PersonPropertiesDescriber), Type = typeof(NorwegianPostalCode))]
        PostalCode,

        /// <summary>
        /// Note how this is candidate for a <see cref="ITypeDescriber"/>
        /// </summary>
        [AgoRapide(Group = typeof(PersonPropertiesDescriber), IsMany = true, Type = typeof(string))]
        PhoneNumber,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, MinValueDbl = 1, ValidValues = new string[] { "A", "B" }, InvalidValues = new string[] { "C", "D" })]
        TestString,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, SampleValues = new string[] { "2017-12-09", "1968-12-09" }, InvalidValues = new string[] { "2017-12-09 00:00", "C", "D" })]
        TestDateOnly,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, SampleValues = new string[] { "2017-01-13 08:00" }, InvalidValues = new string[] { "2017-12-09", "C", "D" })]
        TestDateAndtime,

        [AgoRapide(
            Description = "Used for demo-method in -" + nameof(AnotherController.DemoDoubler) + "-",
            SampleValues = new string[] { "42", "1968", "2001" },
            InvalidValues = new string[] { "1.0" },
            Type = typeof(long))]
        SomeNumber,

        [AgoRapide(
            Type = typeof(Colour),
            Parents = new Type[] { typeof(Car) },
            IsObligatory = true,
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        Colour,

        #region CoreP
        /// This region changes names and meaning of <see cref="CoreP"/> values


        /// <summary>
        /// Note how the name (in the form av <see cref="AgoRapideAttributeEnriched.PToString"/>) changes also
        /// for <see cref="CoreP.Username"/> from <see cref="CoreP.Username"/> into <see cref="P.Email"/>. 
        /// This reflects all the way into the core of AgoRapide resulting in <see cref="P.Email"/> being used even for storing in database.
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.Username)]
        Email,

        [AgoRapide(Group = typeof(PersonPropertiesDescriber), InheritAndEnrichFromProperty = CoreP.Password)]
        Password,

        [AgoRapide(Group = typeof(PersonPropertiesDescriber), InheritAndEnrichFromProperty = CoreP.EntityToRepresent)]
        EntityToRepresent,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.AccessLevelGiven)]
        AccessLevelGiven,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.RepresentedByEntity)]
        RepresentedByEntity,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.RejectCredentialsNextTime)]
        RejectCredentialsNextTime,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.AuthResult)]
        AuthResult,

        #endregion
    }

    public static class Extensions {
        public static PropertyKey A(this P p) => EnumMapper.GetA(p);
    }

    /// <summary>
    /// Describes in one place common user changeable attributes for <see cref="Person"/>
    /// 
    /// TODO: Possible change:
    /// TODO: <see cref="IGroupDescriber"/> could be replaced by enum-"class" level attributes now that we (Mar 2017) map
    /// TODO: TO <see cref="CoreP"/> instead of FROM <see cref="CoreP"/> and can have multiple enums in each project.
    /// TODO: (the enums again can be placed inside each entity class that we want to use)
    /// TODO: In other words, you can, inside the <see cref="Person"/>-class implement an enum called PersonP. 
    /// </summary>
    public class PersonPropertiesDescriber : IGroupDescriber {
        public void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) {
            agoRapideAttribute.AddParent(typeof(Person));
            agoRapideAttribute.A.AccessLevelRead = AccessLevel.Relation;
            agoRapideAttribute.A.AccessLevelWrite = AccessLevel.Relation;
        }
    }

    /// <summary>
    /// TODO: Find a more internationally recognized example instead of Norwegian postal code
    /// TODO: (and preferable something that would be difficult to solve with just <see cref="AgoRapideAttribute.RegExpValidator"/>)
    /// 
    /// <see cref="NorwegianPostalCode"/> is a string "between" 0000 and 9999.
    /// 
    /// This class is maybe a bit over-engineered (and still not complete) but it illustrates in detail how your classes may cooperate with the
    /// AgoRapide validation mechanism.
    /// 
    /// You may use it strongly typed like <see cref="BaseEntityT.PV{NorwegianPostalCode}"/>
    /// 
    /// Please note that you could also have solved the whole validation issue by just having 
    /// used <see cref="AgoRapideAttribute.RegExpValidator"/> in this case, something which would have been a lot simpler.
    /// (TODO: As of March 2017 support for <see cref="AgoRapideAttribute.RegExpValidator"/> is not implemented)
    /// </summary>
    public class NorwegianPostalCode : ITypeDescriber {
        public string Value { get; private set; }
        public override string ToString() => Value ?? throw new NullReferenceException(nameof(Value));

        private NorwegianPostalCode(string value) => Value = value; // Private constructor, value to be trusted

        public static bool TryParse(string value, out NorwegianPostalCode norwegianPostalCode) => TryParse(value, out norwegianPostalCode, out _);
        public static bool TryParse(string value, out NorwegianPostalCode norwegianPostalCode, out string errorResponse) {
            var validatorResult = Validator(value);
            if (validatorResult != null) {
                norwegianPostalCode = null;
                errorResponse = validatorResult;
                return false;
            }
            norwegianPostalCode = new NorwegianPostalCode(value);
            errorResponse = null;
            return true;
        }

        private static Func<string, string> Validator = value => {
            if (value == null) return "value == null";
            if (value.Length != 4) return "value.Length != 4";
            /// Note how result of <see cref="int.TryParse"/> is not wasted because we actually would not use it anyway
            if (!int.TryParse(value, out _)) return "Invalid integer";
            return null;
        };

        /// <see cref="EnrichAttribute"/> is the method that MUST be implemented
        /// 
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// enumAttribute.Cleaner=
        /// 
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!

        /// <summary>
        /// TODO: Do away with need for double overloads (for both <see cref="P"/> and <see cref="CoreP"/>)
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public static void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) => agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value =>
                TryParse(value, out var retval, out var errorResponse) ?
                    new ParseResult(new Property(agoRapideAttribute, retval), retval) :
                    new ParseResult(errorResponse));

        public class InvalidNorwegianPostalCodeException : ApplicationException {
            public InvalidNorwegianPostalCodeException(string message) : base(message) { }
            public InvalidNorwegianPostalCodeException(string message, Exception inner) : base(message, inner) { }
        }
    }
}