using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;
using AgoRapide.Core;
using AgoRapide.Database;

namespace AgoRapide {
    /// <summary>
    /// Also used internally by AgoRapide like <see cref="Parameters{TProperty}"/>, <see cref="Result{TProperty}"/>, 
    /// <see cref="ApplicationPart{TProperty}"/>, <see cref="APIMethod{TProperty}"/> and so on, in order to reuse the
    /// mechanisms developed for storing, querying and presenting data.-
    /// 
    /// This class is deliberately not made abstract since that faciliates "where T: new()" constraint in method signatures like
    /// <see cref="IDatabase{TProperty}.GetEntityById{T}(long)"/> 
    /// </summary>
    /// <typeparam name="TProperty">The Enum used for indicating properties, usually called P</typeparam>
    [AgoRapide(
        Description = "Represents a basic data object in your model like Person, Order, Product",
        DefinedForClass = "BaseEntityT" /// Necessary for <see cref="AgoRapideAttribute.IsInherited"/> to be set correctly. TODO: Turn into Type (will require more work for deducing <see cref="AgoRapideAttribute.IsInherited"/>). 
    )]
    public class BaseEntityT<TProperty> : BaseEntity where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"

        /// <summary>
        /// The entity which represents this entity. 
        /// See <see cref="IDatabase{TProperty}.SwitchIfHasEntityToRepresent"/>. 
        /// </summary>
        public BaseEntityT<TProperty> RepresentedByEntity { get; set; }

        public override string Name => PV(M(CoreProperty.Name), Id.ToString());

        /// <summary>
        /// <see cref="RootProperty"/>.Id is same as (<see cref="BaseEntity.Id"/>. 
        /// 
        /// <see cref="RootProperty"/>.<see cref="Property{TProperty}.KeyT"/> will usually correspond to <see cref="CoreProperty.Type"/>
        /// 
        /// Note: Not relevant for <see cref="Property{TProperty}"/>
        /// </summary>
        public Property<TProperty> RootProperty { get; set; }

        /// <summary>
        /// Note that also <see cref="Property{TProperty}"/> inherits <see cref="BaseEntityT{TProperty}"/> 
        /// and may therefore have <see cref="Properties"/> (although not set as default). 
        /// (you may check for <see cref="Properties"/> == null and call <see cref="IDatabase{TProperty}.GetChildProperties"/> accordingly)
        /// 
        /// Note how <see cref="AgoRapideAttribute.IsMany"/>-properties (#x-properties) are stored in-memory with a <see cref="AgoRapideAttribute.IsMany"/>-parent and
        /// the different properties as properties under that again with dictionary index equal to <see cref="int.MaxValue"/> minus index
        /// </summary>
        public Dictionary<TProperty, Property<TProperty>> Properties { get; set; }

        public BaseEntityT() {
            if (!typeof(TProperty).IsEnum) throw new NotOfTypeEnumException<TProperty>();
        }

        /// <summary>
        /// Returns existing properties available to <paramref name="currentUser"/> according to <paramref name="accessType"/>. 
        /// 
        /// Missing some properties in your HTML / JSON <see cref="Result{TProperty}"/>? See important comment for <see cref="AgoRapideAttribute.Parents"/> about access rights. 
        /// </summary>
        /// <param name="currentUser">
        /// May be null in which case <see cref="AccessLevel.Anonymous"/> 
        /// will be assumed by <see cref="Extensions.GetChildPropertiesForUser"/>
        /// </param>
        /// <param name="accessType"></param>
        /// <returns></returns>
        public Dictionary<TProperty, Property<TProperty>> GetExistingProperties(BaseEntityT<TProperty> currentUser, AccessType accessType) {
            var possible = GetType().GetChildPropertiesForUser(currentUser, this, accessType);
            var allForType = GetType().GetChildProperties<TProperty>();
            return Properties.Where(p =>
                possible.ContainsKey(p.Key) || /// This is the "ordinary" check, ensuring that <param name="currentUser"/> has access.
                !allForType.ContainsKey(p.Key) /// This check makes it non-mandatory to specify <see cref="AgoRapideAttribute.Parents"/> for all properties for all your entities. Without this check the system would be too strict and cumbersome to get started with (since no properties would be shown until specification of <see cref="AgoRapideAttribute.Parents"/>. 
            ).ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>
        /// Throws detailed exception message if property not found in collection.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Property<TProperty> GetProperty(TProperty key) => Properties.GetValue2(key, () => "Entity " + ToString());

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
        public T PVM<T>() => PV<T>(Util.MapTToTProperty<T, TProperty>()); // What we really would want is "where T : Enum"

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
        public T PVM<T>(T defaultValue) => PV(Util.MapTToTProperty<T, TProperty>(), defaultValue); // What we really would want is "where T : Enum"

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, throws exception if fails
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p"></param>
        /// <returns></returns>
        public T PV<T>(TProperty p) => TryGetPV(p, out T retval) ? retval : throw new InvalidPropertyException<T>(p, PExplained(p));

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, returns <paramref name="defaultValue"/> if that fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T PV<T>(TProperty p, T defaultValue) => TryGetPV(p, out T retval) ? retval : defaultValue;

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
        public bool TryGetPVM<T>(out T t) => TryGetPV(Util.MapTToTProperty<T, TProperty>(), out t); // What we really would want is "where T : Enum"

        /// <summary>
        /// Returns FALSE if p does not exist at all
        /// Else returns result of <see cref="Property{TProperty}.TryGetV{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p"></param>
        /// <param name="pAsT"></param>
        /// <returns></returns>
        public bool TryGetPV<T>(TProperty p, out T pAsT) {
            // TODO: Decide whether to do this:
            // TODO: if (Properties == null) throw new NullReferenceException(nameof(Properties) + ". Details: " + ToString());
            // TODO: or this:
            if (Properties == null) { pAsT = default(T); return false; }

            if (!Properties.TryGetValue(p, out var property)) { pAsT = default(T); return false; }
            // Type checking here was considered Jan 2017 but left out. Instead we leave it to property to
            // convert as needed (double to int for instance or DateTime to string)
            // var type = typeof(T);
            //if (type.Equals(typeof(string))) {
            //    // It is conceivable to ask for string even when a more precise type is available. 
            //} else {
            //    if (property.A.A.Type != null && !type.IsAssignableFrom(property.A.A.Type)) throw new InvalidTypeException(property.A.A.Type, type, nameof(p) + ": " + p.ToString());
            //}
            return property.TryGetV(out pAsT);
        }

        /// <summary>
        /// Safe to call method that returns a human readable explanation of what is found for the given property. 
        /// Useful for logging and exception messages
        /// Returns either "[NOT_FOUND]" or the result of <see cref="Property{TProperty}.ToString"/>
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public string PExplained(TProperty p) => Properties.TryGetValue(p, out var retval) ? retval.ToString() : "[NOT_FOUND]";

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
        public void AddPropertyM<T>(T value) => AddProperty(Util.MapTToTProperty<T, TProperty>(), value);

        public void AddProperty<T>(TProperty key, T value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var property = Property<TProperty>.Create(key, value);
            property.ParentId = Id;
            property.Parent = this;
            property.Initialize();
            // TODO: Decide if this is as wanted. Maybe structure better how Properties is initialized
            if (Properties == null) Properties = new Dictionary<TProperty, Property<TProperty>>();
            Properties.AddValue2(key, property);
        }

        ///// <summary>
        ///// TODO: Add more as needed!
        ///// TODO: OR JUST USE Generics instead? 
        ///// TODO: We can instead of <see cref="Property{TProperty}.SetValueThroughValueSetter(Action{Property{TProperty}})"/>
        ///// TODO: have a generic Property.Create.
        ///// </summary>
        ///// <param name="pKey"></param>
        ///// <param name="value"></param>
        //public void AddProperty(TProperty pKey, long value) => AddProperty(pKey, p => p.LngValue = value);
        //public void AddProperty(TProperty pKey, string value) => AddProperty(pKey, p => p.StrValue = value);

        ////// TODO: Add more as needed!
        ////public void AddProperty(string key, long value) => AddProperty(key, p => p.LngValue = value);
        ////public void AddProperty(string key, string value) => AddProperty(key, p => p.StrValue = value);

        //public void AddProperty(TProperty key, Action<Property<TProperty>> valueSetter) => Properties.AddValue(key, new Property<TProperty>(key) {
        //    ParentId = Id,
        //    Parent = this
        //}.SetValueThroughValueSetter(valueSetter).Initialize()); // Note how Initialize will assert against BAttribute.TryValidate

        /// <summary>
        /// Note existence of both <see cref="Property{TProperty}.InvalidPropertyException"/> and <see cref="BaseEntityT{TProperty}.InvalidPropertyException{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InvalidPropertyException<T> : ApplicationException {
            public InvalidPropertyException(TProperty p, string value) : base("The value found for " + typeof(TProperty) + "." + p + " (" + value + ") is not valid for " + typeof(T)) { }
        }

        public virtual string ToHTMLTableHeading(Request<TProperty> request) => "<tr><th>" + nameof(Name) + "</th><th>" + nameof(Created) + "</th></tr>";

        public virtual string ToHTMLTableRow(Request<TProperty> request) => "<tr><td>" +
            (Id <= 0 ? Name.HTMLEncode() : request.CreateAPILink(this)) + "</td><td>" +
            (RootProperty?.Created.ToString(DateTimeFormat.DateHourMin) ?? "&nbsp;") + "</td></tr>\r\n";

        /// <summary>
        /// For example of override see <see cref="BaseEntityTWithLogAndCount{TProperty}.ToHTMLDetailed"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToHTMLDetailed(Request<TProperty> request) {
            var retval = new StringBuilder();
            var result = this as Result<TProperty>;
            if (result != null && result.ResultCode == ResultCode.ok) {
                // Do not show type or name because it will only be confusing
            } else {
                var description = GetType().GetAgoRapideAttribute().Description;
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

        public virtual string CreateHTMLForExistingProperties(Request<TProperty> request) {
            var retval = new StringBuilder();
            if (Properties != null) {

                /// Note that is is tempting to do something like this, ensuring that you do not
                /// have to specify <see cref="AgoRapideAttribute.Parents"/> for every property for each and every type of entity:
                /// ---------
                //var existing = GetType().GetAgoRapideAttribute().AccessLevelRead <= AccessLevel.Anonymous ?
                //     Properties : 
                //     GetExistingProperties(request.CurrentUser, AccessType.Read);
                /// ---------
                // But it will maybe confuse more than it helps. Therefore we have settled for this:
                var existing = GetExistingProperties(request.CurrentUser, AccessType.Read);

                if (existing.Count > 0) {
                    var changeableProperties = GetType().GetChildPropertiesForAccessLevel<TProperty>(AccessType.Write, request.CurrentUser?.AccessLevelGiven ?? AccessLevel.Anonymous);

                    retval.AppendLine("<h2>Properties</h2>\r\n");
                    // This would be the normal approach but we can use the const-value instead:
                    // retval.AppendLine("<table>" + Properties.Values.First().ToHTMLTableHeading(request));
                    retval.AppendLine("<table>" + Property<TProperty>.HTMLTableHeading);
                    // TODO: Note the (potentially performance degrading) sorting. It is not implemented for JSON on purpose.
                    retval.AppendLine(string.Join("", existing.Values.OrderBy(p => p.KeyA.A.PriorityOrder).Select(p => {
                        p.IsChangeableByCurrentUser = changeableProperties.ContainsKey(p.KeyT); /// Hack implemented because of difficulty of adding parameter to <see cref="Property{TProperty}.ToHTMLTableRow"/>
                        return p.ToHTMLTableRow(request);
                    })));
                    retval.AppendLine("</table>");
                }
            }
            return retval.ToString();
        }

        public virtual string CreateHTMLForAddingProperties(Request<TProperty> request) {
            var retval = new StringBuilder();
            var addableProperties = GetType().GetChildPropertiesForUser(request.CurrentUser, this, AccessType.Write).ToList();

            if (addableProperties.Count == 0) {
                // Give hint about situation if considered relevant (because AgoRapide mechanism may be somewhat confusing at first)

                if (Util.Configuration.Environment == Environment.Production) {
                    /// The hint given below is a <see cref="Environment.Development"/> / <see cref="Environment.Test"/> issue only. 
                } else if (request.CurrentUser == null) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if (request.CurrentUser.AccessLevelGiven <= AccessLevel.Anonymous) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if ("AgoRapide".Equals(GetType().Assembly.GetName().Name)) {
                    /// Developers of AgoRapide library itself are expected to understand this issue
                } else {

                    /// In all other cases, give hint about access
                    var a = GetType().GetAgoRapideAttribute();
                    var childProperties = GetType().GetChildProperties<TProperty>();

                    // TODO: Create general mechanism for adding links to HTML text like this (links are indicated with starting -'s and trailing -'s.)
                    // TODO: And of course cache result, expect for request.CurrentUser.Name which will change. 
                    retval.AppendLine("<p>HINT: " +
                        "There are no " + nameof(addableProperties) + " for this entity for " + request.CurrentUser.Name.HTMLEncode() + " (" + nameof(request.CurrentUser.AccessLevelGiven) + " = " + request.CurrentUser.AccessLevelGiven + ")." +
                        "<br><br>\r\n" +
                        (a.IsDefault || a.IsInherited ?
                            ("Most probably because there are no -" + nameof(AgoRapideAttribute) + "- (with -" + nameof(AgoRapideAttribute.AccessLevelWrite) + "-) defined for -" + GetType().ToString() + "- meaning -" + nameof(AgoRapideAttribute.AccessLevelWrite) + "- defaults to -" + a.AccessLevelWrite + "-.") :
                            ("[" + nameof(AgoRapideAttribute) + "(" + nameof(AgoRapideAttribute.AccessLevelWrite) + " = " + a.AccessLevelWrite + "...)] for -" + GetType().ToStringShort() + "-.")
                        ) +
                        "<br><br>\r\n" +
                        "In order to have any " + nameof(addableProperties) + " you must in general (for all the relevant enum values of -" + typeof(TProperty) + "-) " +
                        "add typeof(" + GetType().ToStringShort() + ") to -" + nameof(AgoRapideAttribute.Parents) + "- and also set " + nameof(AgoRapideAttribute.AccessLevelWrite) + ". " +
                        "<br><br>\r\n" +
                        "(currently -" + typeof(TProperty) + "- has -" + nameof(AgoRapideAttribute.Parents) + "- set to typeof(" + GetType().ToStringShort() + ") for " +
                        (childProperties.Count == 0 ? "no values at all" :
                            ("the following values:<br>\r\n" + string.Join("<br>\r\n", childProperties.Values.Select(v => v.PToString + " (" + v.A.AccessLevelWrite + ")"))) + "<br>\r\n") +
                        "). " +
                        "</p>");
                }
            } else {
                var notExisting = addableProperties.Where(p => !Properties.ContainsKey(p.Key)).ToList();
                if (notExisting.Count == 0) {
                    /// Do not give any explanation now. All relevant properties are already shown by <see cref="CreateHTMLForExistingProperties"/>
                } else {
                    retval.AppendLine("<h2>Properties you may add</h2>");
                    retval.AppendLine("<table>" + Property<TProperty>.HTMLTableHeading);
                    retval.AppendLine(string.Join("", notExisting.Select(p => {
                        return new Property<TProperty>(p.Key) {
                            IsTemplateOnly = true, /// Important in order for <see cref="Property{TProperty}.V{T}"/> and <see cref="Property{TProperty}.ValueA"/> or similar not to be called
                            Parent = this, /// TODO: Make a separate property class for PropertyTemplate instead of using <see cref="Property{TProperty}.IsTemplateOnly"/>
                            ParentId = Id, /// TODO: Make a separate property class for PropertyTemplate instead of using <see cref="Property{TProperty}.IsTemplateOnly"/>
                            IsChangeableByCurrentUser = true, /// Hack implemented because of difficulty of adding parameter to <see cref="Property{TProperty}.ToHTMLTableRow"/>
                        }.ToHTMLTableRow(request);
                    })));
                    retval.AppendLine("</table>");
                }
            }
            return retval.ToString();
        }

        protected static CorePropertyMapper<TProperty> _cpm = new CorePropertyMapper<TProperty>();
        protected static TProperty M(CoreProperty coreProperty) => _cpm.Map(coreProperty);

        /// <summary>
        /// For example of override see <see cref="BaseEntityTWithLogAndCount{TProperty}.ToJSONEntity"/> or <see cref="Property{TProperty}.ToJSONEntity"/>
        /// </summary>
        /// <returns></returns>
        public virtual JSONEntity0 ToJSONEntity(Request<TProperty> request) {
            var retval = new JSONEntity1 { Id = Id };
            if (Properties != null) {
                retval.Properties = new Dictionary<string, JSONProperty0>();
                GetExistingProperties(request.CurrentUser, AccessType.Read).ForEach(i => {
                    retval.Properties.Add(i.Value.KeyA.PToString, i.Value.ToJSONProperty());
                });
                // Note that we do not bother with Type when Properties is not set
                if (!retval.Properties.ContainsKey(nameof(CoreProperty.Type))) retval.Properties.Add(nameof(CoreProperty.Type), new JSONProperty0 { Value = GetType().ToStringShort() });
            }
            // TODO: ADD THIS:
            // AddUserChangeablePropertiesToSimpleEntity(retval);
            return retval;
        }
    }

    /// <summary>
    /// See <see cref="BaseEntityT{TProperty}.ToJSONEntity"/>
    /// 
    /// Simpler version of a <see cref="BaseEntityT{TProperty}"/>-class, more suited for transfer to client as JSON-data.
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