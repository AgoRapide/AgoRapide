// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;
using AgoRapide.API;

namespace AgoRapide.Database {

    /// <summary>
    /// 
    /// 
    /// 
    /// </summary>
    [Class(
        Description =
            "Synchronizes data from external data storage " +
            "(for instance a CRM system from which the AgoRapide based application will analyze data).",
        LongDescription =
            "The data found is stored within -" + nameof(FileCache) + "-, only identifiers are stored within -" + nameof(BaseDatabase) + "-"
    )]
    public abstract class BaseSynchronizer : APIDataObject {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">
        /// This is mostly a hint about what needs to be synchronized. 
        /// It is up to the synchronizer to do a fuller synchronization if deemed necessary or practical. 
        /// </typeparam>
        /// <param name="db"></param>
        /// <param name="fileCache"></param>
        [ClassMember(Description = "Synchronizes between local database / local file storage and external source")]
        public abstract void Synchronize<T>(BaseDatabase db, FileCache fileCache, Result result) where T : BaseEntity, new();

        protected void Reconcile<T>(List<T> externalEntities, BaseDatabase db, FileCache fileCache, Result result) where T : BaseEntity, new() {

            var type = typeof(T);
            var primaryKey = type.GetChildProperties().Values.Single(k => k.Key.A.PrimaryKeyOf != null, () => nameof(PropertyKeyAttribute.PrimaryKeyOf) + " != null for " + type);

            /// TODO: Add some identificator here for "workspace" or similar.
            /// TODO: And call <see cref="BaseDatabase.TryGetEntities"/> instead with query like
            /// TOOD: WHERE workspace = {workspaceId}
            /// TODO: We could have workSpaceId as a property of ourselves. The id could be the same as 
            /// TODO: the <see cref="DBField.id"/> of a root administrative <see cref="Person"/> for our customer.
            var internalEntities = db.GetAllEntities<T>();
            var internalEntitiesByPrimaryKey = internalEntities.ToDictionary(e => e.PV<long>(primaryKey), e => e);
            externalEntities.ForEach(e => { /// Reconcile through <see cref="PropertyKeyAttribute.PrimaryKeyOf"/>
                if (!internalEntitiesByPrimaryKey.TryGetValue(e.PV<long>(primaryKey), out var internalEntity)) {
                    internalEntity = db.GetEntityById<T>(db.CreateEntity<T>(Id,
                        new List<(PropertyKeyWithIndex key, object value)> {
                            (primaryKey.PropertyKeyWithIndex, e.PV<long>(primaryKey))
                        }, result));                    
                }
                // Transfer from internal to external (assumed to the least amount to transfer)
                e.Id = internalEntity.Id;                
                e.AddProperty(CoreP.DBId.A(), e.Id); // TODO:  Most probably unnecessary. Should be contained below. 
                internalEntity.Properties.Values.ForEach(p => e.AddProperty(p, null));
            });
            fileCache.StoreToDisk(externalEntities);
        }

        ///// <summary>
        ///// TDOO: REPLACE WITH DETERMINISTIC (PSEUDO-RANDOM) GENERATION BASED ON ATTRIBUTES
        ///// 
        ///// TODO: TO BE DELETED!
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="properties"></param>
        ///// <returns></returns>
        //protected static T EntityGenerator<T>(List<Property> properties) where T : BaseEntity, new() {
        //    var retval = new T();
        //    properties.ForEach(p => retval.AddProperty(p, null));
        //    return retval;
        //}
    }
}
