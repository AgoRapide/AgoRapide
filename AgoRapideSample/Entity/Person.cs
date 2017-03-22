using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AgoRapide;
using AgoRapide.Database;
using AgoRapide.Core;
          
namespace AgoRapideSample {

    [AgoRapide(
        Description = "Represents a person like employee, customer or similar",
        AccessLevelRead = AccessLevel.Relation,
        AccessLevelWrite = AccessLevel.Relation
    )]
    public class Person : BaseEntityT<P> {

        /// <summary>
        /// Note that this way of storing names is valid for only some cultures. See instead 
        /// https://www.w3.org/International/questions/qa-personal-names
        /// for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        public override string Name {
            get {
                // Due to bug in Visual Studio 2017 RC build 15.0.26014.0 we can not inline variable declaration here. Leads to CS1003	Syntax error, ',' expected
                string retval; if (TryGetPV<string>(M(CoreProperty.Name), out retval)) return retval;
                var firstName = PV(P.FirstName, "");
                var lastName = PV(P.LastName, "");
                if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)) {
                    return PV(P.Email, Id.ToString()); // Use Id if everything else fails.
                }
                if (string.IsNullOrEmpty(firstName)) return lastName;
                if (string.IsNullOrEmpty(lastName)) return firstName;
                return firstName + " " + lastName; // Or maybe lastName + ", " + firstName
            }
        }
        
        public override AccessLevel AccessLevelGiven => PV(P.AccessLevelGiven, defaultValue: AccessLevel.Relation); 
    }
}