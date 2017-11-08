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

        public Tertile AsTertile { get; private set; }
        public Quartile AsQuartile { get; private set; }
        public Quintile AsQuintile { get; private set; }

        private Percentile(int value) {
            Value = value;
            AsTertile = ToTertile(this);
            AsQuartile = ToQuartile(this);
            AsQuintile = ToQuintile(this);
        }

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
                retval.Add(new Percentile(i));
            }
            if (retval.Count != 101) throw new InvalidCountException(retval.Count, 101);
            return retval;
        })());

        public static void EnrichKey(PropertyKeyAttributeEnriched key) =>
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    ParseResult.Create(key, retval) :
                    ParseResult.Create(errorResponse);
            });

        public class InvalidPercentileException : ApplicationException {
            public InvalidPercentileException(string message) : base(message) { }
        }

        public static Tertile ToTertile(Percentile percentile) {
            if (percentile.Value <= 33) return Tertile.Tertile1;
            if (percentile.Value <= 67) return Tertile.Tertile2;
            return Tertile.Tertile3;
        }

        public static Percentile FromTertile(Tertile tertile) {
            switch (tertile) {
                case Tertile.Tertile1: return Get(33); // NOTE: Not exact!
                case Tertile.Tertile2: return Get(67); // NOTE: Not exact!
                default: return Get(100);
            }
        }

        public static Quartile ToQuartile(Percentile percentile) {
            if (percentile.Value <= 25) return Quartile.Quartile1;
            if (percentile.Value <= 50) return Quartile.Quartile2;
            if (percentile.Value <= 75) return Quartile.Quartile3;
            return Quartile.Quartile4;
        }

        public static Percentile FromQuartile(Quartile quartile) {
            switch (quartile) {
                case Quartile.Quartile1: return Get(25);
                case Quartile.Quartile2: return Get(50);
                case Quartile.Quartile3: return Get(75);
                default: return Get(100);
            }
        }

        public static Quintile ToQuintile(Percentile percentile) {
            if (percentile.Value <= 20) return Quintile.Quintile1;
            if (percentile.Value <= 40) return Quintile.Quintile2;
            if (percentile.Value <= 60) return Quintile.Quintile3;
            if (percentile.Value <= 80) return Quintile.Quintile4;
            return Quintile.Quintile5;
        }

        public static Percentile FromQuintile(Quintile Quintile) {
            switch (Quintile) {
                case Quintile.Quintile1: return Get(20);
                case Quintile.Quintile2: return Get(40);
                case Quintile.Quintile3: return Get(60);
                case Quintile.Quintile4: return Get(80);
                default: return Get(100);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="entities"></param>
        /// <param name="db">TODO: REMOVE, NOT NEEDED</param>
        public static void Calculate(Type type, List<BaseEntity> entities, BaseDatabase db) => 
            // Introduced Parallel.ForEach 3 Nov 2017
            Parallel.ForEach(type.GetChildProperties().Values.Where(key => key.Key.A.IsSuitableForPercentileCalculation), key => {
            Calculate(type, entities, key);
        });

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="queryid">Has to be <see cref="QueryIdAll"/> as of Sep 2017</param>
        /// <param name="key"></param>
        public static void Calculate(Type type, QueryId queryid, PropertyKey key) {
            switch (queryid) {
                case QueryIdAll q:
                    /// Calculate and store directly within <see cref="Property.Percentile"/>
                    // Calculate(type, InMemoryCache.EntityCache.Values.Where(e => type.IsAssignableFrom(e.GetType())).ToList(), key); break;
                    Calculate(type, InMemoryCache.EntityCacheWhereIs(type), key); break;
                default:
                    /// TODO: Decide where to store this. 
                    /// TODO: Make a cache with <param name="queryid"/>.ToString() for instance.
                    /// TODO: Look out for <see cref="QueryIdContext"/> which complicates (and which probably is exact the one that we want to use)
                    throw new InvalidObjectTypeException(queryid, "Not implemented");
            }
        }

        /// <summary>
        /// Calculates and stores within <see cref="Property.Percentile"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        public static void Calculate(Type type, List<BaseEntity> entities, PropertyKey key) {
            key.Key.A.AssertSuitableForPercentileCalculation();
            InvalidTypeException.AssertEquals(key.Key.A.Type, typeof(long), () => "Only supported for long as of Sep 2017");
            if (entities.Count == 0) return;
            var properties = entities.
                Select(e => e.Properties.TryGetValue(key.Key.CoreP, out var p) ? p : null).Where(p => p != null).
                Select(p => (Property: p, Value: p.V<long>())). // Extract value only once for each property. Should improve sorting and percentile evaluation
                OrderBy(p => p.Value).ToList();
            if (properties.Count == 0) {
                // TODO: Remove commented out code.
                // TODO: Consider just returning quietly here instead.
                // throw new InvalidCountException("No properties found for " + type + " " + key.ToString() + ". Are values read into " + nameof(InMemoryCache.EntityCache) + " (Count " + InMemoryCache.EntityCache.Count + ")? " + nameof(entities) + ".Count: " + entities.Count);

                return;
            }
            var lastValue = properties[0].Value;
            var lastIndex = 0;
            for (var i = 1; i < properties.Count; i++) { // TOOD: This is a quick-and-dirty attempt at Percentile-evaluation. It does probably have room for improvement.
                var newValue = properties[i].Value;
                if (newValue > lastValue) {
                    var p = Get((int)((((i - 1) / (double)properties.Count) * 100) + 1));
                    for (var j = lastIndex; j < i; j++) {
                        properties[j].Property.Percentile = p;
                    }
                    lastIndex = i;
                    lastValue = newValue;
                }
            }
            // Remaining values are at 100P
            {
                var p = Get(100);
                for (var j = lastIndex; j < properties.Count; j++) {
                    properties[j].Property.Percentile = p;
                }
            }
        }
    }

    public enum Tertile {
        None,
        Tertile1,
        Tertile2,
        Tertile3
    }

    public enum Quartile {
        None,
        Quartile1,
        Quartile2,
        Quartile3,
        Quartile4
    }

    public enum Quintile {
        None,
        Quintile1,
        Quintile2,
        Quintile3,
        Quintile4,
        Quintile5
    }

    // TODO: Implement To and From

    public enum Sextile {
        None,
        Sextile1,
        Sextile2,
        Sextile3,
        Sextile4,
        Sextile5,
        Sextile6
    }

    // TODO: Implement To and From

    public enum Septile {
        None,
        Septile1,
        Septile2,
        Septile3,
        Septile4,
        Septile5,
        Septile6,
        Septile7,
    }
}