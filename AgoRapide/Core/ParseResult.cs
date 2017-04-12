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

        /// <summary>
        /// Note HACK <see cref="Property.SetKey"/> which can be used to improve this value.
        /// </summary>
        public Property Result { get; private set; }

        /// <summary>
        /// Corresponds to <see cref="Result"/>.<see cref="Property.ADotTypeValue"/>
        /// TODO: Ideally we would like to do without this parameter but that would lead to <see cref="Property.ADotTypeValue"/> 
        /// calling itself 
        /// (see code line
        ///    aDotTypeValue = KeyA.TryValidateAndParse(V<string>(), out var temp) ? temp.ObjResult : null;
        /// )
        /// 
        /// TODO: AFTER INTRODUCTION OF GENERIC <see cref="PropertyT{T}"/> there is a great chance that this member may be omitted. 
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
        /// 
        /// TODO: As of Apr 2017 <see cref="AgoRapideAttribute.IsMany"/> is not supported for <paramref name="key"/>
        /// TODO: (Unable to create a <see cref="Property"/>-object because a <see cref="PropertyKey"/> will be needed).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="objResult"></param>
        /// <returns></returns>
        public static ParseResult Create<T>(AgoRapideAttributeEnriched key, T objResult) =>
            new ParseResult(
                new PropertyT<T>(
                    new Func<PropertyKey>(() => {
                        if (!key.A.IsMany) return new PropertyKey(key); /// TODO: Clean up all handling of <see cref="PropertyKey"/> and <see cref="PropertyKeyNonStrict"/>
                        var retval = new PropertyKeyNonStrict(key);
                        retval.SetPropertyKeyAndPropertyKeyAsIsManyParentOrTemplate(); 
                        return retval.PropertyKeyAsIsManyParentOrTemplate; // Note that this may be just delaying throwing of the inevitable exception
                    })(),
                    // TODO: Alternative to code above.
                    //new PropertyKey(!key.A.IsMany ? 
                    //    key : 
                    //    throw new PropertyKey.InvalidPropertyKeyException(Util.BreakpointEnabler + "Unable to create for " + nameof(AgoRapideAttribute.IsMany) + ".\r\nDetails: " + nameof(objResult) + ": " + objResult.GetType() + " = " + objResult + "\r\n" + key.ToString())), 
                    objResult
                ),
                (object)objResult /// TODO: AFTER INTRODUCTION OF GENERIC <see cref="PropertyT{T}"/> there is a great chance that this member may be omitted. 
            );

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