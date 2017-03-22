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
    public class APIMethodDocumentator<TProperty> where TProperty : struct, IFormattable, IConvertible, IComparable {

        public static void DocumentMethods(List<APIMethod<TProperty>> methods, Request<TProperty> request) {
            //System.IO.File.WriteAllText(Util.Configuration.ApiDocumentationRootPath + Util.Configuration.ApiDocumentationIndexFilename,

            //    "<table>" + methods.First() +
            //    string.Join("", methods.Select(m => m.ToHTMLTableRow(null))) +
            //    "</table>",System.Text.Encoding.UTF8);
        }
    }
}
