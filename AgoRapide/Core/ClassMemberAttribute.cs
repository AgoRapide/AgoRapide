using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description = "Describes a member of a class (a method)")]
    public class ClassMemberAttribute : BaseAttribute {

        public Type ClassType { get; private set; }
        public string Member { get; private set; }

        /// <summary>
        /// TODO: Not tested as of Apr 2017. May be completely wrong.
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetAttribute(Type classType, string member) {
            var field = classType.GetField(member) ?? throw new NullReferenceException(nameof(classType.GetField) + "(): Cause: " + classType.ToStringShort() + "." + member.ToString() + " is most probably not defined.");
            var retval = new Func<ClassMemberAttribute>(() => {
                var attributes = field.GetCustomAttributes(typeof(ClassMemberAttribute), true);
                switch (attributes.Length) {
                    case 0:
                        /// TODO: Duplicate code!
                        var tester = new Action<Type>(t => {
                            object found = field.GetCustomAttributes(t, true);
                            if (found != null) throw new IncorrectAttributeTypeUsedException(found, typeof(EnumMemberAttribute), classType + "." + member);
                        });
                        tester(typeof(ClassAttribute));
                        // tester(typeof(ClassMemberAttribute));
                        tester(typeof(EnumAttribute));
                        tester(typeof(EnumMemberAttribute));
                        tester(typeof(AgoRapideAttribute));
                        return new ClassMemberAttribute { IsDefault = true };
                    case 1:
                        return (ClassMemberAttribute)attributes[0];
                    default:
                        throw new AttributeException(nameof(attributes) + ".Length > 1 (" + attributes.Length + ") for " + classType.ToStringVeryShort() + "." + member.ToString());
                }
            })();
            retval.ClassType = classType;
            retval.Member = member;
            return retval;
        }
    }
}
