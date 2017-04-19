using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description = "Not to be instantiated directly. See derived classes " +
        "-" + nameof(ClassAttribute) + "-" +
        "-" + nameof(ClassMemberAttribute) + "-" +
        "-" + nameof(EnumAttribute) + "-" +
        "-" + nameof(EnumValueAttribute) + "-" +
        "-" + nameof(PropertyKeyAttribute) + "-"
    )]
    public class BaseAttribute : Attribute {

        public static BaseAttribute GetNewDefaultInstance() => new BaseAttribute { IsDefault = true };

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
    }
}
