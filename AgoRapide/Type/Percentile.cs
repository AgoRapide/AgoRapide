using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {

    /// <summary>
    /// TODO: Implement comparisions operators for this class. 
    /// 
    /// Note how only 100 of these objects will ever be instantiated. 
    /// </summary>
    [Class(Description = "Describes a percentile like 5P or 95P")]
    public class Percentile : ITypeDescriber {

        public int Value { get; private set; }

        private Percentile() { }

        // Hide constructor, we do not want millions of these objects generated.
        //public Percentile(int value) {
        //    if (value < 1 || value > 100) throw new InvalidPercentileException("Outside valid range 1-100 (" + value + ")");
        //    Value = value;
        //}

        public static bool TryParse(string value, out Percentile percentile, out string errorResponse) {

            if (value == null) {
                percentile = null;
                errorResponse = nameof(value) + " == null";
                return false;
            }

            if (!AllValuesDict.TryGetValue(value, out percentile)) {
                percentile = null;
                errorResponse = "Not recognized, must be on the form \"[1-100]P\" like 9P or 69P";
                return false;
            }

            errorResponse = null;
            return true;
        }

        public static Percentile Get(int percentile) {
            if (percentile < 1 || percentile > 100) throw new ArgumentException("Must be between 1 and 100, not " + percentile, nameof(percentile));
            return AllValuesList[percentile] ?? throw new NullReferenceException("Index " + percentile + " not found");
        }

        private static Dictionary<string, Percentile> _allValuesDict;
        private static Dictionary<string, Percentile> AllValuesDict => _allValuesDict ?? (_allValuesDict = AllValuesList.ToDictionary(p => p.ToString(), p => p));
        // private static Dictionary<string, Percentile> AllValuesDict = AllValuesList.Where(p => p != null).ToDictionary(p => p.ToString(), p => p);

        private static List<Percentile> _allValuesList;
        private static List<Percentile> AllValuesList => _allValuesList ?? (_allValuesList = new Func<List<Percentile>>(() => {
        // private static List<Percentile> AllValuesList = new Func<List<Percentile>>(() => {
            var retval = new List<Percentile> { null }; /// Note null as first index
            for (var i = 1; i <= 100; i++) {
                retval.Add(new Percentile { Value = i });
            }
            if (retval.Count != 101) throw new InvalidCountException(retval.Count, 101);
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
                case Tertile.Tertile1: return Get(33); // NOTE: Not exact!
                case Tertile.Tertile2: return Get(67); // NOTE: Not exact!
                default: return Get(100);
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

        [ClassMember(Description = "Calculates percentiles based on -" + nameof(FileCache) + "-.")]
        public static void Calculate(Type type, PropertyKey key, QueryId queryid) {
            //type.GetChildProperties().Values.Where(key =>
            //    // key.Key.A.Type.Equals(typeof(int)) ||
            //    key.Key.A.Type.Equals(typeof(long)) /// Note that long is the preferred property type
            //    // key.Key.A.Type.Equals(typeof(double)) 
            //).ForEach(key => {
            switch (queryid) {
                case QueryIdAll q:
                    /// Calculate and store directly within <see cref="Property.Percentile"/>
                    var properties = InMemoryCache.EntityCache.Values.
                        Where(e => type.IsAssignableFrom(e.GetType())).
                        Select(e => e.Properties.TryGetValue(key.Key.CoreP, out var p) ? p : null).Where(p => p != null).
                        Select(p => (Property: p, Value: p.V<long>())). // Extract value only once for each property. Should improve sorting and percentile evaluation
                        OrderBy(p => p.Value).ToList();
                    if (properties.Count == 0) throw new InvalidCountException("No properties found for " + type + " " + key.ToString() + ". Are values read into " + nameof(InMemoryCache.EntityCache) + "? Have x");
                    var lastValue = properties[0].Value;
                    var lastIndex = 0;
                    for (var i = 1; i < properties.Count; i++) { // TOOD: This is a quick-and-dirty attempt at Percentile-evaluation. It does probably have room for improvement.
                        var newValue = properties[i].Value;
                        if (newValue > lastValue) {
                            var p = Get((int)(((i - 1) / (double)properties.Count) + 1));
                            for (var j = lastIndex; j < i; j++) {
                                properties[j].Property.Percentile = p;
                            }
                            lastIndex = i;
                        }
                    }
                    // Remaining values are at 100P
                    {
                        var p = Get(100);
                        for (var j = lastIndex; j < properties.Count; j++) {
                            properties[j].Property.Percentile = p;
                        }
                    }
                    break;
                default:
                    /// TODO: Decide where to store this. 
                    /// TODO: Make a cache with <param name="queryid"/>.ToString() for instance.
                    /// TODO: Look out for <see cref="QueryIdContext"/> which complicates (and which probably is exact the one that we want to use)
                    throw new InvalidObjectTypeException(queryid, "Not implemented");
            }
            // });
        }
    }
}