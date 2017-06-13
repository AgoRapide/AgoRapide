// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(
        Description = "Building block for drill down functionality and AgoRapide query language"
    )]
    public class Context : ITypeDescriber {


        public static Context Parse(string value) => TryParse(value, out var retval, out var errorResponse) ? retval : throw new InvalidContextException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(string value, out Context id) => TryParse(value, out id, out var dummy);
        public static bool TryParse(string value, out Context id, out string errorResponse) {
            var pos = 0;
            value += " "; // Simplifies parsing
            var nextWord = new Func<string>(() => {
                var nextPos = value.IndexOf(';', pos);
                if (nextPos == -1) return null;
                var word = value.Substring(pos, nextPos - pos);
                pos = nextPos + 1;
                return word;
            });
            if (!CoreP.SetOperator.tr)

        }

        public static void EnrichAttribute(PropertyKeyAttributeEnriched key) {
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(value, out var retval, out var errorResponse) ?
                    ParseResult.Create(key, retval) :
                    ParseResult.Create(errorResponse);
            });
        }

        public class InvalidContextException : ApplicationException {
            public InvalidContextException(string message) : base(message) { }
            public InvalidContextException(string message, Exception inner) : base(message, inner) { }
        }


    }
}
