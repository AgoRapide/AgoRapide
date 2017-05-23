﻿// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// NOTE: Never stored in database. 
    /// </summary>
    [Class(Description = "Incoming parameters to an API call. Offered through -" + nameof(ValidRequest) + "-")]
    public class Parameters : BaseEntity { 
        public Parameters(Dictionary<CoreP, Property> parameters) => Properties = parameters;        
        public override string IdFriendly => "Collection of parameters to API call";
    }
}
