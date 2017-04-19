using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    [Class(Description = "Describes a member of a class (a method). The class itself is described by -" + nameof(ClassAttribute) + "-.")]
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
            var field = classType.GetField(member) ?? throw new NullReferenceException(nameof(classType.GetField) + "(): Cause: " + classType + "." + member.ToString() + " is most probably not defined.");
            var retval = GetAttributeThroughFieldInfo<ClassMemberAttribute>(field, () => classType + "." + member);
            retval.ClassType = classType;
            retval.Member = member;
            return retval;
        }
    }
}
