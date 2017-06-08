// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
        /// Will be null if Result is set
        /// </summary>
        public string ErrorResponse { get; private set; }

        /// <summary>
        /// Typically called from <see cref="PropertyKeyAttributeEnriched.ValidatorAndParser"/>. 
        /// 
        /// TODO: Ensure that correct constructor for <see cref="Property"/> will be called.
        /// 
        /// TODO: As of Apr 2017 <see cref="PropertyKeyAttribute.IsMany"/> is not supported for <paramref name="key"/>
        /// TODO: (Unable to create a <see cref="Property"/>-object because a <see cref="PropertyKeyWithIndex"/> will be needed).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="objResult"></param>
        /// <returns></returns>
        public static ParseResult Create<T>(PropertyKeyAttributeEnriched key, T objResult) =>
            new ParseResult(
                new PropertyT<T>(
                    new Func<PropertyKeyWithIndex>(() => {
                        if (!key.A.IsMany) return new PropertyKeyWithIndex(key); /// TODO: Clean up all handling of <see cref="PropertyKeyWithIndex"/> and <see cref="PropertyKey"/>
                        var retval = new PropertyKey(key);
                        retval.SetPropertyKeyWithIndexAndPropertyKeyAsIsManyParentOrTemplate(); 
                        return retval.PropertyKeyAsIsManyParentOrTemplate; // Note that this may be just delaying throwing of the inevitable exception
                    })(),
                    // TODO: Alternative to code above.
                    //new PropertyKey(!key.A.IsMany ? 
                    //    key : 
                    //    throw new PropertyKey.InvalidPropertyKeyException(Util.BreakpointEnabler + "Unable to create for " + nameof(AgoRapideAttribute.IsMany) + ".\r\nDetails: " + nameof(objResult) + ": " + objResult.GetType() + " = " + objResult + "\r\n" + key.ToString())), 
                    objResult
                )
            );

        private ParseResult(Property result) {
            Result = result ?? throw new NullReferenceException(nameof(result));
            ErrorResponse = null;
        }

        public static ParseResult Create(string errorResponse) => new ParseResult(errorResponse);
        private ParseResult(string errorResponse) {
            ErrorResponse = errorResponse ?? throw new NullReferenceException(nameof(errorResponse));
            Result = null;
        }
    }
}