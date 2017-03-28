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

    /// <summary>
    /// This is the generic TProperty that is used against AgoRapide
    /// A short name is used since it is repeated all over the application
    /// 
    /// Note how the general goal of AgoRapide is to put as much of your application logic into 
    /// this enum (and <see cref="CoreProperty"/>). A lot of functionality can then be autogenerated based on this enum like
    /// 1) Validation of input parameters (<see cref="BaseController.TryGetRequest"/>.<br>
    /// 2) Documentation with sample parameters.<br>  TODO: ADD LINKS HERE!
    /// 3) Unit testing and so on.<br>
    /// 
    /// -------------------------------
    /// AgoRapide Vision
    /// -------------------------------
    /// The overall vision of AgoRapide is actually to enable you to specify all (or almost all) of the 
    /// functionality of your application inside this enum.
    /// </summary>
    public enum P {
        None,

        /// <summary>
        /// TODO: CONSIDER REMOVING THIS
        /// 
        /// TODO: CREATE COREPROPERTY IF WE ARE GOING TO USE THIS!
        /// </summary>
        [AgoRapide(Type = typeof(System.Net.HttpStatusCode), DefaultValue = nameof(System.Net.HttpStatusCode.NotFound))]
        http_status_code,

        //[Enum(Type = typeof(string))]
        //message,

        /// <summary>
        /// This way of storing names is valid for only some cultures. See 
        /// https://www.w3.org/International/questions/qa-personal-names
        /// for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [AgoRapide(Group = typeof(PUD), Type = typeof(string))]
        FirstName,

        /// <summary>
        /// This way of storing names is valid for only some cultures. See 
        /// https://www.w3.org/International/questions/qa-personal-names
        /// for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [AgoRapide(Group = typeof(PUD), Type = typeof(string))]
        LastName,

        [AgoRapide(Group = typeof(PUD), Type = typeof(NorwegianPostalCode))]
        PostalCode,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, MinValueDbl = 1, ValidValues = new string[] { "A", "B" }, InvalidValues = new string[] { "C", "D" })]
        TestString,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, SampleValues = new string[] { "2016-12-09", "2017-01-13 08:00" }, InvalidValues = new string[] { "C", "D" })]
        TestDate,

        [AgoRapide(
            Description = "Used for demo-method in -" + nameof(AnotherController.DemoDoubler) + "-",
            SampleValues = new string[] { "42", "1968", "2001" },
            InvalidValues = new string[] { "1.0" },
            Type = typeof(long))]
        SomeNumber,

        #region CoreProperty 
        /// Refer to corresponding <see cref="CoreProperty"/> values for documentation.
        /// You only have to map those <see cref="CoreProperty"/> values for which you want to specify attributes.
        /// The other <see cref="CoreProperty"/> will be "silently" mapped to P anyway through <see cref="EnumMapper"/>

        [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.Username)] // TODO: Add email class like Type = typeof(EMail) with standardised AgoRapide interface.
        Email,

        [AgoRapide(Group = typeof(PUD), CoreProperty = CoreProperty.Password)]
        Password,

        [AgoRapide(Group = typeof(PUD), CoreProperty = CoreProperty.EntityToRepresent)]
        EntityToRepresent,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.AccessLevelGiven)]
        AccessLevelGiven,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.RepresentedByEntity)]
        RepresentedByEntity,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.RejectCredentialsNextTime)]
        RejectCredentialsNextTime,

        [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.AuthResult)]
        AuthResult,

        [AgoRapide(
            Type = typeof(Colour),
            Parents = new Type[] { typeof(Car) },
            IsObligatory = true,
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Anonymous)]
        Colour,

        // You only have to map those CoreProperty values for which you want to specify attributes.
        // (because CorePropertyMapper will anyway silently map the missing CoreProperty-enums 
        // to the next integer values for TProperty (P))

        #endregion

        /// <summary>
        /// See <see cref="CorePropertyMapper.Map(CoreProperty)"/> (dict) for a rationale for this
        /// (values after <see cref="Last"/> will be used for silently mapping of <see cref="CoreProperty"/>-values)
        /// See also <see cref="AgoRapideAttribute.IsMany"/> for why you should not use <see cref="int.MaxValue"/> here
        /// </summary>
        Last = 100000
    }

    public static class Extensions {
        public static CPA M(this P p) => EnumMapper.GetCPA(p);
        public static CoreProperty CP(this P p) => EnumMapper.GetCPA(p).cp;
    }

    /// <summary>
    /// Person user-changeable describer. 
    /// The abbrevation used is just a hint for how to shorten down declarations in P-eum.
    /// 
    /// TODO: <see cref="IGroupDescriber"/> could be replaced by enum-"class" level attributes now that we (Mar 2017) map
    /// TODO: TO <see cref="CoreProperty"/> instead of FROM <see cref="CoreProperty"/> and can have multiple enums in each project.
    /// TODO: (the enums again can be placed inside each entity class that we want to use)
    /// </summary>
    public class PUD : IGroupDescriber {
        public void EnrichAttribute(AgoRapideAttributeT<CoreProperty> agoRapideAttribute) {
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
        /// TODO: Do away with need for double overloads (for both <see cref="P"/> and <see cref="CoreProperty"/>)
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public static void EnrichAttribute(AgoRapideAttributeT<P> agoRapideAttribute) => agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => throw new NotImplementedException("We have a chicken-and-egg problem because of the need for P.CP() below which will call " + nameof(EnumMapper) + " again"));
        // public static void EnrichAttribute(AgoRapideAttributeT<P> agoRapideAttribute) => agoRapideAttribute.ValidatorAndParser = GetValidatorAndParser(agoRapideAttribute.P.CP());

        /// <summary>
        /// TODO: Do away with need for double overloads (for both <see cref="P"/> and <see cref="CoreProperty"/>)
        /// </summary>
        /// <param name="agoRapideAttribute"></param>
        public static void EnrichAttribute(AgoRapideAttributeT<CoreProperty> agoRapideAttribute) => agoRapideAttribute.ValidatorAndParser = GetValidatorAndParser(agoRapideAttribute.P);
        /// <summary>
        /// TODO: Do away with need for double overloads (for both <see cref="P"/> and <see cref="CoreProperty"/>)
        /// </summary>
        /// <param name="coreProperty"></param>
        /// <returns></returns>
        private static Func<string, ParseResult> GetValidatorAndParser(CoreProperty coreProperty) => new Func<string, ParseResult>(value =>
                TryParse(value, out var retval, out var errorResponse) ?
                    new ParseResult(new Property(coreProperty, retval), retval) :
                    new ParseResult(errorResponse));
                
        public class InvalidNorwegianPostalCodeException : ApplicationException {
            public InvalidNorwegianPostalCodeException(string message) : base(message) { }
            public InvalidNorwegianPostalCodeException(string message, Exception inner) : base(message, inner) { }
        }
    }
}