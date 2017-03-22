﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;


namespace AgoRapide.Core {

    /// <summary>
    /// This class should be as small as possible!
    /// 
    /// TODO: Always look out for functionality here that could be moved elsewhere    
    /// </summary>
    public static class Util {

        public static string DEFAULT_LOG_NAME => "AgoRapideLog_[DATE_HOUR].txt";

        /// <summary>
        /// You should definitely set this instance yourself at startup because 
        /// this default instance has two major fault:
        /// 1) The automatic generated links to API methods will not work at all since we do not
        ///    have any sensible value for <see cref="Configuration.RootUrl"/>.
        /// 2) Logging will most probably not work
        /// </summary>
        public static Configuration Configuration { get; set; } = new Configuration(
            logPath: @"c:\AgoRapideLog_[DATE_HOUR].txt", // TODO: Find a better default value than this
            rootUrl: "[RootUrlNotSetInAgoRapideConfiguration]"
        );

        /// <summary>
        /// Note that .None will not be returned.
        /// You probably do not need this method. Most probably you need the generic Enum.GetValues
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<string> EnumGetValues(Type type) {
            if (!type.IsEnum) throw new NotOfTypeEnumException(type);
            return Enum.GetNames(type).Where(s => !"None".Equals(s)).ToList();
        }

        public static List<T> EnumGetValues<T>() where T : struct, IFormattable, IConvertible, IComparable => // What we really would want is "where T : Enum"
            EnumGetValues<T>(exclude: (T)(object)0);

        public static List<T> EnumGetValues<T>(T exclude) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            var type = typeof(T);
            if (!type.IsEnum) throw new NotOfTypeEnumException(type);
            var retval = new List<T>();

            foreach (T value in Enum.GetValues(type)) {
                if (value.Equals(exclude)) continue;
                retval.Add(value);
            }
            return retval;
        }

        public static T EnumParse<T>(string _string) where T : struct, IFormattable, IConvertible, IComparable => // What we really would want is "where T : Enum"
            EnumTryParse(_string, out T retval) ? retval : throw new InvalidEnumException<T>(_string);

        /// <summary>
        /// Same as <see cref="Enum.TryParse{TEnum}"/> but will in addition check that: 
        /// 1) It is defined and (since <see cref="Enum.TryParse{TEnum}"/> returns true for all integers)
        /// 2) The int-value is not 0 (we assume that None = 0 is the first defined element 
        ///    for all enums and that this value is not to be accepted as valid)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_string"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool EnumTryParse<T>(string _string, out T result) where T : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (!typeof(T).IsEnum) throw new NotOfTypeEnumException<T>();
            result = default(T);
            if (!Enum.TryParse(_string, out result)) return false;
            if (!Enum.IsDefined(typeof(T), result)) return false;
            if (((int)(object)result) == 0) return false;
            return true;
        }

        ///// <summary>
        ///// Same as <see cref="EnumTryParse{T}(string, out T)"/> except with less restrictions for T
        ///// TODO: Consider removing
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="_string"></param>
        ///// <param name="result"></param>
        ///// <returns></returns>
        //public static bool EnumTryParse2<T>(string _string, out T result) where T : struct {
        //    if (!typeof(T).IsEnum) throw new NotOfTypeEnumException<T>();
        //    result = default(T);
        //    if (!Enum.TryParse(_string, out result)) return false;
        //    if (!Enum.IsDefined(typeof(T), result)) return false;
        //    if (((int)(object)result) == 0) return false;
        //    return true;
        //}

        /// <summary>
        /// See also generic version <see cref="EnumParse{T}"/> which most often is more practical
        /// </summary>
        /// <param name="type"></param>
        /// <param name="_string"></param>
        /// <returns></returns>
        public static object EnumParse(Type type, string _string) => EnumTryParse(type, _string, out var retval) ? retval : throw new InvalidEnumException(type, _string);

        /// <summary>
        /// See also generic version <see cref="EnumTryParse{T}"/> which most often is more practical
        /// (also see that version for documentation)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_string"></param>
        /// <returns></returns>
        public static bool EnumTryParse(Type type, string _string, out object result) {
            if (!type.IsEnum) throw new NotOfTypeEnumException(type);
            try {
                result = Enum.Parse(type, _string);
            } catch (Exception) {
                result = null; return false;
            }
            if (!Enum.IsDefined(type, result)) return false;
            if (((int)result) == 0) return false;
            return true;
        }

        /// <summary>
        /// Would typically contain only one key, typeof(TProperty) (usually called P)
        /// value has a key for each 
        /// typeof(T) which points to the actual value of TProperty   OR to an errorResponse
        /// like 
        /// typeof(Money) pointing to P.Money)                        OR typeof(Money) pointing to "No mapping exists from Money to P"
        /// </summary>
        private static ConcurrentDictionary<Type, ConcurrentDictionary<Type, object>> tToTPropertyMappings = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, object>>();
        /// <summary>
        /// Maps the type name of <typeparamref name="T"/> to a corresponding value for <typeparamref name="TProperty"/>
        /// See 
        /// <see cref="BaseEntityT{TProperty}.PVM{T}"/>
        /// <see cref="BaseEntityT{TProperty}.TryGetPVM{T}(out T)"/>
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static TProperty MapTToTProperty<T, TProperty>() where TProperty : struct, IFormattable, IConvertible, IComparable => TryMapTToTProperty<T, TProperty>(out var retval, out var errorResponse) ? retval : throw new InvalidMappingException<T, TProperty>(errorResponse);

        /// <summary>
        /// Maps the type name of <typeparamref name="T"/> to a corresponding value for <typeparamref name="TProperty"/>
        /// See 
        /// <see cref="BaseEntityT{TProperty}.PVM{T}"/>
        /// <see cref="BaseEntityT{TProperty}.TryGetPVM{T}(out T)"/>
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="tProperty"></param>
        /// <returns></returns>
        public static bool TryMapTToTProperty<T, TProperty>(out TProperty tProperty) where TProperty : struct, IFormattable, IConvertible, IComparable => TryMapTToTProperty<T, TProperty>(out tProperty, out _);

        /// <summary>
        /// Maps the type name of <typeparamref name="T"/> to a corresponding value for <typeparamref name="TProperty"/>
        /// Note how result is cached. 
        /// Now how also looks into <see cref="CoreProperty"/>-values silently mapped by <see cref="CorePropertyMapper{TProperty}"/>
        /// <see cref="BaseEntityT{TProperty}.PVM{T}"/>
        /// <see cref="BaseEntityT{TProperty}.TryGetPVM{T}(out T)"/>
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="tProperty"></param>
        /// <param name="errorResponse"></param>
        /// <returns></returns>
        public static bool TryMapTToTProperty<T, TProperty>(out TProperty tProperty, out string errorResponse)
            where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
            if (typeof(T).Equals(typeof(TProperty))) throw new InvalidTypeException(typeof(T),
                "Attempt of mapping from " + typeof(T) + " to " + typeof(TProperty) + ". " +
                "A common cause is mistakenly calling " +
                nameof(BaseEntityT<TProperty>) + "." + nameof(BaseEntityT<TProperty>.PVM) + " / " + nameof(BaseEntityT<TProperty>) + "." + nameof(BaseEntityT<TProperty>.TryGetPVM) + " instead of " +
                nameof(BaseEntityT<TProperty>) + "." + nameof(BaseEntityT<TProperty>.PV) + " / " + nameof(BaseEntityT<TProperty>) + "." + nameof(BaseEntityT<TProperty>.TryGetPV) + " " +
                "(note for instance how the overload of " + nameof(BaseEntityT<TProperty>.PVM) + " with defaultValue-parameter " +
                "looks very similar to " + nameof(BaseEntityT<TProperty>.PV) + " if you forget the explicit type parameter for the latter method)");
            var mappingsForTProperty = tToTPropertyMappings.GetOrAdd(typeof(TProperty), type => new ConcurrentDictionary<Type, object>());
            var mapping = mappingsForTProperty.GetOrAdd(typeof(T), type => {
                var candidates = EnumGetValues<TProperty>().Where(p => p.GetAgoRapideAttribute().A.Type?.Equals(type) ?? false).ToList();
                switch (candidates.Count) {
                    case 0:
                        /// Search through <see cref="CoreProperty"/> because <see cref="EnumGetValues{T}"/> above did not include the silently mapped ones.
                        /// Mote how there is some duplicity in searching because all non-silently mapped <see cref="CoreProperty"/> values would
                        /// already have been checked above but since the whole thing is cached anyway it really does not matter. 
                        var cpm = new CorePropertyMapper<TProperty>();
                        var M = new Func<AgoRapide.CoreProperty, TProperty>(p => cpm.Map(p));
                        var corePropertyCandidates =  EnumGetValues<CoreProperty>().Where(p => M(p).GetAgoRapideAttribute().A.Type?.Equals(type) ?? false).ToList();
                        switch (corePropertyCandidates.Count) {
                            case 0: return "No mapping exists from " + typeof(T).ToStringShort() + " to " + typeof(TProperty).ToStringShort() + " (not even from " + typeof(CoreProperty).ToStringShort() + ")";
                            case 1: return new CorePropertyMapper<TProperty>().Map(corePropertyCandidates[0]);
                            default: return 
                                "Multiple mappings (from " + typeof(CoreProperty).ToStringShort() + ") exists from " + typeof(T).ToStringShort() + " to " + typeof(TProperty).ToStringShort() + ".\r\n" +
                                "The actual mappings found where: " + string.Join(", ", corePropertyCandidates.Select(c => typeof(CoreProperty).ToStringShort() + "." + c) + ".");
                        }
                    case 1: return candidates[0];
                    default: return 
                        "Multiple mappings exists from " + typeof(T).ToStringShort() + " to " + typeof(TProperty).ToStringShort() + ".\r\n" +
                        "The actual mappings found where: " + string.Join(", ", candidates.Select(c => typeof(TProperty).ToStringShort() + "." + c) + ".");
                }
            });
            if (mapping is string) {
                tProperty = default(TProperty);
                errorResponse = (string)mapping;
                return false;
            }
            if (!(mapping is TProperty)) throw new InvalidObjectTypeException(mapping, typeof(TProperty));
            tProperty = (TProperty)mapping;
            errorResponse = null;
            return true;

            // Old method, much more primitive
            //if (!typeof(T).IsEnum) throw new NotOfTypeEnumException<T>();
            //if (!typeof(TProperty).IsEnum) throw new NotOfTypeEnumException<TProperty>();
            //throw new NotImplementedException("Check AgoRapideAttribute.Type");
            //return EnumTryParse(typeof(T).ToStringShort(), out TProperty retval) ? retval : throw new Exception("Unable to map from type " + typeof(T).ToString() + " to a valid value for enum " + typeof(TProperty) + " (Because Enum value " + (typeof(TProperty).ToString() + "." + typeof(T).ToStringShort()) + " does not exist");
        }

        /// <summary>
        /// Encodings to be done 
        /// before System.Web.HttpUtility.UrlEncode and
        /// after  System.Web.HttpUtility.UrlDecode 
        /// (in order words preEncoding could also be called postDecoding)
        /// 
        /// By using these additional encodings you get a greater range of characters that can be 
        /// used as parameters when using AgoRapide's REST API GET-syntax
        /// </summary>
        public static List<Tuple<string, string>> preEncoding = new List<Tuple<string, string>> {
            T2("+", "_PLUS_"),
            T2("&", "_AMP_"),
            T2("%", "_PERCENT_"),
            T2("-","_DASH_"),
            T2(":","_COLON_"),
            T2("/","_SLASH_"),
            T2("#", "_HASH_"),
            T2(".","_STOP_"),
            T2("?","_QUESTION_"),
            T2("*", "_STAR_"),
            T2("'", "_APOSTROPHE_"),
            T2(">", "_GREATER_"),
            T2("<","_LESSER_")
        };

        public static string UrlEncode(string str) {
            preEncoding.ForEach(t => str = str.Replace(t.Item1, t.Item2));
            return System.Web.HttpUtility.UrlEncode(str.Replace("+", "%20%"));
        }

        public static string UrlDecodeAdditional(string str) {
            preEncoding.ForEach(t => str = str.Replace(t.Item2, t.Item1));
            return str;
        }

        private static Tuple<string, string> T2(string s1, string s2) => new Tuple<string, string>(s1, s2);

        /// <summary>
        /// Useful for getting shorter exception messages (shorter stack traces)
        /// <see cref="LogException"/> will remove all occurrences in this list when listing out stack traces
        /// </summary>
        public static List<string> SuperfluousStackTraceStrings = new List<string> {
            @"c:\AgoRapide"
        };

        private static long exceptionSerialNo = 0;
        /// <summary>
        /// Logs exception on disk, both in ordinary logfile (summary) and in separate file (detailed)
        /// Uses <see cref="Configuration.LogPath"/> as given through <see cref="Util.Configuration"/>
        /// 
        /// Fails silently if fails to write information to disk
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static void LogException(Exception ex) {
            var logPath = Configuration.LogPath;
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
                var exceptionPath = logPath + "_" + "Exception " + timeStamp.Replace(":", ".") + " " + (serialNo).ToString("0000") + " " + ex.GetType().ToStringShort().Replace("<","{").Replace(">","}") + ".txt";
                System.IO.File.WriteAllText(exceptionPath, text + "\r\n");
                Log("\r\n--\r\nAn exception of type " + ex.GetType().ToStringShort() + " occurred.\r\nSee\r\n" + exceptionPath + "\r\nfor details\r\n--\r\n");
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

        /// <summary>
        /// Gives as much information about exception as possible.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetExeptionDetails(Exception ex) {
            var msg = new StringBuilder();
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
                if (SuperfluousStackTraceStrings != null) {
                    // TODO: DEBUG THIS, NOT WORKING AS OF MARCH 2017
                    SuperfluousStackTraceStrings.ForEach(s => stackTrace = stackTrace.Replace(s, ""));
                }

                // Remove parameters. We assume that line numbers are sufficient.
                var start = stackTrace.IndexOf("(");
                while (start > -1) {
                    var slutt = stackTrace.IndexOf(")", start);
                    if (slutt == -1) break;
                    stackTrace = stackTrace.Substring(0, start) + stackTrace.Substring(slutt + 1);
                    start = stackTrace.IndexOf("(");
                }

                msg.AppendLine("Stacktrace: " + stackTrace.Replace(" at ","\r\n\r\nat ").Replace(" in ","\r\nin ") + "\r\n");

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

        /// <summary>
        /// Used by LogException in order to add data to exception message
        /// Also used as object for locking when writing to log file
        /// </summary>
        private static LinkedList<string> lastLogData = new LinkedList<string>();

        /// <summary>
        /// Log-elements waiting to be written to disc
        /// By writing at regular intervals we reduce dramatically the load on the disc
        /// See also LOGGER_THREAD_SLEEP_PERIOD 
        /// Item1 is LogPath, Item2 is LogText
        /// </summary>
        private static ConcurrentQueue<Tuple<string, string>> logQueue = new ConcurrentQueue<Tuple<string, string>>();
        private static long loggerThreadIsRunning = 0;

        private static string InsertDateTimeIntoLogPath(string logPath) => logPath.Replace("[DATE]", DateTime.Now.ToString("yyyy-MM-dd")).Replace("[DATE_HOUR]", DateTime.Now.ToString("yyyy-MM-dd_HH")); // Note "hard coded" date formats since the resulting filename has to be guaranteed to be valid

        /// <summary>
        /// Logs a specific event (parameter text) to <see cref="Configuration.LogPath"/> given through <see cref="Util.Configuration"/>
        /// 
        /// Note delayed logging through separate thread. This might be a concern in cases where the application terminates abruptly 
        /// because you risk loosing log information explaining events leading up to the application terminating. 
        /// </summary>
        /// <param name="text"></param>
        public static void Log(string text) {
            var logPath = Configuration.LogPath;
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
                if (lastLogData.Count > Configuration.LAST_LOG_DATA_MAX_SIZE) lastLogData.RemoveFirst();
            }

            logQueue.Enqueue(new Tuple<string, string>(logPath, logText));
            var wasRunning = System.Threading.Interlocked.Exchange(ref loggerThreadIsRunning, 1);
            if (wasRunning == 0) System.Threading.Tasks.Task.Factory.StartNew(() => {               // Start separate thread for actual logging to disk (the idea is to reduce dramatically the number of disk I/O-operations needed)
                try {
                    while (true) {                                                                  // Empty queue continuously, use a separate StringBuilder for each logPath
                        var allLogData = new Dictionary<string, StringBuilder>();                   // This code is a bit hastily thrown together. We could most probably increase performance here, especially for applications where logPath never changes.
                        while (logQueue.TryDequeue(out var logTuple)) { // (building allLogData is a bit expensive)
                            if (!allLogData.TryGetValue(logTuple.Item1, out var logData)) allLogData.Add(logTuple.Item1, logData = new StringBuilder());
                            logData.Append(logTuple.Item2);
                        }
                        allLogData.ForEach(e => System.IO.File.AppendAllText(InsertDateTimeIntoLogPath(e.Key), e.Value.ToString()));
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                } finally {
                    loggerThreadIsRunning = 0;
                }
            });
        }

        /// <summary>
        /// TODO: Move <see cref="EntityCache"/> into <see cref="AgoRapide.Database.IDatabase{TProperty}"/>?
        /// 
        /// Contains cache for entities. 
        /// 
        /// The cache is not guaranteed to be 100% correct.
        /// In general the cache should therefore mostly be used for nice-to-have functionality, like showing names instead
        /// of ids in HTML interface without any performance hits. 
        /// The system does however make a "best effort" attempt at keeping the cache up-to-date
        /// and invalidating known no-longer-valid  entries
        /// 
        /// Note subtle point about the entity being stored in the cache, not the root-property (in other words, entity root-properties
        /// are not found in cache per se)
        /// </summary>
        public static ConcurrentDictionary<long, BaseEntity> EntityCache = new ConcurrentDictionary<long, BaseEntity>();
        /// <summary>
        /// TODO: Move <see cref="ResetEntityCache"/> into <see cref="AgoRapide.Database.IDatabase{TProperty}"/>?
        /// </summary>
        public static void ResetEntityCache() => EntityCache = new ConcurrentDictionary<long, BaseEntity>();

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
            if (string.IsNullOrEmpty(password)) throw new IllegalPasswordException("Null or empty " + nameof(password) + " not allowed");
            lock (MD5) { // ComputeHash is not thread safe, will return wrong results if simultaneous access.  // TODO: Maybe call System.Security.Cryptography.MD5.Create() each time, in order to avod locking here.
                return string.Join("", MD5.ComputeHash(Encoding.UTF8.GetBytes(salt.ToString() + "_" + password)).Select(h => h.ToString("X2")));
            }
        }

        public static ConcurrentDictionary<string, Type> _typeToStringCache = new ConcurrentDictionary<string, Type>();
        public static Type GetTypeFromString(string strType) => TryGetTypeFromString(strType, out var retval) ? retval : throw new InvalidTypeException(strType);
        /// <summary>
        /// <see cref="Extensions.ToStringDB"/> corresponds to <see cref="Util.TryGetTypeFromString"/>
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
                    case 1: s = t[0].Trim(); break;
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
    }

    public class IllegalPasswordException : Exception {
        public IllegalPasswordException(string message) : base(message) { }
    }

    public class NotNullReferenceException : ApplicationException {
        public NotNullReferenceException(string message) : base(message) { }
        public NotNullReferenceException(string message, Exception inner) : base(message, inner) { }
    }

    public class EmptyStringException : ApplicationException {
        public EmptyStringException(string message) : base(message) { }
        public EmptyStringException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidMappingException<T, TProperty> : ApplicationException         
        where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where TProperty : Enum"
        public InvalidMappingException(string message) : base(
            "It is not possible to map from " + typeof(T).ToStringShort() + " to " + typeof(TProperty).ToStringShort() + ".\r\n" +
            "Explanation: Exact one of the enum values for " + typeof(TProperty).ToStringShort() + " must specify\r\n" +
            "   [" + nameof(AgoRapideAttribute) + "(" + nameof(AgoRapideAttribute.Type) + " = typeof(" + typeof(T).ToStringShort() + "))]\r\n" +
            "\r\nDetails:\r\n" + message) { }
    }

    public class KeyAlreadyExistsException : ApplicationException {
        public KeyAlreadyExistsException(string message) : base(message) { }
    }

    public class KeyAlreadyExistsException<TProperty> : ApplicationException where TProperty : struct, IFormattable, IConvertible, IComparable { // What we really would want is "where T : Enum"
        public KeyAlreadyExistsException(TProperty key) : this(key, null) { }
        public KeyAlreadyExistsException(TProperty key, string message) : base("The key " + key.GetAgoRapideAttribute().PExplained + " already exists" + (string.IsNullOrEmpty(message) ? "" : (". Details: " + message))) { }
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
        private static string GetMessage(object _enum, string message) => "Invalid / unknown value for enum (" + _enum.GetType().ToString() + "." + _enum.ToString() + ")." + (string.IsNullOrEmpty(message) ? "" : ("\r\nDetails: " + message));
        public InvalidEnumException(object _enum) : base(GetMessage(_enum, null)) { }
        public InvalidEnumException(object _enum, string message) : base(GetMessage(_enum, message)) { }
        public InvalidEnumException(Type type, string _string) : base("Unable to parse '" + _string + "' as " + type) { }
    }

    public class InvalidEnumException<T> : ApplicationException {
        public InvalidEnumException(string _string) : base("Value '" + _string + "' is not valid for Enum " + typeof(T).ToString()) { }
    }

    public class InvalidObjectTypeException : ApplicationException {
        private static string GetMessage(object _object, string message) => "Invalid / unknown type of object (" + _object.GetType().ToString() + "). Object: '" + _object.ToString() + "'." + (string.IsNullOrEmpty(message) ? "" : ("\r\nDetails: " + message + ")"));
        public InvalidObjectTypeException(object _object) : base(GetMessage(_object, null)) { }
        public InvalidObjectTypeException(object _object, Type typeExpected) : base(GetMessage(_object, "Expected object of type " + typeExpected + " but got object of type " + _object.GetType() + " instead")) { }
        public InvalidObjectTypeException(object _object, Type typeExpected, string message) : base(GetMessage(_object, "Expected object of type " + typeExpected + " but got object of type " + _object.GetType() + " instead.\r\nDetails: " + message)) { }
        public InvalidObjectTypeException(object _object, string message) : base(GetMessage(_object, message)) { }
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
        public static void AssertAssignable(Type foundType, Type expectedType, Func<string> detailer) {
            if (!expectedType.IsAssignableFrom(foundType)) throw new InvalidTypeException(foundType, expectedType, detailer.Result(""));
        }

        public InvalidTypeException(string typeFound) : this(typeFound, null) { }
        public InvalidTypeException(string typeFound, string details) : base("Unable to reconstruct type based on " + nameof(typeFound) + " (" + typeFound + "). Possible cause (if " + nameof(typeFound) + " originates from database): Assembly name may have changed since storing in database" + (string.IsNullOrEmpty(details) ? "" : (". Details: " + details))) { }
        public InvalidTypeException(Type type) : base("Type:" + type.ToStringShort()) { }
        public InvalidTypeException(Type type, string details) : base("Type: " + type.ToStringShort() + ", " + nameof(details) + ": " + details) { }
        /// <summary>
        /// TODO: CHANGE ORDERING OF FOUND/EXPECTED
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        public InvalidTypeException(Type foundType, Type expectedType) : base(nameof(expectedType) + ": " + expectedType.ToString() + ", " + nameof(foundType) + ": " + foundType) { }
        /// <summary>
        /// TODO: CHANGE ORDERING OF FOUND/EXPECTED
        /// </summary>
        /// <param name="foundType"></param>
        /// <param name="expectedType"></param>
        /// <param name="details"></param>
        public InvalidTypeException(Type foundType, Type expectedType, string details) : base(nameof(expectedType) + ": " + expectedType.ToString() + ", " + nameof(foundType) + ": " + foundType + ", " + nameof(details) + ": " + details) { }
    }

    /// <summary>
    /// As of March 2017 support of <see cref="int"/> has been deliberately left out of AgoRapide due to
    /// conversion issues and use of <see cref="Property{TProperty}.ADotTypeValue"/>. 
    /// If it is desired in future to support <see cref="int"/> anyway then you may just look up 
    /// all uses <see cref="TypeIntNotSupportedByAgoRapideException"/> for 
    /// information about where to change the code. 
    /// </summary>
    public class TypeIntNotSupportedByAgoRapideException: ApplicationException {
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
        public NotOfTypeEnumException(Type type) : base("Expected Type.IsEnum but got type " + type.ToString()) { }
    }

    public class NotOfTypeEnumException<T> : ApplicationException {
        public NotOfTypeEnumException() : base("Expected Type.IsEnum but got type " + typeof(T).ToString()) { }
    }

    /// <summary>
    /// TODO: Not in use as of Jan 2017
    /// </summary>
    public class UnknownExceptionWhenLoggingException : ApplicationException {
        public UnknownExceptionWhenLoggingException(string message, Exception inner) : base(message, inner) { }
    }
}