using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Note how <see cref="PropertyKey"/> became ubiquitous throughout the library at introduction.
    /// TDOO: Consider if some use of it can be changed back to use of <see cref="AgoRapideAttributeEnriched"/> instead. 
    /// TODO: Notice how connected everything is, starting with the need for describing a <see cref="Property.Key"/>, this
    /// TODO: is assumed to be a suboptimal situation at present.
    /// </summary>
    [AgoRapide(
        Description =
            "Corresponds normally directly to the name of -" + nameof(EnumType.EntityPropertyEnum) + "- like -" + nameof(CoreP) + "-, -P-) used in your application. " +
            "For -" + nameof(AgoRapideAttribute.IsMany) + "- will also contain the " + nameof(Index) + ", like PhoneNumber#1, PhoneNumber#2"
    )]
    public class PropertyKey : ITypeDescriber {

        public AgoRapideAttributeEnriched Key { get; private set; }

        /// <summary>
        /// 0 means not given. This may happen even for <see cref="AgoRapideAttribute.IsMany"/>. 
        /// If relevant then has value 1 or greater. 
        /// </summary>
        public int Index { get; private set; }

        public PropertyKey(AgoRapideAttributeEnriched key) : this(key, 0) { }
        public PropertyKey(AgoRapideAttributeEnriched key, int index) {
            Key = key;
            Index = index;
        }

        public static PropertyKey Parse(string value) => Parse(value, null);
        public static PropertyKey Parse(string value, Func<string> detailer) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidPropertyKeyException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse + detailer.Result("\r\nDetails: "));
        public static bool TryParse(string value, out PropertyKey key) => TryParse(value, out key, out var dummy);
        public static bool TryParse(string value, out PropertyKey key, out string errorResponse) {

            if (EnumMapper.TryGetA(value, out key)) { errorResponse = null; return true; }
            //var retval = EnumMapper.GetAOrDefault(value);
            //if (retval.Key.CoreP != CoreP.None) {
            //    key = new PropertyKey(retval.Key);
            //}

            var t = value.Split('#');
            if (t.Length != 2) {
                key = null;
                errorResponse = "Not a valid " + nameof(CoreP) + " and single # not found.";
                return false;
            }

            var retval = EnumMapper.GetAOrDefault(t[0]);
            if (retval.Key.CoreP == CoreP.None) {
                key = null;
                errorResponse = "First part (" + t[0] + ") not a valid " + nameof(CoreP) + ".";
                return false;
            }

            if (!retval.Key.A.IsMany) {
                key = null;
                errorResponse = "Illegal to use # when not a " + nameof(AgoRapideAttribute.IsMany) + " " + nameof(AgoRapideAttribute) + ".";
                return false;
            }

            if (!int.TryParse(t[1], out var index)) {
                key = null;
                errorResponse = "Second part (" + t[1] + ") not a valid int.";
                return false;
            }

            errorResponse = null;
            key = new PropertyKey(retval.Key, index);
            return false;
        }

        public static void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) =>
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                new ParseResult(new Property(new PropertyKey(agoRapideAttribute), retval), retval) :
                new ParseResult(errorResponse);
            });

        public static PropertyKey GetNextAvailableKey(CoreP coreP, BaseEntityT entity) {
            throw new NotImplementedException();
        }

        public class InvalidPropertyKeyException : ApplicationException {
            public InvalidPropertyKeyException(string message) : base(message) { }
            public InvalidPropertyKeyException(string message, Exception inner) : base(message, inner) { }
        }

        public override string ToString() => Key.PToString + (Index >= 0 ? "" : ("#" + Index));
    }
}