﻿// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.API {

    /// <summary>
    /// TODO: Add some kind of SuggestedNextMethod / SuggestedNextCommand, that will be automatically added in HTML-responses
    /// TODO: to client. We have a problem of mapping them though, but autogenerated methods could contain such a thing. 
    /// </summary>
    [Class(Description = "General attributes for a -" + nameof(APIMethod) + "-.")]
    public class APIMethodAttribute : BaseAttribute {

        public CoreAPIMethod CoreMethod { get; set; }
        public void AssertCoreMethod(CoreAPIMethod coreMethod) {
            if (CoreMethod != coreMethod) throw new InvalidEnumException(CoreMethod, "Expected " + coreMethod);
        }

        /// <summary>
        /// TODO: Implement support for <see cref="APIMethodAttribute.RouteTemplate"/>.
        /// </summary>
        public string RouteTemplate { get; set; }

        [ClassMember(Description = "Route segment 1. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S1 { get; set; }
        [ClassMember(Description = "Route segment 2. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S2 { get; set; }
        [ClassMember(Description = "Route segment 3. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S3 { get; set; }
        [ClassMember(Description = "Route segment 4. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S4 { get; set; }
        [ClassMember(Description = "Route segment 5. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S5 { get; set; }
        [ClassMember(Description = "Route segment 6. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S6 { get; set; }
        [ClassMember(Description = "Route segment 7. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S7 { get; set; }
        [ClassMember(Description = "Route segment 8. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S8 { get; set; }
        [ClassMember(Description = "Route segment 9. Turned into -" + nameof(RouteSegmentClass) + "- by -" + nameof(APIMethod.CreateSemiAutogeneratedMethods) + "-.")]
        public object S9 { get; set; }

        /// <summary>
        /// <see cref="Request.CurrentUser"/> (<see cref="BaseEntity.AccessLevelGiven"/>) must by equal to <see cref="APIMethodAttribute.AccessLevelUse"/> or HIGHER in order for access to be granted to <see cref="APIMethod"/>
        /// </summary>
        public AccessLevel AccessLevelUse { get; set; } = AccessLevel.User;

        /// <summary>
        /// TRUE means that detailed result information like <see cref="Result.LogData"/> and <see cref="Result.Counts"/> 
        /// shall be returned to client. This is useful for instance for long duration complex methods where the (human) client usually wants to 
        /// understand what is going on internally in the API.
        /// </summary>
        public bool ShowDetailedResult { get; set; } = false;
    
        /// <summary>
        /// The current <see cref="Util.Configuration"/>.<see cref="ConfigurationAttribute.Environment"/> has to be equivalent or lower in order for the method to be included in the API routing
        /// </summary>
        public Environment Environment { get; set; } = Environment.Production;

        /// <summary>
        /// TODO: Not inplemented as of Jan 2017
        /// 
        /// Next method that client is suggested to call. 
        /// Typical would be to suggest Person/{id} after doing Person/Add/{first_name}/{last_name} for instance. 
        /// <see cref="Result.ToHTMLDetailed"/> could add this, since it knows request, parameters and so on.
        /// Typical would be to replace {id} in this string with result values like <see cref="CoreP.Id"/>. 
        /// We could also add this to <see cref="Result.ToJSONDetailed"/>
        /// </summary>
        public string SuggestedNextMethod { get; set; }

        protected override Dictionary<CoreP, Property> GetProperties() {
            /// TODO: Replace <see cref="PropertiesParent"/> with a method someting to <see cref="BaseEntity.AddProperty{T}"/> instead.
            /// TODO: Maybe with [System.Runtime.CompilerServices.CallerMemberName] string caller = "" in order to
            /// TDOO: call <see cref="ClassMemberAttribute.GetAttribute(Type, string)"/> automatically for instance.
            PropertiesParent.Properties = new Dictionary<CoreP, Property>(); // Hack, since maybe reusing collection
            Func<string> d = () => ToString();

            /// Note how we are not adding None-values since they will be considered invalid at later reading from database.
            /// Note how string value and <see cref="Property.ValueA"/> (<see cref="BaseAttribute"/>) are easily deduced by <see cref="PropertyT{T}"/> in this case so we do not need to add those as parameters here.
            if (CoreMethod != CoreAPIMethod.None) PropertiesParent.AddProperty(APIMethodP.CoreAPIMethod.A(), CoreMethod, d);
            if (AccessLevelUse != AccessLevel.None) PropertiesParent.AddProperty(CoreP.AccessLevelUse.A(), AccessLevelUse, d);
            if (Environment != Environment.None) PropertiesParent.AddProperty(CoreP.Environment.A(), Environment, d);

            /// Note adding of string value and <see cref="Property.ValueA"/> (<see cref="BaseAttribute"/>) here
            PropertiesParent.AddProperty(APIMethodP.ShowDetailedResult.A(), ShowDetailedResult, ShowDetailedResult.ToString(), GetType().GetClassMemberAttribute(nameof(ShowDetailedResult)), d);

            PropertiesParent.AddProperty(CoreP.Message.A(), "TODO: ADD MORE PROPERTIES IN " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name, d);
            /// TODO: Add more values to this list. Expand <see cref="ConfigurationKey"/> as needed.
            return PropertiesParent.Properties;
        }

        /// <summary>
        /// TODO: Expand on this, include information from <see cref="S1"/> and onwards for instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => nameof(CoreMethod) + ": " + CoreMethod + ".\r\n" + base.ToString();

        private Id _id;
        /// <summary>
        /// Note how APIMethod is added in front of the identifier
        /// </summary>
        /// <param name="id"></param>
        public void SetId(Id id) {
            if (_id != null) throw new NotNullReferenceException(nameof(_id) + ". Details: " + ToString());
            _id = id;
        }
        protected override Id GetId() => _id ?? throw new NullReferenceException(Util.BreakpointEnabler + nameof(_id) + ". Must be set by call to -" + nameof(SetId) + "-. Details: " + ToString());
    }
}
