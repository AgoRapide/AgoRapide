using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;
namespace AgoRapide.API {

    /// <summary>
    /// TEST 2016-02-16
    /// 
    /// TODO: REMOVE THIS CLASS now that documentation if offered through general API mechanism
    /// 
    /// Documents the API methods. 
    /// Generates HTML-files. 
    /// 
    /// TODO: MAYBE TO BE REMOVED? Since storing in database / generating as ordinary entities instead?
    /// </summary>
    public class APIMethodDocumentator { 

        public static void DocumentMethods(List<APIMethod> methods, Request request) {
            //System.IO.File.WriteAllText(Util.Configuration.ApiDocumentationRootPath + Util.Configuration.ApiDocumentationIndexFilename,

            //    "<table>" + methods.First() +
            //    string.Join("", methods.Select(m => m.ToHTMLTableRow(null))) +
            //    "</table>",System.Text.Encoding.UTF8);
        }
    }
}
