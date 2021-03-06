﻿// Copyright (c) 2016, 2017, 2018 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AgoRapide.API;
using AgoRapide.Database;

namespace AgoRapide.Core {
    /// <summary>
    /// TODO: DELETE THIS CLASS. Not needed after all, move into <see cref="Configuration"/>. 
    /// TODO: Or rather, keep, if <see cref="Configuration"/> will be turned into an <see cref="ApplicationPart"/>
    /// 
    /// This class has mostly usable default values in the sense that they will not result 
    /// in NullReferenceExceptions being thrown or similar. 
    /// In other words, when initializing this class you only have to set the values that you want to change from default. 
    /// (but note how <see cref="LogPath"/> and <see cref="RootUrl"/> should always be set, 
    /// as the default values offered in the default instance of 
    /// <see cref="ConfigurationAttribute"/> offered through <see cref="Util.Configuration"/> clearly are not very useful)
    /// 
    /// Always access this class through instance <see cref="Util.Configuration"/>
    /// 
    /// Note how some of the offered properties are cached. This means that changing some values, like
    /// <see cref="HTMLPostfixIndicator"/> AFTER having accessed cached values like <see cref="HTMLPostfixIndicatorWithoutLeadingSlash"/> 
    /// will not change the cached value. Therefore always do a full initialization of this class at once at startup, and do not change
    /// any values afterwards after accessing any cached value. 
    /// </summary>
    [Class(Description = "General attributes for a -" + nameof(Configuration) + "-.")]
    public class ConfigurationAttribute : BaseAttribute { /// NOTE: This class does not use any <see cref="Attribute"/> functionality

        /// <summary>
        /// Example: @"c:\p\Logfiles\AgoRapide\AgoRapideLog_[DATE_HOUR].txt"
        /// 
        /// See how <see cref="Util.InsertDateTimeIntoLogPath"/> handles [DATE] and [DATE_HOUR]
        /// See <see cref="ConfigurationAttribute.LOGGER_THREAD_SLEEP_PERIOD"/>
        /// 
        /// Note the useless default value offered in the default instance of <see cref="ConfigurationAttribute"/> offered through <see cref="Util.Configuration"/>
        /// </summary>
        public string LogPath { get; private set; }

        /// <summary>
        /// Example: https://sample.agorapide.com/ or http://localhost:59294/
        /// 
        /// Note the useless default value offered in the default instance of <see cref="ConfigurationAttribute"/> offered through <see cref="Util.Configuration"/>
        /// </summary>
        public Uri RootUrl { get; private set; }

        /// <summary>
        /// Returns a new instance of <see cref="BaseDatabase"/> for every call. 
        /// Note the useless default value offered in the default instance of <see cref="ConfigurationAttribute"/> offered through <see cref="Util.Configuration"/>
        /// </summary>
        public Func<Type, BaseDatabase> DatabaseGetter { get; private set; }

        /// <summary>
        /// <see cref="LogPath"/> and <see cref="RootUrl"/> are the only values that have to be set when initializing this class. 
        /// All other properties have sensible default values.
        /// </summary>
        /// <param name="rootUrl"></param>
        /// <param name="databaseGetter">This is supposed to return a new instance of <see cref="BaseDatabase"/> for every call.</param>
        public ConfigurationAttribute(string logPath, Uri rootUrl, Func<Type, BaseDatabase> databaseGetter) {
            Description = "Contains all Configuration information for AgoRapide";
            LogPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            RootUrl = rootUrl ?? throw new ArgumentNullException(nameof(rootUrl));
            DatabaseGetter = databaseGetter ?? throw new ArgumentNullException(nameof(databaseGetter));

            RootUrlUsesHTTPS = "https".Equals(RootUrl.Scheme);
        }

        public bool RootUrlUsesHTTPS { get; private set; }

        public void AssertHTTPSAsRelevant(Uri requestUri) { if (!TryAssertHTTPSAsRelevant(requestUri, out var errorReponse)) throw new InvalidProtocolException(errorReponse); }
        [ClassMember(Description = "Asserts that https is used for request (but only if -" + nameof(RootUrl) + "- uses https).")]
        public bool TryAssertHTTPSAsRelevant(Uri requestUri, out string errorResponse) {
            if (!RootUrlUsesHTTPS) { errorResponse = null; return true; }
            if ("https".Equals(requestUri.Scheme)) { errorResponse = null; return true; }
            errorResponse = "Invalid protocol for request (" + requestUri.Scheme + "). Since " + nameof(RootUrlUsesHTTPS) + ", every request has to be made with https. Possible resolution: Start your url with https:// instead of http://";
            return false;
        }

        public string ApplicationName { get; set; } = "AgoRapide";

        public Environment Environment { get; set; } = Environment.Test;

        private Type _TPersonType;
        /// <summary>
        /// Type of <see cref="AnonymousUser"/> and <see cref="SystemUser"/> and
        /// the type normally used for <see cref="CoreAPIMethod.GeneralQuery"/>, in other words, the object type used for storing
        /// information about human users of your system.
        /// 
        /// Normally set from <see cref="CoreStartup.Initialize"/>
        /// </summary>
        public Type TPersonType {
            get => _TPersonType ?? throw new NullReferenceException(nameof(TPersonType) + ". Should have been set at application startup");
            set {
                if (_TPersonType != null) throw new NotNullReferenceException(nameof(TPersonType) + ": Call to " + nameof(TPersonType) + "[Set] should only be done once at application startup");
                if (value == null) throw new ArgumentNullException(nameof(TPersonType));
                if (!typeof(BaseEntity).IsAssignableFrom(value)) throw new InvalidTypeException(value, typeof(BaseEntity), nameof(TPersonType) + " must be assignable to " + typeof(BaseEntity).ToStringShort());
                _TPersonType = value;
            }
        }

        private BaseEntity _anonymousUser;
        /// <summary>
        /// The <see cref="AnonymousUser"/> should be created at application startup if it does not exist in database.  
        /// 
        /// Normally set from <see cref="CoreStartup.Initialize"/>. Must be of type <see cref="TPersonType"/>. 
        /// 
        /// Used by <see cref="BasicAuthenticationAttribute.AuthenticateAsync"/> when the relevant API-method does not 
        /// require authentication. 
        /// </summary>
        public BaseEntity AnonymousUser {
            get => _anonymousUser ?? throw new NullReferenceException(nameof(AnonymousUser) + ". Should have been set at application startup");
            set {
                if (_anonymousUser != null) throw new NotNullReferenceException(nameof(_anonymousUser) + ": Call to " + nameof(AnonymousUser) + "[Set] should only be done once at application startup");
                if (value == null) throw new ArgumentNullException(nameof(AnonymousUser));
                if (value.Id <= 0) throw new ArgumentException(nameof(AnonymousUser) + " not set up correctly (" + nameof(value.Id) + ": " + value.Id + "), should be 1 or greater.");
                InvalidTypeException.AssertEquals(value.GetType(), TPersonType, () => nameof(AnonymousUser) + " must be of type specified by configuration attribute " + nameof(TPersonType) + " (" + TPersonType.ToStringShort() + ")");
                _anonymousUser = value;
            }
        }

        private BaseEntity _systemUser;
        /// <summary>
        /// The <see cref="SystemUser"/> is normally not stored in the database as there is no need for using it as 
        /// <see cref="DBField.cid"/> / <see cref="DBField.vid"/> / <see cref="DBField.iid"/> since we use 
        /// <see cref="ClassMember"/> for that purpose. 
        /// 
        /// Note that <see cref="AnonymousUser"/> on the other hand, IS stored in database.
        /// 
        /// Normally set from <see cref="CoreStartup.Initialize"/>. Must be of type <see cref="TPersonType"/>. 
        /// 
        /// Used for making queries towards database when <see cref="AccessLevel.System"/> is needed.
        /// </summary>
        public BaseEntity SystemUser {
            get => _systemUser ?? throw new NullReferenceException(nameof(SystemUser) + ". Should have been set at application startup");
            set {
                if (_systemUser != null) throw new NotNullReferenceException(nameof(_systemUser) + ": Call to " + nameof(SystemUser) + "[Set] should only be done once at application startup");
                if (value == null) throw new ArgumentNullException(nameof(SystemUser));
                InvalidTypeException.AssertEquals(value.GetType(), TPersonType, () => nameof(SystemUser) + " must be of type specified by configuration attribute " + nameof(TPersonType) + " (" + TPersonType.ToStringShort() + ")");
                _systemUser = value;
            }
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
        /// (only relevant if you use <see cref="Util.Log"/> and <see cref="Util.LogException"/>.
        /// 
        /// Note that due to this delayed logging you always run the risk of conditions resulting
        /// in the application going down (like a StackOverflowException) not being logged
        /// Decreasing this value is thought not to help very much however
        /// (and setting it to zero will result in extremely high CPU usage by the logging thread)
        /// </summary>
        public TimeSpan LOGGER_THREAD_SLEEP_PERIOD = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Defaults to "en-US
        /// </summary>
        public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

        // ----------------------------------------------- DateTime formats ------------------------------------------------ //

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
        /// This format is (within AgoRapide) only used by <see cref="Util.Log(string)"/>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="ConfigurationAttribute.DateAndHourMinSecMsFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSecMs"/> 
        /// </summary>
        public string DateAndHourMinSecMsFormat { get; set; } = _defaultDateAndHourMinSecMsFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="ConfigurationAttribute.DateAndHourMinSecFormat"/> corresponds to <see cref="DateTimeFormat.DateHourMinSec"/> 
        /// </summary>
        public string DateAndHourMinSecFormat { get; set; } = _defaultDateAndHourMinSecFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="ConfigurationAttribute.DateAndHourMinFormat"/> corresponds to <see cref="DateTimeFormat.DateHourSec"/> 
        /// </summary>
        public string DateAndHourMinFormat { get; set; } = _defaultDateAndHourMinFormat;

        /// <summary>
        /// Note: <see cref="ValidDateFormats"/> must be a superset of { <see cref="DateAndHourMinSecMsFormat"/>, <see cref="DateAndHourMinSecFormat"/>, <see cref="DateAndHourMinFormat"/>, <see cref="DateAndHourFormat"/>, <see cref="DateOnlyFormat"/> }
        /// <see cref="ConfigurationAttribute.DateOnlyFormat"/> corresponds to <see cref="DateTimeFormat.DateOnly"/> 
        /// </summary>
        public string DateOnlyFormat { get; set; } = _defaultDateOnlyFormat;

        // ----------------------------------------------- Number formats ------------------------------------------------ //

        private const string _defaultNumberIdFormat = "#";
        private const string _defaultNumberIntegerFormat = "n0";
        private const string _defaultNumberDecimalFormat = "n";

        /// <summary>
        /// TODO: Considering deleting. Modelled on <see cref="ValidDateFormats"/> but since <see cref="PropertyKeyAttribute.NumberFormat"/> does not affect parsing
        /// TODO: so is most probably uneeded here.
        /// 
        /// Note: <see cref="ValidNumberFormats"/> must be a superset of { <see cref="NumberIdFormat"/>, <see cref="NumberIntegerFormat"/>, <see cref="NumberDecimalFormat"/>
        /// </summary>
        public string[] ValidNumberFormats { get; set; } = new string[] {
            _defaultDateAndHourMinSecMsFormat,
            _defaultDateAndHourMinSecFormat,
            _defaultDateAndHourMinFormat,
            _defaultDateOnlyFormat
        };

        /// <summary>
        /// TODO: Considering deleting. Modelled on <see cref="ValidDateFormatsByResolution"/> but since <see cref="PropertyKeyAttribute.NumberFormat"/> does not affect parsing
        /// TODO: so is most probably uneeded here.
        /// </summary>
        public Dictionary<NumberFormat, string[]> ValidNumberFormatsByResolution { get; set; } = new Dictionary<NumberFormat, string[]> {
            { NumberFormat.None, new string[] {
                _defaultNumberIdFormat,
                _defaultNumberIntegerFormat,
                _defaultNumberDecimalFormat,
                }
            },
            { NumberFormat.Id, new string[] { _defaultNumberIdFormat } },
            { NumberFormat.Integer, new string[] { _defaultNumberIntegerFormat } },
            { NumberFormat.Decimal, new string[] { _defaultNumberDecimalFormat } }
        };

        /// <summary>
        /// TODO: Considering deleting. Modelled on <see cref="DateFormatsByResolution"/> but since <see cref="PropertyKeyAttribute.NumberFormat"/> does not affect parsing
        /// TODO: so is most probably uneeded here.
        /// </summary>
        public Dictionary<NumberFormat, string> NumberFormatsByResolution { get; set; } = new Dictionary<NumberFormat, string> {
            { NumberFormat.None, _defaultNumberDecimalFormat },
            { NumberFormat.Id, _defaultNumberIdFormat },
            { NumberFormat.Integer, _defaultNumberIntegerFormat },
            { NumberFormat.Decimal, _defaultNumberDecimalFormat }
        };

        public string NumberIdFormat { get; set; } = _defaultNumberIdFormat;
        public string NumberIntegerFormat { get; set; } = _defaultNumberIntegerFormat;
        public string NumberDecimalFormat { get; set; } = _defaultNumberDecimalFormat;

        // ------------------------------------------------------------------------------------------------------------- //

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

        private Uri _baseUrl;
        /// <summary>
        /// Example: https://sample.agorapide.com/api/ or http://localhost:59294/api/
        /// </summary>
        [ClassMember(
            Description =
                "URL that is prepended to every API-command generated through code. " +
                "See -" + nameof(APICommandCreator.CreateAPIUrl) + "-",
            LongDescription =
                "Equivalent to -" + nameof(RootUrl) + "- plus -" + nameof(APIPrefix) + "-"
        )]
        public Uri BaseUrl => _baseUrl ?? (_baseUrl = new Uri(RootUrl.ToString() + APIPrefix));

        private string _APIPrefix = "api/";
        [ClassMember(
              Description =
                "This prefix will be added to -" + nameof(RootUrl) + "- for every -" + nameof(APIMethod) + "- mapped. " +
                "Default value is \"api/\"",
             LongDescription =
                "Useful if you want to host static content together with your REST API on the same web server. " +
                "You may then cleanly separate the static content from the REST API routes. " +
                "Set to null or empty value if not used (empty value will be stored). " +
                "If not null or empty then leading slash / will be removed and trailing slash / added as necessary"
        )]
        public string APIPrefix {
            get => _APIPrefix;
            set {
                if (string.IsNullOrEmpty(value)) {
                    _APIPrefix = "";
                } else {
                    _APIPrefix = value;
                    if (_APIPrefix.StartsWith("/")) _APIPrefix = _APIPrefix.Substring(1); // Or maybe throw an exception instead?
                    if (!_APIPrefix.EndsWith("/")) _APIPrefix += "/"; // Or maybe throw an exception instead?
                }
            }
        }

        private string _apiPrefixToLower;
        public string ApiPrefixToLower => _apiPrefixToLower ?? (_apiPrefixToLower = APIPrefix.ToLower());

        public string CSSRelativePath { get; set; } = "css.css";

        public List<string> ScriptRelativePaths { get; set; } = new List<string> { "Scripts/AgoRapide-0.1.js", "Scripts/jquery-3.1.1.min.js", "Scripts/tablesort.min.js", "Scripts/tablesort.number.js" };

        [ClassMember(Description = "Indicator at end of API request URL indicating that -" + nameof(ResponseFormat.HTML) + "- is desired by client.")]
        public string HTMLPostfixIndicator { get; set; } = "/HTML";

        private string _HTMLPostfixIndicatorToLower;
        public string HTMLPostfixIndicatorToLower => _HTMLPostfixIndicatorToLower ?? (_HTMLPostfixIndicatorToLower = HTMLPostfixIndicator.ToLower());

        private string _HTMLPostfixIndicatorWithoutLeadingSlash;
        public string HTMLPostfixIndicatorWithoutLeadingSlash => _HTMLPostfixIndicatorWithoutLeadingSlash ?? (_HTMLPostfixIndicatorWithoutLeadingSlash = (!(string.IsNullOrEmpty(HTMLPostfixIndicator) && HTMLPostfixIndicator.Length > 1) && HTMLPostfixIndicator.StartsWith("/")) ? HTMLPostfixIndicator.Substring(1) : throw new Exception("Invalid " + nameof(HTMLPostfixIndicator) + " (" + HTMLPostfixIndicator + "), unable to generate " + nameof(HTMLPostfixIndicatorWithoutLeadingSlash) + "."));

        private string _HTMLPostfixIndicatorWithoutLeadingSlashToLower;
        public string HTMLPostfixIndicatorWithoutLeadingSlashToLower => _HTMLPostfixIndicatorWithoutLeadingSlashToLower ?? (_HTMLPostfixIndicatorWithoutLeadingSlashToLower = HTMLPostfixIndicatorWithoutLeadingSlash.ToLower());

        [ClassMember(Description = "Maximum length that will be shown for strings in -" + nameof(BaseEntity.ToHTMLTableRow) + "- representation")]
        public int HTMLTableRowStringMaxLength { get; set; } = 80;

        [ClassMember(Description = "Indicator at end of API request URL indicating that -" + nameof(ResponseFormat.PDF) + "- is desired by client.")]
        public string PDFPostfixIndicator { get; set; } = "/PDF";

        private string _PDFPostfixIndicatorToLower;
        public string PDFPostfixIndicatorToLower => _PDFPostfixIndicatorToLower ?? (_PDFPostfixIndicatorToLower = PDFPostfixIndicator.ToLower());

        private string _PDFPostfixIndicatorWithoutLeadingSlash;
        public string PDFPostfixIndicatorWithoutLeadingSlash => _PDFPostfixIndicatorWithoutLeadingSlash ?? (_PDFPostfixIndicatorWithoutLeadingSlash = (!(string.IsNullOrEmpty(PDFPostfixIndicator) && PDFPostfixIndicator.Length > 1) && PDFPostfixIndicator.StartsWith("/")) ? PDFPostfixIndicator.Substring(1) : throw new Exception("Invalid " + nameof(PDFPostfixIndicator) + " (" + PDFPostfixIndicator + "), unable to generate " + nameof(PDFPostfixIndicatorWithoutLeadingSlash) + "."));

        private string _PDFPostfixIndicatorWithoutLeadingSlashToLower;
        public string PDFPostfixIndicatorWithoutLeadingSlashToLower => _PDFPostfixIndicatorWithoutLeadingSlashToLower ?? (_PDFPostfixIndicatorWithoutLeadingSlashToLower = PDFPostfixIndicatorWithoutLeadingSlash.ToLower());

        [ClassMember(Description = "Indicator at end of API request URL indicating that -" + nameof(ResponseFormat.CSV) + "- is desired by client.")]
        public string CSVPostfixIndicator { get; set; } = "/CSV";

        private string _CSVPostfixIndicatorToLower;
        public string CSVPostfixIndicatorToLower => _CSVPostfixIndicatorToLower ?? (_CSVPostfixIndicatorToLower = CSVPostfixIndicator.ToLower());

        private string _CSVPostfixIndicatorWithoutLeadingSlash;
        public string CSVPostfixIndicatorWithoutLeadingSlash => _CSVPostfixIndicatorWithoutLeadingSlash ?? (_CSVPostfixIndicatorWithoutLeadingSlash = (!(string.IsNullOrEmpty(CSVPostfixIndicator) && CSVPostfixIndicator.Length > 1) && CSVPostfixIndicator.StartsWith("/")) ? CSVPostfixIndicator.Substring(1) : throw new Exception("Invalid " + nameof(CSVPostfixIndicator) + " (" + CSVPostfixIndicator + "), unable to generate " + nameof(CSVPostfixIndicatorWithoutLeadingSlash) + "."));

        private string _CSVPostfixIndicatorWithoutLeadingSlashToLower;
        public string CSVPostfixIndicatorWithoutLeadingSlashToLower => _CSVPostfixIndicatorWithoutLeadingSlashToLower ?? (_CSVPostfixIndicatorWithoutLeadingSlashToLower = CSVPostfixIndicatorWithoutLeadingSlash.ToLower());

        public string GenericMethodRouteTemplate = "{*url}";

        [ClassMember(Description = "Relevant when using -" + nameof(ResponseFormat.PDF) + "- / -" + nameof(PDFView) + "-.")]
        public string MiKTeXPDFLatexPath { get; set; } = "\"" + @"C:\Program Files\MiKTeX 2.9\miktex/bin/x64\pdflatex" + "\" "; // Note trailing space

        /// <summary>
        /// TODO: Consider moving these "outside" of <see cref="ConfigurationAttribute"/>-class like other <see cref="EnumType.PropertyKey"/>
        /// </summary>
        [Enum(AgoRapideEnumType = EnumType.PropertyKey)]
        public enum ConfigurationP {
            None,
            [PropertyKey(AccessLevelRead = AccessLevel.Admin)]
            ConfigurationLogPath,
            [PropertyKey(Type = typeof(Uri), AccessLevelRead = AccessLevel.Anonymous)]
            ConfigurationRootUrl,
            [PropertyKey(AccessLevelRead = AccessLevel.Anonymous)]
            ConfigurationAPIPrefix,
            [PropertyKey(Type = typeof(Uri), AccessLevelRead = AccessLevel.Anonymous)]
            ConfigurationBaseUrl
        }

        protected override ConcurrentDictionary<CoreP, Property> GetProperties() {
            var p = Util.GetNewPropertiesParent();
            Func<string> d = () => ToString();

            /// Note how we are not adding None-values since they will be considered invalid at later reading from database.
            /// Note how string value and <see cref="Property.ValueA"/> (<see cref="BaseAttribute"/>) are easily deduced by <see cref="PropertyT{T}"/> in this case so we do not need to add those as parameters here.
            if (Environment != Environment.None) p.AddProperty(CoreP.Environment.A(), Environment);

            /// Note adding of string value and <see cref="Property.ValueA"/> (<see cref="BaseAttribute"/>) here
            p.AddProperty(ConfigurationP.ConfigurationLogPath.A(), LogPath, LogPath, GetType().GetClassMemberAttribute(nameof(LogPath)), d);
            p.AddProperty(ConfigurationP.ConfigurationRootUrl.A(), RootUrl, RootUrl.ToString(), GetType().GetClassMemberAttribute(nameof(RootUrl)), d);
            p.AddProperty(ConfigurationP.ConfigurationAPIPrefix.A(), APIPrefix, APIPrefix, GetType().GetClassMemberAttribute(nameof(APIPrefix)), d);
            p.AddProperty(ConfigurationP.ConfigurationBaseUrl.A(), BaseUrl, BaseUrl.ToString(), GetType().GetClassMemberAttribute(nameof(BaseUrl)), d);

            p.AddProperty(CoreP.Message.A(), "TODO: ADD MORE PROPERTIES IN " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name, d);
            /// TODO: Add more values to this list. Expand <see cref="ConfigurationP"/> as needed.

            return p.Properties;
        }

        public override string ToString() => base.ToString();
        protected override Id GetId() => new Id(
            idString: new QueryIdString(GetType().ToStringShort().Replace("Attribute", "") + "_Configuration"),
            idFriendly: "Configuration",
            idDoc: new List<string> { "Configuration" }
        );
    }

    public static class ConfigurationExtension {
        public static PropertyKey A(this ConfigurationAttribute.ConfigurationP configurationKey) => PropertyKeyMapper.GetA(configurationKey);
    }
}
