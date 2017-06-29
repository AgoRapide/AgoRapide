using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Class(Description = "Explicit mutable -" + nameof(Property) + "- for counting. Used by -" + nameof(BaseEntityWithLogAndCount) + "-.")]
    public class PropertyCounter : Property { 

        public PropertyCounter(PropertyKeyWithIndex key, long initialValue) : base(null) {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _value = initialValue;
        }

        public void Count(long increment) {
            _value = ((long)_value) + increment;
        }
    }
}
