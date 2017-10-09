// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// Main inheriting classes:
    /// <see cref="ApplicationPart"/>
    /// <see cref="APIDataObject"/> 
    /// 
    /// Other examples of inheriting classes: <see cref="Parameters"/>, <see cref="Result"/>. 
    /// 
    /// This class is deliberately not made abstract in order to faciliate use of "where T: new()" constraint in method signatures like
    /// <see cref="BaseDatabase.GetEntityById{T}(long)"/>. Since there are no natural abstract methods in this class this should be quite ok 
    /// (we actually want to avoid abstract methods anyway in order to make inheritance of <see cref="BaseEntity"/> as simple as possible)
    /// 
    /// Note how <see cref="BaseEntity"/> inherits <see cref="BaseCore"/> meaning you can listen to <see cref="BaseCore.LogEvent"/> and
    /// <see cref="BaseCore.HandledExceptionEvent"/> but these are NOT used internally in AgoRapide as of Januar 2017 
    /// (it is felt unnecessary for entity classes to do logging). 
    /// Note however <see cref="BaseEntityWithLogAndCount.LogInternal"/>. 
    /// </summary>
    [Class(
        Description = "Basic entity supporting storage in database and collection of -" + nameof(Properties) + "-",
        DefinedForClass = nameof(BaseEntity) /// Necessary for <see cref="ClassAttribute.IsInherited"/> to be set correctly. TODO: Turn into Type (will require more work for deducing <see cref="ClassAttribute.IsInherited"/>). 
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

        private QueryId _idString;
        /// <summary>
        /// <see cref="IdString"/> is used against <see cref="CoreAPIMethod.EntityIndex"/>. 
        /// <see cref="IdFriendly"/> is used in textual contexts 
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// 
        /// Note how this is a always available "safe-to-use" property degrading to <see cref="BaseEntity.Id"/> as necessary.
        /// 
        /// Is often of type <see cref="QueryIdString"/>, therefore called <see cref="IdString"/>.
        /// </summary>
        [ClassMember(Description =
            "May be used as replacement of -" + nameof(BaseEntity.Id) + "-, for instance for use as parameter against -" + nameof(CoreAPIMethod.EntityIndex) + "- " +
            "(because will often give a more human friendly value)."
        )]
        public QueryId IdString => _idString ?? (_idString = PV<QueryId>(CoreP.QueryId.A(), (Id > 0 ? (QueryId)new QueryIdInteger(Id) : (QueryId)new QueryIdString("UNKNOWN"))));

        private string _idFriendly;
        /// <summary>
        /// <see cref="IdString"/> is used against <see cref="CoreAPIMethod.EntityIndex"/>. 
        /// <see cref="IdFriendly"/> is used in textual contexts 
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// 
        /// Note how this is a always available "safe-to-use" property degrading to <see cref="BaseEntity.Id"/> as necessary.
        /// 
        /// Made virtual in cases where it is not practical to add a <see cref="CoreP.IdFriendly"/> property.
        /// </summary>
        /// <returns></returns>
        public virtual string IdFriendly => _idFriendly ?? (_idFriendly = PV(CoreP.IdFriendly.A(), Id.ToString()));

        /// <summary>
        /// <see cref="IdString"/> is used against <see cref="CoreAPIMethod.EntityIndex"/>. 
        /// <see cref="IdFriendly"/> is used in textual contexts 
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => GetType().ToString() + ": " + Id + ", created: " + Created.ToString(DateTimeFormat.DateHourMin);

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
        /// See <see cref="BaseDatabase.SwitchIfHasEntityToRepresent"/>. 
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
        /// (you may check for <see cref="Properties"/> == null and call <see cref="BaseDatabase.GetChildProperties"/> accordingly)
        /// 
        /// Note how <see cref="PropertyKeyAttribute.IsMany"/>-properties (#x-properties) are stored in-memory with a <see cref="PropertyKeyAttribute.IsMany"/>-parent and
        /// the different properties as properties under that again with dictionary index equal to <see cref="int.MaxValue"/> minus index
        /// 
        // TODO: Initialize more deterministically for the different classes.
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
        public T PV<T>(PropertyKey key) => TryGetPV(key, out T retval) ? retval : throw new InvalidPropertyException<T>(key.Key.CoreP, PExplained(key), ToString());

        /// <summary>
        /// Calls <see cref="TryGetPV{T}(TProperty, out T)"/>, returns <paramref name="defaultValue"/> if that fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T PV<T>(PropertyKey key, T defaultValue) => TryGetPV(key, out T retval) ? retval : defaultValue;

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
        /// 
        /// NOTE: We could theoretically make an overload with only <see cref="CoreP"/> instead of <see cref="PropertyKey"/>
        /// NOTE: That would some lookups a little bit more efficient. BUT, based on the assumption that the great majority of lookups will
        /// NOTE: be based on something other than <see cref="CoreP"/> anyway there is little need for such an overload as of Sep 2017.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="pAsT"></param>
        /// <returns></returns>
        public bool TryGetPV<T>(PropertyKey key, out T pAsT) {
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
        /// Returns either detailed explanation (like "[NOT_FOUND]") or the result of <see cref="Property.ToString"/>
        /// 
        /// TODO: Solve for <see cref="PropertyKeyAttribute.IsMany"/> properties
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string PExplained(PropertyKey key) => Properties == null ? ("[" + nameof(Properties) + " == null],\r\n" + key.Key.ToString()) : (Properties.TryGetValue(key.Key.CoreP, out var retval) ? retval.ToString() : "[NOT_FOUND]");

        public void AddOrUpdatePropertyM<T>(T value) => AddOrUpdateProperty(Util.MapTToCoreP<T>(), value, null, null, null);
        public void AddOrUpdateProperty<T>(PropertyKey key, T value) => AddOrUpdateProperty(key, value, null, null, null);
        public void AddOrUpdateProperty<T>(PropertyKey key, T value, Func<string> detailer) => AddOrUpdateProperty(key, value, null, null, detailer);
        public void AddOrUpdateProperty<T>(PropertyKey key, T value, string strValue, Func<string> detailer) => AddOrUpdateProperty(key, value, strValue, null, detailer);
        /// <summary>
        /// Note how <see cref="PropertyKeyAttribute.IsMany"/> is not accepted.
        /// TODO: Accept IsMany, but only if <paramref name="value"/> is complete list, not only a single value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="strValue"></param>
        /// <param name="valueAttribute"></param>
        /// <param name="detailer"></param>
        [ClassMember(Description = "Deletes property if already exists, then calls -" + nameof(AddProperty) + "-.")]
        public void AddOrUpdateProperty<T>(PropertyKey key, T value, string strValue, BaseAttribute valueAttribute, Func<string> detailer) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            key.Key.A.AssertNotIsMany(detailer);
            if (Properties != null && Properties.ContainsKey(key.Key.CoreP)) Properties.Remove(key.Key.CoreP);
            AddProperty(key, value, strValue, valueAttribute, detailer);
        }


        /// <summary>
        /// Convenience method making it possible to call 
        /// entity.AddPropertyM(new Money("EUR 42"));
        /// instead of
        /// entity.AddProperty(P.money, new Money("EUR 42")). 
        /// 
        /// Calls <see cref="AddProperty{T}"/> vith help of <see cref="Util.MapTToTProperty{T, TProperty}"/> (which throws Exception if no mapping exists).
        /// 
        /// Note that this is intentionally called AddPropertyM instead of AddProperty because there is too great risk of confusion between these two in practical use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void AddPropertyM<T>(T value) => AddProperty(Util.MapTToCoreP<T>(), value, null, null, null);
        public void AddProperty<T>(PropertyKey key, T value) => AddProperty(key, value, null, null, null);
        public void AddProperty<T>(PropertyKey key, T value, Func<string> detailer) => AddProperty(key, value, null, null, detailer);

        /// <summary>
        /// Note how accepts either single values or complete List for <see cref="PropertyKeyAttribute.IsMany"/>
        /// </summary>
        /// <typeparam name="T">
        /// Must correspond to <paramref name="key"/>'s Type.
        /// </typeparam>
        /// <param name="key"></param>
        /// <param name="value">
        /// </param>
        /// <param name="strValue">
        /// May be null. 
        /// See <see cref="PropertyT{T}.PropertyT(PropertyKeyWithIndex, T, string, BaseAttribute)"/> for documentation
        /// </param>
        /// <param name="valueAttribute">
        /// May be null. 
        /// See <see cref="PropertyT{T}.PropertyT(PropertyKeyWithIndex, T, string, BaseAttribute)"/> for documentation
        /// </param>
        /// <param name="detailer">May be null</param>
        [ClassMember(Description = "Adds the property to this entity (in-memory operation only, does not create anything in database)")]
        public void AddProperty<T>(PropertyKey key, T value, string strValue, BaseAttribute valueAttribute, Func<string> detailer) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (Properties == null) Properties = new Dictionary<CoreP, Property>(); // TODO: Maybe structure better how Properties is initialized
            if (detailer == null) detailer = () => ToString();

            if (!key.Key.A.IsMany) {
                InvalidTypeException.AssertAssignable(typeof(T), key.Key.A.Type, () => key.Key.PToString); // Added 20 Sep 2017
                AddProperty(new PropertyT<T>(key.PropertyKeyWithIndex, value, strValue, valueAttribute), detailer);
                //Properties.AddValue2(key.Key.CoreP, new PropertyT<T>(key.PropertyKeyWithIndex, value, strValue, valueAttribute) {
                //    ParentId = Id,
                //    Parent = this
                //}, detailer);
                return;
            }

            // IsMany
            var temp = this as Property;
            var isManyParent = temp != null && temp.IsIsManyParent ? temp : null;
            if (isManyParent != null) { // We are an IsMany parent, add at next available id.
                if (key is PropertyKeyWithIndex) throw new PropertyKeyWithIndex.InvalidPropertyKeyException(nameof(key) + " as " + nameof(PropertyKeyWithIndex) + " not allowed (" + nameof(PropertyKeyWithIndex.Index) + " not allowed)");
                isManyParent.Properties.Add(isManyParent.GetNextIsManyId().IndexAsCoreP, new PropertyT<T>(key.PropertyKeyWithIndex, value)); /// Note how <see cref="PropertyT{T}.PropertyT"/> will fail if value is a List now (not corresponding to <see cref="PropertyKeyAttribute.IsMany"/>)
            } else {
                var t = typeof(T);
                if (t.IsGenericType) {
                    AddProperty(Util.ConvertListToIsManyParent(this, key, value, detailer), detailer);
                    // Properties.AddValue2(key.Key.CoreP, Util.ConvertListToIsManyParent(this, key, value, detailer));
                } else {
                    isManyParent = Properties.GetOrAddIsManyParent(key);
                    var id = isManyParent.GetNextIsManyId();
                    // We can not do this:
                    // isManyParent.Properties.Add(id.IndexAsCoreP, new PropertyT<T>(id, value));
                    // but must do this:
                    /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
                    isManyParent.AddPropertyForIsManyParent(id.IndexAsCoreP, new PropertyT<T>(id, value, strValue, valueAttribute)); // Important in order for cached _value to be reset
                }
            }
        }

        public void AddProperty(Property p, Func<string> detailer = null) {
            p.ParentId = Id;
            p.Parent = this;
            Properties.AddValue2(p.Key.Key.CoreP, p, detailer);
        }

        /// <summary>
        /// Note existence of both <see cref="Property.InvalidPropertyException"/> and <see cref="BaseEntity.InvalidPropertyException{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InvalidPropertyException<T> : ApplicationException {
            public InvalidPropertyException(CoreP p, string value, string details) : base(
                    "The value found for " + p.A().Key.PToString + "\r\n" +
                    "(" + value + ")\r\n" +
                    "is not valid for " + typeof(T) + ".\r\n" +
                    "Details: " + details) { }
        }

        private static ConcurrentDictionary<
            string, // Key is GetType + _ + PriorityOrder
            string> _tableRowHeadingCache = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// Note that may be overridden if you need finer control about how to present your entities (like <see cref="Property.ToHTMLTableRowHeading"/>). 
        /// 
        /// NOTE: Remember to always override correspondingly for <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/>
        /// </summary>
        /// <returns></returns>
        public virtual string ToHTMLTableRowHeading(Request request) => _tableRowHeadingCache.GetOrAdd(GetType() + "_" + request.PriorityOrderLimit, k => {
            var thisType = GetType().ToStringVeryShort();
            return "<tr><th>" + nameof(IdFriendly) + "</th>" +
                string.Join("", GetType().GetChildPropertiesByPriority(request.PriorityOrderLimit).Select(key => "<th>" + new Func<string>(() => {
                    var retval = key.Key.PToString;
                    if (retval.StartsWith(thisType)) { // Note shortening of name here (often names will start with the same as the entity type, we then assume that we can safely remove the type-part).
                        // TODO: Add mouseover for showing complete name here.
                        retval = retval.Substring(thisType.Length);
                        if (retval.StartsWith("_")) retval = retval.Substring(1); /// Typical for <see cref="Database.PropertyKeyForeignKeyAggregate"/>
                    }
                    return retval;
                })() + "</th>")) +
                // "<th>" + nameof(Created) + "</th>" +
                "</tr>";
        });

        /// <summary>
        /// Note that may be overridden if you need finer control about how to present your entities (like <see cref="Property.ToHTMLTableRow"/>).
        /// 
        /// NOTE: Remember to always override correspondingly for <see cref="BaseEntity.ToHTMLTableRowHeading"/> and <see cref="BaseEntity.ToHTMLTableRow"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToHTMLTableRow(Request request) => "<tr><td>" +
            (Id <= 0 ? IdFriendly.HTMLEncode() : request.API.CreateAPILink(this)) + "</td>" +
            string.Join("", GetType().GetChildPropertiesByPriority(request.PriorityOrderLimit).Select(key => "<td>" + (
                Properties.TryGetValue(key.Key.CoreP, out var p) ? p.V<Property.HTML>().ToString() : "&nbsp;"
            ) + "</td>")) +
            //"<td>" + 
            //Created.ToString(DateTimeFormat.DateHourMin) + "</td>" +
            "</tr>\r\n";

        /// <summary>
        /// Note that may be overridden if you need finer control about how to present your entities (like <see cref="Property.ToHTMLDetailed"/> and <see cref="Result.ToHTMLDetailed"/>). 
        /// 
        /// There are three levels of packaging HTML information.
        /// <see cref="HTMLView.GenerateResult"/>
        ///   <see cref="HTMLView.GetHTMLStart"/>
        ///   <see cref="Result.ToHTMLDetailed"/>
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///     <see cref="Result.ToHTMLDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToHTMLDetailed"/>). 
        ///     <see cref="BaseEntity.ToHTMLDetailed"/> (called from <see cref="Result.ToHTMLDetailed"/>)
        ///   <see cref="HTMLView.GetHTMLEnd"/>
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
                    (string.IsNullOrEmpty(description) ? "" : ("<span title=\"" + description.HTMLEncode() + "\">")) +
                    GetType().ToStringVeryShort().HTMLEncode() +
                    (string.IsNullOrEmpty(description) ? "" : " (+)</span>") +
                    "<br>Name: " + IdFriendly.HTMLEncode() + "</h1>");
            }
            var a = GetType().GetClassAttribute();

            /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
            /// TOOD: Consider removing this. Should be available from <see cref="Property.ToHTMLTableRow"/> anyway.
            if (a.ParentType != null && TryGetPV<QueryId>(CoreP.QueryIdParent.A(), out var queryIdParent)) { // Link from child to parent
                queryIdParent.AssertIsSingle(() => ToString());
                retval.Append("<p>" + request.API.CreateAPILink(CoreAPIMethod.EntityIndex, "Parent " + a.ParentType.ToStringVeryShort(), a.ParentType, queryIdParent) + "</p>");
            }
            /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
            if (a.ChildrenType != null) { // Link from parent to children
                retval.Append("<p>" + request.API.CreateAPILink(CoreAPIMethod.EntityIndex, "Children " + a.ChildrenType.ToStringVeryShort(), a.ChildrenType, new QueryIdKeyOperatorValue(CoreP.QueryIdParent.A().Key, Operator.EQ, IdString.ToString())) + "</p>");
            }

            var whereForeignKey = GetType().GetTypesWhereIsForeignKey();
            if (whereForeignKey.Count > 0) retval.Append("<p>Related entities:<br>" + string.Join("<br>", whereForeignKey.Select(t =>
                request.API.CreateAPILink(CoreAPIMethod.EntityIndex, t.type.ToStringVeryShort(), t.type, new QueryIdKeyOperatorValue(t.key.Key, Operator.EQ, Id)))) + "</p>");

            Context.GetPossibleContextOperationsForCurrentUserAndEntity(request, this, strict: false).ForEach(c => {
                /// Note how <see cref="CoreP.SuggestedUrl"/> really is a IsMany-property and how <see cref="Property.TryGetV{T}"/> contains code
                /// for explicit allowing asking for only a single value now. TODO: This is a somewhat dubious practice. 
                retval.Append("<p><a href=\"" + c.PV<Uri>(CoreP.SuggestedUrl.A()) + "\">" + c.PV<string>(CoreP.Description.A()).HTMLEncode() + "</a></p>");
            });

            // Suggested URLs for this specific entity
            if (Id > 0) retval.Append("<p>" + string.Join("<br>", BaseEntityUrls.Select(url => request.API.CreateAPILink(url))) + "</p>");

            retval.AppendLine("<!--DELIMITER-->"); // Useful if sub-class wants to insert something in between here
            retval.AppendLine(CreateHTMLForExistingProperties(request));
            retval.AppendLine(CreateHTMLForAddingProperties(request));
            return retval.ToString();
        }

        private List<string> _baseEntityUrls;
        /// <summary>
        /// TODO: Maybe rename into "UrlsForOperationOnThisEntity" or "RelevantUrls"
        /// 
        /// Returns <see cref="Extensions.GetBaseEntityMethods(Type)"/> relevant for this instance, with this.<see cref="Id"/> filled in, that is, returns complete URLs.
        /// 
        /// See also <see cref="Context.GetPossibleContextOperationsForCurrentUserAndEntity"/>
        /// </summary>
        [ClassMember(Description = "Returns urls for operation on this entity.")]
        public List<string> BaseEntityUrls => _baseEntityUrls ?? (_baseEntityUrls = GetType().GetBaseEntityMethods().SelectMany(m => m.PV<List<Uri>>(APIMethodP.BaseEntityMethodUrl.A()).Select(uri => uri.ToString().Replace("{" + CoreP.QueryId + "}", Id.ToString()))).OrderBy(url => url).ToList());

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
                //var existing = GetType().GetChildProperties() where .AccessLevelRead <= AccessLevel.Anonymous ?
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
                    /// TODO: Implement a common comparer for use by both <see cref="CreateHTMLForExistingProperties"/> and <see cref="CreateCSVForExistingProperties"/>
                    retval.AppendLine(string.Join("", existing.Values.OrderBy(p => ((long)p.Key.Key.A.PriorityOrder + int.MaxValue).ToString("0000000000") + p.Key.Key.PToString).Select(p => {
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

                if (Util.Configuration.C.Environment == Environment.Production) {
                    /// The hint given below is a <see cref="Environment.Development"/> / <see cref="Environment.Test"/> issue only. 
                } else if (request.CurrentUser == null) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if (request.CurrentUser.AccessLevelGiven <= AccessLevel.Anonymous) {
                    /// It is quite expected that <see cref="AccessType.Write"/> is not allowed now
                } else if ("AgoRapide".Equals(GetType().Assembly.GetName().Name)) {
                    /// Developers of AgoRapide library itself are expected to understand this issue
                } else {
                    var childProperties = GetType().GetChildProperties();
                    if (childProperties.Any(c => c.Value.Key.A.ExternalPrimaryKeyOf != null)) {
                        /// Looks like originates from external source through <see cref="BaseSynchronizer"/>. For those it is quite normal if nothing can be added. 
                    } else {
                        /// In all other cases, give hint about access
                        var a = GetType().GetClassAttribute();


                        // TODO: Create general mechanism for adding links to HTML text like this (links are indicated with starting -'s and trailing -'s.)
                        // TODO: And of course cache result, expect for request.CurrentUser.Name which will change. 
                        retval.AppendLine("<p>HINT: " +
                            "There are no " + nameof(addableProperties) + " for this entity for " + request.CurrentUser.IdFriendly.HTMLEncode() + " (" + nameof(request.CurrentUser.AccessLevelGiven) + " = " + request.CurrentUser.AccessLevelGiven + ")." +
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

        private static ConcurrentDictionary<
            string, // Key is GetType + _ + PriorityOrder
            string> _CSVTableRowHeadingCache = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// May be overridden if you need finer control about how to present your entities.
        /// 
        /// NOTE: Remember to always override correspondingly for <see cref="BaseEntity.ToCSVTableRowHeading"/> and <see cref="BaseEntity.ToCSVTableRow"/>
        /// </summary>
        /// <returns></returns>
        public virtual string ToCSVTableRowHeading(Request request) => _CSVTableRowHeadingCache.GetOrAdd(GetType() + "_" + request.PriorityOrderLimit, k => {
            var thisType = GetType().ToStringVeryShort();
            return nameof(Id) + request.CSVFieldSeparator +
            string.Join(request.CSVFieldSeparator, GetType().GetChildPropertiesByPriority(
                    /// request.PriorityOrderLimit   Replaced 29 Sep 2017 with <see cref="PriorityOrder.Everything"/>
                    PriorityOrder.Everything // We assume that all information is required for CSV
                ).Select(key => new Func<string>(() => {
                    var retval = key.Key.PToString;
                    if (retval.StartsWith(thisType)) { // Note shortening of name here (often names will start with the same as the entity type, we then assume that we can safely remove the type-part).
                        retval = retval.Substring(thisType.Length);
                        if (retval.StartsWith("_")) retval = retval.Substring(1); /// Typical for <see cref="Database.PropertyKeyForeignKeyAggregate"/>
                    }
                    return retval;
                })())
            );
            /// request.CSVFieldSeparator + nameof(Created); // When used with <see cref="BaseSynchronizer"/> Created is especially of little value since it is only the date for the first synchronization.
        });

        /// <summary>
        /// Note that may be overridden if you need finer control about how to present your entities (like <see cref="Property.ToCSVTableRow"/>).
        /// 
        /// NOTE: Remember to always override correspondingly for <see cref="BaseEntity.ToCSVTableRowHeading"/> and <see cref="BaseEntity.ToCSVTableRow"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToCSVTableRow(Request request) => (Id <= 0 ? "" : Id.ToString()) + request.CSVFieldSeparator +
            string.Join(request.CSVFieldSeparator, GetType().GetChildPropertiesByPriority(
                    /// request.PriorityOrderLimit   Replaced 29 Sep 2017 with <see cref="PriorityOrder.Everything"/>
                    PriorityOrder.Everything // We assume that all information is required for CSV
                ).Select(key => Properties.TryGetValue(key.Key.CoreP, out var p) ?
                p.V<string>().Replace(request.CSVFieldSeparator, ":") :  // Note replacement here with colon. TODO: Document better / create alternatives
                "")
            ) +
            // request.CSVFieldSeparator + Created.ToString(DateTimeFormat.DateHourMin) + // When used with <see cref="BaseSynchronizer"/> Created is especially of little value since it is only the date for the first synchronization.
            "\r\n";

        /// <summary>
        /// Note that may be overridden if you need finer control about how to present your entities (like <see cref="Property.ToCSVDetailed"/> and <see cref="Result.ToCSVDetailed"/>). 
        /// 
        /// There are three levels of packaging CSV information.
        /// <see cref="CSVView.GenerateResult"/>
        ///   <see cref="CSVView.GetCSVStart"/>
        ///   <see cref="Result.ToCSVDetailed"/>
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///     <see cref="Result.ToCSVDetailed"/> (actual result, inserts itself at "!--DELIMITER--" left by <see cref="BaseEntity.ToCSVDetailed"/>). 
        ///     <see cref="BaseEntity.ToCSVDetailed"/> (called from <see cref="Result.ToCSVDetailed"/>)
        ///   <see cref="CSVView.GetCSVEnd"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string ToCSVDetailed(Request request) {
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
                retval.AppendLine(
                    "Type" + request.CSVFieldSeparator + GetType().ToStringVeryShort() + (string.IsNullOrEmpty(description) ? "" : (request.CSVFieldSeparator + request.CSVFieldSeparator + request.CSVFieldSeparator + description.Replace("\r\n", "\r\n" + request.CSVFieldSeparator + request.CSVFieldSeparator + request.CSVFieldSeparator + request.CSVFieldSeparator))) + "\r\n" +
                    "Name" + request.CSVFieldSeparator + IdFriendly);
            }
            var a = GetType().GetClassAttribute();

            /// NOTE: Sep 2017: Code below (parent, children, related entities, operations, suggested URLs) was copied form <see cref="ToHTMLDetailed"/>. 
            /// NOTE: Something of it might not be needed for CSV-format

            /// TODO: RECONSIDER IF THESE ARE NEEDED FOR CSV!
            /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
            /// TOOD: Consider removing this. 
            if (a.ParentType != null && TryGetPV<QueryId>(CoreP.QueryIdParent.A(), out var queryIdParent)) { // Link from child to parent
                queryIdParent.AssertIsSingle(() => ToString());
                retval.AppendLine("Parent" + request.CSVFieldSeparator +
                    queryIdParent + request.CSVFieldSeparator +
                    a.ParentType.ToStringVeryShort() + request.CSVFieldSeparator +
                    request.API.CreateAPIUrl(CoreAPIMethod.EntityIndex, a.ChildrenType, new QueryIdKeyOperatorValue(CoreP.QueryIdParent.A().Key, Operator.EQ, IdString.ToString()))
                );
            }

            /// TODO: RECONSIDER IF THESE ARE NEEDED FOR CSV!
            /// TODO: Should <see cref="ClassAttribute.ParentType"/> and <see cref="ClassAttribute.ChildrenType"/> be replaced with <see cref="PropertyKeyAttribute.ForeignKeyOf"/>?
            if (a.ChildrenType != null) { // Link from parent to children
                retval.AppendLine("Children" + request.CSVFieldSeparator +
                    request.API.CreateAPIUrl(CoreAPIMethod.EntityIndex, a.ChildrenType, new QueryIdKeyOperatorValue(CoreP.QueryIdParent.A().Key, Operator.EQ, IdString.ToString()))
                );
            }

            /// TODO: RECONSIDER IF THESE ARE NEEDED FOR CSV!
            var whereForeignKey = GetType().GetTypesWhereIsForeignKey();
            if (whereForeignKey.Count > 0) retval.AppendLine("Related entities:" + request.CSVFieldSeparator + "\r\n" + string.Join("\r\n", whereForeignKey.Select(t =>
                t.type.ToStringVeryShort() + request.CSVFieldSeparator + request.API.CreateAPIUrl(CoreAPIMethod.EntityIndex, t.type, new QueryIdKeyOperatorValue(t.key.Key, Operator.EQ, Id))))
            );

            // NOT NECESSARY FOR CSV. REMOVE!
            //// TODO: Add heading here?
            //Context.GetPossibleContextOperationsForCurrentUserAndEntity(request, this, strict: false).ForEach(c => {
            //    // TODO: Maybe switch positions for these two
            //    retval.AppendLine(c.PV<Uri>(CoreP.SuggestedUrl.A()) + request.CSVFieldSeparator + c.PV<string>(CoreP.Description.A()));
            //});

            // NOT NECESSARY FOR CSV. REMOVE!
            //// TODO: Add heading here?
            //// Suggested URLs for this specific entity
            //if (Id > 0) retval.AppendLine(string.Join("\r\n", BaseEntityUrls));

            // TODO: REPLACE WITH SOMETHING BETTER, PREFERABLE SOMETHING INVISIBLE IN A TYPICAL SPREADSHEET PROGRAM
            retval.AppendLine();
            retval.AppendLine("<!--DELIMITER-->"); // Useful if sub-class wants to insert something in between here

            retval.AppendLine();
            retval.AppendLine(CreateCSVForExistingProperties(request));
            // retval.AppendLine(CreateCSVForAddingProperties(request)); This is considered unnecessary. Eventually add as desired later.
            return retval.ToString();
        }

        /// <summary>
        /// Creates a CSV representation of the existing properties for this entity. 
        /// Copied from <see cref="CreateHTMLForExistingProperties(Request)"/>. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual string CreateCSVForExistingProperties(Request request) {
            var retval = new StringBuilder();
            if (Properties != null) {
                var existing = GetExistingProperties(request.CurrentUser, AccessType.Read);
                if (existing.Count > 0) {
                    // retval.AppendLine("Properties:");
                    retval.AppendLine(Property.ToCSVTableRowHeadingStatic(request));
                    /// TODO: Implement a common comparer for use by both <see cref="CreateHTMLForExistingProperties"/> and <see cref="CreateCSVForExistingProperties"/>
                    retval.AppendLine(string.Join("\r\n", existing.Values.OrderBy(p => ((long)p.Key.Key.A.PriorityOrder + int.MaxValue).ToString("0000000000") + p.Key.Key.PToString).Select(p => {
                        return p.ToCSVTableRow(request);
                    })));
                }
            }
            return retval.ToString();
        }

        /// <summary>
        /// For example of override see <see cref="Property.ToJSONEntity"/>
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
                    if (i.Value.Key.Key.A.IsMany) { // Flatten out for JSON result.
                        i.Value.Properties.ForEach(p => { // TODO: Consider other possibilities here. Use of array for instance just like in the C# code.
                            retval.Properties.Add(p.Value.Key.ToString(), p.Value.ToJSONProperty());
                        });
                    } else {
                        retval.Properties.Add(i.Value.Key.ToString(), i.Value.ToJSONProperty());
                    }
                });
                // Note that we do not bother with Type when Properties is not set
                if (!retval.Properties.ContainsKey(nameof(CoreP.RootProperty))) retval.Properties.Add(nameof(CoreP.RootProperty), new JSONProperty0 { Value = GetType().ToStringShort() });
            }
            // TODO: ADD THIS SOMEHOW. That is, explain what kind of Properties may be added.
            // AddUserChangeablePropertiesToSimpleEntity(retval);
            return retval;
        }

        /// <summary>
        /// Returns a list with <paramref name="maxN"/> mock-entities based on <see cref="PropertyKeyAttributeEnriched.GetSampleProperty{TParent}"/>
        /// </summary>
        /// <param name="propertyPredicate">
        /// Select which properties returned from <see cref="Extensions.GetChildProperties(Type)"/> to include.  
        /// Would typically be  "p => p.Key.A.IsExternal" when used by <see cref="Agent"/></param>
        /// <param name="maxN">
        /// Gives the maximum number of elements that will be generated for all types, not only <typeparamref name="T"/>. 
        /// Number for all types is needed by <see cref="PropertyKeyAttributeEnriched.GetSampleProperty{TParent}"/> in order to
        /// determine range of foreign keys (<see cref="PropertyKeyAttribute.ForeignKeyOf"/>)
        /// </param>
        /// <returns></returns>
        public static List<BaseEntity> GetMockEntities(Type type, Func<PropertyKey, bool> propertyPredicate, Dictionary<Type, int> maxN) { // where T : BaseEntity, new() {
            InvalidTypeException.AssertAssignable(type, typeof(BaseEntity));
            var retval = new List<BaseEntity>();
            // var type = typeof(T);
            var properties = type.GetChildProperties().Values.Where(propertyPredicate).ToList(); // Turning into list improves performance since accessed many times. 
            var maxT = maxN.GetValue(type);
            for (var n = 1; n <= maxT; n++) {
                var e = Activator.CreateInstance(type) as BaseEntity ?? throw new InvalidTypeException(type, "Very unexpected since was just asserted OK");
                // var e = new T() {
                e.Properties = new Dictionary<CoreP, Property>();
                // };
                properties.ForEach(p => {
                    e.AddProperty(p.Key.GetSampleProperty(type, n, maxN));
                });
                retval.Add(e);
            }
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