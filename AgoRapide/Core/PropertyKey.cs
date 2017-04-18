using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: The whole distinction between <see cref="PropertyKeyNonStrict"/> and <see cref="Core.PropertyKey"/> is a bit messy as of Apr 2017
    /// TODO: Especially the hack with <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
    /// TODO: There is also the question of creating a new subclass called PropertyKeyIsMany (and moving the Index-property there)
    /// </summary>
    [AgoRapide(
        Description = 
        "Strict version of -" + nameof(PropertyKeyNonStrict) + "-. " +
        "For -" + nameof(AgoRapideAttribute.IsMany) + "- will also contain the " + nameof(Index) + ", like PhoneNumber#1, PhoneNumber#2.\r\n" +
        "Inconsistency between -" + nameof(AgoRapideAttribute.IsMany) + "- and -" + nameof(Index) + "- is not allowed")]
    public class PropertyKey : PropertyKeyNonStrict {

        private int _index;
        /// <summary>
        /// Only allowed to read if 1 or greater. 
        /// (Check for <see cref="AgoRapideAttribute.IsMany"/> before attempting to access this property)
        /// TODO: Try to solve in a better manner by creating a subclass PropertyKeyIsMany or similar.
        /// </summary>
        public int Index {
            get => _index > 0 ? _index : throw new InvalidPropertyKeyException("Invalid to read " + nameof(Index) + " when not set.\r\nDetails: " + Key.A.ToString()); // Careful not to call ToString() here.
            protected set => _index = value;
        }

        public CoreP IndexAsCoreP => (CoreP)(object)(int.MaxValue - (_index > 0 ? _index : throw new InvalidPropertyKeyException(nameof(Index) + " not set. Details: " + Key.ToString())));

        public PropertyKey(AgoRapideAttributeEnriched key) : this(key, 0) { }
        public PropertyKey(AgoRapideAttributeEnriched key, int index) : base(key) {
            Index = index;
            if (index == IS_MANY_PARENT_OR_TEMPLATE_INDEX) {
                // Do not check for consistency. HACK.
            } else {
                if (key.A.IsMany) {
                    if (index <= 0) {
                        if (this is PropertyKey) {
                            throw new InvalidPropertyKeyException(
                                /// TODO: As of Apr 2017 it is not possible to use <see cref="AgoRapideAttribute.IsMany"/> in connection with <see cref="CoreMethod.AddEntity"/>. 
                                nameof(Index) + " missing for " + nameof(Key.A.IsMany) + " for " + ToString() + ".\r\n" +
                                "Possible resolution: You may call constructor for " + nameof(PropertyKeyNonStrict) + " instead of " + nameof(PropertyKey) + " if you do not need a " + nameof(PropertyKey) + " instance now.\r\n" +
                                "Details: " + key.ToString());
                        }
                    }
                } else {
                    if (index > 0) {
                        throw new InvalidPropertyKeyException("Invalid to specify " + nameof(index) + " (" + index + ") when not " + nameof(key.A.IsMany) + ".\r\nDetails: " + key.ToString());
                    }
                }
            }
        }

        public static PropertyKey Parse(string value) => Parse(value, null);
        public static PropertyKey Parse(string value, Func<string> detailer) => TryParse(value, out var retval, out var strErrorResponse, out var enumErrorResponse, out _, out _) ? retval : throw new InvalidPropertyKeyException(nameof(value) + ": " + value + ",\r\n" + nameof(strErrorResponse) + ": " + strErrorResponse + "\r\n" + nameof(enumErrorResponse) + ": " + enumErrorResponse + detailer.Result("\r\nDetails: "));
        public static bool TryParse(string value, out PropertyKey key) => TryParse(value, out key, out var _, out _, out _, out _);
        public static bool TryParse(string value, out PropertyKey key, out IsManyInconsistency enumErrorResponse) => TryParse(value, out key, out _, out enumErrorResponse, out _, out _);
        public static bool TryParse(string value, out PropertyKey key, out string strErrorResponse) => TryParse(value, out key, out strErrorResponse, out _, out _, out _);
        /// <summary>
        /// Note the multiple drastically simpler overloads. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key">
        /// The actual result normally sought by the caller.
        /// </param>
        /// <param name="strErrorResponse"></param>
        /// <param name="enumErrorResponse">
        /// Facilitates cleanup in database when value of <see cref="AgoRapideAttribute.IsMany"/> has been changed. 
        /// Normally used by <see cref="IDatabase"/>-implementation when reading a single <see cref="Property"/> from database. 
        /// </param>
        /// <param name="nonStrictAlternative">
        /// Used by <see cref="PropertyKeyNonStrict.TryParse"/> in order to have only one parser (not duplicating code)
        /// </param>
        /// <param name="unrecognizedCoreP">
        /// Facilitates dynamic mapping of any unknown properties found. 
        /// IsMany is determined by presence of # in <paramref name="value"/>   
        /// Caller (normally used by <see cref="IDatabase"/>-implementation when reading a single <see cref="Property"/> from database) 
        /// uses this value in order to present <see cref="EnumMapper"/> with a new
        /// </param>
        /// <returns></returns>
        public static bool TryParse(
            string value, 
            out PropertyKey key, 
            out string strErrorResponse, 
            out IsManyInconsistency enumErrorResponse, 
            out PropertyKeyNonStrict nonStrictAlternative, 
            out (string unrecognizedCoreP, bool isMany)? unrecognizedCoreP) {

            if (EnumMapper.TryGetA(value, out nonStrictAlternative)) {
                if (nonStrictAlternative.Key.A.IsMany) {
                    key = null;
                    strErrorResponse = IsManyInconsistency.IsManyButIndexNotGiven + " (meaning # was missing in " + nameof(value) + ". " + nameof(value) + " given was -" + value + "- but expected something like -" + value + "#3-))";
                    enumErrorResponse = IsManyInconsistency.IsManyButIndexNotGiven;
                    unrecognizedCoreP = null;
                    return false;
                }
                key = nonStrictAlternative.PropertyKey; /// Note how this is a "costless" "conversion" since <see cref="PropertyKeyNonStrict.PropertyKey"/> already exists.
                strErrorResponse = null;
                enumErrorResponse = IsManyInconsistency.None;
                unrecognizedCoreP = null;
                return true;
            }

            var t = value.Split('#');
            if (t.Length != 2) {
                key = null;
                strErrorResponse = "Not a valid " + nameof(CoreP) + " and single # not found.";
                enumErrorResponse = IsManyInconsistency.None;
                unrecognizedCoreP = (value, false);
                return false;
            }

            if (!EnumMapper.TryGetA(t[0], out var retval)) { 
                key = null;
                strErrorResponse = "First part (" + t[0] + ") not a valid " + nameof(CoreP) + ".";
                enumErrorResponse = IsManyInconsistency.None;
                unrecognizedCoreP = (t[0], true);
                return false;
            }

            if (!retval.Key.A.IsMany) {
                key = null;
                strErrorResponse = "Illegal to use # when not a " + nameof(AgoRapideAttribute.IsMany) + " " + nameof(AgoRapideAttribute) + ".";
                enumErrorResponse = IsManyInconsistency.NotIsManyButIndexGiven;
                unrecognizedCoreP = null;
                return false;
            }

            if (!int.TryParse(t[1], out var index)) {
                key = null;
                strErrorResponse = "Second part (" + t[1] + ") not a valid int.";
                enumErrorResponse = IsManyInconsistency.None;
                unrecognizedCoreP = null;
                return false;
            }

            key = new PropertyKey(retval.Key, index);
            strErrorResponse = null;
            enumErrorResponse = IsManyInconsistency.None;
            unrecognizedCoreP = null;
            return true;
        }

        [Class(Description = "See -" + nameof(AgoRapideAttribute.IsMany) + "-")]
        public enum IsManyInconsistency {
            None,
            NotIsManyButIndexGiven,
            IsManyButIndexNotGiven
        }


        public new static void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) =>
            agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out string errorResponse) ?
                ParseResult.Create(agoRapideAttribute, retval) :
                ParseResult.Create(errorResponse);
            });

        public override string ToString() => Key.PToString + (_index <= 0 ? "" : ("#" + _index));

        public bool Equals(PropertyKey other) => Key.CoreP.Equals(other.Key.CoreP) && (!Key.A.IsMany || Index.Equals(other.Index));
        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <param name="detailer">May be null</param>
        public void AssertEquals(PropertyKey other, Func<string> detailer) {
            if (!Equals(other)) throw new InvalidPropertyKeyException(ToString() + " != " + other.ToString() + "." + detailer.Result("\r\nDetails: "));
        }
    }
}