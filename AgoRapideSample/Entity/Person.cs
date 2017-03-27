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
    public class Person : BaseEntityT {

        /// <summary>
        /// Note that this way of storing names is valid for only some cultures. See instead 
        /// https://www.w3.org/International/questions/qa-personal-names
        /// for a thorough explanation about how to represent names in different cultures world-wide.
        /// </summary>
        public override string Name {
            get {
                if (TryGetPV(M(CoreProperty.Name), out string retval)) return retval;
                var firstName = PV(P.FirstName.CP(), "");
                var lastName = PV(P.LastName.CP(), "");
                if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName)) {
                    return PV(P.Email.CP(), Id.ToString()); // Use Id if everything else fails.
                }
                if (string.IsNullOrEmpty(firstName)) return lastName;
                if (string.IsNullOrEmpty(lastName)) return firstName;
                return firstName + " " + lastName; // Or maybe lastName + ", " + firstName
            }
        }
        
        public override AccessLevel AccessLevelGiven => PV(P.AccessLevelGiven.CP(), defaultValue: AccessLevel.Relation); 
    }
}