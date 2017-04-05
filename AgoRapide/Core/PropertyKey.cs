using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Note how <see cref="PropertyKey"/> became ubiquitous throughout the library at introduction.
    /// TDOO: Consider if some use of it can be changed back to use of <see cref="AgoRapideAttributeEnriched"/> instead. 
    /// TODO: Notice how connected everything is, starting with the need for describing <see cref="Property.Key"/>.
    /// TODO: this is assumed to be a suboptimal situation at present.
    /// 
    /// TODO: This class has the weakness that functionality as exposed to the outside world matches exactly that of
    /// TODO: <see cref="PropertyKey"/>
    /// 
    /// TODO: The whole distinction between <see cref="PropertyKeyNonStrict"/> and <see cref="PropertyKey"/> is a bit messy as of Apr 2017
    /// TODO: Especially the hack with <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
    /// </summary>
    [AgoRapide(
        Description =
            "Corresponds normally directly to the name of -" + nameof(EnumType.EntityPropertyEnum) + "- like -" + nameof(CoreP) + "-, -P-) used in your application. " +
            "For -" + nameof(AgoRapideAttribute.IsMany) + "- will also contain the " + nameof(Index) + ", like PhoneNumber#1, PhoneNumber#2",
        LongDescription =
            "The difference between -" + nameof(PropertyKeyNonStrict) + "- and -" + nameof(PropertyKey) + "- " +
            "is that the former allows -" + nameof(AgoRapideAttribute.IsMany) + "- without -" + nameof(Index) + "-. " +
            "This is useful for -" + nameof(CoreMethod.UpdateProperty) + "-"
    )]
    public class PropertyKeyNonStrict : ITypeDescriber {
        public AgoRapideAttributeEnriched Key { get; protected set; }

        /// <summary>
        /// This is hack to allow <see cref="Property.IsIsManyParent"/> and <see cref="Property.IsTemplateOnly"/> 
        /// to have a "strict" <see cref="PropertyKey"/> as key (instead of <see cref="PropertyKeyNonStrict"/>. 
        /// 
        /// Not how reading <see cref="Index"/> with this value will result in an exception being thrown
        /// 
        /// See <see cref="PropertyKeyAsIsManyParentOrTemplate"/>
        /// 
        /// TODO: Replace this with a sub-class of PropertyKey called PropertyKeyAsIsManyParent or something similar
        /// TODO: (and keep the exception when trying to read Index, since Index for that would be meaningless)
        /// </summary>
        protected const int IS_MANY_PARENT_OR_TEMPLATE_INDEX = -1;
        private int _index;
        /// <summary>
        /// 0 means not given. This may happen even for <see cref="AgoRapideAttribute.IsMany"/>. 
        /// If relevant then has value 1 or greater. 
        /// </summary>
        public int Index {
            get => _index != IS_MANY_PARENT_OR_TEMPLATE_INDEX ? _index : throw new InvalidPropertyKeyException("Invalid to read " + nameof(Index) + " when value is " + nameof(IS_MANY_PARENT_OR_TEMPLATE_INDEX) + " = " + IS_MANY_PARENT_OR_TEMPLATE_INDEX + ". Details: " + Key.A.ToString()); // Careful not to call ToString() here.
            protected set => _index = value;
        }

        public CoreP IndexAsCoreP => (CoreP)(object)(int.MaxValue - (Index > 0 ? Index : throw new InvalidPropertyKeyException(nameof(Index) + " not set. Details: " + Key.ToString())));

        public bool Equals(PropertyKey other) => Key.CoreP.Equals(other.Key.CoreP) && Index.Equals(other.Index);
        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <param name="detailer">May be null</param>
        public void AssertEquals(PropertyKey other, Func<string> detailer) {
            if (!Equals(other)) throw new InvalidPropertyKeyException(ToString() + " != " + other.ToString() + "." + detailer.Result("\r\nDetails: "));
        }

        private PropertyKey _propertyKey;
        /// <summary>
        /// HACK. Relevant when !<see cref="AgoRapideAttribute.IsMany"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// </summary>
        public PropertyKey PropertyKey => _propertyKey ?? throw new NullReferenceException(nameof(_propertyKey) + ". Most probably because this instance " + nameof(AgoRapideAttribute.IsMany) + " (" + Key.A.IsMany + ") = TRUE\r\nMaybe because this instance does not originate from " + nameof(EnumMapper) + ".\r\nDetails: " + ToString());
        public bool PropertyKeyIsSet => _propertyKey != null;

        private PropertyKey _propertyKeyAsIsManyParentOrTemplate;
        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// </summary>
        public PropertyKey PropertyKeyAsIsManyParentOrTemplate => _propertyKeyAsIsManyParentOrTemplate ?? throw new NullReferenceException(nameof(_propertyKeyAsIsManyParentOrTemplate) + ". Most probably because this instance does not originate from " + nameof(EnumMapper) + ".\r\nDetails: " + ToString());

        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// </summary>
        public void SetPropertyKeyAndPropertyKeyAsIsManyParentOrTemplate() {
            if (!Key.A.IsMany) _propertyKey = new PropertyKey(Key);
            if (Key.CoreP == CoreP.AccessLevelGiven) {
                var a = 1;
            }
            _propertyKeyAsIsManyParentOrTemplate = new PropertyKey(Key, IS_MANY_PARENT_OR_TEMPLATE_INDEX);
        }

        public static bool TryParse(string value, out PropertyKeyNonStrict key, out string strErrorResponse) {

            if (PropertyKey.TryParse(value, out var retval, out strErrorResponse, out _, out var nonStrictAlternative, out _)) {
                key = retval;
                return true;
            }

            if (nonStrictAlternative != null) {
                key = nonStrictAlternative;
                strErrorResponse = null;
                return true;
            }

            key = null;
            return false;
        }

        public static void EnrichAttribute(AgoRapideAttributeEnriched agoRapideAttribute) =>
        agoRapideAttribute.ValidatorAndParser = new Func<string, ParseResult>(value => {
            return TryParse(value, out var retval, out string errorResponse) ?
            ParseResult.Create(agoRapideAttribute, retval) :
            ParseResult.Create(errorResponse);
        });

        /// <summary>
        /// TODO: REMOVE COMMENT, WE REMOVE THE FORMER EXCEPTION
        /// TOOD: Do not confuse <see cref="AgoRapide.Database.InvalidPropertyKeyException"/> and <see cref="PropertyKey.InvalidPropertyKeyException"/>,
        /// TODO: RENAME ONE OF THESE INTO SOMETHING ELSE
        /// </summary>
        public class InvalidPropertyKeyException : ApplicationException {
            public InvalidPropertyKeyException(string message) : base(message) { }
            public InvalidPropertyKeyException(string message, Exception inner) : base(message, inner) { }
        }

        public override string ToString() => Key.PToString + (_index <= 0 ? "" : ("#" + _index));
    }

    [AgoRapide(Description = "Strict version of -" + nameof(PropertyKeyNonStrict) + "-. Inconsistency between -" + nameof(AgoRapideAttribute.IsMany) + "- and -" + nameof(PropertyKeyNonStrict.Index) + "- is not allowed")]
    public class PropertyKey : PropertyKeyNonStrict {
        public PropertyKey(AgoRapideAttributeEnriched key) : this(key, 0) { }
        public PropertyKey(AgoRapideAttributeEnriched key, int index) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Index = index;
            if (index == IS_MANY_PARENT_OR_TEMPLATE_INDEX) {
                // Do not check for consistency. HACK.
            } else {
                if (key.A.IsMany) {
                    if (index == 0) {
                        if (this is PropertyKeyNonStrict) {
                            // OK
                        } else {
                            throw new InvalidPropertyKeyException(nameof(Index) + " missing for " + nameof(Key.A.IsMany) + " for " + ToString() + ".\r\nDetails: " + key.ToString());
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
        public static bool TryParse(string value, out PropertyKey key, out string strErrorResponse, out IsManyInconsistency enumErrorResponse, out PropertyKeyNonStrict nonStrictAlternative, out (string unrecognizedCoreP, bool isMany)? unrecognizedCoreP) {
            if (EnumMapper.TryGetA(value, out nonStrictAlternative)) {
                if (nonStrictAlternative.Key.A.IsMany) {
                    key = null;
                    strErrorResponse = IsManyInconsistency.IsManyButIndexNotGiven + " (meaning # was missing in " + nameof(value) + " (" + value + "))";
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

            var retval = EnumMapper.GetAOrDefault(t[0]);
            if (retval.Key.CoreP == CoreP.None) {
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

        [AgoRapide(Description = "See -" + nameof(AgoRapideAttribute.IsMany) + "-")]
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
    }
}