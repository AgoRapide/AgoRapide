using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.API {
    public abstract class BaseView<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable {
        protected Request<TProperty> Request;
        public BaseView(Request<TProperty> request) => Request = request ?? throw new ArgumentNullException(nameof(request));
        public abstract object GenerateResult();

        protected static CorePropertyMapper<TProperty> _cpm = new CorePropertyMapper<TProperty>();
        protected static TProperty M(CoreProperty coreProperty) => _cpm.Map(coreProperty);
    }
}
