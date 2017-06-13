using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    /// <summary>
    /// TODO: Implement comparisions operators for this class. 
    /// TODO: Also implement    
    /// </summary>
    [Class(Description = "Describes a percentile like 5P or 95P")]
    public class Percentile : ITypeDescriber {

        public int Value { get; private set; }

        // TODO: Implement, together with comparisions operators.
        //public Quintile ToQuintile() => {

        //}

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

            if (!value.EndsWith("P")) {
                percentile = null;
                errorResponse = "!" + nameof(value) + ".EndsWith(\"P\")";
                return false;
            }

            if (!int.TryParse(value.Substring(0, value.Length - 1), out var intValue)) {
                percentile = null;
                errorResponse = "Invalid integer value";
                return false;
            }

            if (intValue < 1 || intValue > 100) {
                percentile = null;
                errorResponse = "Outside valid range [1-100] (" + intValue + ")";
                return false;
            }

            percentile = new Percentile(intValue);
            errorResponse = null;
            return true;
        }

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

        // TODO: Do not do like this. Do in a more flexible manner like having a Size-class which itself gives different descriptions.
        // TODO: Or just have a Size-enum which does the same ting.
        // public string[] TertileDescriberSize = new string[] { "Small", "Normal", "Big" };

        public enum Quartile {
            Quartile1,
            Quartile2,
            Quartile3,
            Quartile4
        }

        // TODO: Do not do like this. Do in a more flexible manner like having a Size-class which itself gives different descriptions.
        // TODO: Or just have a Size-enum which does the same ting.
        // public string[] QuantileDescriberSize = new string[] { "Small", "A little below average (???)", "A little above average (???)", "Big" };

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
