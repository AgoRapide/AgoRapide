using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(
        Description =
            "Represents a Class. " +
            "Based on -" + nameof(ClassAttribute) + "-.",
        ChildrenType = typeof(ClassMember),
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class Class : ApplicationPart {

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public Class() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public Class(ClassAttribute attribute) : base(attribute) { }

        public static void RegisterAndIndexCoreClass(IDatabase db) =>typeof(Configuration).Assembly.GetTypes().Where(t => !t.IsEnum).ForEach(t => RegisterAndIndexClass(t, db)); /// Going through <see cref="Configuration"/> ensures we get a reference to the AgoRapide assembly
        
        /// <summary>
        /// Note how only public members are found by this method. 
        /// Later calls to <see cref="ApplicationPart.GetClassMember"/> may identify further members.
        /// Note how default <see cref="ClassAttribute"/> and <see cref="ClassMemberAttribute"/> will be ignored. 
        /// </summary>
        /// <param name="db"></param>
        public static void RegisterAndIndexClass(Type type, IDatabase db) {
            var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);

            {
                var a = type.GetClassAttribute();
                if (a.IsDefault) {
                    // We have no interest in documenting classes without attributes. Will only generate noise.
                } else {
                    var _class = new Class(a);
                    _class.ConnectWithDatabase(db);
                    Documentator.IndexEntity(_class);
                }
            }

            type.GetMembers().ForEach(e => {
                if ((e.MemberType & System.Reflection.MemberTypes.NestedType) == System.Reflection.MemberTypes.NestedType) return; /// Would most probably result in a <see cref="BaseAttribute.IncorrectAttributeTypeUsedException"/>
                var a = e.GetClassMemberAttribute();
                if (a.IsDefault) return; // We have no interest in documenting members without attributes. Will only generate noise.
                var classMember = new ClassMember(a);
                classMember.ConnectWithDatabase(db);
                Documentator.IndexEntity(classMember);
            });
        }

        public override void ConnectWithDatabase(IDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
