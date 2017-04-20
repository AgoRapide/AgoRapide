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

namespace AgoRapide.Core {

    [Class(AccessLevelRead = AccessLevel.Admin, AccessLevelWrite = AccessLevel.System)]
    public class Configuration : ApplicationPart {

        public ConfigurationAttribute _ca { get; private set; }
        public ConfigurationAttribute CA { get => _ca ?? throw new NullReferenceException(nameof(CA)); set => _ca = value ?? throw new NullReferenceException(nameof(value)); }

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.GetOrAdd{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public Configuration() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }

        public Configuration(ConfigurationAttribute configurationAttribute) :base(configurationAttribute) => CA = configurationAttribute;
        
        public void ConnectWithDatabase(IDatabase db) {
            var cid = GetOrAdd(System.Reflection.MethodBase.GetCurrentMethod(), db).Id;

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