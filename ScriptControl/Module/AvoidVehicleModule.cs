using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System.Collections.Generic;
using System.Linq;

namespace com.mirle.ibg3k0.sc.Module
{
    public class AvoidVehicleModule
    {
        private VehicleBLL vehicleBLL = null;
        private SectionBLL sectionBLL = null;
        public AvoidVehicleModule()
        {

        }
        public void start(SCApplication app)
        {
            vehicleBLL = app.VehicleBLL;
            sectionBLL = app.SectionBLL;
        }



    }
}
