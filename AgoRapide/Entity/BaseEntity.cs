using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.Database;
using AgoRapide;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// <see cref="BaseEntity"/> represent a basic data object in your model like Person, Order and so on.
    /// Also used internally by AgoRapide like <see cref="Parameters"/>, <see cref="Result"/>, <see cref="ApplicationPart"/>, <see cref="APIMethod"/>
    /// 
    /// Immediate subclass is <see cref="BaseEntityT"/>
    /// 
    /// This class <see cref="BaseEntity"/> is an attempt to escape the need for carrying the TProperty in <see cref="BaseEntityT"/> 
    /// throughout the system. All non-generic information (that is, all information independent of TProperty) 
    /// is collected here, and this is the class exposed most throughout.
    /// (Jan 2017, note: In reality we ended up using <see cref="BaseEntityT"/> almost everywhere, negating the 
    /// rationale for having a separate <see cref="BaseEntity"/>-class.
    /// 
    /// Note how <see cref="BaseEntity"/> inherits <see cref="BaseCore"/> meaning you can listen to <see cref="BaseCore.LogEvent"/> and
    /// <see cref="BaseCore.HandledExceptionEvent"/> but these are not used internally in AgoRapide as of Januar 2017 
    /// (it is felt unnecessary for entity classes to do logging). 
    /// Note however <see cref="BaseEntityTWithLogAndCount.LogInternal"/> 
    /// </summary>
    public abstract class BaseEntity : BaseCore {

        /// <summary>
        /// Corresponds to field id in database
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Environment as used to characterize this entity
        /// 
        /// One idea for implementation:
        /// If we find a property called "Environment" when reading from database and that
        /// parses as a valid Environment-enum, then Environment here will be set accordingly
        /// 
        /// See Environment for more information
        /// </summary>
        public Environment Environment => throw new NotImplementedException();

        public DateTime Created { get; set; }

        /// <summary>
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// <see cref="Name"/> is used in contexts when a more user friendly value is needed. 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => GetType().ToString() + ": " + Id + ", created: " + Created.ToString(DateTimeFormat.DateHourMin);

        /// <summary>
        /// <see cref="ToString"/> is used in logs and in exception messages. 
        /// <see cref="Name"/> is used in contexts when a more user friendly value is needed. 
        /// </summary>
        /// <returns></returns>
        public abstract string Name { get; }

        //private string _HTMLLink;
        ///// <summary>
        ///// HTML link for querying entity in HTML format
        ///// </summary>
        //public virtual string HTMLLink => _HTMLLink ?? (_HTMLLink = "<a href=\"" + IndexURL + Util.Configuration.HTMLPostfixIndicator + ">" + Name.HTMLEncode() + "</a>");

        //private string _IndexUrl;
        ///// <summary>
        ///// TODO: Use something like Method[CoreMethod.EntityIndex].GenerateUrl(42, ResponseFormat.HTML)        
        ///// </summary>
        //public virtual string IndexURL => _IndexUrl ?? (_IndexUrl = Util.Configuration.RootUrl + Util.Configuration.ApiPrefix + "Entity/" + Id);

        /// <summary>
        /// Override this method in all your classes which you want to give <see cref="AccessLevel"/> as a right. 
        /// 
        /// Typically that would be a class that you call Person, User, Customer or similar which would
        /// call <see cref="BaseEntityT.PV{AccessLevel}(TProperty, AccessLevel)"/> with 
        /// <see cref="AgoRapide.AccessLevel.User"/> or <see cref="AgoRapide.AccessLevel.None"/> as default value.
        /// 
        /// A typical code example for this would be 
        ///   public override AccessLevel AccessLevelGiven => PV(P.AccessLevelGiven, defaultValue: AccessLevel.User)
        /// <see cref="Request.CurrentUser"/> (<see cref="BaseEntity.AccessLevelGiven"/>) must by equal to <see cref="MethodAttribute.AccessLevel"/> or HIGHER in order for access to be granted to <see cref="APIMethod"/>
        /// </summary>
        public virtual AccessLevel AccessLevelGiven => AccessLevel.None;
    }
}
