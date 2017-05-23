// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.API {
    public abstract class BaseView {
        protected Request Request;
        public BaseView(Request request) => Request = request ?? throw new ArgumentNullException(nameof(request));
        public abstract object GenerateResult();
    }
}
