using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide;
using AgoRapide.API;

namespace AgoRapide {

    [Enum(EnumTypeY = EnumType.EnumValue)]
    public enum Colour {
        None,

        [EnumValue(Description = "Bjørn's favourite colour")]
        Red,

        Green,

        Blue
    }

    /// <summary>
    /// Also used internally by AgoRapide like <see cref="Parameters"/>, <see cref="Result"/>, 
    /// <see cref="ApplicationPart"/>, <see cref="APIMethod"/> and so on, in order to reuse the
    /// mechanisms developed for storing, querying and presenting data.-
    /// 
    /// This class is deliberately not made abstract in order to faciliate use of "where T: new()" constraint in method signatures like
    /// <see cref="IDatabase.GetEntityById{T}(long)"/> 
    /// 
    /// Note how <see cref="BaseEntity"/> inherits <see cref="BaseCore"/> meaning you can listen to <see cref="BaseCore.LogEvent"/> and
    /// <see cref="BaseCore.HandledExceptionEvent"/> but these are not used internally in AgoRapide as of Januar 2017 
    /// (it is felt unnecessary for entity classes to do logging). 
    /// Note however <see cref="BaseEntityWithLogAndCount.LogInternal"/> 
    /// </summary>
    [Class(
        Description = "Represents a basic data object in your model like Person, Order, Product",

        /// Do not do this. Make exception for <see cref="CoreP.AccessLevelUse"/> for <see cref="CoreAPIMethod.EntityIndex"/> instead
        // AccessLevelRead = AccessLevel.Anonymous, /// Necessary for <see cref="CoreMethod.EntityIndex"/> to accept all kind of queries. 

        DefinedForClass = nameof(BaseEntity) /// Necessary for <see cref="PropertyKeyAttribute.IsInherited"/> to be set correctly. TODO: Turn into Type (will require more work for deducing <see cref="PropertyKeyAttribute.IsInherited"/>). 
    )]
    public class BaseEntity : BaseCore {

        /// <summary>
        /// Corresponds to field id in database
        /// </summary>
        public long Id { get; set; }

        public void AssertIdIsSet() {
            if (Id <= 0) throw new IdNotSetException(ToString());
        }
        public class IdNotSetException : ApplicationException {
            public IdNotSetException(string message) : base(nameof(BaseEntity.Id) + " was not set. Possible cause: An object was assumed to originate from the database but did not.\r\nDetails:\r\n" + message) { }
        }


        /// <summary>
        /// Environment as used to characterize this entity
        /// 
        /// One idea for implementation:
        /// If we find a property called "Environment" when reading from database and that
        /// parses as a valid Environment-enum, then Environment here will be set accordingly
        /// 
        /// See Environment for more information
        /// </summary>
        public Environment Environment => throw new NotImplementedException();

        public DateTime Created { get; set; }

        /// <summary>
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// <see cref="Name"/> is used in contexts when a more user friendly value is needed. 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => GetType().ToString() + ": " + Id + ", created: " + Created.ToString(DateTimeFormat.DateHourMin);

        /// <summary>
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// <see cref="Name"/> is used in contexts when a more user friendly value is needed. 
        /// </summary>
        /// <returns></returns>
        public virtual string Name => PV(CoreP.Name.A(), Id.ToString());

        /// <summary>
        /// Override this method in all your classes which you want to give <see cref="AccessLevel"/> as a right. 
        /// 
        /// Typically that would be a class that you call Person, User, Customer or similar which would
        /// call <see cref="BaseEntity.PV{AccessLevel}(TProperty, AccessLevel)"/> with 
        /// <see cref="AgoRapide.AccessLevel.User"/> or <see cref="AgoRapide.AccessLevel.None"/> as default value.
        /// 
        /// A typical code example for this would be 
        ///   public override AccessLevel AccessLevelGiven => PV(P.AccessLevelGiven, defaultValue: AccessLevel.User)
        /// <see cref="Request.CurrentUser"/> (<see cref="BaseEntity.AccessLevelGiven"/>) must by equal to <see cref="APIMethodAttribute.AccessLevel"/> or HIGHER in order for access to be granted to <see cref="APIMethod"/>
        /// </summary>
        public virtual AccessLevel AccessLevelGiven => AccessLevel.None;
        /// <summary>
        /// The entity which represents this entity. 
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>. 
        /// </summary>
        public BaseEntity RepresentedByEntity { get; set; }

        /// <summary>
        /// <see cref="RootProperty"/>.Id is same as (<see cref="BaseEntity.Id"/>. 
        /// 
        /// <see cref="RootProperty"/>.<see cref="Property.KeyT"/> will usually correspond to <see cref="CoreP.RootProperty"/>
        /// 
        /// Note: Not relevant for <see cref="Property"/>
        /// </summary>
        public Property RootProperty { get; set; }

        /// <summary>
        /// Note that also <see cref="Property"/> inherits <see cref="BaseEntity"/> 
        /// and may therefore have <see cref="Properties"/> (although not set as default). 
        /// (you may check for <see cref="Properties"/> == null and call <see cref="IDatabase.GetChildProperties"/> accordingly)
        /// 
        /// Note how <see cref="PropertyKeyAttribute.IsMany"/>-properties (#x-properties) are stored in-memory with a <see cref="PropertyKeyAttribute.IsMany"/>-parent and
        /// the different properties as properties under that again with dictionary index equal to <see cref="int.MaxValue"/> minus index
        /// </summary>
        public Dictionary<CoreP, Property> Properties { get; set; }

        public BaseEntity() {
        }

        /// <summary>
        /// Returns existing properties available to <paramref name="currentUser"/> according to <paramref name="accessType"/>. 
        /// 
        /// Missing some properties in your HTML / JSON <see cref="Result"/>? See important comment for <see cref="PropertyKeyAttribute.Parents"/> about access rights. 
        /// </summary>
        /// <param name="currentUser">
        /// May be null in which case <see cref="AccessLevel.Anonymous"/> 
        /// will be assumed by <see cref="Extensions.GetChildPropertiesForUser"/>
        /// </param>
        /// <param name="accessType"></param>
        /// <returns></returns>
        public Dictionary<CoreP, Property> GetExistingProperties(BaseEntity currentUser, AccessType accessType) {
            var possible = GetType().GetChildPropertiesForUser(currentUser, this, accessType);
            var allForType = GetType().GetChildProperties();
            return Properties.Where(p =>
                possible.ContainsKey(p.Key) || /// This is the "ordinary" check, ensuring that <param name="currentUser"/> has access.
                !allForType.ContainsKey(p.Key) /// This check makes it non-mandatory to specify <see cref="PropertyKeyAttribute.Parents"/> for all properties for all your entities. Without this check the system would be too strict and cumbersome to get started with (since no properties would be shown until specification of <see cref="PropertyKeyAttribute.Parents"/>. 
            ).ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>
        /// Throws detailed exception message if property not found in collection.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Property GetProperty(CoreP key) => Properties.GetValue2(key, () => "Entity " + ToString());

        /// <summary>
        /// Convenience method making it possible to call 
        /// entity.PVM{Money}()
        /// instead of
        /// entity.PV{Money}(P.money). 
        /// 
        /// Calls <see cref="PV{T}(TProperty)"/> vith help of <see cref="Util.MapTToTProperty{T, TProperty}"/> (which throws Exception if no mapping exists).
        /// 
        /// Note that this is intentionally called PVM instead of PV because there is too great risk of confusion between these two in practical use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T PVM<T>() => PV<T>(Util.MapTToCoreP<T>()); // What we really would want is "where T : Enum"

        /// <summary>
        /// Convenience method making it possible to call 
        /// entity.PVM(Money.Zero)
        /// instead of
        /// entity.PV(P.money, Money.Zero). 
        /// 
        /// Calls <see cref="PV{T}(TProperty, T)"/> vith help of <see cref="Util.MapTToTProperty{T, TProperty}"/> (which throws Exception if no mapping exists).
        /// 
        /// Note that this is intentionally called PVM instead of PV because there is too great risk of confusion between these two in practical use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T PVM<T>(T defaultValue) => PV(Util.MapTToCoreP<T>(), defaultValue); // What we really would want is "where T : Enum"

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, throws exception if fails
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T PV<T>(PropertyKeyNonStrict key) => TryGetPV(key, out T retval) ? retval : throw new InvalidPropertyException<T>(key.Key.CoreP, PExplained(key));

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, returns <paramref name="defaultValue"/> if that fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T PV<T>(PropertyKeyNonStrict key, T defaultValue) => TryGetPV(key, out T retval) ? retval : defaultValue;

        /// <summary>
        /// Convenience method making it possible to call 
        /// entity.TryGetPVM(out Money money)
        /// instead of
        /// entity.TryGetPV(P.money, out Money money). 
        /// 
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/> vith help of <see cref="Util.MapTToTProperty{T, TProperty}"/> (which throws Exception if no mapping exists).
        /// 
        /// Note that this is intentionally called TryGetPVM instead of TryGetPV because there is too great risk of confusion between these two in practical use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetPVM<T>(out T t) => TryGetPV(Util.MapTToCoreP<T>(), out t); // What we really would want is "where T : Enum"

        /// <summary>
        /// Returns FALSE if p does not exist at all
        /// Else returns result of <see cref="Property.TryGetV{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pAsT"></param>
        /// <returns></returns>
        public bool TryGetPV<T>(PropertyKeyNonStrict key, out T pAsT) {
            // TODO: Decide whether to do this:
            // TODO: if (Properties == null) throw new NullReferenceException(nameof(Properties) + ". Details: " + ToString());
            // TODO: or this:
            if (Properties == null) { pAsT = default(T); return false; }
            if (!Properties.TryGetValue(key.Key.CoreP, out var property)) { pAsT = default(T); return false; }
            return property.TryGetV(out pAsT);
        }

        /// <summary>
        /// Safe to call method that returns a human readable explanation of what is found for the given property. 
        /// Useful for logging and exception messages
        /// Returns either "[NOT_FOUND]" or the result of <see cref="Property.ToString"/>
        /// 
        /// TODO: Solve for <see cref="PropertyKeyAttribute.IsMany"/> properties
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string PExplained(PropertyKeyNonStrict key) => Properties.TryGetValue(key.Key.CoreP, out var retval) ? retval.ToString() : "[NOT_FOUND]";

        /// <summary>
        /// Convenience method making it possible to call 
        /// entity.AddPropertyM(new Money("EUR 42"));
        /// instead of
        /// entity.AddProperty(P.money, new Money("EUR 42")). 
        /// 
        /// Calls <see cref="AddProperty{T}(TProperty, out T)"/> vith help of <see cref="Util.MapTToTProperty{T, TProperty}"/> (which throws Exception if no mapping exists).
        /// 
        /// Note that this is intentionally called AddPropertyM instead of AddProperty because there is too great risk of confusion between these two in practical use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddPropertyM<T>(T value) => AddProperty(Util.MapTToCoreP<T>(), value);

        /// <summary>
        /// Adds the property to this entity (in-memory operation only, does not create anything in database)
        /// 
        /// Note how accepts either single values or complete List for <see cref="PropertyKeyAttribute.IsMany"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddProperty<T>(PropertyKeyNonStrict key, T value) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (Properties == null) Properties = new Dictionary<CoreP, Property>(); // TODO: Maybe structure better how Properties is initialized
            if (key.Key.A.IsMany) {
                var temp = this as Property;
                var isManyParent = temp != null && temp.IsIsManyParent ? temp : null;
                if (isManyParent != null) { // We are an IsMany parent, add at next available id.
                    if (key is PropertyKeyWithIndex) throw new PropertyKeyWithIndex.InvalidPropertyKeyException(nameof(key) + " as " + nameof(PropertyKeyWithIndex) + " not allowed (" + nameof(PropertyKeyWithIndex.Index) + " not allowed)");
                    isManyParent.Properties.Add(isManyParent.GetNextIsManyId().IndexAsCoreP, new PropertyT<T>(key.PropertyKeyWithIndex, value)); /// Note how <see cref="PropertyT{T}.PropertyT"/> will fail if value is a List now (not corresponding to <see cref="PropertyKeyAttribute.IsMany"/>)
                } else {
                    var t = typeof(T);
                    if (t.IsGenericType) {
                        Properties.AddValue2(key.Key.CoreP, Util.ConvertListToIsManyParent(this, key, value, () => ToString()));
                    } else {
                        isManyParent = Properties.GetOrAddIsManyParent(key);
                        var id = isManyParent.GetNextIsManyId();
                        // We can not do this:
                        // isManyParent.Properties.Add(id.IndexAsCoreP, new PropertyT<T>(id, value));
                        // but must do this:
                        /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
                        isManyParent.AddPropertyForIsManyParent(id.IndexAsCoreP, new PropertyT<T>(id, value)); // Important in order for cached _value to be reset
                    }
                }
            } else {
                Properties.AddValue2(key.Key.CoreP, new PropertyT<T>(key.PropertyKeyWithIndex, value) {
                    ParentId = Id,
                    Parent = this
                });
            }
        }

        /// <summary>
        /// Note existence of both <see cref="Property.InvalidPropertyException"/> and <see cref="BaseEntity.InvalidPropertyException{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InvalidPropertyException<T> : ApplicationException {
            public InvalidPropertyException(CoreP p, string value) : base("The value found for " + typeof(CoreP) + "." + p + " (" + value + ") is not valid for " + typeof(T)) { }
        }

        public virtual string ToHTMLTableRowHeading(Request request) => "<tr><th>" + nameof(Name) + "</th><th>" + nameof(Created) + "</th></tr>";

        public virtual string ToHTMLTableRow(Request request) => "<tr><td>" +
            (Id <= 0 ? Name.HTMLEncode() : request.CreateAPILink(this)) + "</td><td>" +
            (RootProperty?.Created.ToString(DateTimeFormat.DateHourMin) ?? "&nbsp;") + "</td></tr>\r\n";

        /// <summary>
        /// For example of override see <see cref="BaseEntityWithLogAndCount.ToHTMLDetailed"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToHTMLDetailed(Request request) {
            var retval = new StringBuilder();
            if (new Func<bool>(() => { // Convoluted code due do erroneous suggestion by compiler to use Pattern matching (version as of March 2017)
                switch (this) {
                    case Result result: return result.ResultCode == ResultCode.ok;
                    default: return false;
                }
            })()) {
                // Do not show type or name because it will only be confusing
            } else {
                var description = GetType().GetClassAttribute().Description;
                retval.AppendLine("<h1>Type: " +
                    (string.IsNullOrEmpty(description) ? "" : "<span title=\"" + description.HTMLEncode() + "\">") +
                    GetType().ToStringVeryShort().HTMLEncode() +
                    (string.IsNullOrEmpty(description) ? "" : " (+)</span>") +
                    "<br>Name: " + Name.HTMLEncode() + "</h1>");
            }
            retval.AppendLine("<!--DELIMITER-->"); // Useful if sub-class wants to insert something in between here
            retval.AppendLine(CreateHTMLForExistingProperties(request));
            retval.AppendLine(CreateHTMLForAddingProperties(request));
            return retval.ToString();
        }

        /// <summary>
        /// Creates an HTML representation of the existing properties for this entity, including input fields and save buttons (through <see cref="Property.ToHTMLTableRow"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string CreateHTMLForExistingProperties(Request request) {
            var retval = new StringBuilder();
            if (Properties != null) {

                /// Note that is is tempting to do something like this, ensuring that you do not
                /// have to specify <see cref="PropertyKeyAttribute.Parents"/> for every property for each and every type of entity:
                /// ---------
                //var existing = GetType().GetAgoRapideAttribute().AccessLevelRead <= AccessLevel.Anonymous ?
                //     Properties : 
                //     GetExistingProperties(request.CurrentUser, AccessType.Read);
                /// ---------
                // But it will maybe confuse more than it helps. Therefore we have settled for this:
                var existing = GetExistingProperties(request.CurrentUser, AccessType.Read);

                if (existing.Count > 0) {
                    var changeableProperties = GetType().GetChildPropertiesForAccessLevel(AccessType.Write, request.CurrentUser?.AccessLevelGiven ?? AccessLevel.Anonymous);

                    retval.AppendLine("<h2>Properties</h2>\r\n");
                    // This would be the normal approach but we can use the const-value instead:
                    // retval.AppendLine("<table>" + Properties.Values.First().ToHTMLTableHeading(request));
                    retval.AppendLine("<table>" + Property.HTMLTableHeading);
                    // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                    retval.AppendLine(string.Join("", existing.Values.OrderBy(p => p.Key.Key.A.PriorityOrder).Select(p => {
                        p.IsChangeableByCurrentUser = changeableProperties.ContainsKey(p.Key.Key.CoreP); /// Hack implemented because of difficulty of adding parameter to <see cref="Property.ToHTMLTableRow"/>
                        return p.ToHTMLTableRow(request);
                    })));
                    retval.AppendLine("</table>");
                }
            }
            return retval.ToString();
        }

        /// <summary>
        /// Creates an HTML representation for properties to add for this entity, including input fields and save buttons (through <see cref="Property.ToHTMLTableRow"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string CreateHTMLForAddingProperties(Request request) {
            var retval = new StringBuilder();
            var addableProperties = GetType().GetChildPropertiesForUser(request.CurrentUser, this, AccessType.Write).ToList();

            if (addableProperties.Count == 0) {
                // Give hint about situation if considered relevant (because AgoRapide mechanism may be somewhat confusing at first)

                if (Util.Configuration.A.Environment == Environment.Production) {
                    /// The hint given below is a <see cref="Environment.Development"/> / <see cref="Environment.Test"/> issue only. 
                } else if (request.CurrentUser == null) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if (request.CurrentUser.AccessLevelGiven <= AccessLevel.Anonymous) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if ("AgoRapide".Equals(GetType().Assembly.GetName().Name)) {
                    /// Developers of AgoRapide library itself are expected to understand this issue
                } else {

                    /// In all other cases, give hint about access
                    var a = GetType().GetClassAttribute();
                    var childProperties = GetType().GetChildProperties();

                    // TODO: Create general mechanism for adding links to HTML text like this (links are indicated with starting -'s and trailing -'s.)
                    // TODO: And of course cache result, expect for request.CurrentUser.Name which will change. 
                    retval.AppendLine("<p>HINT: " +
                        "There are no " + nameof(addableProperties) + " for this entity for " + request.CurrentUser.Name.HTMLEncode() + " (" + nameof(request.CurrentUser.AccessLevelGiven) + " = " + request.CurrentUser.AccessLevelGiven + ")." +
                        "<br><br>\r\n" +
                        (a.IsDefault || a.IsInherited ?
                            ("Most probably because there are no -" + nameof(ClassAttribute) + "- (with -" + nameof(ClassAttribute.AccessLevelWrite) + "-) defined for -" + GetType().ToString() + "- meaning -" + nameof(ClassAttribute.AccessLevelWrite) + "- defaults to -" + a.AccessLevelWrite + "-.") :
                            ("[" + nameof(ClassAttribute) + "(" + nameof(ClassAttribute.AccessLevelWrite) + " = " + a.AccessLevelWrite + "...)] for -" + GetType().ToStringShort() + "-.")
                        ) +
                        "<br><br>\r\n" +
                        "In order to have any " + nameof(addableProperties) + " you must in general (for all the relevant enum values of -" + typeof(CoreP) + "-) " +
                        "add typeof(" + GetType().ToStringShort() + ") to -" + nameof(PropertyKeyAttribute.Parents) + "- and also set " + nameof(PropertyKeyAttribute.AccessLevelWrite) + ". " +
                        "<br><br>\r\n" +
                        "(currently -" + typeof(CoreP) + "- has -" + nameof(PropertyKeyAttribute.Parents) + "- set to typeof(" + GetType().ToStringShort() + ") for " +
                        (childProperties.Count == 0 ? "no values at all" :
                            ("the following values:<br>\r\n" + string.Join("<br>\r\n", childProperties.Values.Select(v => v.Key.PToString + " (" + v.Key.A.AccessLevelWrite + ")"))) + "<br>\r\n") +
                        "). " +
                        "</p>");
                }
            } else {
                var notExisting = addableProperties.Where(p => p.Value.Key.A.IsMany || !Properties.ContainsKey(p.Key)).ToList();
                if (notExisting.Count == 0) {
                    /// Do not give any explanation now. All relevant properties are already shown by <see cref="CreateHTMLForExistingProperties"/>
                } else {
                    retval.AppendLine("<h2>Properties you may add</h2>");
                    retval.AppendLine("<table>" + Property.HTMLTableHeading);
                    retval.AppendLine(string.Join("", notExisting.Select(p => {
                        var property = Property.CreateTemplate(p.Value.PropertyKeyAsIsManyParentOrTemplate, this);
                        property.IsChangeableByCurrentUser = true; /// Hack implemented because of difficulty of adding parameter to <see cref="Property.ToHTMLTableRow"/>
                        return property.ToHTMLTableRow(request);
                    })));
                    retval.AppendLine("</table>");
                }
            }
            return retval.ToString();
        }

        /// <summary>
        /// For example of override see <see cref="BaseEntityWithLogAndCount.ToJSONEntity"/> or <see cref="Property.ToJSONEntity"/>
        /// </summary>
        /// <returns></returns>
        public virtual JSONEntity0 ToJSONEntity(Request request) {
            var retval = new JSONEntity1 { Id = Id };
            if (Properties != null) {
                retval.Properties = new Dictionary<string, JSONProperty0>();
                /// Missing some properties in your HTML / JSON <see cref="Result"/>? See important comment for <see cref="PropertyKeyAttribute.Parents"/> about access rights. 
                /// (note how you may get different results for <see cref="Result.MultipleEntitiesResult"/> for HTML and JSON because HTML will use
                /// <see cref="BaseEntity.ToHTMLTableRow"/> which does not check access at all, while JSON data here checks for each individual property. 
                GetExistingProperties(request.CurrentUser, AccessType.Read).ForEach(i => {
                    if (i.Value.Key.Key.A.IsMany) throw new NotImplementedException(nameof(i.Value.Key.Key.A.IsMany));
                    retval.Properties.Add(i.Value.Key.Key.PToString, i.Value.ToJSONProperty());
                });
                // Note that we do not bother with Type when Properties is not set
                if (!retval.Properties.ContainsKey(nameof(CoreP.RootProperty))) retval.Properties.Add(nameof(CoreP.RootProperty), new JSONProperty0 { Value = GetType().ToStringShort() });
            }
            // TODO: ADD THIS:
            // AddUserChangeablePropertiesToSimpleEntity(retval);
            return retval;
        }
    }

    /// <summary>
    /// See <see cref="BaseEntity.ToJSONEntity"/>
    /// 
    /// Simpler version of a <see cref="BaseEntity"/>-class, more suited for transfer to client as JSON-data.
    /// 
    /// You  may inherit or extend this class as needed as final conversion into JSON format is done in a generic manner by <see cref="System.Web.Helpers.Json.Encode"/>
    /// </summary>
    public abstract class JSONEntity0 {
    }

    public class JSONEntity1 : JSONEntity0 {
        public Dictionary<string, JSONProperty0> Properties { get; set; }
        public long Id { get; set; }
    }

}