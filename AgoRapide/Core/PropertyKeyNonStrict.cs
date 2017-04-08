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
    /// TODO: The whole distinction between <see cref="PropertyKeyNonStrict"/> and <see cref="PropertyKey"/> is a bit messy as of Apr 2017
    /// TODO: Especially the hack with <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
    /// </summary>
    [AgoRapide(
        Description =
            "Corresponds normally directly to the name of -" + nameof(EnumType.EntityPropertyEnum) + "- like -" + nameof(CoreP) + "-, -P-) used in your application. ",
        LongDescription =
            "The difference between -" + nameof(PropertyKeyNonStrict) + "- and -" + nameof(Core.PropertyKey) + "- " +
            "is that the latter also specifies an -" + nameof(Core.PropertyKey.Index) + "- for -" + nameof(AgoRapideAttribute.IsMany) + "-"
    )]
    public class PropertyKeyNonStrict : ITypeDescriber {
        public AgoRapideAttributeEnriched Key { get; protected set; }

        public PropertyKeyNonStrict(AgoRapideAttributeEnriched key) => Key = key;
        
        private PropertyKey _propertyKey;
        /// <summary>
        /// HACK. Relevant when !<see cref="AgoRapideAttribute.IsMany"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// Constitues the "strict" version 
        /// </summary>
        public PropertyKey PropertyKey => _propertyKey ?? throw new NullReferenceException(
            nameof(_propertyKey) + ". " +
            "Most probably because this instance " + nameof(AgoRapideAttribute.IsMany) + " (" + Key.A.IsMany + ") = TRUE\r\n" +
            "Maybe because this instance does not originate from " + nameof(EnumMapper) + ".\r\n" +
            nameof(Key) + ":\r\n" + (Key?.ToString() ?? "[NULL]") + "\r\n\r\n" +
            "Details: " + ToString());
        public bool PropertyKeyIsSet => _propertyKey != null;

        private PropertyKey _propertyKeyAsIsManyParentOrTemplate;
        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// </summary>
        public PropertyKey PropertyKeyAsIsManyParentOrTemplate => _propertyKeyAsIsManyParentOrTemplate ?? throw new NullReferenceException(nameof(_propertyKeyAsIsManyParentOrTemplate) + ". Most probably because this instance does not originate from " + nameof(EnumMapper) + ".\r\nDetails: " + ToString());

        /// <summary>
        /// This is a hack to allow <see cref="Property.IsIsManyParent"/> and <see cref="Property.IsTemplateOnly"/> 
        /// to have a "strict" <see cref="Core.PropertyKey"/> as key (instead of <see cref="PropertyKeyNonStrict"/>. 
        /// 
        /// Not how reading <see cref="PropertyKey.Index"/> with this value will result in an exception being thrown
        /// 
        /// See <see cref="PropertyKeyAsIsManyParentOrTemplate"/>
        /// 
        /// TODO: Replace this with a sub-class of PropertyKey called PropertyKeyAsIsManyParent or something similar
        /// TODO: (and keep the exception when trying to read Index, since Index for that would be meaningless)
        /// </summary>
        protected const int IS_MANY_PARENT_OR_TEMPLATE_INDEX = -1;

        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="EnumMapper"/>
        /// </summary>
        public void SetPropertyKeyAndPropertyKeyAsIsManyParentOrTemplate() {
            if (!Key.A.IsMany) _propertyKey = new PropertyKey(Key);
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

        public override string ToString() => Key.PToString;
    }
}
