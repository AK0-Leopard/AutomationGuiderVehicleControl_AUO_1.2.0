//------------------------------------------------------------------------------
// <auto-generated>
//    這個程式碼是由範本產生。
//
//    對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//    如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace com.mirle.ibg3k0.sc
{
    using System;
    using System.Collections.Generic;
    
    public partial class ACMD_OHTC
    {
        public string CMD_ID { get; set; }
        public string VH_ID { get; set; }
        public string CARRIER_ID { get; set; }
        public string CMD_ID_MCS { get; set; }
        public E_CMD_TYPE CMD_TPYE { get; set; }
        public string SOURCE { get; set; }
        public string DESTINATION { get; set; }
        public int PRIORITY { get; set; }
        public Nullable<System.DateTime> CMD_START_TIME { get; set; }
        public Nullable<System.DateTime> CMD_END_TIME { get; set; }
        public E_CMD_STATUS CMD_STAUS { get; set; }
        public int CMD_PROGRESS { get; set; }
        public Nullable<int> INTERRUPTED_REASON { get; set; }
        public int ESTIMATED_TIME { get; set; }
        public int ESTIMATED_EXCESS_TIME { get; set; }
        public Nullable<int> REAL_CMP_TIME { get; set; }
        public Nullable<com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.CompleteStatus> COMPLETE_STATUS { get; set; }
    }
}
