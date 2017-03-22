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
    /// See <see cref="CorePropertyMapper{TProperty}"/>.
    /// 
    /// <see cref="CoreProperty"/> represents core AgoRapide properties that must always be available in the client application. 
    /// 
    /// If you want the enum names themselves to be different in your application 
    /// you may decorate with <see cref="AgoRapideAttribute.CoreProperty"/> attribute like this:
    /// 
    /// public enum P {
    ///   ...
    ///   [AgoRapideAttribute(CoreProperty = CoreProperty.EntityToRepresent)]
    ///   LoggedInAs,
    ///   ...
    /// }
    /// 
    /// Often you will keep the name but add more information, like this:
    /// public enum P {
    ///   ...
    ///   [AgoRapide(Parents = new Type[] { typeof(Person) }, CoreProperty = CoreProperty.EntityToRepresent)]
    ///   EntityToRepresent,
    ///   ...
    /// }
    /// 
    /// See <see cref="AgoRapideAttribute.EnrichFrom"/> for how attributes given here are added to those of
    /// your chosen "TProperty"-enum.
    /// 
    /// See <see cref="CoreMethod"/> for example of the recommended approach to setting attributes when the type given (<see cref="AgoRapideAttribute.Type"/>) 
    /// is one of your own classes / enums, or one of the AgoRapide classes / enums 
    /// </summary>
    public enum CoreProperty {
        None,

        /// <summary>
        /// General type of entity.
        /// 
        /// For <see cref="BaseEntityT{TProperty}"/> will usually correspond to <see cref="BaseEntityT{TProperty}.RootProperty"/>
        /// 
        /// Also added to <see cref="BaseEntityT{TProperty}.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [AgoRapide(
            Description = "Corresponds to C# / .NET Type-object.",
            Type = typeof(Type), Parents = new Type[] { typeof(GeneralQueryResult<CoreProperty>) }, CanHaveChildren = true)]
        Type,

        /// <summary>
        [AgoRapide(
            Description = "The root property of an entity. Added to entity object by -" + nameof(IDatabase<CoreProperty>.TryGetEntityById) + "-.",
            Type = typeof(Property<CoreProperty>), CanHaveChildren = true)]
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
        /// We do not want for instance <see cref="RouteSegmentClass{TProperty}"/> to produce 
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
            Description = "Access level necessary for -" + nameof(AccessType.Read) + "--access to an object in the sense of using that objects functionality (typical access level necessary in order to call an -" + nameof(APIMethod<CoreProperty>) + "-).",
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
        /// Added to <see cref="BaseEntityT{TProperty}.Properties"/> by <see cref="IDatabase.TryGetEntityById"/>
        /// </summary>
        [AgoRapide(
            Description = "-" + nameof(DBField.id) + "- of entity as stored in database.",
            Type = typeof(long))]
        DBId,

        [AgoRapide(Description = "Application specific general query request", Type = typeof(string), SampleValues = new string[] { "a", "b", "c" })]
        GeneralQueryId,

        [AgoRapide(
            Type = typeof(QueryId<CoreProperty>))] /// Note how <see cref="AgoRapideAttributeT{TProperty}"/> changes CoreProperty into TProperty
        QueryId,

        [AgoRapide(
            Type = typeof(IntegerQueryId<CoreProperty>))] /// Note how <see cref="AgoRapideAttributeT{TProperty}"/> changes CoreProperty into TProperty
        IntegerQueryId,

        [AgoRapide(
            Type = typeof(PropertyValueQueryId<CoreProperty>))] /// Note how <see cref="AgoRapideAttributeT{TProperty}"/> changes CoreProperty into TProperty
        PropertyAndValueQueryId,

        [AgoRapide(
            Description = "General key. Describes a value for the TProperty-type (usually P) used in your application.",
            Parents = new Type[] { typeof(ApplicationPart<CoreProperty>) }, Type = typeof(CoreProperty))]
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
            Parents = new Type[] { typeof(EnumClass<CoreProperty>) }, Type = typeof(string))]
        EnumValue,

        /// <summary>
        /// See <see cref="IDatabase{TProperty}.SwitchIfHasEntityToRepresent"/>
        /// 
        /// TODO: DO WE NEED THE AccessLevelWrite = AgoRapide.AccessLevel.Admin restriction here? Or can we
        /// TODO: instead have <see cref="IDatabase{TProperty}.SwitchIfHasEntityToRepresent"/> be more strict?
        /// </summary>
        [AgoRapide(
            Description = "The entity from whose perspective the API will show data. See also -" + nameof(RepresentedByEntity) + "-.",
            Type = typeof(long), AccessLevelRead = AgoRapide.AccessLevel.User, AccessLevelWrite = AgoRapide.AccessLevel.Admin)]
        EntityToRepresent,

        /// <summary>
        /// See <see cref="IDatabase{TProperty}.SwitchIfHasEntityToRepresent"/>
        /// </summary>
        [AgoRapide(Description = "The entity that this entity is represented by. See also -" + nameof(EntityToRepresent) + "-.")]
        RepresentedByEntity,

        [AgoRapide(
            Description = "Used to simulate 'logout'. Value 1 means that the next 'login' (that is, the next authentication attempt) will be denied.",
            LongDescription = "Set by an API-method called Logout. See -" + nameof(IDatabase<CoreProperty>.TryVerifyCredentials) + "-. Usually set no-longer-current after each use.",
            Type = typeof(bool))]
        RejectCredentialsNextTime,

        [AgoRapide(Type = typeof(bool))]
        AuthResult,

        /// <summary>
        /// Note how will be removed by <see cref="Result{TProperty}.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.ok"/> and not <see cref="MethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [AgoRapide(
            Description = "General log (will not show up in result if considered not needed).",
            Type = typeof(string))]
        Log,

        /// <summary>
        /// Typical used by <see cref="Request{TProperty}"/> / <see cref="Result{TProperty}"/>. 
        /// </summary>
        [AgoRapide(
            Description = "General message.",
            Type = typeof(string))]
        Message,

        [AgoRapide(Type = typeof(PropertyOperation))]
        PropertyOperation,

        /// <summary>
        /// See <see cref="Result{Tproperty}"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of new properties created.",
            Type = typeof(long))]
        PCreatedCount,

        /// <summary>
        /// See <see cref="Result{Tproperty}"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of properties affected (as result of some database operation like INSERT, DELETE or UPDATE).",
            Type = typeof(long))]
        PAffectedCount,

        /// <summary>
        /// See <see cref="Result{Tproperty}"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of changed properties.",
            Type = typeof(long))]
        PChangedCount,

        /// <summary>
        /// See <see cref="Result{Tproperty}"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of unchanged properties.",
            Type = typeof(long))]
        PUnchangedCount,

        /// <summary>
        /// See <see cref="Result{Tproperty}"/>
        /// </summary>
        [AgoRapide(
            Description = "Count of all properties considered.",
            Type = typeof(long))]
        PTotalCount,

        [AgoRapide(Type = typeof(APIMethodOrigin))]
        APIMethodOrigin,

        [AgoRapide(Type = typeof(ResultCode), Parents = new Type[] { typeof(Result<CoreProperty>) })]
        ResultCode,

        /// <summary>
        /// The <see cref="AgoRapideAttribute.Description"/>-attribute of <see cref="ResultCode"/>
        /// Set by <see cref="Result{TProperty}.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [AgoRapide(Type = typeof(string), Parents = new Type[] { typeof(Result<CoreProperty>) })]
        ResultCodeDescription,

        [AgoRapide(
            Description =
                "URL suggested to client. " +
                "Will often accompany a -" + nameof(AgoRapide.ResultCode.missing_parameter_error) + "- or -" + nameof(AgoRapide.ResultCode.invalid_parameter_error) + "-. " +
                "Used in " + nameof(APIMethod<CoreProperty>) + " for giving samples. " +
                "Also useful for suggesting follow-up API-calls. ",
            Type = typeof(Uri), Parents = new Type[] { typeof(GeneralQueryResult<CoreProperty>) }, AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = -1)]
        SuggestedUrl,

        /// <summary>
        /// Set by <see cref="Result{TProperty}.AdjustAccordingToResultCodeAndMethod"/> when not <see cref="ResultCode.ok"/>
        /// </summary>
        [AgoRapide(
            Description = "Suggestions of relevant documentation for the API-method accessed.",
            Type = typeof(Uri))]
        APIDocumentationUrl,

        /// <summary>
        /// Set by <see cref="Result{TProperty}.AdjustAccordingToResultCodeAndMethod"/> when <see cref="ResultCode.exception_error"/>
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
        /// on the <see cref="BaseController{TProperty}"/>-method.
        /// </summary>
        [AgoRapide(
            Description = "Value TRUE signifies that API client needs to supply credentials in order to query API method.",
            Parents = new Type[] { typeof(APIMethod<CoreProperty>) }, Type = typeof(bool))]
        RequiresAuthorization,

        /// <summary>
        /// See <see cref="MethodAttribute.Environment"/>. 
        /// </summary>
        [AgoRapide(Type = typeof(Environment))]
        Environment,

        /// <summary>
        /// See <see cref="MethodAttribute.Description"/>. 
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod<CoreProperty>), typeof(GeneralQueryResult<CoreProperty>) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous, PriorityOrder = -1)]
        Description,

        /// <summary>
        /// See <see cref="MethodAttribute.LongDescription"/>
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod<CoreProperty>) }, Type = typeof(string), AccessLevelRead = AccessLevel.Anonymous)]
        LongDescription,

        /// <summary>
        /// Does not originate from <see cref="MethodAttribute.RouteTemplate"/> but from
        /// <see cref="APIMethod{TProperty}.RouteTemplates"/>[0]
        /// TODO: Document better!
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod<CoreProperty>) }, Type = typeof(string))]
        RouteTemplate,

        /// <summary>
        /// Controller + method which implements method.
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod<CoreProperty>) }, Type = typeof(string))]
        Implementator,

        /// <summary>
        /// See <see cref="MethodAttribute.ShowDetailedResult"/>
        /// </summary>
        [AgoRapide(Parents = new Type[] { typeof(APIMethod<CoreProperty>) }, Type = typeof(bool))]
        ShowDetailedResult,

        [AgoRapide(
            Description = "Describes an entity that functions as the anonymous user. One such entity should always exist in the database.",
            IsUniqueInDatabase = true,
            Type = typeof(bool))]
        IsAnonymous,
    }
}
