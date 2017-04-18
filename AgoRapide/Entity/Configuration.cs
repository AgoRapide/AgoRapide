using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using System.Web.Http;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// TODO: Move to API-folder (no need to collect all <see cref="BaseEntity"/> in one place.
    /// 
    /// TODO: Add MemberAttribute information for each property given by <see cref="ConfigurationAttribute.Properties"/>
    /// </summary>
    [AgoRapide(AccessLevelRead = AccessLevel.Admin, AccessLevelWrite = AccessLevel.System)]
    public class Configuration : ApplicationPart {

        public ConfigurationAttribute A { get; private set; }
        public Configuration(ConfigurationAttribute configurationAttribute) {
            A = configurationAttribute;
        }

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase"/>. DO NOT USE!
        /// </summary>
        public Configuration() {
        }

        public void ConnectWithDatabase(IDatabase db) {
            var cid = GetOrAdd<ClassAndMethod>(typeof(Configuration), System.Reflection.MethodBase.GetCurrentMethod().Name, db).Id;

            /// TODO: Duplicate code in <see cref="APIMethod.FilterConnectWithDatabaseAndAddMethod"/> and <see cref="Configuration.ConnectWithDatabase"/>
            GetOrAdd(typeof(Configuration), null, db, enrichAndReturnThisObject: this);

            /// TODO: Duplicate code in <see cref="APIMethod.FilterConnectWithDatabaseAndAddMethod"/> and <see cref="Configuration.ConnectWithDatabase"/>
            // TDOO: MOVE THIS TO A MORE GENERAL PLACE (Into BaseEntity for instance?
            //
            // TODO: BIG WEAKNESS HERE. We do not know the generic value of what we are asking for
            // TODO: The result will be to store as DBField.strv instead of a more precise type.
            // TODO: Implement some kind of copying of properties in order to avoid this!
            // TODO: (or rather, solve the general problem of using generics with properties)
            this.A.Properties.Values.ForEach(p => {
                if (typeof(bool).Equals(p.Key.Key.A.Type)) {
                    db.UpdateProperty(cid, this, p.Key, p.V<bool>(), result: null);
                    // TODO: Maybe replace this check with extension-method IsStoredAsStringInDatabase or similar...
                } else if (typeof(Type).Equals(p.Key.Key.A.Type) || p.Key.Key.A.Type.IsEnum || typeof(string).Equals(p.Key.Key.A.Type)) {
                    var value = p.V<string>();
                    db.UpdateProperty(cid, this, p.Key, value, result: null);
                } else {
                    throw new InvalidTypeException(p.Key.Key.A.Type, "Not implemented copying of properties. Details: " + p.ToString());
                }
            });
        }
    }
}