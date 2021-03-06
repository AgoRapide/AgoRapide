﻿// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
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
    /// 
    /// </summary>
    [Class(
        Description =
            "Instances of this class are used as source of documentation and data for the API. " +
            "Note how the information NORMALLY originates from within the C# code in contrast to the -" + nameof(BaseEntity) + "- where " +
            "information NORMALLY originates from the api client / database. " +
            nameof(Properties) + " is used to transfer properties to the \"standard\" -" + nameof(BaseEntity) + "- concept " +
            "(and from there again to API clients or to be stored in the database for documentation purposes)",
        LongDescription =
            "-" + nameof(BaseAttribute) + "- is not to be instantiated directly. See instead the derived classes " +
            "" +
            "-" + nameof(ConfigurationAttribute) + "- (which does not use any -" + nameof(Attribute) + "- functionality), " +
            "-" + nameof(ClassAttribute) + "-, " +
            "-" + nameof(ClassMemberAttribute) + "-, " +
            "-" + nameof(EnumAttribute) + "-, " +
            "-" + nameof(EnumValueAttribute) + "-, " +
            "-" + nameof(PropertyKeyAttribute) + "-, "
    )]
    public class BaseAttribute : Attribute {

        private Id _id;
        public Id Id => _id ?? (_id = GetId());

        [ClassMember(Description = "Indicates that the actual attribute is not defined and instead a default instance was generated")]
        public bool IsDefault { get; protected set; }
        public static BaseAttribute GetNewDefaultInstance() => new BaseAttribute { IsDefault = true };
        public void AssertNotDefault() {
            if (IsDefault) throw new AttributeException(nameof(IsDefault) + ". Details: " + ToString());
        }

        [ClassMember(Description = "See -" + nameof(GetStaticNotToBeUsedInstance) + "-.")]
        private bool IsNotToBeUsed;
        /// <summary>
        /// Used by dummy constructors of <see cref="ApplicationPart"/>
        /// 
        /// TODO: Add some mechanism that will throw an exception if properties of returned instance are accessed.
        /// 
        /// TODO: Especially an 
        /// </summary>
        public static BaseAttribute GetStaticNotToBeUsedInstance = new BaseAttribute { IsDefault = true, IsNotToBeUsed = true }; // Add GenerateExceptionWhenPropertyAccessed or similar here.
        public void AssertToBeUsed() {
            if (IsNotToBeUsed) throw new AttributeException(nameof(IsNotToBeUsed) + ". Details: " + ToString());
        }

        /// <summary>
        /// TODO: Fix comment text, what is ???
        /// Note: If ??? is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="PropertyKeyAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreP.CoreAPIMethod"/> and <see cref="AgoRapide.CoreAPIMethod"/>
        /// 
        /// See also <see cref="CoreP.Description"/>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// TODO: Fix comment text, what is ???
        /// Note: If ??? is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="ClassAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreP.CoreAPIMethod"/> and <see cref="AgoRapide.CoreAPIMethod"/>
        /// 
        /// See also <see cref="CoreP.LongDescription"/>
        /// </summary>
        public string LongDescription { get; set; }

        private string _wholeDescription;
        public string WholeDescription => _wholeDescription ?? (_wholeDescription = string.IsNullOrEmpty(LongDescription) ? Description : (Description + "\r\n\r\n" + LongDescription));

        [Class(Description = "Helps to clean up any confusion about which -" + nameof(BaseAttribute) + "- to use in a given concept")]
        public class IncorrectAttributeTypeUsedException : ApplicationException {
            public IncorrectAttributeTypeUsedException(object foundAttribute, Type expectedType, string member) : base(
                "Incorrect attribute used for " + member + ".\r\n" +
                "Expected " + expectedType + " but found " + foundAttribute.GetType() + ".\r\n" +
                "Resolution: Change to " + expectedType + " for " + member + ".\r\n" +
                "(Code will most probably still compile as properties are identically named or shared through inheritance.)\r\n\r\n" +
                "Details for " + nameof(foundAttribute) + ":\r\n" + foundAttribute.ToString()) { }
        }

        public class AttributeException : ApplicationException {
            public AttributeException(string message) : base(message) { }
            public AttributeException(string message, Exception inner) : base(message, inner) { }
        }

        private static List<Type> allAgoRapideAttributeTypes = new List<Type> {
            typeof(ClassAttribute),
            typeof(ClassMemberAttribute),
            typeof(EnumAttribute),
            typeof(EnumValueAttribute),
            typeof(PropertyKeyAttribute)
        };

        /// <summary>
        /// Gets attribute of type <typeparamref name="T"/> from <paramref name="fieldInfo"/>
        /// 
        /// Attempts to clarify incorrect attribute type used (through <see cref="IncorrectAttributeTypeUsedException"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldInfo"></param>
        /// <param name="memberInfo">Provides debug information in case an exception is thrown</param>
        /// <returns></returns>
        protected static T GetAttributeThroughFieldInfo<T>(System.Reflection.FieldInfo fieldInfo, Func<string> memberInfo) where T : BaseAttribute, new() {
            var attributes = fieldInfo.GetCustomAttributes(typeof(EnumValueAttribute), true);
            switch (attributes.Length) {
                case 0:
                    allAgoRapideAttributeTypes.ForEach(t => { // Test for incorrect attribute type used (clarify any misunderstandings)
                        if (t.Equals(typeof(T))) return;
                        var found = fieldInfo.GetCustomAttributes(t, true);
                        if (found != null && found.Length > 0) throw new IncorrectAttributeTypeUsedException(found[0], typeof(T), memberInfo());
                    });
                    return new T { IsDefault = true };
                case 1:
                    return (T)attributes[0];
                default:
                    throw new AttributeException(nameof(attributes) + ".Length > 1 (" + attributes.Length + ") for " + memberInfo());
            }
        }

        protected static T GetAttributeThroughType<T>(Type type) where T : BaseAttribute, new() {
            var retval = (T)GetCustomAttribute(type, typeof(T));
            if (retval != null) return retval;
            allAgoRapideAttributeTypes.ForEach(t => { // Test for incorrect attribute type used (clarify any misunderstandings)
                if (t.Equals(typeof(T))) return;
                var found = GetCustomAttribute(type, t);
                if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(T), type.ToString());
            });
            return new T { IsDefault = true };
        }

        protected static T GetAttributeThroughMemberInfo<T>(System.Reflection.MemberInfo memberInfo) where T : BaseAttribute, new() {
            var retval = (T)GetCustomAttribute(memberInfo, typeof(T));
            if (retval != null) return retval;
            allAgoRapideAttributeTypes.ForEach(t => { // Test for incorrect attribute type used (clarify any misunderstandings)
                if (t.Equals(typeof(T))) return;
                var found = GetCustomAttribute(memberInfo, t);
                if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(T), memberInfo.Name);
            });
            return new T { IsDefault = true };
        }

        private ConcurrentDictionary<CoreP, Property> _properties;
        /// <summary>
        /// Returns a <see cref="BaseEntity.Properties"/> collection based on properties of this instance.
        /// </summary>
        public ConcurrentDictionary<CoreP, Property> Properties => _properties ?? (_properties = new Func<ConcurrentDictionary<CoreP, Property>>(() => {
            var retval = GetProperties();
            var p = Util.GetNewPropertiesParent();

            Func<string> d = () => ToString();

            p.AddProperty(CoreP.QueryId.A(), Id.IdString, d);
            p.AddProperty(CoreP.IdFriendly.A(), Id.IdFriendly, d);
            p.AddProperty(CoreP.IdDoc.A(), Id.IdDoc, d);
            if (Id.Parent != null) p.AddProperty(CoreP.QueryIdParent.A(), Id.Parent, d);

            p.AddProperty(CoreP.Description.A(), Description + "", Description + "", GetType().GetClassMemberAttribute(nameof(Description)), d); // (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))
            p.AddProperty(CoreP.LongDescription.A(), LongDescription + "", LongDescription + "", GetType().GetClassMemberAttribute(nameof(LongDescription)), d); // (TODO: Implement mechanism for setting no-longer-current of existing property instead (when this value becomes null))

            p.Properties.ForEach(e => {
                retval.AddValue2(e.Key, e.Value, d);
            });
            return retval;
        })());
        protected virtual ConcurrentDictionary<CoreP, Property> GetProperties() => new ConcurrentDictionary<CoreP, Property>();

        public override string ToString() => nameof(Description) + ":\r\n" + Description + "\r\n" + nameof(LongDescription) + ":\r\n" + LongDescription + "\r\n" + nameof(BaseAttribute) + "Subclass: " + GetType().ToString() + (!IsDefault ? "" : ", " + (nameof(IsDefault) + ": " + IsDefault)) + (!IsNotToBeUsed ? "" : ", " + (nameof(IsNotToBeUsed) + ": " + IsNotToBeUsed));
        /// <summary>
        /// The implementator should return a value satisfying both 
        /// <see cref="PropertyKeyAttribute.IsUniqueInDatabase"/> and
        /// <see cref="System.CodeDom.Compiler.CodeDomProvider.IsValidIdentifier"/>
        /// (Note assertion for the last criteria in <see cref="Id"/>)
        ///  </summary>
        /// <returns></returns>
        protected virtual Id GetId() => throw new NullReferenceException(
            nameof(GetId) + ". " +
            (GetType().Equals(typeof(BaseAttribute)) ?
                ("Illegal to call " + System.Reflection.MethodBase.GetCurrentMethod().Name + " for " + GetType() + " / " + nameof(IsDefault)) :
                ("Should have been implemented by sub-class " + GetType())
            ) + ".\r\n" +
            "Details: " + ToString()
        );
    }
}
