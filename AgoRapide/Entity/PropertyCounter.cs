﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Clarify. Is this ever meant for storing in database, or will it only exist in-memory?
    /// TODO: If never stored in database then consider moving to API-namespace
    /// </summary>
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
