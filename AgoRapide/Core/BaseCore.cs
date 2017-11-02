// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide.Core {

    /// <summary>
    /// Base core class giving logging and exception handling.
    /// </summary>    
    public class BaseCore {

        [ClassMember(
            Description =
                "Seldom in use. " +
                "Note that logging is only practised in a few classes inheriting -" + nameof(BaseCore) + "-, like -" + nameof(BaseDatabase) + "-. " +
                "A typical -" + nameof(BaseEntity) + "- class for instance will most probably not do any logging at all. " +
                "Correspondingly -" + nameof(LogEvent) + "- is usually not subscribed to."
        )]
        public event Action<string> LogEvent;

        [ClassMember(Description =
            "Calls to this method will often have no effect. " +
            "See comment for -" + nameof(LogEvent) + "-. " +
            "See instead overload with -" + nameof(BaseEntityWithLogAndCount) + "- parameter.")]
        protected virtual void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => LogEvent?.Invoke(GetType().ToStringShort() + "." + caller + ": " + text);

        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result">Often an instance of <see cref="Result"/></param>
        /// <param name="caller"></param>
        [ClassMember(Description = "Logs both \"ordinary\" through -" + nameof(LogEvent) + "- and \"internally\" through -" + nameof(BaseEntityWithLogAndCount.LogInternal) + "-.")]
        protected virtual void Log(string text, BaseEntityWithLogAndCount result, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            Log(text, caller);
            result?.LogInternal(text, GetType(), caller);
        }

        [ClassMember(Description =
            "Seldom in use, see comment for -" + nameof(LogEvent) + "-. " +
            "See -" + nameof(BaseController) + "-.-" + nameof(BaseController.HandledExceptionEvent) + "- for documentation. "
        )]
        public event Action<Exception> HandledExceptionEvent;
        public virtual void HandleException(Exception ex) => HandledExceptionEvent?.Invoke(ex);
    }
}
