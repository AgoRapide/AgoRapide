﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static BaseAttribute GetNewDefaultInstance() => new BaseAttribute { IsDefault = true };
        /// <summary>
        /// Used by dummy constructors of <see cref="ApplicationPart"/>
        /// 
        /// TODO: Add some mechanism that will throw an exception if properties of returned instance are accessed.
        /// </summary>
        public static BaseAttribute GetStaticNotToBeUsedInstance = new BaseAttribute { IsDefault = true }; // Add GenerateExceptionWhenPropertyAccessed or similar here.

        [ClassMember(Description = "Indicates that the actual attribute is not defined and instead a default instance was generated")]
        public bool IsDefault { get; protected set; }

        /// <summary>
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="PropertyKeyAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreP.CoreAPIMethod"/> and <see cref="AgoRapide.CoreAPIMethod"/>
        /// 
        /// See also <see cref="CoreP.Description"/>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
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

        private Property _propertiesParent;
        /// <summary>
        /// Serves the purpose of getting access to <see cref="BaseEntity.AddProperty{T}"/> for the purpose of generating the 
        /// collection accessed through <see cref="Properties"/>
        /// 
        /// NOTE: IMPORTANT. 
        /// NOTE: IMPORTANT. Do not attempt to eliminate <see cref="_propertiesParent"/> and shorten this to = new Property ... because then you will get type initializer exception at application startup because
        /// NOTE: IMPORTANT: <see cref="EnumMapper.MapEnum{T}"/> will not have been called yet in application lifetime for <see cref="CoreP"/>. 
        /// NOTE: IMPORTANT: In other words you will trip a chicken-and-egg trap
        /// NOTE: IMPORTANT. 
        /// </summary>
        protected Property PropertiesParent => _propertiesParent ?? (_propertiesParent = new PropertyT<string>(CoreP.Value.A().PropertyKeyWithIndex, ""));
        private Dictionary<CoreP, Property> _properties;
        /// <summary>
        /// Returns a <see cref="BaseEntity.Properties"/> collection based on properties of this instance.
        /// </summary>
        public Dictionary<CoreP, Property> Properties => _properties ?? (_properties = GetProperties());
        protected virtual Dictionary<CoreP, Property> GetProperties() => new Dictionary<CoreP, Property>();
    }
}