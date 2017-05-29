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
    public class BaseSynchronizer : APIDataObject {
    }
}
