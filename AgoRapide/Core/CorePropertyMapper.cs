using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// Helper class matching <see cref="CoreProperty"/> to the chosen <see cref="TProperty"/> used in your project (usually P).
    /// 
    /// Usually initialized like
    ///    static CorePropertyMapper[P] _cpm = new CorePropertyMapper[P]();
    /// together with 
    ///    static TProperty M(CoreProperty coreProperty) => _cpm.Map(coreProperty);
    /// in all classes where you need this functionality.
    /// 
    /// TODO: Initialization of <see cref="dict"/> could accept missing CoreProperties by just mapping them to integer values not 
    /// TODO: defined as TProperty.
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public class CorePropertyMapper<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// Note how all valid <see cref="CoreProperty"/> values are asserted (at initialization of this class) that they match <see cref="TProperty"/> so
        /// any use of <see cref="Map"/>(<see cref="CoreProperty"/>) is guaranteed to give a result.
        /// </summary>
        public TProperty Map(CoreProperty coreProperty) => dict.GetValue2(coreProperty);

        /// <summary>
        /// Note how "missing" <see cref="CoreProperty"/>-values are mapped silently to integer values "after" the max value for <see cref="TProperty"/>
        /// 
        /// Recursivity warning: Note how <see cref="Extensions.GetAgoRapideAttribute{T}"/> uses <see cref="CorePropertyMapper{TProperty}.dict"/> 
        /// which again uses <see cref="Extensions.GetAgoRapideAttribute{T}"/>. This recursive call is OK as long as 
        /// <see cref="CorePropertyMapper{TProperty}.dict"/> only calls <see cref="Extensions.GetAgoRapideAttribute{T}"/> with
        /// defined values for <see cref="TProperty"/>.
        /// </summary>
        private Dictionary<CoreProperty, TProperty> dict = new Func<Dictionary<CoreProperty, TProperty>>(() => {
            var retval = new Dictionary<CoreProperty, TProperty>();
            var lastId = (int)(object)Util.EnumGetValues<TProperty>().Max();
            Util.EnumGetValues<CoreProperty>().ForEach(coreProperty => {
                // See recursivity warning above.
                var matching = Util.EnumGetValues<TProperty>().Where(tProperty => tProperty.GetAgoRapideAttribute().A.CoreProperty == coreProperty).ToList();
                switch (matching.Count) {
                    case 0: retval[coreProperty] = (TProperty)(object)(++lastId); break; // Silently map those missing
                    case 1: retval[coreProperty] = matching[0]; break;
                    case 2: throw new CorePropertyNotMatchedException(coreProperty, "Multiple matches found (" + string.Join(", ", matching) + "), only one is allowed");
                }
            });
            return retval;
        })();
        private Dictionary<TProperty, CoreProperty> _reverseDict = null;
        private Dictionary<TProperty, CoreProperty> ReverseDict { // We can not use field initializer here because we have to refer to dict which is also field initialized
            get => _reverseDict ?? (_reverseDict = new Func<Dictionary<TProperty, CoreProperty>>(() => {
                var retval = new Dictionary<TProperty, CoreProperty>();
                dict.ForEach(e => retval.Add(e.Value, e.Key));
                return retval;
            })());
        }
        /// <summary>
        /// <see cref="MapReverse(TProperty)"/> is provided only for completeness. Most probably you need to use <see cref="TryMapReverse"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public CoreProperty MapReverse(TProperty p) => TryMapReverse(p, out var retval) ? retval : throw new Exception("MapReverse failed for " + typeof(TProperty) + "." + p.ToString() + ", corresponding " + typeof(CoreProperty) + " not found");
        public bool TryMapReverse(TProperty p, out CoreProperty coreProperty) => ReverseDict.TryGetValue(p, out coreProperty);

        public class CorePropertyNotMatchedException : ApplicationException {
            public CorePropertyNotMatchedException(CoreProperty coreProperty) : this(coreProperty, null) { }
            /// <summary>
            /// TODO: Error message is no longer relevant since we now (Jan 2017) silently map missing CoreProperty-enums to the next integer values for TProperty
            /// </summary>
            /// <param name="coreProperty"></param>
            /// <param name="message"></param>
            public CorePropertyNotMatchedException(CoreProperty coreProperty, string message) : base(
                nameof(CoreProperty) + "." + coreProperty.ToString() + " does not have a matching " +
                nameof(TProperty) + " (" + typeof(TProperty) + ") value defined. " +
                "Resolution: You must define either " + typeof(TProperty) + "." + coreProperty.ToString() + " or " +
                "define a " + typeof(TProperty) + " with " + nameof(AgoRapideAttribute) + "." + nameof(AgoRapideAttribute.CoreProperty) + " = " + nameof(CoreProperty) + "." + coreProperty.ToString() +
                (string.IsNullOrEmpty(message) ? "" : (" (Additional explanation: " + message + ")"))) { }
        }
    }
}
