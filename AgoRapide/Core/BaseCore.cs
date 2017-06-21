// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// Base core class giving logging and exception handling.
    /// 
    /// AgoRapide uses logging wherever it is considered helpful for debugging and system administration
    /// Note that logging in entity classes is not practised although <see cref="BaseEntity"/> also inherit <see cref="BaseCore"/>. 
    /// Instead exceptions generated from entity classes usually contains detailed entity information 
    /// through the <see cref="BaseEntity.ToString"/> method. 
    /// </summary>
    public class BaseCore {
        public event Action<string> LogEvent;

        /// <summary>
        /// Note difference between <see cref="BaseCore.Log"/> and <see cref="BaseEntityWithLogAndCount.LogInternal"/>
        /// The former communicates via <see cref="BaseCore.LogEvent"/> and is usually meant for ordinary server logging to disk or similar while
        /// the latter is used for more short-lived in-memory only logging where really detailed information is desired. 
        /// 
        /// See overload <see cref="Log(string, Result, string)"/> if you want to do both at the same time. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="caller"></param>
        protected void Log(string text, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => LogEvent?.Invoke(GetType().ToStringShort() + "." + caller + ": " + text);

        /// <summary>
        /// Logs both "ordinary" through <see cref="Log(string, string)"/> and "internally" through <see cref="BaseEntityWithLogAndCount.LogInternal"/>. 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="result"></param>
        /// <param name="caller"></param>
        protected void Log(string text, Result result, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") {
            Log(text, caller);
            result?.LogInternal(text, GetType(), caller);
        }

        /// <summary>
        /// <see cref="HandledExceptionEvent"/> is used for already handled exceptions in the sense that
        /// what is left for the event handler to do is to log the exception as desired. 
        /// </summary>
        public event Action<Exception> HandledExceptionEvent;
        public void HandleException(Exception ex) => HandledExceptionEvent?.Invoke(ex);
    }
}
