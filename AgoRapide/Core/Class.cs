// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
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
        /// Dummy constructor for use by <see cref="BaseDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// </summary>
        public Class() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public Class(ClassAttribute attribute, BaseDatabase db) : base(attribute) => ConnectWithDatabase(db);
        
        public static void RegisterAndIndexCoreClass(BaseDatabase db) =>typeof(Configuration).Assembly.GetTypes().Where(t => !t.IsEnum).ForEach(t => RegisterAndIndexClass(t, db)); /// Going through <see cref="Configuration"/> ensures we get a reference to the AgoRapide assembly
        
        /// <summary>
        /// Note how only public members are found by this method. 
        /// Later calls to <see cref="ApplicationPart.GetClassMember"/> may identify further members.
        /// Note how default <see cref="ClassAttribute"/> and <see cref="ClassMemberAttribute"/> will be ignored. 
        /// </summary>
        /// <param name="db"></param>
        public static void RegisterAndIndexClass(Type type, BaseDatabase db) {
            var cid = GetClassMember(System.Reflection.MethodBase.GetCurrentMethod(), db);

            {
                var a = type.GetClassAttribute();
                if (a.IsDefault) {
                    // We have no interest in documenting classes without attributes. It would only generate noise. 
                } else {
                    Documentator.IndexEntity(new Class(a, db));
                }
            }

            type.GetMembers().ForEach(e => { /// Note that duplicate calls will be made for inherited members (both for base class and for sub class, by separate calls to <see cref="RegisterAndIndexClass"/>)
                if ((e.MemberType & System.Reflection.MemberTypes.NestedType) == System.Reflection.MemberTypes.NestedType) return; /// Would most probably result in a <see cref="BaseAttribute.IncorrectAttributeTypeUsedException"/>
                var a = e.GetClassMemberAttribute();
                if (a.IsDefault) return; // We have no interest in documenting members without attributes. Will only generate noise.
                Documentator.IndexEntity(new ClassMember(a, db));
            });
        }

        protected override void ConnectWithDatabase(BaseDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
