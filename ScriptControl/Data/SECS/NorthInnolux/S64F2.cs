using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;

namespace com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux
{
    /// <summary>
    /// Individual Report Data
    /// </summary>
    [Serializable]
    public class S64F2 : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        public string ACK641;
        public S64F2()
        {
            StreamFunction = "S64F2";
            StreamFunctionName = "Acknowledgement to Destination ChangeRequest";
            W_Bit = 1;
            IsBaseType = true;
        }
    }
}
