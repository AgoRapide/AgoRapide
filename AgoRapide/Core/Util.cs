// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using AgoRapide.API;

namespace AgoRapide.Core {

    /// <summary>
    /// This class should be as small as possible!
    /// 
    /// TODO: Always look out for functionality here that could be moved elsewhere    
    /// </summary>
    public static class Util {

        public static bool CurrentlyStartingUp = true;
        [ClassMember(
            Description =
                "Normally called from non-thread safe methods that should be run single-threaded at application startup only.",
            LongDescription =
                "NOTE: If you want to remove a call to this method somewhere in the AgoRapide code " +
                "then you must first make that code, and all corresponding collections and methods, thread-safe first.\r\n" +
                "NOTE: Most probably you should NEVER remove any such calls as there is also a performance advantage of finished initialization " +
                "in \"peace and quiet\" at application startup.")]
        public static void AssertCurrentlyStartingUp() {
            if (!CurrentlyStartingUp) throw new SomeCodeOnlyToBeRunAtStartupHasBeenCalledAfterStartupFinishedException();
        }
        private class SomeCodeOnlyToBeRunAtStartupHasBeenCalledAfterStartupFinishedException : ApplicationException { }

        public static void AssertNotCurrentlyStartingUp() {
            if (CurrentlyStartingUp) throw new SomeCodeNotToBeRunAtStartupHasBeenCalledBeforeStartupFinishedException();
        }
        private class SomeCodeNotToBeRunAtStartupHasBeenCalledBeforeStartupFinishedException : ApplicationException { }

        /// <summary>
        /// Note that the default instance is only meant for temporary use at application startup. 
        /// 
        /// </summary>
        public static Configuration Configuration { get; set; } = new Configuration(new ConfigurationAttribute(
            logPath: @"c:\AgoRapideLog_[DATE_HOUR].txt", // TODO: Find a better default value than this
            rootUrl: new Uri("http://example.com/[" + nameof(ConfigurationAttribute.RootUrl) + "NotSetInDefaultInstanceOf" + nameof(Configuration) + "]"),
            databaseGetter: ownersType => throw new NullReferenceException(nameof(ConfigurationAttribute.DatabaseGetter) + "NotSetInDefaultInstanceOf" + nameof(Configuration))
        ));

        public static System.Globalization.CultureInfo Culture_en_US = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

        private static ConcurrentDictionary<Type, List<string>> _enumNamesCache = new ConcurrentDictionary<Type, List<string>>();
        /// <summary>
        /// Note that .None will not be returned.
        /// You probably do not need this method. Most probably you need the generic <see cref="EnumGetValues{T}"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<string> EnumGetNames(Type type) => _enumNamesCache.GetOrAdd(type, t => {
            NotOfTypeEnumException.AssertEnum(t);
            return System.Enum.GetNames(type).Where(s => !"None".Equals(s)).ToList();
        });

        private static ConcurrentDictionary<Type, List<object>> _enumValuesCache = new ConcurrentDictionary<Type, List<object>>();
        public static List<object> EnumGetValues(Type type) => _enumValuesCache.GetOrAdd(type, t => {
            NotOfTypeEnumException.AssertEnum(t);
            var retval = new List<object>();
            foreach (var o in System.Enum.GetValues(type)) {
                if (!"None".Equals(o.ToString())) retval.Add(o);
            }
            return retval;
        });

        public static List<T> EnumGetValues<T>() where T : struct, IFormattable, IConvertible, IComparable => // What we really would want is "where T : Enum"
            EnumGetValues<T>(exclude: (T)(object)0);

        private static ConcurrentDictionary<Type, object> _enumValuesTCache = new ConcurrentDictionary<Type, object>();
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exclude"></param>
        /// <returns></returns>
        public static List<T> EnumGetValues<T>(T exclude) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var temp = _enumValuesTCache.GetOrAdd(typeof(T), t => {
                var type = typeof(T);
                NotOfTypeEnumException.AssertEnum(type);
                var retval = new List<T>();

                foreach (var value in System.Enum.GetValues(type)) {
                    if (value.Equals(exclude)) continue;
                    retval.Add((T)value);
                }
                return retval;
            });
            return temp as List<T> ?? throw new InvalidObjectTypeException(temp, typeof(List<T>));
        }

        public static T EnumParse<T>(string _string) where T : struct, IFormattable, IConvertible, IComparable => EnumTryParse(_string, out T retval, out var errorResponse) ? retval : throw new InvalidEnumException<T>(_string + ". Details: " + errorResponse); // What we really would want is "where T : Enum"
        public static bool EnumTryParse<T>(string _string, out T result) where T : struct, IFormattable, IConvertible, IComparable => EnumTryParse(_string, out result, out _);  // What we really would want is "where T : Enum"
        /// <summary>
        /// Same as <see cref="Enum.TryParse{TEnum}"/> but will in addition check that: 
        /// 1) It is defined and (since <see cref="Enum.TryParse{TEnum}"/> returns true for all integers)
        /// 2) The int-value is not (0 and "None"). 
        ///    Note: All AgoRapide enums start with None in order to catch missing setting of values. 
        ///    This value is considered illegal in a parsing context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_string"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool EnumTryParse<T>(string _string, out T result, out string errorResponse) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (!typeof(T).IsEnum) throw new NotOfTypeEnumException<T>();
            result = default(T);
            if (!System.Enum.TryParse(_string, out result)) { errorResponse = "Not a valid " + typeof(T) + " (" + _string + ")"; return false; } // Duplicate code below
            if (!System.Enum.IsDefined(typeof(T), result)) { errorResponse = "!" + nameof(System.Enum.IsDefined) + " for " + typeof(T) + " (" + result + ")"; return false; } // Duplicate code below
            if (((int)(object)result) == 0 && result.ToString().Equals("None")) { errorResponse = "0 (None) is not allowed for " + typeof(T) + " (Note: All AgoRapide enums start with None in order to catch missing setting of values. This value is considered illegal in a parsing context)."; return false; } // Duplicate code below
            errorResponse = null;
            return true;
        }

        public static object EnumParse(Type type, string _string) => EnumTryParse(type, _string, out var retval, out var errorResponse) ? retval : throw new InvalidEnumException(type, _string + ". Details: " + errorResponse);
        public static bool EnumTryParse(Type type, string _string, out object result) => EnumTryParse(type, _string, out result, out _);
        /// <summary>
        /// See also generic version <see cref="EnumTryParse{T}"/> which most often is more practical
        /// (also see that version for documentation)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_string"></param>
        /// <returns></returns>
        public static bool EnumTryParse(Type type, string _string, out object result, out string errorResponse) {
            NotOfTypeEnumException.AssertEnum(type);
            try {
                result = System.Enum.Parse(type, _string);
            } catch (Exception) {
                result = null;
                errorResponse = "Not a valid " + type + " (" + _string + ")"; // Duplicate code above
                return false;
            }
            if (!System.Enum.IsDefined(type, result)) { errorResponse = "!" + nameof(System.Enum.IsDefined) + " for " + type + " (" + result + ")"; return false; } // Duplicate code above
            if (((int)result) == 0 && result.ToString().Equals("None")) { errorResponse = "0 (None) is not allowed for " + type + " (Note: All AgoRapide enums start with None in order to catch missing setting of values. This value is considered illegal in a parsing context)."; return false; } // Duplicate code above
            errorResponse = null;
            return true;
        }

        public static double DoubleParse(string strValue) => DoubleTryParse(strValue, out var retval) ? retval : throw new InvalidEnumException(strValue);
        public static bool DoubleTryParse(string strValue, out double dblValue) {
            if (strValue == null) {
                dblValue = 0;
                return false;
            }
            return double.TryParse(strValue.Replace(",", "."),
                System.Globalization.NumberStyles.AllowDecimalPoint |
                System.Globalization.NumberStyles.Number |
                System.Globalization.NumberStyles.AllowLeadingSign, Culture_en_US,
                out dblValue);
        }

        public class InvalidDoubleException : ApplicationException {
            public InvalidDoubleException(string message) : base(message) { }
            public InvalidDoubleException(string message, Exception inner) : base(message, inner) { }
        }

        private static ConcurrentDictionary<Type, ConcurrentDictionary<Type, (string, PropertyKeyWithIndex)>> _tToCorePCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, (string, PropertyKeyWithIndex)>>();
        public static PropertyKeyWithIndex MapTToCoreP<T>() => TryMapTToCoreP<T>(out var retval, out var errorResponse) ? retval : throw new InvalidMappingException<T>(errorResponse);
        public static bool TryMapTToCoreP<T>(out PropertyKeyWithIndex a) => TryMapTToCoreP<T>(out a, out _);
        /// <summary>
        /// Maps the type name of <typeparamref name="T"/> to a corresponding value for <see cref="CoreP"/> based on <see cref="PropertyKeyAttribute.Type"/>. 
        /// Example: See how enum-"class" <see cref="CoreAPIMethod"/> is linked to enum value <see cref="CoreP.CoreAPIMethod"/>
        /// 
        /// <see cref="BaseEntity.PVM{T}"/>
        /// <see cref="BaseEntity.TryGetPVM{T}(out T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public static bool TryMapTToCoreP<T>(out PropertyKeyWithIndex a, out string errorResponse) {
            if (typeof(T).Equals(typeof(CoreP))) throw new InvalidTypeException(typeof(T),
                "Attempt of mapping from " + typeof(T) + " to " + typeof(CoreP) + ". " +
                "A common cause is mistakenly calling " +
                nameof(BaseEntity) + "." + nameof(BaseEntity.PVM) + " / " + nameof(BaseEntity) + "." + nameof(BaseEntity.TryGetPVM) + " instead of " +
                nameof(BaseEntity) + "." + nameof(BaseEntity.PV) + " / " + nameof(BaseEntity) + "." + nameof(BaseEntity.TryGetPV) + " " +
                "(note for instance how the overload of " + nameof(BaseEntity.PVM) + " with defaultValue-parameter " +
                "looks very similar to " + nameof(BaseEntity.PV) + " if you forget the explicit type parameter for the latter method)");
            var mapping = _tToCorePCache.
                GetOrAdd(typeof(CoreP), type => new ConcurrentDictionary<Type, (string, PropertyKeyWithIndex)>()).
                GetOrAdd(typeof(T), type => {
                    /// NOTE: Note how <see cref="PropertyKeyMapper.AllCoreP"/> itself is cached but that should not matter
                    /// NOTE: as long as all enums are registered with <see cref="PropertyKeyMapper.MapEnum{T}"/> at application startup
                    var candidates = PropertyKeyMapper.AllCoreP.Where(key => key.Key.A.Type?.Equals(type) ?? false).ToList();
                    switch (candidates.Count) {
                        case 0:
                            return (
                        "No mapping exists from " + typeof(T).ToStringShort() + " to " + typeof(CoreP).ToStringShort() + "\r\n" +
                        "Possible cause (probably): No -" + nameof(EnumType) + "." + nameof(EnumType.PropertyKey) + "- has defined " + nameof(PropertyKeyAttribute.Type) + " = " + typeof(T).ToStringShort() + ".\r\n" +
                        "Possible cause (less probable): There is missing a call to " + nameof(PropertyKeyMapper) + "." + nameof(PropertyKeyMapper.MapEnum) + " in your Startup.cs.\r\n" +
                        "Possible resolution (less probable): Look for missing calls to \r\n" +
                        nameof(PropertyKeyMapper) + "." + nameof(PropertyKeyMapper.MapEnum) + "<...>()\r\n" +
                        "in your Startup.cs (as of Jun 2017 look for 'mapper1<...>').",
                        null);
                        case 1:
                            var key = candidates[0];
                            return (null, (key.PropertyKeyIsSet ? key.PropertyKeyWithIndex : key.PropertyKeyAsIsManyParentOrTemplate)); // Note how that last on may fail
                        default:
                            return (
                                "Multiple mappings exists from " + typeof(T).ToStringShort() + " to " + typeof(CoreP).ToStringShort() + ".\r\n" +
                                "The actual mappings found where:\r\n" + string.Join(", ", candidates.Select(c => c.Key.PToString + " (" + nameof(CoreP) + "." + c.Key.CoreP + ")")) + ".\r\n" +
                                /// TODO: Search for <see cref="System.Diagnostics.StackFrame"/> to see if tip below is relevant:
                                "Possible resolution: Call " + nameof(BaseEntity) + "." + nameof(BaseEntity.PV) + " instead of " + nameof(BaseEntity) + "." + nameof(BaseEntity.PVM) + ", that is, specify the actual " + nameof(CoreP) + " to use.",
                                null);
                    }
                });
            errorResponse = mapping.Item1;
            a = mapping.Item2;
            return a != null;
        }

        /// <summary>
        /// Encodings to be done 
        /// before <see cref="System.Web.HttpUtility.UrlEncode"/>  and
        /// after  <see cref="System.Web.HttpUtility.UrlDecode"/> 
        /// (in order words preEncoding could also be called postDecoding)
        /// 
        /// By using these additional encodings you get a greater range of characters that can be 
        /// used as parameters when using AgoRapide's REST API GET-syntax (instead of resorting to POST-syntax)
        /// </summary>
        public static List<(string unencoded, string encoded)> preEncoding = new List<(string unencoded, string encoded)> {
            ("+", "_PLUS_"),
            ("&", "_AMP_"),
            ("%", "_PERCENT_"),
            ("-","_DASH_"),
            (":","_COLON_"),
            ("/","_SLASH_"),
            ("#", "_HASH_"),
            (".","_STOP_"),
            ("?","_QUESTION_"),
            ("*", "_STAR_"),
            ("'", "_APOSTROPHE_"),
            (">", "_GREATER_"),
            ("<","_LESSER_")
        };

        public static string UrlEncode(string str) {
            preEncoding.ForEach(t => str = str.Replace(t.unencoded, t.encoded));
            return System.Web.HttpUtility.UrlEncode(str.Replace("+", "%20%"));
        }

        public static string UrlDecodeAdditional(string str) {
            preEncoding.ForEach(t => str = str.Replace(t.encoded, t.unencoded));
            return str;
        }

        private static long exceptionSerialNo = 0;
        /// <summary>
        /// Logs exception on disk, both in ordinary logfile (summary) and in separate file (detailed)
        /// Uses <see cref="ConfigurationAttribute.LogPath"/> as given through <see cref="Util.Configuration"/>
        /// 
        /// Fails silently if fails to write information to disk
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static void LogException(Exception ex) {
            var logPath = Configuration.C.LogPath;
            if (string.IsNullOrWhiteSpace(logPath)) {
                // Will most probably not happen since Configuration.LogPath has a default value
                // Give up totally. You might want to add some code here
                return;
            }
            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Note "hard coded" date formats since the resulting filename has to be guaranteed to be valid
            var text =
                    timeStamp + ":" +
                    "Thread " + GetCurrentThreadIdWithName() + ":\r\n\r\n" +
                    GetExeptionDetails(ex) + "\r\n\r\n";

            logPath = InsertDateTimeIntoLogPath(logPath);

            var serialNo = System.Threading.Interlocked.Increment(ref exceptionSerialNo);
            var writer = new Action(() => {
                var exceptionPath = logPath + "_" + "Exception " + timeStamp.Replace(":", ".") + " " + (serialNo).ToString("0000") + " " + ex.GetType().ToStringShort().Replace("<", "{").Replace(">", "}") + ".txt";
                System.IO.File.WriteAllText(exceptionPath, text + "\r\n");
                Log(null, "\r\n--\r\nAn exception of type " + ex.GetType().ToStringShort() + " occurred.\r\nSee\r\n" + exceptionPath + "\r\nfor details\r\n--\r\n");
            });
            try {
                writer();
            } catch (System.IO.DirectoryNotFoundException) {
                try {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    writer();
                } catch (Exception) {
                    // Give up totally. You might want to add some code here
                }
            } catch (Exception) {
                // Give up totally. You might want to add some code here                
            }
        }

        [ClassMember(Description =
            "Follows the chain of " + nameof(Exception.InnerException) + " and returns types as comma-separated string"
        )]
        public static string GetExceptionChainAsString(Exception ex) {
            var retval = new StringBuilder();
            while (ex != null) {
                if (retval.Length > 0) retval.Append(", ");
                retval.Append(ex.GetType().ToStringShort());
                ex = ex.InnerException;
            }
            return retval.ToString();
        }

        /// <summary>
        /// Gives as much information about exception as possible.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetExeptionDetails(Exception ex) {
            var msg = new StringBuilder();
            msg.AppendLine(GetExceptionChainAsString(ex));
            while (ex != null) {
                msg.AppendLine("Exception: " + ex.GetType().ToStringShort() + "\r\n");
                msg.AppendLine("Message: " + ex.Message.ToString() + "\r\n");
                if (ex.Data != null && ex.Data.Count > 0) {
                    msg.Append("Data:\r\n");
                    foreach (var temp in ex.Data) {
                        var e = (System.Collections.DictionaryEntry)temp; // Going through "var temp" in order to escape compilator warning (Dec 2016)
                        msg.Append(e.Key.ToString() + ": " + e.Value.ToString() + "\r\n");
                    }
                    msg.AppendLine();
                }
                msg.AppendLine("Source : " + ex.Source + "\r\n");

                if (ex is AggregateException) {
                    msg.AppendLine("=================");
                    msg.AppendLine(ex.GetType().ToStringShort() + ", details will only be shown for the first inner Exception. All InnerExceptions are:");
                    ((AggregateException)ex).InnerExceptions.ForEach(inner => {
                        msg.AppendLine(inner.GetType().ToStringShort() + ": " + inner.Message);
                    });
                    msg.AppendLine("=================");
                }

                var stackTrace = ex.StackTrace + "";
                Configuration.C.SuperfluousStackTraceStrings.ForEach(s => stackTrace = stackTrace.Replace(s, ""));

                // Remove parameters. We assume that line numbers are sufficient.
                var start = stackTrace.IndexOf("(");
                while (start > -1) {
                    var slutt = stackTrace.IndexOf(")", start);
                    if (slutt == -1) break;
                    stackTrace = stackTrace.Substring(0, start) + stackTrace.Substring(slutt + 1);
                    start = stackTrace.IndexOf("(");
                }

                msg.AppendLine("Stacktrace: " + stackTrace.Replace(" at ", "\r\n\r\nat ").Replace(" in ", "\r\nin ") + "\r\n");

                ex = ex.InnerException;
                if (ex != null) {
                    msg.AppendLine();
                    msg.AppendLine("INNER EXCEPTION:");
                }
            }
            msg.AppendLine();
            msg.AppendLine("-----------------------------------------------------------");

            lock (lastLogData) {
                msg.AppendLine("Last log-data (" + lastLogData.Count + " items (sorted by newest first))");
                msg.AppendLine("-----------------------------------------------------------");
                lastLogData.Reverse().ForEach(l => msg.Append(l));
            }
            return msg.ToString();
        }

        public static string GetCurrentThreadIdWithName() {
            var threadId = (System.Threading.Thread.CurrentThread.ManagedThreadId).ToString();
            var name = System.Threading.Thread.CurrentThread.Name;
            if (!string.IsNullOrEmpty(name)) {
                threadId += "(" + name + ")";
            }
            return threadId;
        }

        [ClassMember(Description = "Contains the latest log-data seen by -" + nameof(Log) + "-. Used by -" + nameof(LogException) + "-.")]
        private static LinkedList<string> lastLogData = new LinkedList<string>();

        /// <summary>
        /// Log-elements waiting to be written to disc
        /// By writing at regular intervals we reduce dramatically the load on the disc
        /// See also LOGGER_THREAD_SLEEP_PERIOD 
        /// </summary>
        private static ConcurrentQueue<(string path, string text)> logQueue = new ConcurrentQueue<(string path, string text)>();
        private static long loggerThreadIsRunning = 0;

        private static string InsertDateTimeIntoLogPath(string logPath) => logPath.Replace("[DATE]", DateTime.Now.ToString("yyyy-MM-dd")).Replace("[DATE_HOUR]", DateTime.Now.ToString("yyyy-MM-dd_HH")); // Note "hard coded" date formats since the resulting filename has to be guaranteed to be valid

        /// <summary>
        /// Logs a specific event (parameter text) to <see cref="ConfigurationAttribute.LogPath"/> given through <see cref="Util.Configuration"/>
        /// 
        /// Note delayed logging through separate thread. This might be a concern in cases where the application terminates abruptly 
        /// because you risk loosing log information explaining events leading up to the application terminating. 
        /// </summary>
        /// <param name="category">
        /// Usually null. 
        /// If set then logging will be done twice, once in ordinary <see cref="ConfigurationAttribute.LogPath"/> and once in 
        /// <see cref="ConfigurationAttribute.LogPath"/> extended with <paramref name="category"/>
        /// Typically used at startup when separate log-files is desired and by <see cref="BasicAuthenticationAttribute"/>. Apart from that not in general use in AgoRapide. 
        /// </param>
        /// <param name="text"></param>
        public static void Log(string category, string text) {
            var logPath = Configuration.C.LogPath;
            if (string.IsNullOrWhiteSpace(logPath)) {
                // Will most probably not happen since Configuration.LogPath has a default value
                // Give up totally. You might want to add some code here
                return;
            }

            var logText =
                DateTime.Now.ToString(DateTimeFormat.DateHourMinSecMs) + ": " +
                GetCurrentThreadIdWithName() + ": " +
                text + "\r\n";
            lock (lastLogData) {
                lastLogData.AddLast(logText);
                if (lastLogData.Count > Configuration.C.LAST_LOG_DATA_MAX_SIZE) lastLogData.RemoveFirst();
            }

            logQueue.Enqueue((logPath, logText));
            if (!string.IsNullOrEmpty(category)) {
                logQueue.Enqueue((logPath + "_" + category + ".txt", logText));
            }

            var wasRunning = System.Threading.Interlocked.Exchange(ref loggerThreadIsRunning, 1);
            if (wasRunning == 0) System.Threading.Tasks.Task.Factory.StartNew(() => {               // Start separate thread for actual logging to disk (the idea is to reduce dramatically the number of disk I/O-operations needed)
                try {
                    while (true) {                                                                  // Empty queue continuously, use a separate StringBuilder for each logPath
                        var allLogData = new Dictionary<string, StringBuilder>();                   // This code is a bit hastily thrown together. We could most probably increase performance here, especially for applications where logPath never changes.
                        while (logQueue.TryDequeue(out var i)) { // (building allLogData is a bit expensive)
                            if (!allLogData.TryGetValue(i.path, out var logData)) allLogData.Add(i.path, logData = new StringBuilder());
                            logData.Append(i.text);
                        }
                        allLogData.ForEach(e => System.IO.File.AppendAllText(InsertDateTimeIntoLogPath(e.Key), e.Value.ToString()));
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                } finally {
                    loggerThreadIsRunning = 0;
                }
            });  
        } 

        public static System.Security.Cryptography.MD5 MD5 = System.Security.Cryptography.MD5.Create();
        /// <summary>
        /// See good article about salting and hashing at https://crackstation.net/hashing-security.htm
        /// (and try their cracker at https://crackstation.net/)
        ///
        /// Our implementation is more pragmatic than the article above. 
        /// For instance <paramref name="salt"/> just equals <see cref="DBField.pid"/> for instance (but note how the range of <see cref="DBField.pid"/> is quite large since it is 
        /// shared with all properties in the database, meaning it is somewhat difficult to guess the <see cref="DBField.pid"/> of a single entity). 
        /// MD5 is chosen because it performs better than SHA256 and because collisions do not play any practical role. 
        /// (especially important if you use Basic authentication instead of OAuth 2.0 or similar, 
        /// although that is of course really not recommended anyway)
        /// </summary>
        /// <param name="salt">Usually the <see cref="DBField.pid"/> of the actual password-property</param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GeneratePasswordHashWithSalt(long salt, string password) {
            if (string.IsNullOrEmpty(password)) throw new InvalidPasswordException("Null or empty " + nameof(password) + " not allowed");
            lock (MD5) { // ComputeHash is not thread safe, will return wrong results if simultaneous access.  // TODO: Maybe call System.Security.Cryptography.MD5.Create() each time, in order to avod locking here.
                return string.Join("", MD5.ComputeHash(Encoding.UTF8.GetBytes(salt.ToString() + "_" + password)).Select(h => h.ToString("X2")));
            }
        }

        public static ConcurrentDictionary<string, Type> _typeToStringCache = new ConcurrentDictionary<string, Type>();
        public static Type GetTypeFromString(string strType) => TryGetTypeFromString(strType, out var retval) ? retval : throw new InvalidTypeException(strType);
        /// <summary>
        /// <see cref="Extensions.ToStringDB"/> corresponds to <see cref="Util.TryGetTypeFromString"/>
        /// Now how types in <see cref="APIMethod.AllBaseEntityDerivedTypes"/> are understood also in a short-hand form. 
        /// 
        /// See <see cref="Extensions.ToStringDB"/> for documentation. 
        /// 
        /// Throws exception if invalid syntax for strType. 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool TryGetTypeFromString(string strType, out Type type) {
            type = _typeToStringCache.GetOrAdd(strType, s => {
                var t = s.Split(':'); // Remove result of ToStringShort placed in front of strType. 
                switch (t.Length) {
                    case 1:
                        s = t[0].Trim();
                        if ("Entity".Equals(s)) return typeof(BaseEntity);
                        var candidates = APIMethod.AllBaseEntityDerivedTypes.Where(e => s.Equals(e.ToStringVeryShort()) || s.Equals(e.ToStringShort()) || s.Equals(e.ToString())).ToList();
                        switch (candidates.Count) {
                            case 0: break; /// Finding type as a <see cref="BaseEntity"/>-type did not succeed. Continue in normal manner.
                            case 1: return candidates[0];
                            default: throw new InvalidCountException(candidates.Count, 1, "Multiple " + nameof(APIMethod.AllBaseEntityDerivedTypes) + " corresponds to '" + strType + "'. (" + string.Join(",", candidates.Select(e => e.ToStringDB())) + ")");
                        }
                        break;
                    case 2: s = t[1].Trim(); break;
                    default: throw new InvalidTypeException(s, "Invalid number of colons : (" + (t.Length - 1) + ")");
                }
                if (!s.Contains('[')) return Type.GetType(s);
                if (!s.TryExtract("[", "]", out var genericArguments)) throw new InvalidTypeException(s, nameof(genericArguments) + " not found between [ and ]");
                var genericBaseTypeAndAssemblyName = s.Replace("[" + genericArguments + "]", "");
                t = genericBaseTypeAndAssemblyName.Split(',');
                switch (t.Length) {
                    case 2:
                        var genericBaseType = Type.GetType(t[0].Trim() + "," + t[1].Trim());
                        if (genericBaseType == null) return null;
                        t = genericArguments.Split(',');
                        switch (t.Length) {
                            case 2:
                                var genericArgument = Type.GetType(genericArguments);
                                if (genericArgument == null) return null;
                                return genericBaseType.MakeGenericType(new Type[] { genericArgument });
                            default: throw new InvalidTypeException(s, "Invalid number of commas : (" + (t.Length - 1) + ") (assembly name not found in " + nameof(genericArguments) + " '" + genericArguments + "') (code only handles one generic argument at present)");
                        }
                    default: throw new InvalidTypeException(s, "Invalid number of commas : (" + (t.Length - 1) + ") (assembly name not found in " + nameof(genericBaseTypeAndAssemblyName) + " '" + genericBaseTypeAndAssemblyName + "')");
                }
            });
            return type != null;
        }

        /// <summary>
        /// Converts a generic list into a <see cref="Property.IsIsManyParent"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="list">Any </param>
        /// <param name="detailer">May be null</param>
        /// <returns></returns>
        public static Property ConvertListToIsManyParent(BaseEntity parent, PropertyKey key, object list, Func<string> detailer) {
            var t = list?.GetType() ?? throw new NullReferenceException(nameof(list));
            InvalidTypeException.AssertList(t, key, () => detailer());
            // Replace all existing values with values in list.
            var retval = Property.CreateIsManyParent(key);
            foreach (var v in (System.Collections.IList)list) {
                var id = retval.GetNextIsManyId();
                var property = (Property)Activator.CreateInstance(
                    typeof(PropertyT<>).MakeGenericType(new Type[] { key.Key.A.Type }),
                    id,
                    v
                );
                property.ParentId = parent.Id;
                property.Parent = parent;
                // We can not do this:
                // isManyParent.Properties.Add(id.IndexAsCoreP, test);
                // but must do this:
                /// TODO: Make a class called PropertyIsManyParent instead of using <see cref="IsIsManyParent" />
                retval.AddPropertyForIsManyParent(id.IndexAsCoreP, property); // Important in order for cached _value to be reset
            }
            return retval;
        }

        [ClassMember(Description =
            "Returns an object that can contain -" + nameof(BaseEntity.Properties) + "-." +
            "Serves the purpose of getting access to -" + nameof(BaseEntity.AddProperty) + "- in order to generate a properties collection.")]
        public static BaseEntity GetNewPropertiesParent() => new PropertyT<string>(PropertyP.PropertyValue.A().PropertyKeyWithIndex, "");

        /// <summary>
        /// Practical property that facilitates placement of breakpoints in expressions in addition to statements
        /// 
        /// Insert this wherever you throw an exception in expressions, especially when using the ? operator
        ///   {boolean expression} ? {some return value} : throw SomeException(Util.BreakpointEnabler + {Your original exception message} 
        /// 
        /// At regular intervals you can remove all uses of this method in the code.
        /// </summary>
        public static string BreakpointEnabler => ""; // <--- Place breakpoint here <---

        private static long lastId;
        /// <summary>
        /// Generic provider of unique ids. Use on HTML-pages for instance.
        /// </summary>
        /// <returns></returns>
        public static long GetNextId() => System.Threading.Interlocked.Increment(ref lastId);
    }

    public class InvalidPasswordException : Exception {
        public InvalidPasswordException(string message) : base(message) { }
    }

    public class NotNullReferenceException : ApplicationException {
        public static void AssertNotNull(object obj) {
            if (obj != null) throw new NotNullReferenceException(obj.GetType() + ". Details: " + obj.ToString());
        }
        public NotNullReferenceException(string message) : base(message) { }
        public NotNullReferenceException(string message, Exception inner) : base(message, inner) { }
    }

    public class EmptyStringException : ApplicationException {
        public EmptyStringException(string message) : base(message) { }
        public EmptyStringException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidMappingException : ApplicationException {
        public InvalidMappingException(string message) : base(message) { }
    }

    public class InvalidMappingException<T> : ApplicationException {
        public InvalidMappingException(string message) : base("Unable to map from " + typeof(T).ToString() + " to " + nameof(CoreP) + ".\r\nDetails:\r\n" + message) { }
        public InvalidMappingException(T _enum, string message) : base("Unable to map from " + _enum.GetType() + "." + _enum.ToString() + " to " + nameof(CoreP) + ".\r\nDetails:\r\n" + message) { }
    }

    public class InvalidMappingException<T, TProperty> : ApplicationException
    where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where TProperty : Enum"
        public InvalidMappingException(string message) : base(
            "It is not possible to map from " + typeof(T).ToStringShort() + " to " + typeof(TProperty).ToStringShort() + ".\r\n" +
            "Explanation: Exact one of the enum values for " + typeof(TProperty).ToStringShort() + " must specify\r\n" +
            "   [" + nameof(PropertyKeyAttribute) + "(" + nameof(PropertyKeyAttribute.Type) + " = typeof(" + typeof(T).ToStringShort() + "))]\r\n" +
            "\r\nDetails:\r\n" + message) { }
    }

    public class KeyAlreadyExistsException : ApplicationException {
        public KeyAlreadyExistsException(string message) : base(message) { }
        public KeyAlreadyExistsException(object key, string collectionName) : base("The key '" + key + "' already exists in collection " + collectionName) { }
        public KeyAlreadyExistsException(object key, string collectionName, string details) : base("The key '" + key + "' already exists in collection " + collectionName + ".\r\nDetails: " + details) { }
    }

    public class KeyAlreadyExistsException<T> : ApplicationException where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public KeyAlreadyExistsException(T key) : this(key, null) { }
        public KeyAlreadyExistsException(T key, string message) : base("The key " + key.GetEnumValueAttribute().EnumValueExplained + " already exists" + (string.IsNullOrEmpty(message) ? "" : (". Details: " + message))) { }
    }

    public class SingleObjectNotFoundOrMultipleFoundException : ApplicationException {
        public SingleObjectNotFoundOrMultipleFoundException(string message) : base(message) { }
        public SingleObjectNotFoundOrMultipleFoundException(string message, Exception inner) : base(message, inner) { }
    }

    public class MultipleObjectsFoundException : ApplicationException {
        public MultipleObjectsFoundException(string message) : base(message) { }
        public MultipleObjectsFoundException(string message, Exception inner) : base(message, inner) { }
    }

    public class UnexpectedStateException : ApplicationException {
        public UnexpectedStateException(string message) : base(message) { }
        public UnexpectedStateException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidEnumException : ApplicationException {
        public static void AssertDefined<T>(T _enum) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (!System.Enum.IsDefined(typeof(T), _enum)) throw new InvalidEnumException(_enum, "Not defined");
            if (((int)(object)_enum) == 0) throw new InvalidEnumException(_enum, "int value is 0");
        }
        private static string GetMessage(object _enum, string message) => "Invalid / unknown value for enum (" + _enum.GetType().ToString() + "." + _enum.ToString() + ")." + (string.IsNullOrEmpty(message) ? "" : ("\r\nDetails: " + message));
        public InvalidEnumException(object _enum) : base(GetMessage(_enum, null)) { }
        public InvalidEnumException(object _enum, string message) : base(GetMessage(_enum, message)) { }
        public InvalidEnumException(Type type, string _string) : base("Unable to parse '" + _string + "' as " + type) { }
        public InvalidEnumException(Type type, string _string, string details) : base("Unable to parse '" + _string + "' as " + type + ".\r\nDetails:\r\n" + details) { }
    }

    public class InvalidEnumException<T> : ApplicationException {
        public InvalidEnumException(string _string) : base("Value '" + _string + "' is not valid for Enum " + typeof(T).ToString()) { }
    }

    public class InvalidObjectTypeException : ApplicationException {
        /// <summary>
        /// Asserts that expectedType.IsAssignableFrom(foundObject.GetType())
        /// TODO: Move this to somewhere else maybe?
        /// </summary>
        /// <param name="foundObject"></param>
        /// <param name="expectedType"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        public static void AssertAssignable(object foundObject, Type expectedType, Func<string> detailer = null) {
            if (foundObject == null) throw new NullReferenceException(nameof(foundObject) + ". (" + nameof(expectedType) + ": " + expectedType + ")" + detailer.Result("\r\nDetails: "));
            if (expectedType == null) throw new NullReferenceException(nameof(expectedType) + ". (" + nameof(foundObject) + ": " + foundObject + ")" + detailer.Result("\r\nDetails: "));
            if (!expectedType.IsAssignableFrom(foundObject.GetType())) throw new InvalidObjectTypeException(foundObject, expectedType, detailer.Result(""));
        }

        /// <summary>
        /// Asserts that expectedType.Equals(foundObject.GetType())
        /// TODO: Move this to somewhere else maybe?
        /// </summary>
        /// <param name="foundObject"></param>
        /// <param name="expectedType"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        public static void AssertEquals(object foundObject, Type expectedType, Func<string> detailer) {
            if (foundObject == null) throw new NullReferenceException(nameof(foundObject) + ". (" + nameof(expectedType) + ": " + expectedType + ")" + detailer.Result("\r\nDetails: "));
            if (expectedType == null) throw new NullReferenceException(nameof(expectedType) + ". (" + nameof(foundObject) + ": " + foundObject + ")" + detailer.Result("\r\nDetails: "));
            if (!expectedType.Equals(foundObject.GetType())) throw new InvalidObjectTypeException(foundObject, expectedType, detailer.Result(""));
        }

        private static string GetMessage(object _object, string message) => "Invalid / unknown type of object (" + _object.GetType().ToString() + "). Object: '" + _object.ToString() + "'." + (string.IsNullOrEmpty(message) ? "" : ("\r\nDetails: " + message));
        public InvalidObjectTypeException(object _object) : base(GetMessage(_object, null)) { }
        public InvalidObjectTypeException(object _object, Type typeExpected) : base(GetMessage(_object, "Expected object of type " + typeExpected + " but got object of type " + _object.GetType() + " instead")) { }
        public InvalidObjectTypeException(object _object, Type typeExpected, string message) : base(GetMessage(_object, "Expected object of type " + typeExpected + " but got object of type " + _object.GetType() + " instead.\r\nDetails: " + message)) { }
        public InvalidObjectTypeException(object _object, string message) : base(GetMessage(_object, message)) { }
    }

    public class InvalidIdentifierException : ApplicationException {
        public static System.CodeDom.Compiler.CodeDomProvider CSharpCodeDomProvider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("C#");

        public static bool TryAssertValidIdentifier(string identifier, out string errorResponse) {
            if (!CSharpCodeDomProvider.IsValidIdentifier(identifier)) {
                errorResponse = nameof(identifier) + " '" + identifier + "' is not valid as a C# identifier.";
                return false;
            }
            errorResponse = null;
            return true;
        }

        public static void AssertValidIdentifier(string identifier) {
            if (!CSharpCodeDomProvider.IsValidIdentifier(identifier)) throw new InvalidIdentifierException(identifier);
        }
        public static void AssertValidIdentifier(string identifier, Func<string> detailer) {
            if (!CSharpCodeDomProvider.IsValidIdentifier(identifier)) throw new InvalidIdentifierException(identifier, detailer.Result(""));
        }
        public InvalidIdentifierException(string identifier) : this(identifier, null) { }
        public InvalidIdentifierException(string identifier, string details) : base(
            nameof(identifier) + " '" + identifier + "' is not valid as a C# identifier." +
            (string.IsNullOrEmpty(details) ? "" : "\r\nDetails: " + details)
        ) { }
    }

    public class InvalidTypeException : ApplicationException {
        /// <summary>
        /// Asserts that expectedType.IsAssignableFrom(foundType)
        /// TODO: Move this to somewhere else maybe?
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        public static void AssertAssignable(Type foundType, Type expectedType, Func<string> detailer = null) {
            if (foundType == null) throw new NullReferenceException(nameof(foundType) + ". (" + nameof(expectedType) + ": " + expectedType + ")" + detailer.Result("\r\nDetails: "));
            if (expectedType == null) throw new NullReferenceException(nameof(expectedType) + ". (" + nameof(foundType) + ": " + foundType + ")" + detailer.Result("\r\nDetails: "));
            if (!expectedType.IsAssignableFrom(foundType)) throw new InvalidTypeException(foundType, expectedType, detailer.Result(""));
        }

        /// <summary>
        /// Asserts that expectedType.Equals(foundType)
        /// TODO: Move this to somewhere else maybe?
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        /// <param name="detailer">
        /// May be null
        /// Used to give details in case of an exception being thrown
        /// </param>
        public static void AssertEquals(Type foundType, Type expectedType, Func<string> detailer) {
            if (foundType == null) throw new NullReferenceException(nameof(foundType) + ". (" + nameof(expectedType) + ": " + expectedType + ")" + detailer.Result("\r\nDetails: "));
            if (expectedType == null) throw new NullReferenceException(nameof(expectedType) + ". (" + nameof(foundType) + ": " + foundType + ")" + detailer.Result("\r\nDetails: "));
            if (!expectedType.Equals(foundType)) throw new InvalidTypeException(foundType, expectedType, detailer.Result(""));
        }

        /// <summary>
        /// Asserts that <paramref name="type"/> is a generic List 
        /// compatible with <see cref="Key"/> (compatible with <see cref="PropertyKeyAttribute.Type"/>)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        public static void AssertList(Type type, PropertyKey key, Func<string> detailer) {
            if (type == null) throw new NullReferenceException(nameof(type) + detailer.Result("\r\nDetails: "));
            if (key == null) throw new NullReferenceException(nameof(key) + detailer.Result("\r\nDetails: "));
            if (!type.GetGenericTypeDefinition().Equals(typeof(List<>))) throw new InvalidTypeException(type, "Only GetGenericTypeDefinition List is allowed for IsGenericType" + detailer.Result("\r\nDetails: "));
            if (type.GenericTypeArguments.Length != 1) throw new InvalidTypeException(type, "Only 1 GenericTypeArguments allowed, not " + type.GenericTypeArguments.Length + detailer.Result("\r\nDetails: "));
            AssertAssignable(type.GenericTypeArguments[0], key.Key.A.Type, () => "Generic type requested was " + type + detailer.Result("\r\nDetails: "));
        }

        public InvalidTypeException(string typeFound) : this(typeFound, null) { }
        public InvalidTypeException(string typeFound, string details) : base("Unable to reconstruct type based on " + nameof(typeFound) + " (" + typeFound + "). Possible cause (if " + nameof(typeFound) + " originates from database and is result of " + nameof(AgoRapide.Core.Extensions.ToStringDB) + "): Assembly name may have changed since storing in database" + (string.IsNullOrEmpty(details) ? "" : (". Details: " + details))) { }
        public InvalidTypeException(Type type) : base("Type:" + type.ToStringShort()) { }
        public InvalidTypeException(Type type, string details) : base("Type: " + type.ToStringShort() + ", " + nameof(details) + ": " + details) { }
        /// <summary>
        /// TODO: CHANGE ORDERING OF FOUND/EXPECTED
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        public InvalidTypeException(Type foundType, Type expectedType) : base(nameof(expectedType) + ": " + expectedType.ToString() + ",\r\n" + nameof(foundType) + ": " + foundType) { }
        /// <summary>
        /// TODO: CHANGE ORDERING OF FOUND/EXPECTED
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        /// <param name="details"></param>
        public InvalidTypeException(Type foundType, Type expectedType, string details) : base(nameof(expectedType) + ": " + expectedType.ToString() + ",\r\n" + nameof(foundType) + ": " + foundType + ",\r\n" + nameof(details) + ": " + details) { }
    }

    /// <summary>
    /// As of March 2017 support of <see cref="int"/> has been deliberately left out of AgoRapide due to
    /// conversion issues and use of <see cref="Property.ADotTypeValue"/>. 
    /// If it is desired in future to support <see cref="int"/> anyway then you may just look up 
    /// all uses <see cref="TypeIntNotSupportedByAgoRapideException"/> for 
    /// information about where to change the code. 
    /// </summary>
    public class TypeIntNotSupportedByAgoRapideException : ApplicationException {
        private const string message = "Resolution: Use long instead of int";
        public TypeIntNotSupportedByAgoRapideException() : base(message) { }
        public TypeIntNotSupportedByAgoRapideException(string details) : base(message + "\r\nDetails: " + details) { }
    }

    public class UnexpectedNumberOfRowsException : Exception {
        /// <summary>
        /// TODO: CHANGE ORDERING OF FOUND/EXPECTED
        /// </summary>
        /// <param name="foundRows"></param>
        /// <param name="expectedRows"></param>
        public UnexpectedNumberOfRowsException(int foundRows, int expectedRows) : base(nameof(foundRows) + ": " + foundRows + ", " + nameof(expectedRows) + ": " + expectedRows) { }
    }

    public class NotOfTypeEnumException : ApplicationException {
        public static void AssertEnum(Type type) {
            if (!type.IsEnum) throw new NotOfTypeEnumException(type);
        }
        public static void AssertEnum(Type type, Func<string> detailer) {
            if (!type.IsEnum) throw new NotOfTypeEnumException(type, detailer());
        }
        public NotOfTypeEnumException(Type type) : base("Expected Type.IsEnum but got type " + type.ToString()) { }
        public NotOfTypeEnumException(Type type, string details) : base("Expected Type.IsEnum but got type " + type.ToString() + ".\r\nDetails: " + details) { }
    }

    public class NotOfTypeEnumException<T> : ApplicationException {
        public NotOfTypeEnumException() : base("Expected Type.IsEnum but got type " + typeof(T).ToString()) { }
    }

    public class OfTypeEnumException : ApplicationException {
        public static void AssertNotEnum(Type type) {
            if (type.IsEnum) throw new OfTypeEnumException(type);
        }
        public static void AssertNotEnum(Type type, Func<string> detailer) {
            if (type.IsEnum) throw new OfTypeEnumException(type, detailer());
        }
        public OfTypeEnumException(Type type) : base("Expected !Type.IsEnum but got type " + type.ToString()) { }
        public OfTypeEnumException(Type type, string details) : base("Expected !Type.IsEnum but got type " + type.ToString() + ".\r\nDetails: " + details) { }
    }

    public class UnknownEnvironmentException : ApplicationException {
        public UnknownEnvironmentException(string message) : base(message) { }
        public UnknownEnvironmentException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidProtocolException : ApplicationException {
        public InvalidProtocolException(string message) : base(message) { }
        public InvalidProtocolException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// TODO: Not in use as of Jan 2017
    /// </summary>
    public class UnknownExceptionWhenLoggingException : ApplicationException {
        public UnknownExceptionWhenLoggingException(string message, Exception inner) : base(message, inner) { }
    }
}
