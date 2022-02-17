using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using dataWorkServiceProtoBuf;

namespace com.mirle.ibg3k0.sc.Service.dataWorkerService
{
    public class dataWorkService : Greeter.GreeterBase //: Greeter.GreeterBase
    {

        public event EventHandler<alarmHappendArgs> alarmHappend;
        public class alarmHappendArgs
        {
            public alarmHappendArgs(string eq_id, string unit_id, int alarm_id, string alarm_desc, string memo)
            {
                EQ_ID = eq_id;
                UNIT_ID = unit_id;
                ALARM_ID = alarm_id;
                ALARM_DESC = alarm_desc;
                MEMO = memo;
            }

            public string EQ_ID { get; }
            public string UNIT_ID { get; }
            public int ALARM_ID { get; }
            public string ALARM_DESC { get; }
            public string MEMO { get; }
            public override string ToString()
            {
                return $"EQ:{EQ_ID}, UNIT_ID:{UNIT_ID}, ALARM_ID:{ALARM_ID.ToString()}, ALARM_DESC:{ALARM_DESC}, MEMO:{MEMO}";
            }
        }

        public override Task<reportAGVReply> IamMethod(reportAGVRequest req, ServerCallContext context)
        {
            reportAGVReply reply = new reportAGVReply();
            reply.Datetime = DateTime.Now.ToString();
            this.alarmHappend?.Invoke(this, new alarmHappendArgs(
                req.EQID, req.UNITID, req.AlarmID, req.AlarmDesc, req.Memo));
            return Task.FromResult(reply);
        }
    }
}
