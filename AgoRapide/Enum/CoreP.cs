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
    /// See <see cref="EnumMapper"/>.
    /// 
    /// <see cref="CoreP"/> represents core AgoRapide properties that must always be available in the client application. 
    /// You may change their names and meaning by using <see cref="AgoRapideAttribute.InheritAndEnrichFromProperty"/> for your own <see cref="EnumType.EntityPropertyEnum"/> enums like this:
    /// 
    /// [AgoRapide(EnumType = EnumType.EntityPropertyEnum)]
    /// public enum P {
    ///   ...
    ///   [AgoRapideAttribute(InheritAndEnrichFromProperty = CoreP.EntityToRepresent)]
    ///   LoggedInAs,
    ///   ...
    /// }
    /// 
    /// More often you will instead keep the name but add more information, like this:
    /// 
    /// [AgoRapide(EnumType = EnumType.EntityPropertyEnum)]
    /// public enum P {
    ///   ...
    ///   [AgoRapide(Parents = new Type[] { typeof(Person) }, InheritAndEnrichFromProperty = CoreP.EntityToRepresent)]
    ///   EntityToRepresent,
    ///   ...
    /// }
    /// 
    /// See <see cref="AgoRapideAttributeEnriched.Initialize"/> for more details about enrichment.
    /// 
    /// See <see cref="CoreMethod"/> for example of the recommended approach to setting attributes when the type given (<see cref="AgoRapideAttribute.Type"/>) 
    /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
    /// </summary>
    [AgoRapide(
        Description = "The core -" + nameof(EnumType.EntityPropertyEnum) + "-. All other -" + nameof(EnumType.EntityPropertyEnum) + "- are mapped to -" + nameof(CoreP) + "- at application startup through -" + nameof(EnumMapper) + "-.",
        EnumType = EnumType.EntityPropertyEnum
    )]
    public enum CoreP {
        None,

        /// <summary>
        /// General type of entity.
        /// 
        /// For <see cref="BaseEntity"/> will usually correspond to <see cref="BaseEntity.RootProperty"/>
        /// 
        /// Also added to <see cref="BaseEntity.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [AgoRapide(
            Description = "Corresponds to C# / .NET Type-object.",
            Type = typeof(Type), Parents = new Type[] { typeof(GeneralQueryResult) }, CanHaveChildren = true)]
        Type,

        /// <summary>
        [AgoRapide(
            Description = "The root property of an entity. Added to entity object by -" + nameof(IDatabase.TryGetEntityById) + "-.",
            Type = typeof(Property), CanHaveChildren = true)]
        /// </summary>
        RootProperty,

        [AgoRapide(
            Description = "The unique property identifying users in your system.",
            IsUniqueInDatabase = true,
            IsObligatory = true,
            Type = typeof(string),
            PriorityOrder = -1)]
        Username,

        /// <summary>
        /// Note deliberate use of blank string for <see cref="AgoRapideAttribute.SampleValues"/>. 
        /// We do not want for instance <see cref="RouteSegmentClass"/> to produce 
        /// a default value being used over and over again in installations worldwide.
        /// </summary>
        [AgoRapide(IsObligatory = true, IsPassword = true, Type = typeof(string), SampleValues = new string[] { "" })]
        Password,

        /// <summary>
        /// TODO: Try to not use this property. Why????
        /// </summary>
        [AgoRapide(
            Description = "Access level as given to an entity (typically a \"Person\"-object) as a right.",
            AccessLevelRead = AccessLevel.Anonymous,
            AccessLevelWrite = AccessLevel.Admin,
            Type = typeof(AccessLevel))]
        AccessLevelGiven,

        [AgoRapide(
            Description = "Access level necessary for -" + nameof(AccessType.Read) + "--access to an object in the sense of using that objects functionality (typical access level necessary in order to call an -" + nameof(APIMethod) + "-).",
            Type = typeof(AccessLevel))]
        AccessLevelUse,

        [AgoRapide(
            Description = "Access level necessary for -" + nameof(AccessType.Read) + "--access to an object. If not given then -" + nameof(AccessLevel.Relation) + "- will typical be assumed.",
            Type = typeof(AccessLevel))]
        AccessLevelRead,

        [AgoRapide(
            Description = "Access level necessary for -" + nameof(AccessType.Write) + "--access to an object. If not given then -" + nameof(AccessLevel.Relation) + "- will typical be assumed.",
            Type = typeof(AccessLevel))]
        AccessLevelWrite,

        [AgoRapide(
            Description = "Generic property for naming objects.",
            Type = typeof(string),
            PriorityOrder = -1)]
        Name,

        /// <summary>
        /// Added to <see cref="BaseEntity.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [AgoRapide(
            Description = "-" + nameof(DBField.id) + "- of entity as stored in database.",
            Type = typeof(long))]
        DBId,

        [AgoRapide(Description = "Application specific general query request", Type = typeof(string), SampleValues = new string[] { "a", "b", "c" })]
        GeneralQueryId,

        [AgoRapide(
            Type = typeof(QueryId))]
        QueryId,

        [AgoRapide(
            Type = typeof(IntegerQueryId),
            ValidValues = new string[] { "42" })]
        IntegerQueryId,

        [AgoRapide(
            Type = typeof(PropertyValueQueryId))]
        PropertyAndValueQueryId,

        [AgoRapide(Description = "General identifier.", IsUniqueInDatabase = true, Parents = new Type[] { typeof(ApplicationPart) })]
        Identifier,

        /// <summary>
        /// Note how this is deliberately <see cref="PropertyKeyNonStrict"/> (and not <see cref="PropertyKey"/>) since there are many situations where it is practical to
        /// allow <see cref="AgoRapideAttribute.IsMany"/> without <see cref="PropertyKey.Index"/> (<see cref="CoreMethod.UpdateProperty"/> for instance). 
        /// </summary>
        [AgoRapide(Type = typeof(PropertyKeyNonStrict))]
        Key,

        [AgoRapide(
            Description = "General conveyor of information.",
            Type = typeof(string))]
        Value,

        /// <summary>
        /// TODO: COMPLETE IMPLEMENTATION OF THIS CONCEPT
        /// </summary>
        [AgoRapide(
            Description = "A single enum value.",
            IsMany = true,
            Parents = new Type[] { typeof(EnumClass) }, Type = typeof(string))]
        EnumValue,

        /// <summary>
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>
        /// 
        /// TODO: DO WE NEED THE AccessLevelWrite = AgoRapide.AccessLevel.Admin restriction here? Or can we
        /// TODO: instead have <see cref="IDatabase.SwitchIfHasEntityToRepresent"/> be more strict?
        /// </summary>
        [AgoRapide(
            Description = "The entity from whose perspective the API will show data. See also -" + nameof(RepresentedByEntity) + "-.",
            Type = typeof(long), AccessLevelRead = AgoRapide.AccessLevel.User, AccessLevelWrite = AgoRapide.AccessLevel.Admin)]
        EntityToRepresent,

        /// <summary>
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>
        /// </summary>
        [AgoRapide(Description = "The entity that this entity is represented by. See also -" + nameof(EntityToRepresent) + "-.")]
        RepresentedByEntity,

        [AgoRapide(
            Description = "Used to simulate 'logout'. Value 1 means that the next 'login' (that is, the next authentication attempt) will be denied.",
            LongDescription = "Set by an API-method called Logout. See -" + nameof(IDatabase.TryVerifyCredentials) + "-. Usually set no-longer-current after each use.",
            Type = typeof(bool))]
        RejectCredentialsNextTime,

        [AgoRapide(Type = typeof(bool))]
        AuthResult,

        /// <summary>
        /// Note how will be removed by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.ok"/> and not <see cref="MethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [AgoRapide(
            Description = "General log (will not show up in result if considered not needed).",
            Type = typeof(string))]
        Log,

        /// <summary>
        /// Typical used by <see cref="Request"/> / <see cref="Result"/>. 
        /// </summary>
        [AgoRapide(
            Description = "General message.",
            Type = typeof(string))]
        Message,

        [AgoRapide(Type = typeof(PropertyOperation))]
        PropertyOperation,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of new properties created.",
            Type = typeof(long))]
        PCreatedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of properties affected (as result of some database operation like INSERT, DELETE or UPDATE).",
            Type = typeof(long))]
        PAffectedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of changed properties.",
            Type = typeof(long))]
        PChangedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of unchanged properties.",
            Type = typeof(long))]
        PUnchangedCount,

        /// <summary>
        /// See <see cref="Result"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of all properties considered.",
            Type = typeof(long))]
        PTotalCount,

        [AgoRapide(Type = typeof(APIMethodOrigin))]
        APIMethodOrigin,

        [AgoRapide(Type = typeof(ResultCode), Parents = new Type[] { typeof(Result) })]
        ResultCode,

        /// <summary>
        /// The <see cref="AgoRapideAttribute.Description"/>-attribute of <see cref="ResultCode"/>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [AgoRapide(Type = typeof(string), Parents = new Type[] { typeof(Result) })]
        ResultCodeDescription,

        [AgoRapide(
            Description =
                "URL suggested to client. " +
                "Will often accompany a -" + nameof(AgoRapide.ResultCode.missing_parameter_error) + "- or -" + nameof(AgoRapide.ResultCode.invalid_parameter_error) + "-. " +
                "Used in " + nameof(APIMethod) + " for giving samples. " +
                "Also useful for suggesting follow-up API-calls. ",
            Type = typeof(Uri), Parents = new Type[] { typeof(GeneralQueryResult) }, AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = -1)]
        SuggestedUrl,

        /// <summary>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [AgoRapide(
            Description = "Suggestions of relevant documentation for the API-method accessed.",
            Type = typeof(Uri))]
        APIDocumentationUrl,

        /// <summary>
        /// Set by <see cref="Result.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.exception_error"/>
        /// </summary>
        [AgoRapide(
            Description = "URL offering details about the last exception that occurred. See also -" + nameof(AgoRapide.CoreMethod) + "." + nameof(AgoRapide.CoreMethod.ExceptionDetails) + "-.",
            Type = typeof(Uri))]
        ExceptionDetailsUrl,

        /// <summary>
        /// See <see cref="MethodAttribute.CoreMethod"/>. 
        /// 
        /// Note how we DO NOT set any <see cref="AgoRapideAttribute.Description"/> description here, but instead rely
        /// on the <see cref="AgoRapideAttribute.Description"/> set for <see cref="AgoRapide.CoreMethod"/>. 
        /// This comment describes the recommended approach to setting attributes when the type given (<see cref="AgoRapideAttribute.Type"/>) 
        /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// </summary>
        [AgoRapide(Type = typeof(CoreMethod))]
        CoreMethod,

        /// <summary>
        /// Corresponds to presence of <see cref="System.Web.Http.AuthorizeAttribute"/> or similar 
        /// (like AgoRapideSample.BasicAuthenticationAttribute)
        /// on the <see cref="BaseController"/>-method.
        /// </summary>
        [AgoRapide(
            Description = "Value TRUE signifies that API client needs to supply credentials in order to query API method.",
            Parents = new Type[] { typeof(APIMethod) }, Type = typeof(bool))]
        RequiresAuthorization,

        /// <summary>
        /// See <see cref="MethodAttribute.Environment"/>. 
        /// </summary>
        [AgoRapide(Type = typeof(Environment))]
        Environment,

        /// <summary>
        /// See <see cref="MethodAttribute.Description"/>. 
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod), typeof(GeneralQueryResult) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = -1)]
        Description,

        /// <summary>
        /// See <see cref="MethodAttribute.LongDescription"/>
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous)]
        LongDescription,

        /// <summary>
        /// Does not originate from <see cref="MethodAttribute.RouteTemplate"/> but from
        /// <see cref="APIMethod.RouteTemplates"/>[0]
        /// TODO: Document better!
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(string))]
        RouteTemplate,

        /// <summary>
        /// Controller + method which implements method.
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(string))]
        Implementator,

        /// <summary>
        /// See <see cref="MethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod) }, Type = typeof(bool))]
        ShowDetailedResult,

        [AgoRapide(
            Description = "Describes an entity that functions as the anonymous user. One such entity should always exist in the database.",
            IsUniqueInDatabase = true,
            Type = typeof(bool))]
        IsAnonymous,
    }
}
