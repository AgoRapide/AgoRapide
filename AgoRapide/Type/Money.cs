using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide {

    [Class(Description = "Immutable. Note -" + nameof(SetCommonValues) + "-.")]
    public class Money : ITypeDescriber, IEquatable<Money> {
        public Currency Currency { get; private set; }
        public double Amount { get; private set; }

        public Money(Currency currency, double amount) {
            Currency = currency;
            Amount = amount;
        }

        public bool Equals(Money other) => Currency == other.Currency && Amount == other.Amount;
        public override bool Equals(object other) {
            if (other == null) return false;
            switch (other) {
                case Money m: return Equals(m);
                default: return false;
            }
        }
        private int? _hashcode;
        public override int GetHashCode() => (int)(_hashcode ?? (_hashcode = ToString().GetHashCode()));

        /// <summary>
        /// TODO: Create an "adder"-class if you want more efficient calculations
        /// TODO: Like SingleCurrencyAdder and MultipleCurrencyAdder
        /// </summary>
        /// <param name="money"></param>
        /// <returns></returns>
        public Money Add(Money money) {
            var newCurrency = Currency;
            if ((Amount == 0) && (Currency == Currency.None)) newCurrency = money.Currency;
            if ((newCurrency != Currency.None) && (money.Currency == Currency.None)) {
                // OK
            } else if (newCurrency != money.Currency) {
                throw new Exception("newCurrency (" + newCurrency.ToString() + ") != money.currency (" + money.Currency.ToString() + "). Impossible to add " + ToString() + " and " + money.ToString());
            }
            return new Money(newCurrency, Amount + money.Amount);
        }

        public override string ToString() => Currency.ToString() + " " + Amount.ToString2();

        /// <summary>
        /// Writes something like 1.1M or 24.2K for easy reading. 
        /// </summary>
        /// <returns></returns>
        public string ToStringRoughNumber() => (Currency.ToString() + " ").Replace("USD ", "$").Replace("None ", "") + new Func<string>(() => {
            if (Amount >= 50000000) return (Amount / 1000000).ToString("0").Replace(",", ".") + "M";
            if (Amount >= 1000000) return (Amount / 1000000).ToString("0.0").Replace(",", ".") + "M";
            if (Amount >= 50000) return (Amount / 1000).ToString("0").Replace(",", ".") + "K";
            if (Amount >= 1000) return (Amount / 1000).ToString("0.0").Replace(",", ".") + "K";
            if (Amount > -1000) return (Amount).ToString("0");
            if (Amount > -50000) return (Amount / 1000).ToString("0.0").Replace(",", ".") + "K";
            if (Amount > -1000000) return (Amount / 1000).ToString("0").Replace(",", ".") + "K";
            if (Amount > -50000000) return (Amount / 1000000).ToString("0.0").Replace(",", ".") + "M";
            return (Amount / 1000000).ToString("0").Replace(",", ".") + "M";
        })();

        public static Money Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidMoneyException(nameof(value) + ": " + value + ". Details: " + errorResponse);
        public static bool TryParse(string value, out Money money) => TryParse(value, out money, out _);
        public static bool TryParse(string value, out Money money, out string errorResponse) {
            if (CommonValues != null && CommonValues.TryGetValue(value, out money)) { errorResponse = null; return true; }
            money = null; errorResponse = null;
            var t = value.Split(' ');
            if (t.Length != 2) { errorResponse = "Not two items separated by space but " + t.Length + " items"; return false; }
            if (!Util.DoubleTryParse(t[1], out var amount)) { errorResponse = "Amount (" + t[1] + ") is not a valid double"; return false; }
            if (!Util.EnumTryParse<Currency>(t[0], out var currency)) { errorResponse = "Currency (" + t[0] + ") is not valid"; return false; }
            money = new Money(currency, amount);
            return true;
        }

        public static Dictionary<string, Money> CommonValues = null;
        [ClassMember(Description = 
            "Practical functionality that is useful if your application uses some values overwhelmingly more than other values. " +
            "Parsing will then be much quicker and less objects will be generated.")]
        public static void SetCommonValues(List<Money> list) {
            Util.AssertCurrentlyStartingUp();
            CommonValues = list.ToDictionary2(m => m.ToString(), m => m);
        }

        private class InvalidMoneyException : Exception {
            public InvalidMoneyException(string message) : base(message) { }
            public InvalidMoneyException(string message, Exception inner) : base(message, inner) { }
        }

        public static void EnrichKey(PropertyKeyAttributeEnriched key) => key.ValidatorAndParser = new Func<string, ParseResult>(value =>
            TryParse(value, out var retval, out var errorResponse) ?
                ParseResult.Create(key, retval) :
                ParseResult.Create(errorResponse));
    }

    /// <summary>
    /// TODO: Expand as needed.
    /// </summary>
    public enum Currency {
        None,
        EUR,
        NOK,
    }
}
