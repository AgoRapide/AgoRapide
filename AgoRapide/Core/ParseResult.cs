using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Contains either <see cref="Result"/> or <see cref="ErrorResponse"/>
    /// </summary>
    public class ParseResult {

        public Property Result { get; private set; }

        /// <summary>
        /// Corresponds to <see cref="Result"/>.<see cref="Property.ADotTypeValue"/>
        /// TODO: Ideally we would like to do without this parameter but that would lead to <see cref="Property.ADotTypeValue"/> 
        /// calling itself 
        /// (see code line
        ///    aDotTypeValue = KeyA.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
        /// )
        /// </summary>
        public object ObjResult { get; private set; }

        /// <summary>
        /// Will be null if Result is set
        /// </summary>
        public string ErrorResponse { get; private set; }

        /// <summary>
        /// Typically called from <see cref="AgoRapideAttributeEnriched.ValidatorAndParser"/>. 
        /// 
        /// TODO: Ensure that correct constructor for <see cref="Property"/> will be called.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="objResult"></param>
        /// <returns></returns>
        public static ParseResult Create<T>(AgoRapideAttributeEnriched key, T objResult) => new ParseResult(Property.Create(new PropertyKey(key), objResult), (object)objResult);
        private ParseResult(Property result, object objResult) {
            Result = result ?? throw new NullReferenceException(nameof(result));
            ObjResult = objResult ?? throw new NullReferenceException(nameof(objResult));
            ErrorResponse = null;
        }

        public static ParseResult Create(string errorResponse) => new ParseResult(errorResponse);
        private ParseResult(string errorResponse) {
            ErrorResponse = errorResponse ?? throw new NullReferenceException(nameof(errorResponse));
            Result = null;
        }
    }
}