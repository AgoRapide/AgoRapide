// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Core;

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
        /// This mostly a hint about what needs to be synchronized. 
        /// It is up to the synchronizer to do a fuller synchronization if deemed necessary or practical. 
        /// </typeparam>
        /// <param name="db"></param>
        /// <param name="fileCache"></param>
        [ClassMember(Description = "Synchronizes between local database / local file storage and external source")]
        public abstract void Synchronize<T>(BaseDatabase db, FileCache fileCache) where T : BaseEntity, new();
    }
}
