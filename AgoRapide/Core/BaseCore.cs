using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoRapide.Core {

    /// <summary>
    /// Base core class giving logging and exception handling.
    /// 
    /// You are recommended to use logging in all business logic 
    /// (but not in your entity classes although entity classes also inherit BaseCore as of Dec 2016)
    /// </summary>
    public class BaseCore {
        public event Action<string> LogEvent;

        /// <summary>
        /// Note difference between <see cref="BaseCore.Log"/> and <see cref="BaseEntityWithLogAndCount.LogInternal"/>
        /// The former communicates via <see cref="BaseCore.LogEvent"/> and is usually meant for ordinary server logging to disk or similar while
        /// the latter is used for more short-lived in-memory only logging where really detailed information is desired.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caller"></param>
        protected void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => LogEvent?.Invoke(GetType().ToStringShort() + "." + caller + ": " + text);

        /// <summary>
        /// <see cref="HandledExceptionEvent"/> is used for already handled exceptions in the sense that
        /// what is left for the event handler to do is to log the exception as desired. 
        /// </summary>
        public event Action<Exception> HandledExceptionEvent;
        public void HandleException(Exception ex) => HandledExceptionEvent?.Invoke(ex);
    }
}
