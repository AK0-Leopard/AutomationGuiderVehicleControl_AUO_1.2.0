using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static com.mirle.ibg3k0.sc.ALINE;
using static com.mirle.ibg3k0.sc.AVEHICLE;

namespace com.mirle.ibg3k0.sc.Service
{
    public class UserControlService
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected SCApplication scApp = null;
        protected ReportBLL reportBLL = null;
        protected LineBLL lineBLL = null;
        protected ALINE line = null;
        public UserControlService()
        {

        }
        public void start(SCApplication _app)
        {
            scApp = _app;
            reportBLL = _app.ReportBLL;
            lineBLL = _app.LineBLL;
            line = scApp.getEQObjCacheManager().getLine();


            //initPublish(line);
        }


    }
}
