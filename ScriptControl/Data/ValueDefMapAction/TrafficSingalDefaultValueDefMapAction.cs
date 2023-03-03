//*********************************************************************************
//      MTLValueDefMapAction.cs
//*********************************************************************************
// File Name: MTLValueDefMapAction.cs
// Description: 
//
//(c) Copyright 2018, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;

namespace com.mirle.ibg3k0.sc.Data.ValueDefMapAction
{
    public class TrafficSingalDefaultValueDefMapAction : IValueDefMapAction
    {
        public const string DEVICE_NAME_MTL = "EQ";
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        TrafficController eqpt = null;
        ASEGMENT segment = null;
        protected SCApplication scApp = null;
        protected BCFApplication bcfApp = null;
        public TrafficSingalDefaultValueDefMapAction()
            : base()
        {
            scApp = SCApplication.getInstance();
            bcfApp = scApp.getBCFApplication();
        }

        public virtual string getIdentityKey()
        {
            return this.GetType().Name;
        }
        public virtual void setContext(BaseEQObject baseEQ)
        {
            this.eqpt = baseEQ as TrafficController;

        }
        public virtual void unRegisterEvent()
        {
            //not implement
        }
        public virtual void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            try
            {
                switch (runLevel)
                {
                    case BCFAppConstants.RUN_LEVEL.ZERO:

                        break;
                    case BCFAppConstants.RUN_LEVEL.ONE:
                        break;
                    case BCFAppConstants.RUN_LEVEL.TWO:
                        break;
                    case BCFAppConstants.RUN_LEVEL.NINE:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }
        }

        private void TrafficSignalAUOPassAckChange(object sender, ValueChangedEventArgs e)
        {
            var function = scApp.getFunBaseObj<TrafficSingalCanPassCheck>(eqpt.EQPT_ID) as TrafficSingalCanPassCheck;
            try
            {
                //1.建立各個Function物件
                function.Read(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.read log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());

                eqpt.IsReadyReplyPass = function.CanPass;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
            finally
            {
                scApp.putFunBaseObj<TrafficSingalCanPassCheck>(function);
            }
        }

        public bool SendTrafficSignalMirlePassAsk(bool signal)
        {
            var function =
                scApp.getFunBaseObj<TrafficSingalPassRequest>(eqpt.EQPT_ID) as TrafficSingalPassRequest;
            try
            {
                //1.建立各個Function物件
                function.RequestPass = signal;
                function.Write(bcfApp, eqpt.EqptObjectCate, eqpt.EQPT_ID);
                //2.write log
                function.Timestamp = DateTime.Now;
                LogManager.GetLogger("com.mirle.ibg3k0.sc.Common.LogHelper").Info(function.ToString());
                return true;

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return false;
            }
            finally
            {
                scApp.putFunBaseObj<TrafficSingalPassRequest>(function);
            }
        }
        string event_id = string.Empty;
        /// <summary>
        /// Does the initialize.
        /// </summary>
        public virtual void doInit()
        {
            try
            {
                ValueRead traffic_singnal_auo_agvc_ack_pass_vr = null;
                if (bcfApp.tryGetReadValueEventstring(SCAppConstants.EQPT_OBJECT_CATE_EQPT, eqpt.EQPT_ID, "TRAFFIC_SINGNAL_AUO_AGVC_ACK_PASS", out traffic_singnal_auo_agvc_ack_pass_vr))
                {
                    traffic_singnal_auo_agvc_ack_pass_vr.afterValueChange += (_sender, _e) => TrafficSignalAUOPassAckChange(_sender, _e);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exection:");
            }

        }

    }
}
