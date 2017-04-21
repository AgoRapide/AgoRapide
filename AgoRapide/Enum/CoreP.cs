using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// <see cref="CoreP"/> represents core AgoRapide properties that must always be available in the client application. 
    /// You may change their names and meaning by using <see cref="PropertyKeyAttribute.InheritAndEnrichFromProperty"/> for your own <see cref="EnumType.PropertyKey"/> enums like this:
    /// 
    /// [Enum(EnumType = EnumType.PropertyKey)]
    /// public enum P {
    ///   ...
    ///   [PropertyKey(InheritAndEnrichFromProperty = CoreP.EntityToRepresent)]
    ///   LoggedInAs,
    ///   ...
    /// }
    /// 
    /// More often you will instead keep the name but add more information, like this:
    /// 
    /// [Enum(EnumType = EnumType.PropertyKey)]
    /// public enum P {
    ///   ...
    ///   [PropertyKey(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.EntityToRepresent)]
    ///   EntityToRepresent,
    ///   ...
    /// }
    /// 
    /// See <see cref="PropertyKeyAttributeEnriched.Initialize"/> for more details about enrichment.
    /// 
    /// See <see cref="CoreAPIMethod"/> for example of the recommended approach to setting attributes when the type given (<see cref="PropertyKeyAttribute.Type"/>) 
    /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
    /// </summary>
    [Enum(
        Description = "The core -" + nameof(EnumType.PropertyKey) + "-. All other -" + nameof(EnumType.PropertyKey) + "- are mapped to -" + nameof(CoreP) + "- at application startup through -" + nameof(EnumMapper) + "-.",
        AgoRapideEnumType = EnumType.PropertyKey
    )]
    public enum CoreP {
        None,

        /// <summary>
        /// For <see cref="BaseEntity"/> will correspond to <see cref="BaseEntity.RootProperty"/>
        /// 
        /// Should not be used in database for other purposes than storing an entity root property. 
        /// </summary>
        [PropertyKey(
            Description = "The root property of a -" + nameof(BaseEntity) + "-",
            Type = typeof(Type), Parents = new Type[] { typeof(BaseEntity) }, CanHaveChildren = true)]
        RootProperty,

        /// <summary>
        /// General type of entity.
        /// 
        /// Added to <see cref="BaseEntity.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [PropertyKey(
            Description = "Corresponds to C# / .NET Type-object.",
            Type = typeof(Type))]
        // Type = typeof(Type), Parents = new Type[] { typeof(GeneralQueryResult) })]
        EntityType,

        ///// <summary>
        //[AgoRapide(
        //    Description = "The root property of an entity. Added to entity object by -" + nameof(IDatabase.TryGetEntityById) + "-.",
        //    Type = typeof(Property), CanHaveChildren = true)]
        ///// </summary>
        //RootProperty,

        [PropertyKey(
            Description = "The unique property identifying users in your system.",
            IsUniqueInDatabase = true,
            IsObligatory = true,
            Type = typeof(string),
            PriorityOrder = PriorityOrder.Important)]
        Username,

        /// <summary>
        /// Note deliberate use of blank string for <see cref="PropertyKeyAttribute.SampleValues"/>. 
        /// We do not want for instance <see cref="RouteSegmentClass"/> to produce 
        /// a default value being used over and over again in installations worldwide.
        /// </summary>
        [PropertyKey(IsObligatory = true, IsPassword = true, Type = typeof(string), SampleValues = new string[] { "" })]
        Password,

        /// <summary>
        /// TODO: Try to not use this property. Why????
        /// </summary>
        [PropertyKey(
            Description = "Access level as given to an entity (typically a \"Person\"-object) as a right.",
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Admin,
            Type = typeof(AccessLevel))]
        AccessLevelGiven,

        [PropertyKey(
            Description = "Access level necessary for -" + nameof(AccessType.Read) + "--access to an object in the sense of using that objects functionality (typical access level necessary in order to call an -" + nameof(APIMethod) + "-).",
            Type = typeof(AccessLevel))]
        AccessLevelUse,

        [PropertyKey(
            Description = "Access level necessary for -" + nameof(AccessType.Read) + "--access to an object. If not given then -" + nameof(AccessLevel.Relation) + "- will typical be assumed.",
            Type = typeof(AccessLevel))]
        AccessLevelRead,

        [PropertyKey(
            Description = "Access level necessary for -" + nameof(AccessType.Write) + "--access to an object. If not given then -" + nameof(AccessLevel.Relation) + "- will typical be assumed.",
            Type = typeof(AccessLevel))]
        AccessLevelWrite,

        [PropertyKey(
            Description = "Generic property for naming objects.",
            Type = typeof(string),
            PriorityOrder = PriorityOrder.Important)]
        Name,

        /// <summary>
        /// Added to <see cref="BaseEntity.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [PropertyKey(
            Description = "-" + nameof(DBField.id) + "- of entity as stored in database.",
            Type = typeof(long))]
        DBId,

        [PropertyKey(Description = "Application specific general query request", Type = typeof(string), SampleValues = new string[] { "a", "b", "c" })]
        GeneralQueryId,

        [PropertyKey(
            Type = typeof(QueryId))]
        QueryId,

        [PropertyKey(
            Type = typeof(QueryIdInteger),
            ValidValues = new string[] { "42" })]
        IntegerQueryId,

        [PropertyKey(
            Type = typeof(QueryIdKeyOperatorValue))]
        PropertyAndValueQueryId,

        [PropertyKey(
            Description =
                "General identifier. Used by -" + nameof(Request.CreateAPILink) + "-.",
            LongDescription =
                "Values chosen should be compatible with HTTP GET URLs. " +
                "(best approach is most probably to make values valid C# identifiers.)",
            IsUniqueInDatabase = true,
            PriorityOrder = PriorityOrder.Important,
            AccessLevelRead = AccessLevel.Anonymous,
            Parents = new Type[] { typeof(ApplicationPart) })]
        Identifier,

        /// <summary>
        /// Note how this is deliberately <see cref="PropertyKey"/> (and not <see cref="PropertyKeyWithIndex"/>) since there are many situations where it is practical to
        /// allow <see cref="PropertyKeyAttribute.IsMany"/> without <see cref="PropertyKeyWithIndex.Index"/> (<see cref="CoreAPIMethod.UpdateProperty"/> for instance). 
        /// </summary>
        [PropertyKey(Type = typeof(PropertyKey))]
        Key,

        [PropertyKey(
            Description = "General conveyor of information.",
            Type = typeof(string))]
        Value,

        ///// <summary>
        ///// TODO: COMPLETE IMPLEMENTATION OF THIS CONCEPT
        ///// </summary>
        //[AgoRapide(
        //    Description = "A single enum value.",
        //    IsMany = true,
        //    Parents = new Type[] { typeof(EnumClass) }, Type = typeof(string))]
        //EnumValue,

        /// <summary>
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>
        /// 
        /// TODO: DO WE NEED THE AccessLevelWrite = AgoRapide.AccessLevel.Admin restriction here? Or can we
        /// TODO: instead have <see cref="IDatabase.SwitchIfHasEntityToRepresent"/> be more strict?
        /// </summary>
        [PropertyKey(
            Description = "The entity from whose perspective the API will show data. See also -" + nameof(RepresentedByEntity) + "-.",
            Type = typeof(long), AccessLevelRead = AgoRapide.AccessLevel.User, AccessLevelWrite = AgoRapide.AccessLevel.Admin)]
        EntityToRepresent,

        /// <summary>
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>
        /// </summary>
        [PropertyKey(Description = "The entity that this entity is represented by. See also -" + nameof(EntityToRepresent) + "-.")]
        RepresentedByEntity,

        [PropertyKey(
            Description = "Used to simulate 'logout'. Value 1 means that the next 'login' (that is, the next authentication attempt) will be denied.",
            LongDescription = "Set by an API-method called Logout. See -" + nameof(IDatabase.TryVerifyCredentials) + "-. Usually set no-longer-current after each use.",
            Type = typeof(bool))]
        RejectCredentialsNextTime,

        [PropertyKey(Type = typeof(bool))]
        AuthResult,

        /// <summary>
        /// Note how will be removed by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.ok"/> and not <see cref="APIMethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [PropertyKey(
            Description = "General log (will not show up in result if considered not needed).",
            Type = typeof(string))]
        Log,

        /// <summary>
        /// Typical used by <see cref="Request"/> / <see cref="Result"/>. 
        /// </summary>
        [PropertyKey(
            Description = "General message.",
            Type = typeof(string))]
        Message,

        [PropertyKey(Type = typeof(PropertyOperation))]
        PropertyOperation,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [PropertyKey(
            Description = "Count of new properties created.",
            Type = typeof(long))]
        PCreatedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [PropertyKey(
            Description = "Count of properties affected (as result of some database operation like INSERT, DELETE or UPDATE).",
            Type = typeof(long))]
        PAffectedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [PropertyKey(
            Description = "Count of changed properties.",
            Type = typeof(long))]
        PChangedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [PropertyKey(
            Description = "Count of unchanged properties.",
            Type = typeof(long))]
        PUnchangedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [PropertyKey(
            Description = "Count of all properties considered.",
            Type = typeof(long))]
        PTotalCount,

        [PropertyKey(Type = typeof(APIMethodOrigin))]
        APIMethodOrigin,

        [PropertyKey(Type = typeof(ResultCode), Parents = new Type[] { typeof(Result) })]
        ResultCode,

        /// <summary>
        /// The <see cref="PropertyKeyAttribute.Description"/>-attribute of <see cref="ResultCode"/>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [PropertyKey(Type = typeof(string), Parents = new Type[] { typeof(Result) })]
        ResultCodeDescription,

        [PropertyKey(
            Description =
                "URL suggested to client. " +
                "Will often accompany a -" + nameof(AgoRapide.ResultCode.missing_parameter_error) + "- or -" + nameof(AgoRapide.ResultCode.invalid_parameter_error) + "-. " +
                "Used in " + nameof(APIMethod) + " for giving samples. " +
                "Also useful for suggesting follow-up API-calls. ",
            Type = typeof(Uri), Parents = new Type[] { typeof(GeneralQueryResult) }, AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = PriorityOrder.Important)]
        SuggestedUrl,

        /// <summary>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [PropertyKey(
            Description = "Suggestions of relevant documentation for the API-method accessed.",
            Type = typeof(Uri))]
        APIDocumentationUrl,

        /// <summary>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.exception_error"/>
        /// </summary>
        [PropertyKey(
            Description = "URL offering details about the last exception that occurred. See also -" + nameof(AgoRapide.CoreAPIMethod) + "." + nameof(AgoRapide.CoreAPIMethod.ExceptionDetails) + "-.",
            Type = typeof(Uri))]
        ExceptionDetailsUrl,

        /// <summary>
        /// See <see cref="APIMethodAttribute.CoreMethod"/>. 
        /// 
        /// Note how we DO NOT set any <see cref="PropertyKeyAttribute.Description"/> description here, but instead rely
        /// on the <see cref="PropertyKeyAttribute.Description"/> set for <see cref="AgoRapide.CoreAPIMethod"/>. 
        /// This comment describes the recommended approach to setting attributes when the type given (<see cref="PropertyKeyAttribute.Type"/>) 
        /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// </summary>
        [PropertyKey(Type = typeof(CoreAPIMethod))]
        CoreAPIMethod,

        /// <summary>
        /// Corresponds to presence of <see cref="System.Web.Http.AuthorizeAttribute"/> or similar 
        /// (like AgoRapideSample.BasicAuthenticationAttribute)
        /// on the <see cref="BaseController"/>-method.
        /// </summary>
        [PropertyKey(
            Description = "Value TRUE signifies that API client needs to supply credentials in order to query API method.",
            Parents = new Type[] { typeof(APIMethod) }, Type = typeof(bool))]
        RequiresAuthorization,

        /// <summary>
        /// See <see cref="APIMethodAttribute.Environment"/>. 
        /// </summary>
        [PropertyKey(Type = typeof(Environment))]
        Environment,

        /// <summary>
        /// See <see cref="APIMethodAttribute.Description"/>. 
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(APIMethod), typeof(GeneralQueryResult) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = PriorityOrder.Important)]
        Description,

        /// <summary>
        /// See <see cref="APIMethodAttribute.LongDescription"/>
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous)]
        LongDescription,

        /// <summary>
        /// Does not originate from <see cref="APIMethodAttribute.RouteTemplate"/> but from
        /// <see cref="APIMethod.RouteTemplates"/>[0]
        /// TODO: Document better!
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(string))]
        RouteTemplate,

        [PropertyKey(
            Description = "The Controller class + the method within that class which implements a given method.",
            Parents = new Type[] { typeof(APIMethod) },
            AccessLevelRead = AccessLevel.Anonymous,
            Type = typeof(string))]
        Implementator,

        /// <summary>
        /// See <see cref="APIMethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [PropertyKey(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(bool))]
        ShowDetailedResult,

        [PropertyKey(
            Description = "Describes an entity that functions as the anonymous user. One such entity should always exist in the database.",
            IsUniqueInDatabase = true,
            Type = typeof(bool))]
        IsAnonymous,
    }
}
