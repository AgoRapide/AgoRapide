using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AgoRapide.API;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: Consider turning this class into a BaseEntity with a Configuration-enum as properties
    /// TODO: Will probably be difficult because of the complexities involved
    /// TODO: However, we could do a simpler version, just create ConfigurationT in the same manner that we have
    /// TODO: <see cref="MethodAttributeT"/>, that class again can then be presented to the "outside".
    /// 
    /// TODO: REMOVE USE OF RootPath now that documentation if offered through general API mechanism
    /// 
    /// This class has mostly usable default values in the sense that they will not result 
    /// in NullReferenceExceptions being thrown or similar. 
    /// In other words, when initializing this class you only have to set the values that you want to change from default. 
    /// (but note how <see cref="LogPath"/>, <see cref="RootUrl"/> and <see cref="RootPath"/> should always be set, 
    /// as the default values offered in the default instance of 
    /// <see cref="Configuration"/> offered through <see cref="Util.Configuration"/> clearly are not very useful)
    /// 
    /// Always access this class through instance <see cref="Util.Configuration"/>
    /// 
    /// Note how some of the offered properties are cached. This means that changing some values, like
    /// <see cref="HTMLPostfixIndicator"/> AFTER having accessed cached values like <see cref="HTMLPostfixIndicatorWithoutLeadingSlash"/> 
    /// will not change the cached value. Therefore always do a full initialization of this class at once at startup, and do not change
    /// any values afterwards after accessing any cached value. 
    /// </summary>
    public class Configuration {

        /// <summary>
        /// Example: @"c:\p\Logfiles\AgoRapide\AgoRapideLog_[DATE_HOUR].txt"
        /// 
        /// See how <see cref="Util.InsertDateTimeIntoLogPath"/> handles [DATE] and [DATE_HOUR]
        /// See <see cref="Configuration.LOGGER_THREAD_SLEEP_PERIOD"/>
        /// 
        /// Note the useless default value offered in the default instance of <see cref="Configuration"/> offered through <see cref="Util.Configuration"/>
        /// </summary>
        public string LogPath { get; private set; }

        /// <summary>
        /// Example: https://api.agorapide.com/ or http://localhost:59294/
        /// 
        /// Note the useless default value offered in the default instance of <see cref="Configuration"/> offered through <see cref="Util.Configuration"/>
        /// </summary>
        public string RootUrl { get; private set; }

        /// <summary>
        /// <see cref="RootUrl"/> and <see cref="RootPath"/> are the only values that have to be set when initializing this class. 
        /// All other properties have sensible default values.
        /// </summary>
        /// <param name="rootUrl"></param>
        public Configuration(string logPath, string rootUrl) {
            LogPath = logPath;
            RootUrl = rootUrl;
        }

        public Environment Environment { get; set; } = Environment.Test;

        private BaseEntity _anonymousUser;
        /// <summary>
        /// The <see cref="AnonymousUser"/> should be created at application startup if it does not exist in database. 
        /// 
        /// TODO: Explain why we create <see cref="AnonymousUser"/> in database but not <see cref="SystemUser"/>
        /// </summary>
        public BaseEntity AnonymousUser {
            get => _anonymousUser ?? throw new NullReferenceException(nameof(AnonymousUser) + ". Should have been set at application startup, like in Startup.cs");
            set => _anonymousUser = value ?? throw new NullReferenceException(nameof(AnonymousUser));
        }

        private BaseEntity _systemUser;
        /// <summary>
        /// The <see cref="SystemUser"/> is normally not stored in the database as there is no need for using it as 
        /// <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> since we use 
        /// <see cref="ClassAndMethod"/> for that purpose. 
        /// 
        /// TODO: Explain why we create <see cref="AnonymousUser"/> in database but not <see cref="SystemUser"/>
        /// </summary>
        public BaseEntity SystemUser {
            get => _systemUser ?? throw new NullReferenceException(nameof(SystemUser) + ". Should have been set at application startup, like in Startup.cs");
            set => _systemUser = value ?? throw new NullReferenceException(nameof(SystemUser));
        }

        private List<string> _superfluousStackTraceStrings = new List<string>();
        /// <summary>
        /// Used in order to remove unnecessary information from the stack trace in exception messages
        /// Typical example would be 
        /// new List[string] {
        ///   "c:\git\AgoRapide",
        ///   "c:\git\YourProject"
        /// }
        /// </summary>        
        public List<string> SuperfluousStackTraceStrings {
            get => _superfluousStackTraceStrings;
            set => _superfluousStackTraceStrings = value ?? throw new NullReferenceException(nameof(SuperfluousStackTraceStrings));
        }

        /// <summary>
        /// TODO: DELETE THIS. Replace with known Method[TProperty] ExceptionDetails.  (????????????)
        /// (that is, link to CoreMethod with syntax)
        /// 
        /// Replace with <see cref="CoreMethod.ExceptionDetails"/>
        /// </summary>
        public string ExceptionDetailsAPISyntax { get; set; } = "ExceptionDetails";

        // Used by <see cref="Extensions.ToStringShort(Type)"/> in order to give a simple human readable version of a type
        /// <summary>
        /// TODO: NOT NEEDED, CANDIDATE FOR DELETION
        /// </summary>
        public List<string> AssemblyNames { get; set; } = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToList();

        /// <summary>
        /// Maximum number of elements of log data that will be included in detailed exception messages.
        /// (only relevant if you use BUtil.Log and BUtil.LogException methods)
        /// 
        /// Increase to a higher value of you have a _very_ busy system with lots of threads as data
        /// which are relevant for the thread the exception was thrown from may already have been
        /// pushed out by newer data logged by other threads 
        /// (this is especially relevant if we are talking about exceptions due to timeout)
        /// </summary>
        public long LAST_LOG_DATA_MAX_SIZE { get; set; } = 150;

        /// <summary>
        /// Maximum period for which logging of a specific event may be delayed.
        /// (only relevant if you use BUtil.Log and BUtil.LogException methods)
        /// 
        /// Note that due to this delayed logging you always run the risk of conditions resulting
        /// in the application going down (like StackOverflowException) not being logged
        /// Decreasing this value is thought not to help very much however
        /// (and setting it to zero may result in very high CPU usage by the logging thread)
        /// </summary>
        public TimeSpan LOGGER_THREAD_SLEEP_PERIOD = TimeSpan.FromSeconds(5);

        /// <summary>
        /// TODO: REMOVE THIS. NO LONGER NEEDED WHEN WE START USING ATTRIBUTES FOR DOCUMENTATION
        /// 
        /// Used for unit testing. 
        /// 
        /// This list is used to insert real entity ids (ids existing 
        /// in your production database) into code for unit tests in order to reduce the amount
        /// of scaffolding needed to execute a unit test
        /// 
        /// Example:
        /// new Dictionary[string, int] {
        ///   { "[WELL_KNOWN_PERSON_ID" , (int)EntityIds.TestCustomer // Where EntityIds is an enum
        /// }
        /// </summary>
        public Dictionary<string, int> WellKnownIds { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Defaults to "en-US
        /// </summary>
        public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

        private const string _defaultDateAndHourMinSecMsFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private const string _defaultDateAndHourMinSecFormat = "yyyy-MM-dd HH:mm:ss";
        private const string _defaultDateAndHourMinFormat = "yyyy-MM-dd HH:mm";
        private const string _defaultDateOnlyFormat = "yyyy-MM-dd";

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// </summary>
        public string[] ValidDateFormats { get; set; } = new string[] {
            _defaultDateAndHourMinSecMsFormat,
            _defaultDateAndHourMinSecFormat,
            _defaultDateAndHourMinFormat,
            _defaultDateOnlyFormat
        };

        public Dictionary<DateTimeFormat, string[]> ValidDateFormatsByResolution { get; set; } = new Dictionary<DateTimeFormat, string[]> {
            { DateTimeFormat.None, new string[] {
                _defaultDateAndHourMinSecMsFormat,
                _defaultDateAndHourMinSecFormat,
                _defaultDateAndHourMinFormat,
                _defaultDateOnlyFormat
                }
            },
            { DateTimeFormat.DateHourMinSecMs, new string[] { _defaultDateAndHourMinSecMsFormat } },
            { DateTimeFormat.DateHourMinSec, new string[] { _defaultDateAndHourMinSecFormat } },
            { DateTimeFormat.DateHourMin, new string[] { _defaultDateAndHourMinFormat } },
            { DateTimeFormat.DateOnly, new string[] { _defaultDateOnlyFormat } }
        };

        public Dictionary<DateTimeFormat, string> DateFormatsByResolution { get; set; } = new Dictionary<DateTimeFormat, string> {
            { DateTimeFormat.None, _defaultDateAndHourMinSecMsFormat },
            { DateTimeFormat.DateHourMinSecMs, _defaultDateAndHourMinSecMsFormat },
            { DateTimeFormat.DateHourMinSec, _defaultDateAndHourMinSecFormat },
            { DateTimeFormat.DateHourMin, _defaultDateAndHourMinFormat },
            { DateTimeFormat.DateOnly, _defaultDateOnlyFormat }
        };

        /// <summary>
        /// This format is (within AgoRapide) only used by BUtil.Log 
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="Configuration.DateAndHourMinSecMsFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSecMs"/> 
        /// </summary>
        public string DateAndHourMinSecMsFormat { get; set; } = _defaultDateAndHourMinSecMsFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="Configuration.DateAndHourMinSecFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSec"/> 
        /// </summary>
        public string DateAndHourMinSecFormat { get; set; } = _defaultDateAndHourMinSecFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="Configuration.DateAndHourMinFormat"/> corresponds to <see cref="DateTimeFormat.DateHourSec"/> 
        /// </summary>
        public string DateAndHourMinFormat { get; set; } = _defaultDateAndHourMinFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="Configuration.DateOnlyFormat"/> corresponds to <see cref="DateTimeFormat.DateOnly"/> 
        /// </summary>
        public string DateOnlyFormat { get; set; } = _defaultDateOnlyFormat;

        /// <summary>
        /// Background colour for this environment. 
        /// Use of colouring is an attempt at reducing confusion for developers changing regularly between Environments
        /// </summary>
        public Color BackgroundColour => BackgroundColours.GetValue2(Environment);
        public Dictionary<Environment, Color> BackgroundColours { get; set; } = new Dictionary<Environment, Color> {
            { Environment.None, Color.Red }, // Not supposed to be used
            { Environment.Development, Color.Beige },
            { Environment.Test, Color.LightGray },
            { Environment.Production, Color.White }
        };

        /// <summary>
        /// Background colour for this environment. HTML-encoded like "#RRGGBB"
        /// Use of colouring is an attempt at reducing confusion for developers changing regularly between Environments
        /// </summary>
        public string BackgroundColourHTML => BackgroundColoursHTML.GetValue2(Environment);
        private Dictionary<Environment, string> _backgroundColoursHTML;
        public Dictionary<Environment, string> BackgroundColoursHTML => _backgroundColoursHTML ?? (_backgroundColoursHTML = new Func<Dictionary<Environment, string>>(() => {
            var retval = new Dictionary<Environment, string>();
            Util.EnumGetValues<Environment>().ForEach(e => {
                BackgroundColours.GetValue2(e).Use(c => retval.AddValue2(e, string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B)));
            });
            return retval;
        })());

        /// <summary>
        /// URL that is prepended to every API-command generated through code. 
        /// Equivalent to RootUrl + ApiPrefix
        /// Example: https://bapi.agorapide.com/api/ or http://localhost:59294/api/
        /// </summary>
        public string BaseUrl => RootUrl + ApiPrefix;

        // public List<string> ScriptUrls

        private string _apiPrefix = "api/";
        /// <summary>
        /// This prefix will be added to every API route mapped by BAPI.Method.MapHTTPRoutes
        /// 
        /// Default value is 'api/'
        /// 
        /// Useful if you want to host static content together with your REST API on the same web server. 
        /// You may then cleanly separate the static content from the REST API routes. 
        /// 
        /// Set to null or empty value if not used (empty value will be stored). 
        /// If not null or empty then leading slash / will be removed and trailing slash / added as necessary
        /// </summary>
        public string ApiPrefix {
            get => _apiPrefix;
            set {
                if (string.IsNullOrEmpty(value)) {
                    _apiPrefix = "";
                } else {
                    _apiPrefix = value;
                    if (_apiPrefix.StartsWith("/")) _apiPrefix = _apiPrefix.Substring(1); // Or maybe throw an exception instead?
                    if (!_apiPrefix.EndsWith("/")) _apiPrefix += "/"; // Or maybe throw an exception instead?
                }
            }
        }

        private string _apiPrefixToLower;
        public string ApiPrefixToLower => _apiPrefixToLower ?? (_apiPrefixToLower = ApiPrefix.ToLower());

        public string CSSRelativePath { get; set; } = "css.css";

        public List<string> ScriptRelativePaths { get; set; } = new List<string> { "Scripts/AgoRapide-0.1.js", "Scripts/jquery-3.1.1.min.js" };

        /// <summary>
        /// Indicator at end of API request URL which indicates that HTML format is desired 
        /// (instead of JSON)
        /// </summary>
        public string HTMLPostfixIndicator { get; set; } = "/HTML";

        private string _HTMLPostfixIndicatorToLower;
        public string HTMLPostfixIndicatorToLower => _HTMLPostfixIndicatorToLower ?? (_HTMLPostfixIndicatorToLower = HTMLPostfixIndicator.ToLower());

        private string _HTMLPostfixIndicatorWithoutLeadingSlash;
        public string HTMLPostfixIndicatorWithoutLeadingSlash => _HTMLPostfixIndicatorWithoutLeadingSlash ?? (_HTMLPostfixIndicatorWithoutLeadingSlash = (!(string.IsNullOrEmpty(HTMLPostfixIndicator) && HTMLPostfixIndicator.Length > 1) && HTMLPostfixIndicator.StartsWith("/")) ? HTMLPostfixIndicator.Substring(1) : throw new Exception("Invalid " + nameof(HTMLPostfixIndicator) + " (" + HTMLPostfixIndicator + "), unable to generate " + nameof(HTMLPostfixIndicatorWithoutLeadingSlash)));

        private string _HTMLPostfixIndicatorWithoutLeadingSlashToLower;
        public string HTMLPostfixIndicatorWithoutLeadingSlashToLower => _HTMLPostfixIndicatorWithoutLeadingSlashToLower ?? (_HTMLPostfixIndicatorWithoutLeadingSlashToLower = HTMLPostfixIndicatorWithoutLeadingSlash.ToLower());

        public string GenericMethodRouteTemplate = "{*url}";
    }
}
