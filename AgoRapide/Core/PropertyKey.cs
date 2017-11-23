// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// TODO: Note how <see cref="Core.PropertyKeyWithIndex"/> became ubiquitous throughout the library at introduction.
    /// TDOO: Consider if some use of it can be changed back to use of <see cref="PropertyKeyAttributeEnriched"/> instead. 
    /// TODO: Notice how connected everything is, starting with the need for describing <see cref="Property.Key"/>.
    /// TODO: this is assumed to be a suboptimal situation at present.
    /// 
    /// TODO: The whole distinction between <see cref="PropertyKey"/> and <see cref="Core.PropertyKeyWithIndex"/> is a bit messy as of Apr 2017
    /// TODO: Especially the hack with <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
    /// TODO: There is also the question of creating a new subclass called PropertyKeyIsMany (and moving the Index-property there)
    /// </summary>
    [Class(
        Description =
            "Corresponds normally directly to the name of -" + nameof(EnumType.PropertyKey) + "- like -" + nameof(CoreP) + "-, -P-) used in your application. "
    )]
    public class PropertyKey : ITypeDescriber {
        public PropertyKeyAttributeEnriched Key { get; protected set; }

        public PropertyKey(PropertyKeyAttributeEnriched key) => Key = key;

        private PropertyKeyWithIndex _propertyKeyWithIndex;
        /// <summary>
        /// HACK. Relevant when !<see cref="PropertyKeyAttribute.IsMany"/>
        /// Only relevant when originates from <see cref="PropertyKeyMapper"/>
        /// Constitutes the "strict" version of <see cref="PropertyKey"/>
        /// </summary>
        public PropertyKeyWithIndex PropertyKeyWithIndex {
            get {
                if (_propertyKeyWithIndex != null) return _propertyKeyWithIndex;
                switch (this) {
                    case PropertyKeyWithIndex temp: return _propertyKeyWithIndex = temp; /// Hack, because often <see cref="PropertyKey.PropertyKeyWithIndex"/> is asked for even in cases when the caller already has a <see cref="PropertyKeyWithIndex"/> object (the caller "belives" it only has a <see cref="PropertyKey"/>  object)
                    default:
                        throw new NullReferenceException(
                            nameof(_propertyKeyWithIndex) + ". " +
                            "Possible reason: " +
                            new Func<string>(() => {
                                if (Key.A.IsMany) return nameof(PropertyKeyAttribute.IsMany) + " (" + Key.A.IsMany + ") = TRUE";
                                return "Maybe this instance does not originate from " + nameof(PropertyKeyMapper);
                            })() +
                            "\r\n" +
                            "Details: " + ToString() + "\r\n" +
                            "\r\n" +
                            "More details: " + Key.ToString()
                        );
                }
            }
        }

        public bool PropertyKeyIsSet => _propertyKeyWithIndex != null;

        private PropertyKeyWithIndex _propertyKeyWithIndexAsIsManyParentOrTemplate;
        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="PropertyKeyMapper"/>
        /// </summary>
        public PropertyKeyWithIndex PropertyKeyAsIsManyParentOrTemplate => _propertyKeyWithIndexAsIsManyParentOrTemplate ?? throw new NullReferenceException(
            nameof(_propertyKeyWithIndexAsIsManyParentOrTemplate) + ". " +
            "Possible cause (1): This instance does not originate from " + nameof(PropertyKeyMapper) + ".\r\n" +
            /// TODO: Cause below would typically arise when calling <see cref="BaseDatabase.UpdateProperty{T}"/>
            (!Key.A.IsMany ? "" : ("Possible cause (2): (Somewhat obscure) You have specified " + nameof(PropertyKey) + "." + nameof(PropertyKey.PropertyKeyWithIndex) + " in IsMany-context where " + nameof(PropertyKey) + " in itself would have been sufficient.\r\n")) +
            "Details: " + ToString());

        /// <summary>
        /// This is a hack to allow <see cref="Property.IsIsManyParent"/> and <see cref="Property.IsTemplateOnly"/> 
        /// to have a "strict" <see cref="Core.PropertyKeyWithIndex"/> as key (instead of <see cref="PropertyKey"/>. 
        /// 
        /// Not how reading <see cref="PropertyKeyWithIndex.Index"/> with this value will result in an exception being thrown
        /// 
        /// See <see cref="PropertyKeyAsIsManyParentOrTemplate"/>
        /// 
        /// TODO: Replace this with a sub-class of PropertyKey called PropertyKeyAsIsManyParent or something similar
        /// TODO: (and keep the exception when trying to read Index, since Index for that would be meaningless)
        /// </summary>
        protected const int IS_MANY_PARENT_OR_TEMPLATE_INDEX = -1;

        /// <summary>
        /// HACK. See <see cref="IS_MANY_PARENT_OR_TEMPLATE_INDEX"/>
        /// Only relevant when originates from <see cref="PropertyKeyMapper"/>
        /// </summary>
        public void SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate() {
            if (!Key.A.IsMany) _propertyKeyWithIndex = new PropertyKeyWithIndex(Key);
            _propertyKeyWithIndexAsIsManyParentOrTemplate = new PropertyKeyWithIndex(Key, IS_MANY_PARENT_OR_TEMPLATE_INDEX);
        }

        public static bool TryParse(string value, out PropertyKey key, out string strErrorResponse) {

            if (PropertyKeyWithIndex.TryParse(value, out var retval, out strErrorResponse, out _, out var nonStrictAlternative, out _)) {
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

        public static void EnrichKey(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out string errorResponse) ?
                ParseResult.Create(key, retval) :
                ParseResult.Create(errorResponse);
            });

        /// <summary>
        /// TODO: REMOVE COMMENT, WE REMOVE THE FORMER EXCEPTION
        /// TOOD: Do not confuse <see cref="AgoRapide.Database.InvalidPropertyKeyException"/> and <see cref="PropertyKeyWithIndex.InvalidPropertyKeyException"/>,
        /// TODO: RENAME ONE OF THESE INTO SOMETHING ELSE
        /// </summary>
        public class InvalidPropertyKeyException : ApplicationException {
            public InvalidPropertyKeyException(string message) : base(message) { }
            public InvalidPropertyKeyException(string message, Exception inner) : base(message, inner) { }
        }

        /// <summary>
        /// TODO: Consider caching (as of Nov 2017 little use of caching since is called either from within a cached context or 
        /// TOOD: from a context where class itself i dynamically on-the-fly generated.
        /// </summary>
        /// <param name="excludeThisPrefix">
        /// May be null.
        /// 
        /// If given then will be removed from result if key starts with this value. 
        /// Typically use of this parameter is in avoiding for instance for Person to write 'Person First Name', 'Person Last Name', 
        /// but instead only write 'First Name', 'Last Name' in the HTML table header row. 
        /// </param>
        /// <returns></returns>
        [ClassMember(Description = "Returns an HTML description of this key suited as header cell in an HTML table complete with 'tool-tip' / help text.")]
        public string ToHTMLTableHeader(string excludeThisPrefix = null) {
            var key = this;
            var retval = key.Key.PToString;
            if (excludeThisPrefix != null) {
                if (retval.StartsWith(excludeThisPrefix)) { // Note shortening of name here (often names will start with the same as the entity type, we then assume that we can safely remove the type-part).
                                                            // TODO: Add mouseover for showing complete name here.
                    retval = retval.Substring(excludeThisPrefix.Length);
                    if (retval.StartsWith("_")) retval = retval.Substring(1); /// Typical for <see cref="Database.PropertyKeyForeignKeyAggregate"/>
                }
            }
            retval = retval.Replace("_", " "); // Space means that browser will often replace with line breaks in display.                   
            var r2 = new StringBuilder();
            for (var i = 0; i < retval.Length; i++) { // Insert space before each capital letter if preceding letter was not a capital letter.
                if (i > 0 && retval[i].ToString().ToUpper() == retval[i].ToString() && retval[i - 1].ToString().ToUpper() != retval[i - 1].ToString()) {
                    r2.Append(" "); // Code is not necessary very efficient but the result is cached anyway so it really does not matter.
                }
                r2.Append(retval[i]);
            }
            return r2.ToString().HTMLEncloseWithinTooltip(key.Key.A.WholeDescription);
        }

        public override string ToString() => Key.PToString;
    }
}
