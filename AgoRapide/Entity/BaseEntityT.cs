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
    /// TODO: REMOVE. PUT FUNCTIONALITY INTO BaseEntity class (because this class is now no longer generic)
    /// 
    /// Also used internally by AgoRapide like <see cref="Parameters"/>, <see cref="Result"/>, 
    /// <see cref="ApplicationPart"/>, <see cref="APIMethod"/> and so on, in order to reuse the
    /// mechanisms developed for storing, querying and presenting data.-
    /// 
    /// This class is deliberately not made abstract since that faciliates "where T: new()" constraint in method signatures like
    /// <see cref="IDatabase.GetEntityById{T}(long)"/> 
    /// </summary>
    [AgoRapide(
        Description = "Represents a basic data object in your model like Person, Order, Product",
        DefinedForClass = "BaseEntityT" /// Necessary for <see cref="AgoRapideAttribute.IsInherited"/> to be set correctly. TODO: Turn into Type (will require more work for deducing <see cref="AgoRapideAttribute.IsInherited"/>). 
    )]
    public class BaseEntityT : BaseEntity {

        /// <summary>
        /// The entity which represents this entity. 
        /// See <see cref="IDatabase.SwitchIfHasEntityToRepresent"/>. 
        /// </summary>
        public BaseEntityT RepresentedByEntity { get; set; }

        public override string Name => PV(CoreP.Name.A(), Id.ToString());

        /// <summary>
        /// <see cref="RootProperty"/>.Id is same as (<see cref="BaseEntity.Id"/>. 
        /// 
        /// <see cref="RootProperty"/>.<see cref="Property.KeyT"/> will usually correspond to <see cref="CoreP.Type"/>
        /// 
        /// Note: Not relevant for <see cref="Property"/>
        /// </summary>
        public Property RootProperty { get; set; }

        /// <summary>
        /// Note that also <see cref="Property"/> inherits <see cref="BaseEntityT"/> 
        /// and may therefore have <see cref="Properties"/> (although not set as default). 
        /// (you may check for <see cref="Properties"/> == null and call <see cref="IDatabase.GetChildProperties"/> accordingly)
        /// 
        /// Note how <see cref="AgoRapideAttribute.IsMany"/>-properties (#x-properties) are stored in-memory with a <see cref="AgoRapideAttribute.IsMany"/>-parent and
        /// the different properties as properties under that again with dictionary index equal to <see cref="int.MaxValue"/> minus index
        /// </summary>
        public Dictionary<CoreP, Property> Properties { get; set; }

        public BaseEntityT() {
        }

        /// <summary>
        /// Returns existing properties available to <paramref name="currentUser"/> according to <paramref name="accessType"/>. 
        /// 
        /// Missing some properties in your HTML / JSON <see cref="Result"/>? See important comment for <see cref="AgoRapideAttribute.Parents"/> about access rights. 
        /// </summary>
        /// <param name="currentUser">
        /// May be null in which case <see cref="AccessLevel.Anonymous"/> 
        /// will be assumed by <see cref="Extensions.GetChildPropertiesForUser"/>
        /// </param>
        /// <param name="accessType"></param>
        /// <returns></returns>
        public Dictionary<CoreP, Property> GetExistingProperties(BaseEntityT currentUser, AccessType accessType) {
            var possible = GetType().GetChildPropertiesForUser(currentUser, this, accessType);
            var allForType = GetType().GetChildProperties();
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
        /// <param name="p"></param>
        /// <returns></returns>
        public T PV<T>(PropertyKey p) => TryGetPV(p, out T retval) ? retval : throw new InvalidPropertyException<T>(p.Key.CoreP, PExplained(p));

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, returns <paramref name="defaultValue"/> if that fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T PV<T>(PropertyKey p, T defaultValue) => TryGetPV(p, out T retval) ? retval : defaultValue;

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
        /// <param name="p"></param>
        /// <param name="pAsT"></param>
        /// <returns></returns>
        public bool TryGetPV<T>(PropertyKey p, out T pAsT) {
            // TODO: Decide whether to do this:
            // TODO: if (Properties == null) throw new NullReferenceException(nameof(Properties) + ". Details: " + ToString());
            // TODO: or this:
            if (Properties == null) { pAsT = default(T); return false; }

            if (p.Key.A.IsMany) throw new NotImplementedException(nameof(p.Key.A.IsMany));

            if (!Properties.TryGetValue(p.Key.CoreP, out var property)) { pAsT = default(T); return false; }
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
        /// Returns either "[NOT_FOUND]" or the result of <see cref="Property.ToString"/>
        /// 
        /// TODO: Solve for <see cref="AgoRapideAttribute.IsMany"/> properties
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public string PExplained(PropertyKey p) => Properties.TryGetValue(p.Key.CoreP, out var retval) ? retval.ToString() : "[NOT_FOUND]";

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

        public void AddProperty<T>(PropertyKey a, T value) {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (a.Key.A.IsMany) throw new NotImplementedException(nameof(a.Key.A.IsMany));
            var property = Property.Create(a, value);
            property.ParentId = Id;
            property.Parent = this;
            property.Initialize();
            // TODO: Decide if this is as wanted. Maybe structure better how Properties is initialized
            if (Properties == null) Properties = new Dictionary<CoreP, Property>();
            Properties.AddValue2(a.Key.CoreP, property);
        }

        /// <summary>
        /// Note existence of both <see cref="Property.InvalidPropertyException"/> and <see cref="BaseEntityT.InvalidPropertyException{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InvalidPropertyException<T> : ApplicationException {
            public InvalidPropertyException(CoreP p, string value) : base("The value found for " + typeof(CoreP) + "." + p + " (" + value + ") is not valid for " + typeof(T)) { }
        }

        public virtual string ToHTMLTableHeading(Request request) => "<tr><th>" + nameof(Name) + "</th><th>" + nameof(Created) + "</th></tr>";

        public virtual string ToHTMLTableRow(Request request) => "<tr><td>" +
            (Id <= 0 ? Name.HTMLEncode() : request.CreateAPILink(this)) + "</td><td>" +
            (RootProperty?.Created.ToString(DateTimeFormat.DateHourMin) ?? "&nbsp;") + "</td></tr>\r\n";

        /// <summary>
        /// For example of override see <see cref="BaseEntityTWithLogAndCount.ToHTMLDetailed"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToHTMLDetailed(Request request) {
            var retval = new StringBuilder();
            var result = this as Result; // NOTE: Pattern matching not possible here (erroneous suggestions by compiler included in v26228.9 of Visual Studio)
            if (result != null && result.ResultCode == ResultCode.ok) {
                // Do not show type or name because it will only be confusing
            } else {
                var description = GetType().GetAgoRapideAttributeForClass().Description;
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

        public virtual string CreateHTMLForExistingProperties(Request request) {
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

        public virtual string CreateHTMLForAddingProperties(Request request) {
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
                    var a = GetType().GetAgoRapideAttributeForClass();
                    var childProperties = GetType().GetChildProperties();

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
                        "In order to have any " + nameof(addableProperties) + " you must in general (for all the relevant enum values of -" + typeof(CoreP) + "-) " +
                        "add typeof(" + GetType().ToStringShort() + ") to -" + nameof(AgoRapideAttribute.Parents) + "- and also set " + nameof(AgoRapideAttribute.AccessLevelWrite) + ". " +
                        "<br><br>\r\n" +
                        "(currently -" + typeof(CoreP) + "- has -" + nameof(AgoRapideAttribute.Parents) + "- set to typeof(" + GetType().ToStringShort() + ") for " +
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
        /// For example of override see <see cref="BaseEntityTWithLogAndCount.ToJSONEntity"/> or <see cref="Property.ToJSONEntity"/>
        /// </summary>
        /// <returns></returns>
        public virtual JSONEntity0 ToJSONEntity(Request request) {
            var retval = new JSONEntity1 { Id = Id };
            if (Properties != null) {
                retval.Properties = new Dictionary<string, JSONProperty0>();
                /// Missing some properties in your HTML / JSON <see cref="Result"/>? See important comment for <see cref="AgoRapideAttribute.Parents"/> about access rights. 
                /// (note how you may get different results for <see cref="Result.MultipleEntitiesResult"/> for HTML and JSON because HTML will use
                /// <see cref="BaseEntityT.ToHTMLTableRow"/> which does not check access at all, while JSON data here checks for each individual property. 
                GetExistingProperties(request.CurrentUser, AccessType.Read).ForEach(i => {
                    if (i.Value.Key.Key.A.IsMany) throw new NotImplementedException(nameof(i.Value.Key.Key.A.IsMany));
                    retval.Properties.Add(i.Value.Key.Key.PToString, i.Value.ToJSONProperty());
                });
                // Note that we do not bother with Type when Properties is not set
                if (!retval.Properties.ContainsKey(nameof(CoreP.Type))) retval.Properties.Add(nameof(CoreP.Type), new JSONProperty0 { Value = GetType().ToStringShort() });
            }
            // TODO: ADD THIS:
            // AddUserChangeablePropertiesToSimpleEntity(retval);
            return retval;
        }
    }

    /// <summary>
    /// See <see cref="BaseEntityT.ToJSONEntity"/>
    /// 
    /// Simpler version of a <see cref="BaseEntityT"/>-class, more suited for transfer to client as JSON-data.
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