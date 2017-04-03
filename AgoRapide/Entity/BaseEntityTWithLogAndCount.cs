using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide {

    /// <summary>
    /// Extension on <see cref="BaseEntityT"/> with internal logging and counting of vital statistics.
    /// Useful for:
    /// 1) Long-lived classes like <see cref="ApplicationPart"/> where you want to record different kind of statistics for their use. 
    /// 2) Classes for which it is desireable to communicate details about their contents like <see cref="Result"/>. 
    /// 
    /// Examples of inheriting classes in AgoRapide are: 
    /// <see cref="Result"/>, 
    /// <see cref="ApplicationPart"/>, 
    /// <see cref="APIMethod"/>
    /// </summary>
    public abstract class BaseEntityTWithLogAndCount : BaseEntityT { 
        public StringBuilder LogData = new StringBuilder();
        /// <summary>
        /// Note difference between <see cref="BaseCore.Log"/> and <see cref="BaseEntityTWithLogAndCount.LogInternal"/>
        /// The former communicates via <see cref="BaseCore.LogEvent"/> and is usually meant for ordinary server logging to disk or similar while
        /// the latter is used for more short-lived in-memory only logging where really detailed information is desired.
        /// </summary>
        /// <param name="text"></param>
        public void LogInternal(string text, Type callerType, [System.Runtime.CompilerServices.CallerMemberName] string caller = "") => LogData.AppendLine(DateTime.Now.ToString(DateTimeFormat.DateHourMinSecMs) + ": " + callerType.ToStringShort() + "." + caller + ": " + text);

        public Dictionary<CoreP, long> Counts = new Dictionary<CoreP, long>();
        /// <summary>
        /// TODO: Consider removing Counts-dictionary altogether. 
        /// 
        /// Use counts to keep track of different problems and general statistics
        /// 
        /// TODO: Consider if Counts can be implemented straight from Properties collection
        /// TODO: At least, let the ToHTML-method transfer all counts to Properties collection
        /// 
        /// TODO: Consider adding IsCount to <see cref="AgoRapideAttribute"/>. 
        /// TODO: This could transfer <see cref="Counts"/> values to / from database automatically. 
        /// </summary>
        /// <param name="id"></param>
        public void Count(CoreP id) => Counts[id] = Counts.TryGetValue(id, out var count) ? ++count : 1;
        public void SetCount(CoreP id, long value) => Counts[id] = value;
        public long GetCount(CoreP id) => TryGetCount(id, out var retval) ? retval : throw new CountNotFoundException(id);
        public bool TryGetCount(CoreP id, out long count) => TryGetCount(id, out count);
        public class CountNotFoundException : ApplicationException {
            public CountNotFoundException(CoreP id) : base(id.ToString()) { }
        }

        /// <summary>
        /// Calls first <see cref="BaseEntityT.ToHTMLDetailed"/> then adds <see cref="LogData"/> and <see cref="Counts"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override string ToHTMLDetailed(Request request) {
            var retval = new StringBuilder();
            retval.Append(base.ToHTMLDetailed(request));
            if (LogData.Length == 0) {
                // Do not bother with any of these
            } else {
                retval.AppendLine("<h1>" + nameof(LogInternal) + "</h1>");
                retval.AppendLine("<p>" + LogData.ToString().HTMLEncode().Replace("\r\n", "<br>\r\n") + "</p>");
            }
            if (Counts.Count == 0) {
                // Do not bother with any of these
            } else {
                retval.AppendLine("<h1>" + nameof(Counts) + "</h1>");
                retval.AppendLine("<table><tr><th>Key</th><th>Value</th></tr>");
                retval.AppendLine(string.Join("", Counts.OrderBy(e => e.Value.ToString()).Select(e => " <tr><td>" + e.Key.A().PToString + "</td><td align=\"right\">" + e.Value + "</td></tr>\r\n")));
                retval.AppendLine("</table>");
            }
            return retval.ToString();
        }

        /// <summary>
        /// Calls first <see cref="BaseEntityT.ToJSONEntity"/> then adds <see cref="LogData"/> and <see cref="Counts"/>
        /// </summary>
        /// <returns></returns>
        public override JSONEntity0 ToJSONEntity(Request request) {
            var retval = base.ToJSONEntity(request) as JSONEntity1 ?? throw new InvalidObjectTypeException(base.ToJSONEntity(request), typeof(JSONEntity1), ToString());
            if (LogData.Length == 0) {
                // Do not bother with any of these
            } else {
                var p = CoreP.Log;
                var key = p.A().Key.PToString;
                if (retval.Properties.TryGetValue(key, out var existing)) {
                    throw new KeyAlreadyExistsException<CoreP>(p,
                        "Unable to add " + nameof(LogData) + "\r\n-------\r\n" + LogData.ToString() + "\r\n" +
                        "-------Because of existing property\r\n-------\r\n" +
                        ((existing as JSONProperty0)?.GetValueShortened() ?? ("[OF_UNKNOWN_TYPE: " + existing.GetType())) + ". Details: " + ToString());
                }
                retval.Properties[key] = new JSONProperty0 { Value = LogData.ToString() };
            }
            if (Counts.Count == 0) {
                // Do not bother with any of these
            } else {
                Counts.ForEach(c => { /// Do not bother with <see cref="AccessLevel"/> for these. 
                    var key = c.Key.A().Key.PToString;
                    if (retval.Properties.TryGetValue(key, out var existing)) {
                        throw new KeyAlreadyExistsException<CoreP>(c.Key, "Unable to add " + nameof(Counts) + "[" + c.Key.GetAgoRapideAttributeT().PExplained + "] = " + c.Value + " because of existing property '" + ((existing as JSONProperty0)?.GetValueShortened() ?? ("[OF_UNKNOWN_TYPE: " + existing.GetType())) + "'. Details: " + ToString());
                    }
                    retval.Properties[key] = new JSONProperty0 { Value = c.Value.ToString() };
                });
            }
            return retval;
        }
    }

    ///// <summary>
    ///// See <see cref="BaseEntityT.ToJSONEntity"/>
    ///// 
    ///// Simpler version of a <see cref="BaseEntityT"/>-class, more suited for transfer to client as JSON-data.
    ///// 
    ///// Extend this class as needed. For examples see xxxx
    ///// </summary>
    //public class JSONEntityWithLogAndCount {
    //    public Dictionary<string, JSONProperty1> Properties { get; set; }
    //    public long Id { get; set; }
    //}
}
