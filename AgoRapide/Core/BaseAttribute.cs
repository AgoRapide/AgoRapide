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
        "-" + nameof(EnumMemberAttribute) + "-" +
        "-" + nameof(AgoRapideAttribute) + "-"
    )]
    public class BaseAttribute : Attribute {

        ///// <summary>
        ///// Private constructor. Class only to be instantiated from <see cref="GetNewDefaultInstance"/>
        ///// </summary>
        //private BaseAttribute() { }

        public static BaseAttribute GetNewDefaultInstance() => new BaseAttribute { IsDefault = true };

        [ClassMember(Description = "Indicates that the actual attribute is not defined and instead a default instance was generated")]
        public bool IsDefault { get; protected set; }

        /// <summary>
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="AgoRapideAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreP.CoreMethod"/> and <see cref="AgoRapide.CoreMethod"/>
        /// 
        /// See also <see cref="CoreP.Description"/>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Note: If <see cref="Type"/> is one of your own classes / enums, or one of the AgoRapide classes / enums 
        /// then you are recommended to not set <see cref="Description"/> / <see cref="LongDescription"/> for the enum value  
        /// but instead rely on using <see cref="ClassAttribute"/> belonging to the enum / class given by <see cref="Type"/>
        /// For an example see how it is implemented for <see cref="CoreP.CoreMethod"/> and <see cref="AgoRapide.CoreMethod"/>
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
                "(Code will most probably still compile.)\r\n\r\n" +
                "Details for " + nameof(foundAttribute) + ":\r\n" + foundAttribute.ToString()) { }
        }

        public class AttributeException : ApplicationException {
            public AttributeException(string message) : base(message) { }
            public AttributeException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
