// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// Originates from <see cref="APIMethodAttribute.S1"/> to <see cref="APIMethodAttribute.S9"/>
    /// 
    /// Typical example:
    ///   [MethodAttribute(S1 = typeof(Person), S2 = "Add", S3 = P.first_name, S4 = P.last_name)]
    /// which will result in the following routing template being given to System.Web.Http.HttpConfiguration.Routes.MapHttpRoute:
    ///   /api/Person/Add/{first_name}/{last_name}/
    ///   /api/Person/Add/{first_name}/{last_name}/HTML
    ///    
    /// Note that overloads without parameters will also be generated, like 
    ///   api/Person/Add/{first_name} and api/Person/Add/
    /// This helps make the API discoverable
    /// </summary>
    public class RouteSegmentClass {

        /// <summary>
        /// Explains from which of <see cref="APIMethodAttribute.S1"/> ... <see cref="APIMethodAttribute.S9"/> segment originates.
        /// </summary>
        public string SegmentName { get; private set; }

        /// <summary>
        /// Possible values to use in the actual URL for this segment. 
        /// 
        /// Will either be 
        /// 1) A single-element List corresponding directly to <see cref="Type"/>, <see cref="RouteSegment"/>, <see cref="String"/> or 
        /// 2) The <see cref="PropertyKeyAttribute.SampleValues"/> relevant for <see cref="Parameter"/>
        ///    (in "worst case", if <see cref="PropertyKeyAttribute.SampleValues"/> is null or empty then a single-element List consisting of 
        ///    "[No sample value defined for ...]" will be given.
        /// (in other words this property will always have at least one non-null value defined, but note how for instance 
        /// <see cref="CoreP.Password"/>) is deliberately set up with a blank value)
        /// </summary>
        public List<string> SampleValues { get; private set; }

        /// <summary>
        /// Typical example would be Person like api/Person/
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Typical example would be <see cref="CoreP.QueryId"/> like api/Person/{QueryId}
        /// </summary>
        public PropertyKey Parameter { get; private set; }

        /// <summary>
        /// Typical example would be Add like api/Person/Add
        /// </summary>
        public string String { get; private set; }

        /// <summary>
        /// Typical example would be Person or {QueryId}. 
        /// For a collection of <see cref="RouteSegmentClass"/> use <see cref="String.Join"/> with "/" as separator in order to get the <see cref="APIMethod.RouteTemplates"/>
        /// </summary>
        public string Template { get; private set; }

        /// <summary> 
        /// TODO: REMOVE <paramref name="segmentName"/>. We are able to reconstruct it from <paramref name="segment"/>
        /// </summary>
        /// <param name="segmentName">
        /// Used in case of exception. See <see cref="SegmentName"/>
        /// TODO: REMOVE <paramref name="segmentName"/>. We are able to reconstruct it from <paramref name="segment"/>
        /// </param>        
        /// <param name="segment">
        /// Must match one of 
        /// <see cref="Type"/> (<see cref="System.Type"/>), 
        /// <see cref="Parameter"/> (either <see cref="PropertyKey"/> or enum understood by <see cref="PropertyKeyMapper"/>) or 
        /// <see cref="String"/> (<see cref="System.String"/>). 
        /// See <see cref="InvalidRouteSegmentClassException"/> for details. 
        /// </param>
        /// <param name="detailer">
        /// Must be set. 
        /// Used to give details in case of an exception being thrown
        /// </param>
        public RouteSegmentClass(string segmentName, object segment, Func<string> detailer) {
            SegmentName = segmentName ?? throw new ArgumentNullException(segmentName);
            if (detailer == null) throw new ArgumentNullException(nameof(detailer));
            Type = (segment ?? throw new ArgumentNullException(nameof(segment))) as Type;
            if (Type != null) {
                if (!typeof(BaseEntity).IsAssignableFrom(Type)) throw new InvalidTypeException(Type, typeof(BaseEntity), "All types used with " + nameof(APIMethod) + " must inherit " + nameof(BaseEntity));
                SampleValues = new List<string> { Type.ToStringVeryShort() };
                TypeToStringShortToLower = SampleValues[0].ToLower();
                Template = Type.ToStringVeryShort();
                return;
            }

            if (segment.GetType().IsEnum) { /// Turn into <see cref="PropertyKey"/>
                if (EnumAttribute.GetAttribute(segment.GetType()).AgoRapideEnumType != EnumType.PropertyKey) {
                    throw new InvalidRouteSegmentClassException(segment.GetType().ToString());
                } else {
                    if (PropertyKeyMapper.TryGetA(segment.ToString(), out var temp)) {
                        segment = temp;
                    } else {
                        throw new InvalidRouteSegmentClassException(
                            segment.GetType() + "." + segment + " not recognized by " + nameof(PropertyKeyMapper) + "\r\n\r\n" +
                            PropertyKeyMapper.GetProbableCauseAndResolutionForInvalidMapping(segment.GetType()));
                    }
                }
            }

            Parameter = segment as PropertyKey;
            if (Parameter != null) {
                SampleValues = new Func<List<string>>(() => {
                    var a = Parameter.Key.A;
                    if (a.SampleValues == null || a.SampleValues.Length == 0) return new List<string> { "[No sample value defined for " + Parameter.Key.A.EnumValueExplained };
                    return a.SampleValues.ToList(); // Note that we do not react to empty sample values (like uses for passwords)
                })();
                PropertyToStringToLower = Parameter.Key.PToString.ToLower();
                Template = "{" + Parameter.Key.PToString + "}";
                return;
            }

            String = segment as string;
            if (String != null) {
                SampleValues = new List<string> { String };
                StringToLower = String.ToLower();
                Template = String;
                return;
            }
            throw new InvalidRouteSegmentClassException("The type of " + SegmentName + "'s value (" + segment.GetType() + ") is not recognized.\r\n\r\nDetails: " + detailer());
        }

        /// Adding to <see cref="MatchesURLSegment"/> or not?
        ///// <param name="strict">
        ///// FALSE means not checking actual value of parameter
        ///// TRUE means checking actual value of parameter
        ///// </param>

        /// <summary>
        /// Returns TRUE if given part of an API call URL corresponds to this class.
        /// 
        /// TODO: Turn into TryMatch... and have out-parameter as finished validated and parsed value
        /// </summary>
        /// <param name="urlSegment"></param>
        /// <param name="urlSegmentToLower">string.ToLower() representation of <paramref name="urlSegment"/></param>
        /// <returns></returns>
        public bool MatchesURLSegment(string urlSegment, string urlSegmentToLower) {
            if (Type != null) {
                return TypeToStringShortToLower.Equals(urlSegmentToLower);
                //} else if (RouteSegmentToStringToLower != null) {
                //    return RouteSegmentToStringToLower.Equals(urlSegmentToLower);
            } else if (Parameter != null) {
                // Removed this 15 March 2017. We also want empty parameters to match at this stage.
                // if (string.IsNullOrEmpty(urlSegment)) return false;
                // ---------------------

                // TODO: IMPLEMENT REST HERE!
                // TODO: DO THIS:
                // if (strSegment is ACCEPTABLE VALUE FOR p) RETURN TRUE;
                // TODO: RETURN FOUND VALUE

                /// OR, MAYBE WAIT WITH THAT, since we want TryRequest to evaluate the value found (in other words, it is "too early" to filter out method candidates here)
                /// On the other hand, that again leads to the problem of duplicate exact mathcing within <see cref="Request.GetMethodsMatchingRequest"/>
                /// Typical example would be
                ///   Autogenerated, Person/{QueryIdInteger}/History, CoreMethod.History
                /// and
                ///   Autogenerated, Person/{QueryId}/{PropertyOperation}, CoreMethod.PropertyOperation

                return true;
            } else if (StringToLower != null) {
                return StringToLower.Equals(urlSegmentToLower);
            } else {
                throw new InvalidRouteSegmentClassException("Type not recognized. This exception should never happen because the constructor should not have allowed it in the first place.");
            }
        }

        /// <summary>
        /// Serves a performance improvement purpose (doing operation Type.ToString().ToLower() only once)
        /// </summary>
        private string TypeToStringShortToLower;

        /// <summary>
        /// Serves a performance improvement purpose (doing operation Property.ToString().ToLower() only once)
        /// </summary>
        private string PropertyToStringToLower;

        /// <summary>
        /// Serves a performance improvement purpose (doing operation String.ToLower() only once)
        /// </summary>
        private string StringToLower;

        public class InvalidRouteSegmentClassException : ApplicationException {
            public InvalidRouteSegmentClassException(string message) : base(
                "A " + nameof(RouteSegmentClass) + " must have one of the following types:\r\n\r\n" +
                "-" + typeof(Type).ToString() + ",\r\n" +
                "-" + typeof(PropertyKeyWithIndex).ToString() + " (or an -" + nameof(EnumType) + "-.-" + nameof(EnumType.PropertyKey) + "--enum recognized by -" + nameof(PropertyKeyMapper) + "),\r\n" +
                "-" + typeof(string).ToString() + ".\r\n\r\nThis does not correspond to the following:\r\n" + message) { }
        }
    }
}
