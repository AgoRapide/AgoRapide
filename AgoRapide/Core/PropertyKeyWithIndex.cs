// Copyright (c) 2016, 2017, 2018 Bj�rn Erling Fl�tten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: The whole distinction between <see cref="PropertyKey"/> and <see cref="Core.PropertyKeyWithIndex"/> is a bit messy as of Apr 2017
    /// TODO: Especially the hack with <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
    /// TODO: There is also the question of creating a new subclass called PropertyKeyIsMany (and moving the Index-property there)
    /// </summary>
    [Class(
        Description = 
        "Strict version of -" + nameof(PropertyKey) + "-. " +
        "For -" + nameof(PropertyKeyAttribute.IsMany) + "- will also contain the " + nameof(Index) + ", like PhoneNumber#1, PhoneNumber#2.\r\n" +
        "Inconsistency between -" + nameof(PropertyKeyAttribute.IsMany) + "- and -" + nameof(Index) + "- is not allowed")]
    public class PropertyKeyWithIndex : PropertyKey {

        private int _index;
        /// <summary>
        /// Only allowed to read if 1 or greater. 
        /// (Check for <see cref="PropertyKeyAttribute.IsMany"/> before attempting to access this property)
        /// TODO: Try to solve in a better manner by creating a subclass PropertyKeyIsMany or similar.
        /// </summary>
        public int Index {
            get => _index > 0 ? _index : throw new InvalidPropertyKeyException("Invalid to read " + nameof(Index) + " when not set.\r\nDetails: " + Key.A.ToString()); // Careful not to call ToString() here.
            protected set => _index = value;
        }

        public CoreP IndexAsCoreP => (CoreP)(object)(int.MaxValue - (_index > 0 ? _index : throw new InvalidPropertyKeyException(nameof(Index) + " not set. Details: " + Key.ToString())));

        public PropertyKeyWithIndex(PropertyKeyAttributeEnriched key) : this(key, 0) { }
        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index">1-based</param>
        public PropertyKeyWithIndex(PropertyKeyAttributeEnriched key, int index) : base(key) {
            Index = index;
            if (index == IS_MANY_PARENT_OR_TEMPLATE_INDEX) {
                // Do not check for consistency. HACK.
            } else {
                if (key.A.IsMany) {
                    if (index <= 0) {
                        if (this is PropertyKeyWithIndex) {
                            throw new InvalidPropertyKeyException(
                                /// TODO: As of Apr 2017 it is not possible to use <see cref="PropertyKeyAttribute.IsMany"/> in connection with <see cref="CoreAPIMethod.AddEntity"/>. 
                                nameof(Index) + " missing for " + nameof(Key.A.IsMany) + " for " + ToString() + ".\r\n" +
                                "Possible resolution: You may call constructor for " + nameof(PropertyKey) + " instead of " + nameof(PropertyKeyWithIndex) + " if you do not need a " + nameof(PropertyKeyWithIndex) + " instance now.\r\n" +
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

        public static PropertyKeyWithIndex Parse(string value) => Parse(value, null);
        public static PropertyKeyWithIndex Parse(string value, Func<string> detailer) => TryParse(value, out var retval, out var strErrorResponse, out var enumErrorResponse, out _, out _) ? retval : throw new InvalidPropertyKeyException(nameof(value) + ": " + value + ",\r\n" + nameof(strErrorResponse) + ": " + strErrorResponse + "\r\n" + nameof(enumErrorResponse) + ": " + enumErrorResponse + detailer.Result("\r\nDetails: "));
        public static bool TryParse(string value, out PropertyKeyWithIndex key) => TryParse(value, out key, out var _, out _, out _, out _);
        public static bool TryParse(string value, out PropertyKeyWithIndex key, out IsManyInconsistency enumErrorResponse) => TryParse(value, out key, out _, out enumErrorResponse, out _, out _);
        public static bool TryParse(string value, out PropertyKeyWithIndex key, out string strErrorResponse) => TryParse(value, out key, out strErrorResponse, out _, out _, out _);
        /// <summary>
        /// Note the multiple drastically simpler overloads. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key">
        /// The actual result normally sought by the caller.
        /// </param>
        /// <param name="strErrorResponse"></param>
        /// <param name="enumErrorResponse">
        /// Facilitates cleanup in database when value of <see cref="PropertyKeyAttribute.IsMany"/> has been changed. 
        /// Normally used by <see cref="BaseDatabase"/>-implementation when reading a single <see cref="Property"/> from database. 
        /// </param>
        /// <param name="nonStrictAlternative">
        /// Used by <see cref="PropertyKey.TryParse"/> in order to have only one parser (not duplicating code)
        /// </param>
        /// <param name="unrecognizedCoreP">
        /// Facilitates dynamic mapping of any unknown properties found. 
        /// IsMany is determined by presence of # in <paramref name="value"/>   
        /// Caller (normally used by <see cref="BaseDatabase"/>-implementation when reading a single <see cref="Property"/> from database) 
        /// uses this value in order to present <see cref="PropertyKeyMapper"/> with a new
        /// </param>
        /// <returns></returns>
        public static bool TryParse(
            string value, 
            out PropertyKeyWithIndex key, 
            out string strErrorResponse, 
            out IsManyInconsistency enumErrorResponse, 
            out PropertyKey nonStrictAlternative, 
            out (string unrecognizedCoreP, bool isMany)? unrecognizedCoreP) {

            if (PropertyKeyMapper.TryGetA(value, out nonStrictAlternative)) {
                if (nonStrictAlternative.Key.A.IsMany) {
                    key = null;
                    strErrorResponse = IsManyInconsistency.IsManyButIndexNotGiven + " (meaning # was missing in " + nameof(value) + ". " + nameof(value) + " given was -" + value + "- but expected something like -" + value + "#3-))";
                    enumErrorResponse = IsManyInconsistency.IsManyButIndexNotGiven;
                    unrecognizedCoreP = null;
                    return false;
                }
                key = nonStrictAlternative.PropertyKeyWithIndex; /// Note how this is a "costless" "conversion" since <see cref="PropertyKey.PropertyKeyWithIndex"/> already exists.
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

            if (!PropertyKeyMapper.TryGetA(t[0], out var retval)) { 
                key = null;
                strErrorResponse = "First part (" + t[0] + ") not a valid " + nameof(CoreP) + ".";
                enumErrorResponse = IsManyInconsistency.None;
                unrecognizedCoreP = (t[0], true);
                return false;
            }

            if (!retval.Key.A.IsMany) {
                key = null;
                strErrorResponse = "Illegal to use # when not a " + nameof(PropertyKeyAttribute.IsMany) + " " + nameof(PropertyKeyAttribute) + ".";
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

            key = new PropertyKeyWithIndex(retval.Key, index);
            strErrorResponse = null;
            enumErrorResponse = IsManyInconsistency.None;
            unrecognizedCoreP = null;
            return true;
        }

        [Enum(Description = "See -" + nameof(PropertyKeyAttribute.IsMany) + "-")]
        public enum IsManyInconsistency {
            None,
            NotIsManyButIndexGiven,
            IsManyButIndexNotGiven
        }


        public new static void EnrichKey(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out string errorResponse) ?
                ParseResult.Create(key, retval) :
                ParseResult.Create(errorResponse);
            });

        public override string ToString() => Key.PToString + (_index <= 0 ? "" : ("#" + _index));

        public bool Equals(PropertyKeyWithIndex other) => Key.CoreP.Equals(other.Key.CoreP) && (!Key.A.IsMany || Index.Equals(other.Index));
        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <param name="detailer">May be null</param>
        public void AssertEquals(PropertyKeyWithIndex other, Func<string> detailer) {
            if (!Equals(other)) throw new InvalidPropertyKeyException(ToString() + " != " + other.ToString() + "." + detailer.Result("\r\nDetails: "));
        }
    }
}