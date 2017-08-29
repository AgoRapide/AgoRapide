using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Implement comparisions operators for this class. 
    /// </summary>
    [Class(Description = "Describes a percentile like 5P or 95P")]
    public class Percentile : ITypeDescriber {

        public int Value { get; private set; }

        public Percentile(int value) {
            if (value < 1 || value > 100) throw new InvalidPercentileException("Outside valid range 1-100 (" + value + ")");
            Value = value;
        }

        public static bool TryParse(string value, out Percentile percentile, out string errorResponse) {

            if (value == null) {
                percentile = null;
                errorResponse = nameof(value) + " == null";
                return false;
            }

            if (!AllValues.TryGetValue(value, out percentile)) {
                percentile = null;
                errorResponse = "Not recognized, must be on the form \"[1-100]P\" like 9P or 69P";
                return false;
            }

            errorResponse = null;
            return true;
        }

        private static Dictionary<string, Percentile> _allValues;
        /// <summary>
        /// Note how having a single static set of values avoid repeating millions of otherwise identical instances
        /// </summary>
        private static Dictionary<string, Percentile> AllValues => _allValues ?? (_allValues = new Func<Dictionary<string, Percentile>>(() => {
            var retval = new Dictionary<string, Percentile>();
            for (var i = 1; i <= 100; i++) retval.Add(i + "P", new Percentile(i));
            return retval;
        })());

        public static void EnrichAttribute(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    ParseResult.Create(key, retval) :
                    ParseResult.Create(errorResponse);
            });

        public class InvalidPercentileException : ApplicationException {
            public InvalidPercentileException(string message) : base(message) { }
        }

        public enum Tertile {
            Tertile1,
            Tertile2,
            Tertile3
        }
        public Tertile ToTertile() {
            if (Value <= 33) return Tertile.Tertile1;
            if (Value <= 67) return Tertile.Tertile2;
            return Tertile.Tertile3;
        }
        public static Percentile FromTertile(Tertile tertile) {
            switch (tertile) {
                case Tertile.Tertile1: return new Percentile(33);
                case Tertile.Tertile2: return new Percentile(67);
                default: return new Percentile(100);
            }
        }

        public enum Quartile {
            Quartile1,
            Quartile2,
            Quartile3,
            Quartile4
        }

        public Quartile ToQuartile() {
            if (Value <= 25) return Quartile.Quartile1;
            if (Value <= 50) return Quartile.Quartile2;
            if (Value <= 75) return Quartile.Quartile3;
            return Quartile.Quartile4;
        }

        public enum Quintile {
            Quintile1,
            Quintile2,
            Quintile3,
            Quintile4,
            Quintile5
        }

        public enum Sextile {
            Sextile1,
            Sextile2,
            Sextile3,
            Sextile4,
            Sextile5,
            Sextile6
        }

        public enum Septile {
            Septile1,
            Septile2,
            Septile3,
            Septile4,
            Septile5,
            Septile6,
            Septile7,
        }
    }
}
