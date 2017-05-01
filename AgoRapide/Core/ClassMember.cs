using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(
        Description = 
            "Represents a class' method in your application like \"{className}.{methodName}\". " +
            "Based on -" + nameof(ClassMemberAttribute) + "-.",
        LongDescription =
            "Used as source of -" + nameof(DBField.cid) + "-, -" + nameof(DBField.vid) + "-, and -" + nameof(DBField.iid) + "- when it is " +
            "\"the system itself\" making changes to your database " +
            "(but this should be the exception, usually -" + nameof(Request.CurrentUser) + "-'s -" + nameof(DBField.id) + "- " +
            "is used in order to pin-point which user credentials was used for any given change in the database).",
        AccessLevelRead = AccessLevel.Anonymous,
        AccessLevelWrite = AccessLevel.System
    )]
    public class ClassMember : ApplicationPart {

        /// <summary>
        /// Dummy constructor for use by <see cref="IDatabase.TryGetEntityById"/>. 
        /// Object meant to be discarded immediately afterwards in <see cref="ApplicationPart.Get{T}"/>. 
        /// DO NOT USE!
        /// 
        /// Note that values identified 
        /// only by <see cref="ApplicationPart.GetClassMember"/> 
        /// (not by <see cref="Class.RegisterAndIndexClass"/>)
        /// will anyway have originated through this constructor.
        /// Note hack in <see cref="ApplicationPart.Get{T}"/> 
        /// in order to set <see cref="ApplicationPart.A"/> correctly.
        /// </summary>
        public ClassMember() : base(BaseAttribute.GetStaticNotToBeUsedInstance) { }
        public ClassMember(ClassMemberAttribute attribute) : base(attribute) { }

        public override void ConnectWithDatabase(IDatabase db) => Get(A, db, enrichAndReturnThisObject: this);
    }
}
