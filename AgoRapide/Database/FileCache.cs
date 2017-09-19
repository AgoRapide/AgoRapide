// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.Database {

    /// <summary>
    /// Each type is stored in its individual file. 
    /// For file format used see <see cref="StoreToDisk{T}"/>
    /// </summary>
    [Class(
        Description =
            "Caches (on disk) information provided by -" + nameof(BaseSynchronizer) + "- in a local file storage (direct disk storage).",
        LongDescription =
            "The rationale for -" + nameof(FileCache) + "- is that there is no point in " +
            "synchronizing towards -" + nameof(BaseDatabase) + "- if the receiving end " +
            "(the AgoRapide based application) only needs to read data, " +
            "especially when a -" + nameof(InMemoryCache) + "- is used anyway for later querying. " +
            "A direct filebased storage system is then much more quicker and efficient, " +
            "and that is exactly what this class (-" + nameof(FileCache) + "-) provides. " +
            "\r\n" +
            "Note that usually -" + nameof(BaseSynchronizer) + "- will store -" + nameof(PropertyKeyAttribute.ExternalPrimaryKeyOf) + "--properties within -" + nameof(BaseDatabase) + "- though, " +
            "in order to link it with -" + nameof(DBField.id) + "- for use by -" + nameof(BaseSynchronizer) + "-. " +
            "\r\n" +
            "In addition data generated from within the AgoRapide based application " +
            "(properties not -" + nameof(PropertyKeyAttribute.IsExternal) + "-) " +
            "will also usually be stored in -" + nameof(BaseDatabase) + "-."
    )]
    public class FileCache : BaseCore {

        private FileCache() { }
        /// <summary>
        /// Singleton makes for easy inheriting of log-methods from <see cref="BaseCore"/>. 
        /// Apart from this need for logging the class could have just been made static instead.
        /// 
        /// TODO: Most probably logging is not "caught" at any external point anyway (see <see cref="BaseCore.LogEvent"/> and <see cref="BaseCore.Log"/> for explanation)
        /// </summary>
        public static readonly FileCache Instance = new FileCache();

        private const string RECORD_SEPARATOR = "\r\n-!-\r\n";
        /// <summary>
        /// TODO: Improve in this. Enable storing of strings containing linefeeds for instead. 
        /// </summary>
        private const string FIELD_SEPARATOR = "\r\n";

        private static ConcurrentDictionary<Type, List<PropertyKey>> _propertyKeyCache = new ConcurrentDictionary<Type, List<PropertyKey>>();
        [ClassMember(
            Description =
                "Returns which properties to store on disk (all that are -" + nameof(PropertyKeyAttribute.IsExternal) + "-), " +
                "ordering is significant (see also -" + nameof(GetFingerprint) + "-).",
            LongDescription =
                "In addition -" + nameof(CoreP.DBId) + "- is stored as first property (at index 0)"
            )]
        public static List<PropertyKey> GetProperties(Type type) => _propertyKeyCache.GetOrAdd(type, t => {
            var retval = new List<PropertyKey> { PropertyKeyMapper.GetA(CoreP.DBId) };
            retval.AddRange(type.GetChildProperties().Where(p =>
                p.Value.Key.A.IsExternal || // This is the normal criteria.
                p.Value.Key.PToString.EndsWith("CorrespondingInternalKey") /// This is a special case, we must either store this on disk (as done now (Sep 2017)), OR, recalculate the value when reading. The former is considered much more efficient. See <see cref="PropertyKeyMapper.MapEnum{T}"/> for more information. 
            ).Select(p => p.Value).ToList());
            return retval;
        });

        private static ConcurrentDictionary<Type, string> _fingerprintCache = new ConcurrentDictionary<Type, string>();
        [ClassMember(Description =
            "Returns fingerprint used to decide whether structure found on disk (as specified by -" + nameof(GetProperties) + "-) " +
            "is still compatible with (or rather identical to) the current implementation. " +
            "If not compatible then the locally stored structure has to be discarded and " +
            "a new call to -" + nameof(BaseSynchronizer.Synchronize) + "- has to be made for a complete update (at least for the given type)")]
        public static string GetFingerprint(Type type) => _fingerprintCache.GetOrAdd(type, t =>
            string.Join("\r\n", GetProperties(t).Select(p => p.Key.PToString)));

        [ClassMember(Description = "Stores all -" + nameof(PropertyKeyAttribute.IsExternal) + "--properties for the given entities to disk.")]
        public void StoreToDisk(BaseSynchronizer synchronizer, Type type, List<BaseEntity> entities) { // where T : BaseEntity, new() {
            InvalidTypeException.AssertAssignable(type, typeof(BaseEntity));
            Log(nameof(type) + ": " + type);
            var filepath = GetFilePath(synchronizer, type);
            Log(nameof(filepath) + ": " + filepath);
            var propertiesOrder = GetProperties(type);
            var dir = System.IO.Path.GetDirectoryName(filepath);
            if (!System.IO.Directory.Exists(dir)) {
                Log("Creating " + dir);
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(
                filepath,
                GetFingerprint(type) + RECORD_SEPARATOR +
                string.Join(RECORD_SEPARATOR, entities.Select(e =>
                    string.Join(FIELD_SEPARATOR, propertiesOrder.Select(p =>
                        e.PV(p, "")
                    ))
                )),
                Encoding.Default
            );
        }

        /// <summary>
        /// TODO: Find a better name for this method.
        /// TODO: TrySynchronizeFromDisk for instance (compared with synchronize from source). Or just TryReadFromDisk.
        /// 
        /// Return value false means that <see cref="BaseSynchronizer.Synchronize"/> has to be called for <paramref name="synchronizer"/>. 
        /// </summary>
        /// <param name="synchronizer"></param>
        /// <param name="type"></param>
        /// <param name="db"></param>
        /// <param name="errorResponse"></param>
        [ClassMember(Description = "Reads from disk -" + nameof(PropertyKeyAttribute.IsExternal) + "--entities earlier found by a -" + nameof(BaseSynchronizer) + "-.")]
        public bool TryEnrichFromDisk(BaseSynchronizer synchronizer, Type type, BaseDatabase db, out string errorResponse) {
            Log(nameof(type) + ": " + type);
            var returnFalseAdditionalInformation = "\r\nNote that the calling method will now most probably do a (very) time-consuming call against " + nameof(BaseSynchronizer.Synchronize);
            var filepath = GetFilePath(synchronizer, type);
            Log(nameof(filepath) + ": " + filepath);
            if (!System.IO.File.Exists(filepath)) {
                Log("!System.IO.File.Exists" + returnFalseAdditionalInformation);
                errorResponse = "File " + filepath + " not found";
                return false;
            }
            var recordsFromDisk = System.IO.File.ReadAllText(filepath, Encoding.Default).Split(RECORD_SEPARATOR);
            var first = true;
            var propertiesOrder = GetProperties(type);
            var resolution = "\r\n\r\nPossible resolution: Delete file\r\n" + filepath;
            var recordNo = -1;
            Dictionary<long, BaseEntity> entitiesFromDatabase = null;
            foreach (var r in recordsFromDisk) {
                if (((recordNo++) % 100) == 0) Log(nameof(recordNo) + ": " + recordNo + " of " + recordsFromDisk.Count);
                if (first) {
                    if (!r.Equals(GetFingerprint(type))) {
                        var msg = "Fingerprint mismatch, incorrect fingerprint found\r\n\r\n" +
                            r + "\r\n\r\n" +
                            "instead of\r\n\r\n" +
                            GetFingerprint(type);
                        Log(msg + returnFalseAdditionalInformation);
                        errorResponse = msg;
                        return false;
                    }
                    /// TODO: <see cref="BaseDatabase.GetAllEntities"/> is very slow as of Sep 2017. 
                    /// TODO: Improve by reading all properties in one go 
                    entitiesFromDatabase = db.GetAllEntities(type).ToDictionary(e => e.Id, e => e);
                    first = false;
                    continue;
                }
                var properties = r.Split(FIELD_SEPARATOR);
                if (properties.Count != propertiesOrder.Count) {
                    if ("".Equals(r)) continue; // Blank line (typical result if no entities exist at all)
                    throw new InvalidCountException(properties.Count, propertiesOrder.Count, r + resolution);
                }
                BaseEntity entity = null;
                var i = 0; propertiesOrder.ForEach(o => {
                    if (i > 0 && string.Empty.Equals(properties[i])) {
                        // This property just does not exist
                        i++; return;
                    }
                    if (!o.Key.TryValidateAndParse(properties[i], out var result)) throw new InvalidFileException(filepath, r, o, properties[i], result.ErrorResponse);
                    if (i == 0) { /// The primary key (<see cref="DBField.id"/>) is stored as first property
                        if (!entitiesFromDatabase.TryGetValue(result.Result.V<long>(), out entity)) throw new InvalidFileException(filepath, r, o, properties[i], "Not found in " + nameof(entitiesFromDatabase));
                        i++; return;
                    }
                    entity.Properties[o.Key.CoreP] = result.Result;
                    i++; return;
                });
            }
            errorResponse = null;
            return true;
        }

        private static string _dataFolder;
        private static string DataFolder => _dataFolder ?? (_dataFolder = System.IO.Path.GetDirectoryName(Util.Configuration.C.LogPath) + System.IO.Path.DirectorySeparatorChar + "Data" + System.IO.Path.DirectorySeparatorChar);

        private static string GetFilePath(BaseSynchronizer synchronizer, Type type) => DataFolder + synchronizer.Id + System.IO.Path.DirectorySeparatorChar + type.ToStringVeryShort() + ".txt";

        public class InvalidFileException : ApplicationException {
            public InvalidFileException(string filename, string record, PropertyKey key, string valueFound, string details) : base(
                "Invalid " + nameof(valueFound) + " '" + valueFound + "' as " + key.Key.PToString + " found in " + nameof(record) + "\r\n" + record + "\r\n" +
                nameof(details) + " : " + details + "\r\n" +
                "Possible resolution: Delete file\r\n" + filename + "\r\nand restart application") { }
            public InvalidFileException(string message) : base(message) { }
            public InvalidFileException(string message, Exception inner) : base(message, inner) { }
        }
    }
}