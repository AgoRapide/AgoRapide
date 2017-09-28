using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {
    /// <summary>
    /// EXAMPLE for use of <see cref="ITypeDescriber"/>. NOT USED in AgoRapide-library itself. 
    /// 
    /// TODO: This class is a bit contrived. 
    /// TODO: Find a more internationally recognized example instead of Norwegian postal code
    /// TODO: (and preferable something that would be difficult to solve with just <see cref="PropertyKeyAttribute.RegExpValidator"/>)
    /// 
    /// TODO: One candidate would be the <see cref="Money"/>-class.
    /// 
    /// <see cref="NorwegianPostalCode"/> is a string "between" 0000 and 9999.
    /// 
    /// This class is maybe a bit over-engineered (and still not complete) but it illustrates in detail how your classes may cooperate with the
    /// AgoRapide validation mechanism.
    /// 
    /// You may use it strongly typed like <see cref="BaseEntity.PV{NorwegianPostalCode}"/>
    /// 
    /// Please note that you could also have solved the whole validation issue by just having 
    /// used <see cref="PropertyKeyAttribute.RegExpValidator"/> in this case, something which would have been a lot simpler.
    /// (TODO: As of March 2017 support for <see cref="PropertyKeyAttribute.RegExpValidator"/> is not implemented)
    /// </summary>
    public class NorwegianPostalCode : ITypeDescriber {
        public string Value { get; private set; }
        public override string ToString() => Value ?? throw new NullReferenceException(nameof(Value));

        private NorwegianPostalCode(string value) => Value = value; // Private constructor, value to be trusted

        public static bool TryParse(string value, out NorwegianPostalCode norwegianPostalCode) => TryParse(value, out norwegianPostalCode, out _);
        public static bool TryParse(string value, out NorwegianPostalCode norwegianPostalCode, out string errorResponse) {
            var validatorResult = Validator(value);
            if (validatorResult != null) {
                norwegianPostalCode = null;
                errorResponse = validatorResult;
                return false;
            }
            norwegianPostalCode = new NorwegianPostalCode(value);
            errorResponse = null;
            return true;
        }

        private static Func<string, string> Validator = value => {
            if (value == null) return "value == null";
            if (value.Length != 4) return "value.Length != 4";
            /// Note how result of <see cref="int.TryParse"/> is not wasted because we actually would not use it anyway
            if (!int.TryParse(value, out _)) return "Invalid integer";
            return null;
        };

        /// <see cref="EnrichKey"/> is the method that MUST be implemented
        /// 
        /// TODO: IMPLEMENT CLEANER AND CHAINING OF CLEANER
        /// enumAttribute.Cleaner=
        /// 
        /// TODO: IMPLEMENT CHAINING OF VALIDATION!

        /// <summary>
        /// TODO: Do away with need for double overloads (for both <see cref="P"/> and <see cref="CoreP"/>)
        /// </summary>
        /// <param name="key"></param>
        public static void EnrichKey(PropertyKeyAttributeEnriched key) => key.ValidatorAndParser = new Func<string, ParseResult>(value =>
            TryParse(value, out var retval, out var errorResponse) ?
                ParseResult.Create(key, retval) :
                ParseResult.Create(errorResponse));

        public class InvalidNorwegianPostalCodeException : ApplicationException {
            public InvalidNorwegianPostalCodeException(string message) : base(message) { }
            public InvalidNorwegianPostalCodeException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
