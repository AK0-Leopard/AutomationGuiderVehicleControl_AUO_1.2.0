﻿using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.StateMachine;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.VO
{
    public class TrafficController : AEQPT
    {
        TrafficControlInfo TrafficControlInfo;
        TrafficControlStateMachine StateMachine;
        Stopwatch StopwatchLastAGVRequestTIme;
        //最大間隔詢問時間的常數設定值
        public const int MAX_AGV_REQUEST_INTRRVAL_TIME_MS = 5_000;
        public bool IsRightOfWayReturning { get; private set; } = false;
        public bool IsRightOfWayCanceling { get; private set; } = false;

        public TrafficController(TrafficControlInfo trafficControlInfo)
        {
            TrafficControlInfo = trafficControlInfo;
            StateMachine = new TrafficControlStateMachine();
            StopwatchLastAGVRequestTIme = new Stopwatch();
        }



        public string[] ControlSections
        {
            get { return TrafficControlInfo.ControlSections; }
        }
        public bool IsReadyReplyPass { get; set; }

        public void AGVRepuestEntry()
        {

            if (StateMachine.canFire(TrafficControlStateMachine.Trigger.AGVRepuestEntry))
                StateMachine.AGVRepuestEntry();
            StopwatchLastAGVRequestTIme.Restart();
        }
        //向Server發出通過的請求
        public void AGVCRequestForRightOfWay()
        {
            bool is_wirte_sucess = getExcuteMapAction().SendTrafficSignalMirlePassAsk(true);
            if (is_wirte_sucess)
            {
                StateMachine.AGVCRequestForRightOfWay();
            }
        }

        public void AGVCAcquireRightOfWay()
        {
            StateMachine.AcquireRightOfWay();
        }
        public void ReturnRightOfWay()
        {
            try
            {
                IsRightOfWayReturning = true;
                bool is_wirte_sucess = getExcuteMapAction().SendTrafficSignalMirlePassAsk(false);
                if (is_wirte_sucess)
                {
                    StateMachine.ReturnRightOfWay();
                }
            }
            finally
            {
                IsRightOfWayReturning = false;
            }
        }
        public void CancelRequestForRightOfWay(bool isForce = false)
        {
            try
            {
                IsRightOfWayCanceling = true;
                bool is_wirte_sucess = getExcuteMapAction().SendTrafficSignalMirlePassAsk(false);
                if (is_wirte_sucess || isForce)
                {
                    StateMachine.CancelRequestForRightOfWay();
                }
            }
            finally
            {
                IsRightOfWayCanceling = false;
            }
        }


        private TrafficSingalDefaultValueDefMapAction getExcuteMapAction()
        {
            TrafficSingalDefaultValueDefMapAction mapAction;
            mapAction = this.getMapActionByIdentityKey(typeof(TrafficSingalDefaultValueDefMapAction).Name) as TrafficSingalDefaultValueDefMapAction;
            return mapAction;
        }



        public bool IsInControlSection(string secID)
        {
            return TrafficControlInfo.ControlSections.Contains(secID);
        }
        public bool IsOverAGVRequestTime()
        {
            return StopwatchLastAGVRequestTIme.ElapsedMilliseconds > MAX_AGV_REQUEST_INTRRVAL_TIME_MS;
        }

        public bool IsNoAGVWiilPass(BLL.VehicleBLL vehicleBLL, BLL.ReserveBLL reserveBLL)
        {
            List<string> traffic_controller_sections = TrafficControlInfo.ControlSections.ToList();
            //1.確認是否有AGV在管制道路上
            //var vhs = vehicleBLL.cache.loadVhBySectionIDs(traffic_controller_sections);
            //if (vhs.Any())
            //{
            //    return false;
            //}
            //1.確認是否有AGV會通過
            var vhs = vehicleBLL.cache.loadHasPassSectionVhBySectionIDs(traffic_controller_sections);
            if (vhs.Any())
            {
                StopwatchLastAGVRequestTIme.Restart();
                return false;
            }

            //2.確認是否有AGV預約了在管制道路上路線
            var reserved_sections = reserveBLL.HasVhReserveSections(traffic_controller_sections);
            if (reserved_sections.hasReserve)
            {
                StopwatchLastAGVRequestTIme.Restart();
                return false;
            }
            //3.如果沒有在預約管制的道路上時，再等一下，確認是否已經超過AGV 預約的間隔時間
            if (!IsOverAGVRequestTime())
            {
                return false;
            }
            return true;
        }
        public bool IsNoAGVWiilPass(BLL.CMDBLL cmdBLL, BLL.VehicleBLL vehicleBLL ,BLL.AddressesBLL addressesBLL)
        {
            if(cmdBLL.isCMD_OHTCExcute())
            {
                StopwatchLastAGVRequestTIme.Restart();
                return false;
            }
            var vhs = vehicleBLL.cache.loadAllVh();
            foreach(var vh in vhs)
            {
                var adr_obj = addressesBLL.cache.GetAddress(vh.CUR_ADR_ID);
                if (adr_obj == null) continue;
                if (adr_obj.IsCanNotStay)
                {
                    StopwatchLastAGVRequestTIme.Restart();
                    return false;
                }
            }
            if (!IsOverAGVRequestTime())
            {
                return false;
            }
            return true;
        }

        public bool IsAllowedEntry => StateMachine.IsAllowedEntry;

        public TrafficControlStateMachine.State TrafficControlState => StateMachine.TrafficControlState;

        public void RestartStopwatchLastAGVRequestTIme()
        {
            StopwatchLastAGVRequestTIme.Restart();
        }
    }

}
