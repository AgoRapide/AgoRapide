using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

namespace AgoRapide.Database {

    [Class(
        Description =
            "Caches information provided by -" + nameof(BaseSynchronizer) + "- in a local file storage (direct disk storage).",
        LongDescription =
            "The rationale is that there is no point in synchronizing towards -" + nameof(BaseDatabase) + "- if the receiving end " +
            "(the AgoRapide based application) only needs to read data the, " +
            "especially when a -" + nameof(InMemoryCache) + "- is used anyway for later querying. " +
            "A direct filebased storage system is then much more quicker and efficient, " +
            "and that is exactly what this class (-" + nameof(FileCache) + "-) provides. " +
            "\r\n" +
            "Note that usually -" + nameof(BaseSynchronizer) + "- will store the foreign entity id within -" + nameof(BaseDatabase) + "- though, " +
            "in order to link it with -" + nameof(DBField.id) + "- for use by -" + nameof(Synchronize) + "-. " +
            // ------------------------
            "In addition data generated from within the AgoRapide based application " +
            "(properties not -" + nameof(PropertyKeyAttribute.IsExternal) + "-) will also usually be stored in -" + nameof(BaseDatabase) + "-."
    )]
    public class FileCache : BaseCore {

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
            retval.AddRange(type.GetChildProperties().Where(p => p.Value.Key.A.IsExternal).Select(p => p.Value).ToList());
            return retval;
        });

        private static ConcurrentDictionary<Type, string> _fingerprintCache = new ConcurrentDictionary<Type, string>();
        [ClassMember(Description =
            "Returns fingerprint used to decide whether structure found on disk (as specified by -" + nameof(GetProperties) + "-) " +
            "is still compatible with the current implementation. If not then the locally stored structure has to be discarded and " +
            "a new call to -" + nameof(BaseSynchronizer) + "- has to be made for a complete update")]
        public static string GetFingerprint(Type type) => _fingerprintCache.GetOrAdd(type, t =>
            string.Join("\r\n", GetProperties(t).Select(p => p.Key.PToString)));

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entitiesFromDatabase">
        /// These entities would normally contain all properties which are not <see cref="PropertyKeyAttribute.IsExternal"/>
        /// </param>
        [ClassMember(Description =
            "Called at application startup. " +
            "Enriches the entities with -" + nameof(PropertyKeyAttribute.IsExternal) + "- as found on disk")]
        public void EnrichFromDisk<T>(Dictionary<long, T> entitiesFromDatabase, BaseSynchronizer synchronizer) where T : BaseEntity, new() {
            var type = typeof(T);
            var filepath = GetFilePath(type);
            Log(nameof(filepath) + ": " + filepath);
            if (!System.IO.File.Exists(filepath)) throw new NotImplementedException("Synchronizing for " + filepath + " (file not existing)");
            var recordsFromDisk = System.IO.File.ReadAllText(filepath).Split("\r\n--\r\n");
            var first = true;
            var propertiesOrder = GetProperties(type);
            var resolution = "\r\n\r\nPossible resolution: Delete file " + filepath;
            var recordNo = -1;
            recordsFromDisk.ForEach(r => {
                if (((recordNo++) % 100) == 0) Log(nameof(recordNo) + ": " + recordNo + " of " + recordsFromDisk.Count);

                if (first) {
                    if (!r.Equals(GetFingerprint(type))) throw new NotImplementedException("Synchronizing for " + filepath + " (changed fingerprint)");
                    first = false;
                    return;
                }
                var properties = r.Split("\r\n");
                if (properties.Count != propertiesOrder.Count) throw new InvalidCountException(properties.Count, propertiesOrder.Count, r + resolution);
                T entity = null;
                var i = 0; propertiesOrder.ForEach(o => {
                    if (!o.Key.TryValidateAndParse(properties[i], out var result)) throw new InvalidFileException(filepath, r, o, properties[i], result.ErrorResponse);
                    if (i == 0) {
                        if (!entitiesFromDatabase.TryGetValue(result.Result.V<long>(), out entity)) throw new InvalidFileException(filepath, r, o, properties[i], "Not found in " + nameof(entitiesFromDatabase));
                        return;
                    }
                    entity.Properties[o.Key.CoreP] = result.Result;
                    i++;
                });
            });
        }

        [ClassMember(Description = "Synchronizes file storage")]
        public void Synchronize<T>(BaseDatabase db, BaseSynchronizer synchronizer) where T : BaseEntity, new() {
            var entities = db.GetAllEntities<T>();
        }

        private static string _dataFolder;
        private static string DataFolder => _dataFolder ?? System.IO.Path.GetDirectoryName(Util.Configuration.C.LogPath) + System.IO.Path.DirectorySeparatorChar + "Data" + System.IO.Path.DirectorySeparatorChar;

        private static string GetFilePath(Type type) => DataFolder + type.ToStringVeryShort() + ".txt";

        public class InvalidFileException : ApplicationException {
            public InvalidFileException(string filename, string record, PropertyKey key, string valueFound, string details) : base(
                "Invalid " + nameof(valueFound) + " '" + valueFound + "' as " + key.Key.PToString + " found in " + nameof(record) + "\r\n" + record + "\r\n" +
                nameof(details) + " : " + details + "\r\n" +
                "Possible resolution: Delete file " + filename) { }
            public InvalidFileException(string message) : base(message) { }
            public InvalidFileException(string message, Exception inner) : base(message, inner) { }
        }

    }
}
