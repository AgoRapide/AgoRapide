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

        private System.Reflection.MemberInfo _memberInfo;
        public System.Reflection.MemberInfo MemberInfo => _memberInfo ?? throw new NullReferenceException(nameof(MemberInfo) + ". Should have been set by -" + nameof(GetAttribute) + "- or similar.\r\nDetails: " + ToString());

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
            retval._memberInfo = memberInfo;
            return retval;
        }

        public override string ToString() => nameof(MemberInfo) + ": " + (_memberInfo?.DeclaringType.ToString() ?? "[NULL]") + "." + (_memberInfo?.ToString() ?? "") + "\r\n" + base.ToString();
        protected override Id GetId() => new Id(
            idString:
                GetType().ToStringShort().Replace("Attribute", "")
                + "_" +
                MemberInfo.ReflectedType.ToStringShort().Replace
                    ("<", "_").Replace
                    (">", "_").Replace
                    ("+", "_")
                + "_" +
                MemberInfo.ToString().Replace // TODO: This is the actual method signature
                    (".", "_").Replace       // TODO: Try to make better readable version
                    (",", "_").Replace       // 
                    (" ", "_").Replace
                    ("`", "_").Replace
                    ("<", "_").Replace
                    (">", "_").Replace
                    ("[", "_").Replace
                    ("]", "_").Replace
                    ("+", "_").Replace
                    ("(", "_").Replace
                    (")", "_").Replace
                    ("Void_", ""),
            idFriendly: MemberInfo.ReflectedType.ToStringShort() + "." + MemberInfo.Name, /// Note choice of Name here instead of ToString(). See use of ToString() below when storing <see cref="CoreP.IdFriendlyDetailed"/>
            idDoc: new List<string> {
                 MemberInfo.ReflectedType.ToStringShort() + "." + MemberInfo.Name,
                 MemberInfo.Name,
            }
         );

        protected override Dictionary<CoreP, Property> GetProperties() {
            /// TODO: Replace <see cref="PropertiesParent"/> with a method someting to <see cref="BaseEntity.AddProperty{T}"/> instead.
            /// TODO: Maybe with [System.Runtime.CompilerServices.CallerMemberName] string caller = "" in order to
            /// TDOO: call <see cref="ClassMemberAttribute.GetAttribute(Type, string)"/> automatically for instance.
            PropertiesParent.Properties = new Dictionary<CoreP, Property>(); // Hack, since maybe reusing collection
            Func<string> d = () => ToString();
            // Adds the full method information (in order to distinguish overloads from each other)
            PropertiesParent.AddProperty(CoreP.IdFriendlyDetailed.A(), MemberInfo.ReflectedType.ToStringShort() + "." + MemberInfo.ToString(), d);
            return PropertiesParent.Properties;
        }
    }
}
