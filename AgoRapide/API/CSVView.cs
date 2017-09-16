using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide;
using AgoRapide.Core;

namespace AgoRapide.API {

    [Class(Description = "Generates -" + nameof(ResponseFormat.CSV) + "--view of results.")]
    class CSVView : BaseView {
        public CSVView(Request request) : base(request) { }

        public override object GenerateResult() {
            try {
                if (Request.Result == null) return JSONView.GenerateEmergencyResult(ResultCode.exception_error, "ERROR: No result-object available, very unexpected");

                var html = new StringBuilder();
                return html;
            } catch (Exception ex) {
                Util.LogException(ex);
                return JSONView.GenerateEmergencyResult(ResultCode.exception_error, "An exception of type " + ex.GetType() + " occurred in " + GetType() + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + ". See logfile on server for details"); // Details: " + Util.GetExeptionDetails(ex)); // Careful, do not give out details now
            }

        }
    }
}