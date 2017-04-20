using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description =
            "Describes a member of a class (a method). Member of -" + nameof(ClassMember) + "-. " +
            "The class itself is described by -" + nameof(ClassAttribute) + "-.")]
    public class ClassMemberAttribute : BaseAttribute {

        public System.Reflection.MemberInfo MemberInfo { get; private set; }

        /// <summary>
        /// NOTE: Use with caution. 
        /// NOTE: Will not work for overloaded methods. 
        /// NOTE: Overload <see cref="GetAttribute(System.Reflection.MemberInfo)"/> is preferred. 
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetAttribute(Type classType, string memberName) {
            var candidates = classType.GetMembers().Where(m => m.Name.Equals(memberName)).ToList();
            switch (candidates.Count) {
                case 0: throw new NullReferenceException(nameof(memberName) + ": " + memberName + ". Cause: " + classType + "." + memberName + " is most probably not defined.");
                case 1: return GetAttribute(candidates[0]);
                default:
                    /// TODO: We could solve this by looking for candidates which actually has a <see cref="ClassMemberAttribute"/>
                    /// TODO: Most probably there will only exist one such attribute anyway.
                    throw new InvalidCountException(
                        "Multiple versions (multiple overloads) found for " + classType + "." + memberName + ". " +
                        "You can only call this method with the name of a non-overloaded method. " +
                        "The versions found where:\r\n" +
                        string.Join("\r\n", candidates.Select(c => c.ToString()))
                    );
            }
        }

        /// <summary>
        /// Preferred overload
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static ClassMemberAttribute GetAttribute(System.Reflection.MemberInfo memberInfo) {
            var retval = GetAttributeThroughMemberInfo<ClassMemberAttribute>(memberInfo);
            retval.MemberInfo = memberInfo;
            return retval;
        }
    }
}
