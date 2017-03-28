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
