// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide {

    /// <summary>
    /// Note that this class is NOT actually used in the AgoRapide-library itself.
    /// Is is provided out of convenience to make it easy to get started with the library. 
    /// 
    /// You may exclude this class at call to <see cref="APIMethod.SetEntityTypes"/> (usually located in your Startup.cs).
    /// 
    /// Note use of "TPerson" type-parameter in methods like <see cref="BaseController.AddFirstAdminUser{TPerson}"/> and <see cref="BaseController.GeneralQuery{TPerson}"/> 
    /// enabling you to implement your own class substituting for <see cref="Person"/>. 
    /// </summary>
    [Class(
        Description = "Represents a person like employee, customer or similar",
        AccessLevelRead = AccessLevel.Relation,
        AccessLevelWrite = AccessLevel.Relation
    )]
    public class Person : APIDataObject {

        /// <summary>
        /// <see cref="CoreAPIMethod.BaseEntityMethod"/>. 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="request"></param>
        [APIMethod(
            Description = "Creates a new report based on the -" + nameof(CoreP.Context) + "- for person identified by {QueryId}.",
            S1 = nameof(AddReport), S2 = "DUMMY", // TODO: REMOVE "DUMMY". Added Summer 2017 because of bug in routing mechanism.
            AccessLevelUse = AccessLevel.Relation
        )]
        public object AddReport(BaseDatabase db, ValidRequest request) {
            // request.Result.LogInternal("Starting", GetType());
            var properties = new List<(PropertyKeyWithIndex, object)>();
            if (Properties.TryGetValue(CoreP.Context.A().Key.CoreP, out var c)) {
                /// TODO: Improve on <see cref="BaseDatabase.CreateEntity"/>, allow use of List and no PropertyKeyWithIndex (only PropertyKey)
                c.Properties.ForEach(p => properties.Add((p.Value.Key.PropertyKeyWithIndex, p.Value.Value)));
            }
            properties.Add((ReportP.ReportAuthor.A().PropertyKeyWithIndex, Id));
            var report = db.GetEntityById<Report>(db.CreateEntity(Id, typeof(Report), properties, request.Result));
            request.Result.ResultCode = ResultCode.ok;
            request.Result.AddProperty(CoreP.SuggestedUrl.A(), request.API.CreateAPIUrl(report));
            // request.Result.LogInternal("Finished", GetType());
            return request.GetResponse();
        }

        /// <summary>
        /// Note that this way of storing names is valid for only some cultures. See instead 
        /// https://www.w3.org/International/questions/qa-personal-names
        /// for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        public override string IdFriendly { 
            get {
                if (TryGetPV(CoreP.IdFriendly.A(), out string retval)) return retval;
                var firstName = PV(PersonP.FirstName.A(), "");
                var lastName = PV(PersonP.LastName.A(), "");
                if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)) {
                    return PV(PersonP.Email.A(), Id.ToString()); // Use Id if everything else fails.
                }
                if (string.IsNullOrEmpty(firstName)) return lastName;
                if (string.IsNullOrEmpty(lastName)) return firstName;
                return firstName + " " + lastName; // Or maybe lastName + ", " + firstName
            }
        }

        public override AccessLevel AccessLevelGiven => PV(PersonP.AccessLevelGiven.A(), defaultValue: AccessLevel.Relation);
    }

    [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
    public enum PersonP {

        None,

        /// <summary>
        /// NOTE: This way of storing names (<see cref="FirstName"/> / <see cref="LastName"/>) is valid for only some cultures. See 
        /// NOTE: https://www.w3.org/International/questions/qa-personal-names
        /// NOTE: for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [PropertyKey(Type = typeof(string), Size = InputFieldSize.Medium, Group = typeof(PersonPropertiesDescriber), SampleValues = new string[] { "John", "Maria", "Peter", "Ann", "Margareth", "Charles", "Eva", "Bob", "Lucy", "Grace", "Albert" })]
        FirstName,

        /// <summary>
        /// NOTE: This way of storing names (<see cref="FirstName"/> / <see cref="LastName"/>) is valid for only some cultures. See 
        /// NOTE: https://www.w3.org/International/questions/qa-personal-names
        /// NOTE: for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        [PropertyKey(Type = typeof(string), Size = InputFieldSize.Medium, Group = typeof(PersonPropertiesDescriber), SampleValues = new string[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "More", "Taylor", "Anderson", "Jackson", "Harris" })]
        LastName,

        [PropertyKey(Type = typeof(DateTime), Group = typeof(PersonPropertiesDescriber), SampleValues = new string[] { "1968-11-09", "1972-10-16", "1981-04-18", "2000-12-13", "2003-09-05", "2006-04-10" }, DateTimeFormat = DateTimeFormat.DateOnly)]
        DateOfBirth,

        /// <summary>
        /// Note how <see cref="NorwegianPostalCode"/> implements <see cref="ITypeDescriber"/>. 
        /// </summary>
        [PropertyKey(Type = typeof(NorwegianPostalCode), Group = typeof(PersonPropertiesDescriber))]
        PostalCode,

        /// <summary>
        /// Note how this is candidate for a <see cref="ITypeDescriber"/>
        /// </summary>
        [PropertyKey(Type = typeof(string), Group = typeof(PersonPropertiesDescriber))]
        PhoneNumber,

        [PropertyKey(
            Description = "Maximum number of elements for -" + nameof(Result.ToHTMLDetailed) + "- to show in HTML-view. Default is 1000. ",
            Type = typeof(long), Group = typeof(PersonPropertiesDescriber))]
        ConfigHTMLMaxCount,

        /// <summary>
        /// TODO: REMOVE
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(Person) }, MinValueDbl = 1, ValidValues = new string[] { "A", "B" }, InvalidValues = new string[] { "C", "D" })]
        TestString,

        /// <summary>
        /// TODO: REMOVE
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(Person) }, SampleValues = new string[] { "2017-12-09", "1968-12-09" }, InvalidValues = new string[] { "2017-12-09 00:00", "C", "D" })]
        TestDateOnly,

        /// <summary>
        /// TODO: REMOVE
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(Person) }, SampleValues = new string[] { "2017-01-13 08:00" }, InvalidValues = new string[] { "2017-12-09", "C", "D" })]
        TestDateAndtime,

        #region CoreP
        /// This region changes names and meaning of <see cref="CoreP"/> values

        /// <summary>
        /// Note how the name (in the form av <see cref="PropertyKeyAttributeEnriched.PToString"/>) changes also
        /// for <see cref="CoreP.Username"/> from <see cref="CoreP.Username"/> into <see cref="P.Email"/>. 
        /// This reflects all the way into the core of AgoRapide resulting in <see cref="P.Email"/> being used even for storing in database.
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.Username)]
        Email,

        [PropertyKey(Group = typeof(PersonPropertiesDescriber), InheritFrom = CoreP.Password)]
        Password,

        [PropertyKey(Group = typeof(PersonPropertiesDescriber), InheritFrom = CoreP.EntityToRepresent)]
        EntityToRepresent,

        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.AccessLevelGiven)]
        AccessLevelGiven,

        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.RepresentedByEntity)]
        RepresentedByEntity,

        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.RejectCredentialsNextTime)]
        RejectCredentialsNextTime,

        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.AuthResult)]
        AuthResult,

        [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritFrom = CoreP.Context)]
        Context,

        #endregion
    }

    public static class ExtensionsPersonP {
        public static PropertyKey A(this PersonP p) => PropertyKeyMapper.GetA(p);
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
        public void EnrichKey(PropertyKeyAttributeEnriched key) {
            key.AddParent(typeof(Person));
            key.A.AccessLevelRead = AccessLevel.Relation;
            key.A.AccessLevelWrite = AccessLevel.Relation;
        }
    }
}