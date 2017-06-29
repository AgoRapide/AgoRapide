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
    public class Agent : BaseEntity {

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
    }
}
