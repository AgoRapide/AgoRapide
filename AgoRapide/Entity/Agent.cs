using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide {
    [Class(
        Description = "See -" + nameof(EntityTypeCategory.Agent) + "-.",
        DefinedForClass = nameof(Agent),
        CacheUse = CacheUse.All // Since there is assumed to be a limited number of elements. 
    )]
    public abstract class Agent : BaseEntity, IStaticProperties {

        protected void SetAndStoreCount(CountP id, long value, Result result, BaseDatabase db) => SetAndStoreCount(id.A(), value, result, db);
        /// <summary>
        /// Assumed to be called only once for each <paramref name="key"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <param name="db"></param>
        [ClassMember(Description = "Practical method for both communicating statistics to API client and for storing in database")]
        protected void SetAndStoreCount(PropertyKey key, long value, Result result, BaseDatabase db) {
            result.Count(key.Key.CoreP, value); // This assumes that has not already been counted. 
            db.UpdateProperty(Id, this, key, value, null); // Keep result itself out of this operation
        }

        /// <summary>
        /// <see cref="IStaticProperties.GetStaticProperties"/>
        /// </summary>
        /// <returns></returns>
        public abstract Dictionary<CoreP, Property> GetStaticProperties();
    }

    /// <summary>
    /// By implementing this interface, a <see cref="BaseEntity"/>-class like <see cref="Agent"/> can describe static properties that are always present for that class. 
    /// A typical example would be <see cref="SynchronizerP.SynchronizerExternalType"/>
    /// TODO: Move to separate file
    /// 
    /// TODO: Consider other implementations than <see cref="IStaticProperties"/> since this is really a static concept. 
    /// TODO: Note also that it is inefficient to store these values for each and every instance of <see cref="Agent"/>
    /// TODO: (but we are helped by the fact the very few instances of <see cref="Agent"/> are thought to be created though)
    /// </summary>
    public interface IStaticProperties {
        [ClassMember(Description = "Usually added by -" + nameof(BaseDatabase.TryGetEntityById) + "-.")]
        Dictionary<CoreP, Property> GetStaticProperties();
    }
}
