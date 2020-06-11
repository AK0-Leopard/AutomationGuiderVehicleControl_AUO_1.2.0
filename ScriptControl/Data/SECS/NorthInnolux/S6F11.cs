//*********************************************************************************
//      FloydAlgorithmRouteGuide.cs
//*********************************************************************************
// File Name: FloydAlgorithmRouteGuide.cs
// Description: 
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
// 2019/08/26    Kevin Wei      N/A            A0.01   由於發現在上報CEID =108時，
//                                                     會有多上報一層的問題，針對該問題進行修正該問題。
//**********************************************************************************
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
    /// Event Report Send
    /// </summary>
    [Serializable]
    public class S6F11 : SXFY
    {
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string CEID;
        [SecsElement(Index = 3)]
        public RPTINFO INFO;

        public S6F11()
        {
            StreamFunction = "S6F11";
            StreamFunctionName = "Transfer Event repor";
            W_Bit = 1;
            IsBaseType = true;
            INFO = new RPTINFO();
        }
        public override string ToString()
        {
            return string.Concat(StreamFunction, "-", CEID);
        }

        [Serializable]
        public class RPTINFO : SXFY
        {
            [SecsElement(Index = 1, ListSpreadOut = true)]
            public RPTITEM[] ITEM;
            [Serializable]
            public class RPTITEM : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                public string RPTID;
                [SecsElement(Index = 2)]
                public SXFY[] VIDITEM;


                [Serializable]
                public class VIDITEM_51 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_55[] CARRIER_INFO;
                }

                [Serializable]
                public class VIDITEM_52 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_66[] TRANSFER_COMMANDs;
                    public VIDITEM_52()
                    {
                        TRANSFER_COMMANDs = new VIDITEM_66[1];
                        TRANSFER_COMMANDs[0] = new VIDITEM_66();
                    }
                }

                [Serializable]
                public class VIDITEM_53 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_71[] VEHICLEINFO;
                    public VIDITEM_53()
                    {
                        VEHICLEINFO = new VIDITEM_71[1];
                        VEHICLEINFO[0] = new VIDITEM_71();
                    }
                }

                [Serializable]
                public class VIDITEM_01 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string ALID;
                    public VIDITEM_01()
                    {
                        ALID = string.Empty;
                    }
                }



                [Serializable]
                public class VIDITEM_1060 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                    public string ALARM_TEXT;
                    public VIDITEM_1060()
                    {
                        ALARM_TEXT = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1070 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string ALARM_LOC;
                    public VIDITEM_1070()
                    {
                        ALARM_LOC = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_03 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, ListElementLength = 1)]
                    public string[] ALIDs;
                    public VIDITEM_03()
                    {
                        ALIDs = new string[0];
                    }
                }
                [Serializable]
                public class VIDITEM_04 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, ListElementLength = 1)]
                    public string[] ALIDs;
                    public VIDITEM_04()
                    {
                        ALIDs = new string[0];
                    }
                }
                [Serializable]
                public class VIDITEM_310 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_308[] VEHICLEP_PASS_COUNT_INFO;
                    public VIDITEM_310()
                    {
                        VEHICLEP_PASS_COUNT_INFO = new VIDITEM_308[1];
                        VEHICLEP_PASS_COUNT_INFO[0] = new VIDITEM_308();
                    }
                }


                [Serializable]
                public class VIDITEM_302 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string ASSIGN_MODE;
                    public VIDITEM_302()
                    {
                        ASSIGN_MODE = string.Empty;
                    }
                }



                [Serializable]
                public class VIDITEM_55 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string VEHICLE_ID;
                    [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_LOC;

                    public VIDITEM_55()
                    {
                        CARRIER_ID = String.Empty;
                        VEHICLE_ID = String.Empty;
                        CARRIER_LOC = String.Empty;
                    }
                }


                [Serializable]
                public class VIDITEM_05 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 16)]
                    public string CLOCK;
                    public VIDITEM_05()
                    {
                        CLOCK = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_57 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
                    public string COMMAND_NAME;
                    public VIDITEM_57()
                    {
                        COMMAND_NAME = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_58 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string COMMAND_ID;
                    public VIDITEM_58()
                    {
                        COMMAND_ID = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_59 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string COMMAND_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PRIORITY;
                    [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string REPLACE;
                    public VIDITEM_59()
                    {
                        COMMAND_ID = string.Empty;
                        PRIORITY = string.Empty;
                        REPLACE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_06 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string CONTROLSTATE;
                    public VIDITEM_06()
                    {
                        CONTROLSTATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_74 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_313[] PORT_INFO;
                    public VIDITEM_74()
                    {
                        PORT_INFO = new VIDITEM_313[1];
                        PORT_INFO[0] = new VIDITEM_313();
                    }
                }





                [Serializable]
                public class VIDITEM_75 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_315[] PORT_LOAD_INFO;
                    public VIDITEM_75()
                    {
                        PORT_LOAD_INFO = new VIDITEM_315[1];
                        PORT_LOAD_INFO[0] = new VIDITEM_315();
                    }
                }




                //[Serializable]
                //public class VIDITEM_76 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public VIDITEM_320[] IOInfo;
                //    public VIDITEM_76()
                //    {
                //        IOInfo = new VIDITEM_320[1];
                //        IOInfo[0] = new VIDITEM_320();
                //    }
                //}





                [Serializable]
                public class VIDITEM_60 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string DEST_PORT;
                    public VIDITEM_60()
                    {
                        DEST_PORT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_304 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string EMPTY_VEHICLE_COUNT;
                    public VIDITEM_304()
                    {
                        EMPTY_VEHICLE_COUNT = string.Empty;
                    }
                }


                [Serializable]
                public class VIDITEM_07 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, ListElementLength = 1)]
                    public string[] CEIDs;
                    public VIDITEM_07()
                    {
                        CEIDs = new string[0];
                    }
                }

                [Serializable]
                public class VIDITEM_305 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string FRONT_EMPTY_VEHICLE_COUNT;
                    public VIDITEM_305()
                    {
                        FRONT_EMPTY_VEHICLE_COUNT = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_307 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string FULL_VEHICLE_COUNT;
                    public VIDITEM_307()
                    {
                        FULL_VEHICLE_COUNT = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_318 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string ID_RESULT_CODE;
                    public VIDITEM_318()
                    {
                        ID_RESULT_CODE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_320 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string EQP_NAME;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string IO_UNIT_ID;
                    [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string IO_UNIT_STATE;
                    [SecsElement(Index = 4, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 4)]
                    public string IO_DATA;
                    public VIDITEM_320()
                    {
                        EQP_NAME = string.Empty;
                        IO_UNIT_ID = string.Empty;
                        IO_UNIT_STATE = string.Empty;
                        IO_DATA = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_321 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 4)]
                    public string IO_DATA;
                    public VIDITEM_321()
                    {
                        IO_DATA = string.Empty;
                    }
                }


                [Serializable]
                public class VIDITEM_322 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string IO_UNIT_ID;
                    public VIDITEM_322()
                    {
                        IO_UNIT_ID = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_323 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string IO_UNIT_STATE;
                    public VIDITEM_323()
                    {
                        IO_UNIT_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_311 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string PORT_ID;
                    public VIDITEM_311()
                    {
                        PORT_ID = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_312 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PORT_STATE;
                    public VIDITEM_312()
                    {
                        PORT_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_313 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string PORT_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PORT_TRANSFER_STATE;
                    public VIDITEM_313()
                    {
                        PORT_ID = string.Empty;
                        PORT_TRANSFER_STATE = string.Empty;
                    }
                }

                public class VIDITEM_314 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PORT_TRANSFER_STATE;
                    public VIDITEM_314()
                    {
                        PORT_TRANSFER_STATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_315 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string PORT_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PORT_LOAD_STATE;
                    public VIDITEM_315()
                    {
                        PORT_ID = string.Empty;
                        PORT_LOAD_STATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_316 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PORT_LOAD_STATE;
                    public VIDITEM_316()
                    {
                        PORT_LOAD_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_62 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PRIORITY;
                    public VIDITEM_62()
                    {
                        PRIORITY = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1210 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PREVIOUS_CONTROL_STATE;
                    public VIDITEM_1210()
                    {
                        PREVIOUS_CONTROL_STATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1240 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string PREVIOUS_TSC_STATE;
                    public VIDITEM_1240()
                    {
                        PREVIOUS_TSC_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_76 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string READ_CARRRIER_ID;
                    public VIDITEM_76()
                    {
                        READ_CARRRIER_ID = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_317 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string ID_RESULT_CODE;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string READ_CARRRIER_ID;
                    public VIDITEM_317()
                    {
                        ID_RESULT_CODE = string.Empty;
                        READ_CARRRIER_ID = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_306 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string REAR_EMPTY_VEHICLE_COUNT;
                    public VIDITEM_306()
                    {
                        REAR_EMPTY_VEHICLE_COUNT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_63 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string REPLACE;
                    public VIDITEM_63()
                    {
                        REPLACE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_64 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string RESULT_CODE;
                    public VIDITEM_64()
                    {
                        RESULT_CODE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_303 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string SCU_NAME;
                    public VIDITEM_303()
                    {
                        SCU_NAME = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_65 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string SOURCE_PORT;
                    public VIDITEM_65()
                    {
                        SOURCE_PORT = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_66 : SXFY
                {
                    [SecsElement(Index = 1)]
                    public VIDITEM_59 COMMAND_INFO;
                    [SecsElement(Index = 2)]
                    public VIDITEM_67[] TRANSFER_INFOs;
                    public VIDITEM_66()
                    {
                        COMMAND_INFO = new VIDITEM_59();
                        TRANSFER_INFOs = new VIDITEM_67[1];
                        TRANSFER_INFOs[0] = new VIDITEM_67();
                    }
                }
                [Serializable]
                public class VIDITEM_67 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public CARRIER_INFO[] CARRIER_INFOs;

                    public VIDITEM_67()
                    {
                        CARRIER_INFOs = new CARRIER_INFO[1];
                        CARRIER_INFOs[0] = new CARRIER_INFO();
                    }
                    [Serializable]
                    public class CARRIER_INFO: SXFY
                    {
                        [SecsElement(Index = 1, ListSpreadOut = true)]
                        public VIDITEM_54 CARRIER_ID;
                        [SecsElement(Index = 2, ListSpreadOut = true)]
                        public VIDITEM_65 SOURCE_PORT;
                        [SecsElement(Index = 3, ListSpreadOut = true)]
                        public VIDITEM_60 DEST_PORT;

                        public CARRIER_INFO()
                        {
                            CARRIER_ID = new VIDITEM_54();
                            SOURCE_PORT = new VIDITEM_65();
                            DEST_PORT = new VIDITEM_60();
                        }
                    }
                }
                [Serializable]
                public class VIDITEM_68 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string TRANSFER_PORT;
                    public VIDITEM_68()
                    {
                        TRANSFER_PORT = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_69 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_68[] TRANSFER_PORTs;
                    public VIDITEM_69()
                    {
                        TRANSFER_PORTs = new VIDITEM_68[1];
                        TRANSFER_PORTs[0] = new VIDITEM_68();
                    }
                }
                [Serializable]
                public class VIDITEM_201 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string TS_AVAIL;
                    public VIDITEM_201()
                    {
                        TS_AVAIL = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_73 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string TSC_STATE;
                    public VIDITEM_73()
                    {
                        TSC_STATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1230 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string TSC_STATE_AVAILABILITY;
                    public VIDITEM_1230()
                    {
                        TSC_STATE_AVAILABILITY = string.Empty;
                    }
                }


                [Serializable]
                public class VIDITEM_71 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string VEHICLE_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_STATE;
                    public VIDITEM_71()
                    {
                        VEHICLE_ID = string.Empty;
                        VEHICLE_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1650 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_AUTO;
                    public VIDITEM_1650()
                    {
                        VEHICLE_AUTO = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_308 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string SCU_NAME;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string EMPTY_VEHICLE_COUNT;
                    [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string FRONT_EMPTY_VEHICLE_COUNT;
                    [SecsElement(Index = 4, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string REAR_EMPTY_VEHICLE_COUNT;
                    [SecsElement(Index = 5, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string FULL_VEHICLE_COUNT;
                    [SecsElement(Index = 6, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_PASS_TIME;

                    public VIDITEM_308()
                    {
                        SCU_NAME = String.Empty;
                        EMPTY_VEHICLE_COUNT = String.Empty;
                        FRONT_EMPTY_VEHICLE_COUNT = String.Empty;
                        REAR_EMPTY_VEHICLE_COUNT = String.Empty;
                        FULL_VEHICLE_COUNT = String.Empty;
                        VEHICLE_PASS_TIME = String.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_309 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_PASS_TIME;
                    public VIDITEM_309()
                    {
                        VEHICLE_PASS_TIME = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1001 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string ALARM_LEVEL;
                    public VIDITEM_1001()
                    {
                        ALARM_LEVEL = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1002 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string FLAG_FOR_ALARM_REPORT;
                    public VIDITEM_1002()
                    {
                        FLAG_FOR_ALARM_REPORT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1003 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CLASSIFICATION;
                    public VIDITEM_1003()
                    {
                        CLASSIFICATION = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_380 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
                    public string COMMAND_TYPE;
                    public VIDITEM_380()
                    {
                        COMMAND_TYPE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_402 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
                    public string COMMAND_TYPE;
                    public VIDITEM_402()
                    {
                        COMMAND_TYPE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_401 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string VEHICLE_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_STATE;
                    [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_FORK_STATUS;
                    [SecsElement(Index = 4, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_LOC;
                    [SecsElement(Index = 5, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string VEHICLE_DISTANCE;
                    [SecsElement(Index = 6, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
                    public string ENROLL_ACTIVE_CARRIER;
                    public VIDITEM_401()
                    {
                        VEHICLE_ID = string.Empty;
                        VEHICLE_STATE = string.Empty;
                        VEHICLE_FORK_STATUS = string.Empty;
                        CARRIER_LOC = string.Empty;
                        VEHICLE_DISTANCE = string.Empty;
                        ENROLL_ACTIVE_CARRIER = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_70 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string VEHICLE_ID;
                    public VIDITEM_70()
                    {
                        VEHICLE_ID = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_72 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_STATE;
                    public VIDITEM_72()
                    {
                        VEHICLE_STATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_56 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_LOC;
                    public VIDITEM_56()
                    {
                        CARRIER_LOC = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_403 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_FORK_STATE;
                    public VIDITEM_403()
                    {
                        VEHICLE_FORK_STATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_404 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string VEHICLE_DISTANCE;
                    public VIDITEM_404()
                    {
                        VEHICLE_DISTANCE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_405 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true)]
                    public VIDITEM_406[] ENROLL_ACTIVE_CARRIERs;
                    public VIDITEM_405()
                    {
                        ENROLL_ACTIVE_CARRIERs = new VIDITEM_406[1];
                        ENROLL_ACTIVE_CARRIERs[0] = new VIDITEM_406();
                    }
                }
                [Serializable]
                public class VIDITEM_406 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_ID;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_PORT;
                    public VIDITEM_406()
                    {
                        CARRIER_ID = string.Empty;
                        VEHICLE_PORT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_54 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CARRIER_ID;
                    public VIDITEM_54()
                    {
                        CARRIER_ID = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_407 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_PORT;
                    public VIDITEM_407()
                    {
                        VEHICLE_PORT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1660 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string MAIN_STATUS;
                    public VIDITEM_1660()
                    {
                        MAIN_STATUS = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1670 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string COUPLER_STATUS_1;
                    [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string COUPLER_STATUS_2;
                    public VIDITEM_1670()
                    {
                        COUPLER_STATUS_1 = string.Empty;
                        COUPLER_STATUS_2 = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1671 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string COUPLER_STATUS_1;
                    public VIDITEM_1671()
                    {
                        COUPLER_STATUS_1 = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1672 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string COUPLER_STATUS_2;
                    public VIDITEM_1672()
                    {
                        COUPLER_STATUS_2 = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1700 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string VEHICLE_TX_STATUS;
                    public VIDITEM_1700()
                    {
                        VEHICLE_TX_STATUS = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1710 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string LOAD_STATUS;
                    public VIDITEM_1710()
                    {
                        LOAD_STATUS = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_61 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                    public string EQP_NAME;
                    public VIDITEM_61()
                    {
                        EQP_NAME = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_2 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string EC_TIMEOUT;
                    public VIDITEM_2()
                    {
                        EC_TIMEOUT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1010 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string INITIALCOMMUNICATIONSTATE;
                    public VIDITEM_1010()
                    {
                        INITIALCOMMUNICATIONSTATE = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1020 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string INITIALCONTROLSTATE;
                    public VIDITEM_1020()
                    {
                        INITIALCONTROLSTATE = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1040 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 6)]
                    public string SOFTWARE_REVISION;
                    public VIDITEM_1040()
                    {
                        SOFTWARE_REVISION = string.Empty;
                    }
                }

                [Serializable]
                public class VIDITEM_1050 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string TIMEFORMAT;
                    public VIDITEM_1050()
                    {
                        TIMEFORMAT = string.Empty;
                    }
                }
                [Serializable]
                public class VIDITEM_1030 : SXFY
                {
                    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 6)]
                    public string MDLN;
                    public VIDITEM_1030()
                    {
                        MDLN = string.Empty;
                    }
                }





                //[Serializable]
                //public class VIDITEM_60 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
                //    public string COMMAND_TYPE;
                //    public VIDITEM_60()
                //    {
                //        COMMAND_TYPE = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_61 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                //    public string DEST_PORT;
                //    public VIDITEM_61()
                //    {
                //        DEST_PORT = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_62 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public ENHANCED_CARRIER_INFO[] ENHANCED_CARRIER_INFOs;

                //    public VIDITEM_62()
                //    {
                //        ENHANCED_CARRIER_INFOs = new ENHANCED_CARRIER_INFO[1];
                //        ENHANCED_CARRIER_INFOs[0] = new ENHANCED_CARRIER_INFO();
                //    }
                //    public class ENHANCED_CARRIER_INFO : SXFY
                //    {
                //        [SecsElement(Index = 1, ListSpreadOut = true)]
                //        public VIDITEM_54 CARRIER_ID_OBJ;
                //        [SecsElement(Index = 2, ListSpreadOut = true)]
                //        public VIDITEM_74 VEHICLE_ID_OBJ;
                //        [SecsElement(Index = 3, ListSpreadOut = true)]
                //        public VIDITEM_56 CARRIER_LOC_OBJ;
                //        [SecsElement(Index = 4, ListSpreadOut = true)]
                //        public VIDITEM_64_2 INSTALL_TIME_OBJ;

                //        public ENHANCED_CARRIER_INFO()
                //        {
                //            CARRIER_ID_OBJ = new VIDITEM_54();
                //            VEHICLE_ID_OBJ = new VIDITEM_74();
                //            CARRIER_LOC_OBJ = new VIDITEM_56();
                //            INSTALL_TIME_OBJ = new VIDITEM_64_2();
                //        }
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_63 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public ENHANCED_TRANSFER_COMMAND[] ENHANCED_CARRIER_INFOs;

                //    public VIDITEM_63()
                //    {
                //        ENHANCED_CARRIER_INFOs = new ENHANCED_TRANSFER_COMMAND[1];
                //        ENHANCED_CARRIER_INFOs[0] = new ENHANCED_TRANSFER_COMMAND();
                //    }
                //    public class ENHANCED_TRANSFER_COMMAND : SXFY
                //    {
                //        [SecsElement(Index = 1, ListSpreadOut = true)]
                //        public VIDITEM_59 COMMAND_INFO_OBJ;
                //        [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //        public VIDITEM_72_2 TRANSFER_STATE;
                //        [SecsElement(Index = 3, ListSpreadOut = true)]
                //        public VIDITEM_70[] TRANSFER_INFOS;

                //        public ENHANCED_TRANSFER_COMMAND()
                //        {
                //            COMMAND_INFO_OBJ = new VIDITEM_59();
                //            TRANSFER_STATE = new VIDITEM_72_2();
                //            TRANSFER_INFOS = new VIDITEM_70[1];
                //            TRANSFER_INFOS[0] = new VIDITEM_70();
                //        }
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_64 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                //    public string EQP_NAME;
                //    public VIDITEM_64()
                //    {
                //        EQP_NAME = string.Empty;
                //    }
                //}
                //public class VIDITEM_64_2 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 16)]
                //    public string INSTALL_TIME;
                //    public VIDITEM_64_2()
                //    {
                //        INSTALL_TIME = string.Empty;
                //    }
                //}





                //[Serializable]
                //public class VIDITEM_69 : SXFY
                //{
                //    [SecsElement(Index = 1)]
                //    public VIDITEM_59 COMMAND_INFO;
                //    //[SecsElement(Index = 2)]
                //    [SecsElement(Index = 2)]
                //    //A0.01 public VIDITEM_70[] TRANSFER_INFOs;
                //    public VIDITEM_70 TRANSFER_INFO;//A0.01
                //    public VIDITEM_69()
                //    {
                //        COMMAND_INFO = new VIDITEM_59();
                //        //A0.01 TRANSFER_INFOs = new VIDITEM_70[1];
                //        //A0.01 TRANSFER_INFOs[0] = new VIDITEM_70();
                //        TRANSFER_INFO = new VIDITEM_70();//A0.01 
                //    }
                //}





                //[Serializable]
                //public class VIDITEM_72 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public VIDITEM_71[] TRANSFER_PORT_LIST;
                //    public VIDITEM_72()
                //    {
                //        TRANSFER_PORT_LIST = new VIDITEM_71[1];
                //        TRANSFER_PORT_LIST[0] = new VIDITEM_71();
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_72_2 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //    public string TRANSFER_STATE;
                //    public VIDITEM_72_2()
                //    {
                //        TRANSFER_STATE = string.Empty;
                //    }
                //}






                //[Serializable]
                //public class VIDITEM_75 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public VEHICLEINFO VHINFO;
                //    public VIDITEM_75()
                //    {
                //        VHINFO = new VEHICLEINFO()
                //        {
                //            VEHICLE_ID = string.Empty,
                //            VEHICLE_STATE = string.Empty
                //        };
                //    }
                //    [Serializable]
                //    public class VEHICLEINFO : SXFY
                //    {
                //        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                //        public string VEHICLE_ID;
                //        [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //        public string VEHICLE_STATE;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_76 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //    public string VEHICLE_STATE;
                //    public VIDITEM_76()
                //    {
                //        VEHICLE_STATE = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_301 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public TRANSFER_COMPLETE_INFO[] TRANSFER_COMPLETE_INFOs;
                //    public VIDITEM_301()
                //    {
                //        TRANSFER_COMPLETE_INFOs = new TRANSFER_COMPLETE_INFO[1];
                //        TRANSFER_COMPLETE_INFOs[0] = new TRANSFER_COMPLETE_INFO();
                //    }

                //    [Serializable]
                //    public class TRANSFER_COMPLETE_INFO : SXFY
                //    {
                //        [SecsElement(Index = 1)]
                //        public VIDITEM_70 TRANSFER_INFO_OBJ;
                //        [SecsElement(Index = 2)]
                //        public VIDITEM_56 CARRIER_LOC_OBJ;
                //        public TRANSFER_COMPLETE_INFO()
                //        {
                //            TRANSFER_INFO_OBJ = new VIDITEM_70();
                //            CARRIER_LOC_OBJ = new VIDITEM_56();
                //        }
                //    }
                //}



                //[Serializable]
                //public class VIDITEM_303 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //    public string PORT_EVT_STATE;
                //    public VIDITEM_303()
                //    {
                //        PORT_EVT_STATE = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_304 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public PORTEVENTSTATE PESTATE;
                //    public VIDITEM_304()
                //    {
                //        PESTATE = new PORTEVENTSTATE()
                //        {
                //            PORT_ID = new VIDITEM_311(),
                //            PORT_EVT_STATE = new VIDITEM_303()
                //        };
                //    }
                //    [Serializable]
                //    public class PORTEVENTSTATE : SXFY
                //    {
                //        [SecsElement(Index = 1)]
                //        public VIDITEM_311 PORT_ID;
                //        [SecsElement(Index = 2)]
                //        public VIDITEM_303 PORT_EVT_STATE;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_305 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true)]
                //    public VIDITEM_304[] PORT_EVENT_STATEs;
                //    public VIDITEM_305()
                //    {
                //        PORT_EVENT_STATEs = new VIDITEM_304[1];
                //        PORT_EVENT_STATEs[0] = new VIDITEM_304();
                //    }
                //}



                //[Serializable]
                //public class VIDITEM_902 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                //    public string CHANGER_ID;
                //    public VIDITEM_902()
                //    {
                //        CHANGER_ID = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_903 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                //    public string ERROR_CODE;
                //    public VIDITEM_903()
                //    {
                //        ERROR_CODE = string.Empty;
                //    }
                //}

                //[Serializable]
                //public class VIDITEM_904 : SXFY
                //{
                //    [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 32)]
                //    public string UNIT_ID;
                //    public VIDITEM_904()
                //    {
                //        UNIT_ID = string.Empty;
                //    }
                //}

            }
        }
    }
    public class VIDCollection
    {
        public VIDCollection()
        {
            VID_51_ActiveCarriers = new S6F11.RPTINFO.RPTITEM.VIDITEM_51();
            VID_52_ActiveTransfers = new S6F11.RPTINFO.RPTITEM.VIDITEM_52();
            VID_53_ActiveVehicles = new S6F11.RPTINFO.RPTITEM.VIDITEM_53();
            VID_01_AlarmID = new S6F11.RPTINFO.RPTITEM.VIDITEM_01();
            VID_1060_AlarmText = new S6F11.RPTINFO.RPTITEM.VIDITEM_1060();
            VID_1070_AlarmLoc = new S6F11.RPTINFO.RPTITEM.VIDITEM_1070();
            VID_03_AlarmEnabled = new S6F11.RPTINFO.RPTITEM.VIDITEM_03();
            VID_04_AlarmSet = new S6F11.RPTINFO.RPTITEM.VIDITEM_04();
            VID_310_AllVehiclePassCountInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_310();
            VID_302_AssignMode = new S6F11.RPTINFO.RPTITEM.VIDITEM_302();
            VID_54_CarrierID = new S6F11.RPTINFO.RPTITEM.VIDITEM_54();
            VID_55_CarrierInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_55();
            VID_56_CarrierLoc = new S6F11.RPTINFO.RPTITEM.VIDITEM_56();
            VID_05_Clock = new S6F11.RPTINFO.RPTITEM.VIDITEM_05();
            VID_57_CommandName = new S6F11.RPTINFO.RPTITEM.VIDITEM_57();
            VID_58_CommandID = new S6F11.RPTINFO.RPTITEM.VIDITEM_58();
            VID_59_CommandInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_59();
            VID_06_ControlState = new S6F11.RPTINFO.RPTITEM.VIDITEM_06();
            VID_74_CurrentPortStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_74();
            VID_75_CurrentLoadStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_75();
            VID_60_DestPort = new S6F11.RPTINFO.RPTITEM.VIDITEM_60();
            VID_304_EmptyVehicleCount = new S6F11.RPTINFO.RPTITEM.VIDITEM_304();
            VID_07_EventEnabled = new S6F11.RPTINFO.RPTITEM.VIDITEM_07();
            VID_305_FrontEmptyVehicleCount = new S6F11.RPTINFO.RPTITEM.VIDITEM_305();
            VID_307_FullVehicleCount = new S6F11.RPTINFO.RPTITEM.VIDITEM_307();
            VID_318_IDResultCode = new S6F11.RPTINFO.RPTITEM.VIDITEM_318();
            VID_320_IOInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_320();
            VID_321_IOData = new S6F11.RPTINFO.RPTITEM.VIDITEM_321();
            VID_322_IOUnitID = new S6F11.RPTINFO.RPTITEM.VIDITEM_322();
            VID_323_IOUnitState = new S6F11.RPTINFO.RPTITEM.VIDITEM_323();
            VID_311_PortID = new S6F11.RPTINFO.RPTITEM.VIDITEM_311();
            VID_312_PortState = new S6F11.RPTINFO.RPTITEM.VIDITEM_312();
            VID_313_PortInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_313();
            VID_314_PortTransferState = new S6F11.RPTINFO.RPTITEM.VIDITEM_314();
            VID_315_PortLoadInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_315();
            VID_316_PortLoadState = new S6F11.RPTINFO.RPTITEM.VIDITEM_316();
            VID_62_Priority = new S6F11.RPTINFO.RPTITEM.VIDITEM_62();
            VID_1210_PriviousControlState = new S6F11.RPTINFO.RPTITEM.VIDITEM_1210();
            VID_1240_PriviousTSCState = new S6F11.RPTINFO.RPTITEM.VIDITEM_1240();
            VID_76_ReadCarrierID = new S6F11.RPTINFO.RPTITEM.VIDITEM_76();
            VID_317_ReadIDInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_317();
            VID_306_RearEmptyVehicleCount = new S6F11.RPTINFO.RPTITEM.VIDITEM_306();
            VID_63_Replace = new S6F11.RPTINFO.RPTITEM.VIDITEM_63();
            VID_64_ResultCode = new S6F11.RPTINFO.RPTITEM.VIDITEM_64();
            VID_303_SCUName = new S6F11.RPTINFO.RPTITEM.VIDITEM_303();
            VID_65_SourcePort = new S6F11.RPTINFO.RPTITEM.VIDITEM_65();
            VID_66_TransferCommand = new S6F11.RPTINFO.RPTITEM.VIDITEM_66();
            VID_67_TransferInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_67();
            VID_68_TransferPort = new S6F11.RPTINFO.RPTITEM.VIDITEM_68();
            VID_69_TransferPortList = new S6F11.RPTINFO.RPTITEM.VIDITEM_69();
            VID_201_TSAvail = new S6F11.RPTINFO.RPTITEM.VIDITEM_201();
            VID_73_TSCState = new S6F11.RPTINFO.RPTITEM.VIDITEM_73();
            VID_1230_TSAvailability = new S6F11.RPTINFO.RPTITEM.VIDITEM_1230();
            VID_70_VehicleID = new S6F11.RPTINFO.RPTITEM.VIDITEM_70();
            VID_71_VehicleInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_71();
            VID_72_VehicleState = new S6F11.RPTINFO.RPTITEM.VIDITEM_72();
            VID_1650_VehicleAuto = new S6F11.RPTINFO.RPTITEM.VIDITEM_1650();
            VID_308_VehiclePassCountInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_308();
            VID_309_VehiclePassTime = new S6F11.RPTINFO.RPTITEM.VIDITEM_309();
            VID_1001_AlarmLevel = new S6F11.RPTINFO.RPTITEM.VIDITEM_1001();
            VID_1002_FlagForAlarmReport = new S6F11.RPTINFO.RPTITEM.VIDITEM_1002();
            VID_1003_Classification = new S6F11.RPTINFO.RPTITEM.VIDITEM_1003();
            VID_380_CommandType = new S6F11.RPTINFO.RPTITEM.VIDITEM_380();
            VID_402_EnrollActiveVehicles = new S6F11.RPTINFO.RPTITEM.VIDITEM_402();
            VID_401_EnrollVehicleInfo = new S6F11.RPTINFO.RPTITEM.VIDITEM_401();
            VID_403_VehicleForkStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_403();
            VID_404_VehicleDistance = new S6F11.RPTINFO.RPTITEM.VIDITEM_404();
            VID_405_EnrollActiveCarrier = new S6F11.RPTINFO.RPTITEM.VIDITEM_405();
            VID_406_EnrollVehicleCarrier = new S6F11.RPTINFO.RPTITEM.VIDITEM_406();
            VID_407_VehiclePort = new S6F11.RPTINFO.RPTITEM.VIDITEM_407();
            VID_1660_MainStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_1660();
            VID_1670_CaplerStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_1670();
            VID_1671_CaplerStatus1 = new S6F11.RPTINFO.RPTITEM.VIDITEM_1671();
            VID_1672_CaplerStatus2 = new S6F11.RPTINFO.RPTITEM.VIDITEM_1672();
            VID_1700_VehicleTXStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_1700();
            VID_1710_LoadStatus = new S6F11.RPTINFO.RPTITEM.VIDITEM_1710();
            VID_61_EqpName = new S6F11.RPTINFO.RPTITEM.VIDITEM_61();
            VID_2_EstablishCommunicationTimeout = new S6F11.RPTINFO.RPTITEM.VIDITEM_2();
            VID_1010_InitialCommunicationState = new S6F11.RPTINFO.RPTITEM.VIDITEM_1010();
            VID_1020_InitialControlState = new S6F11.RPTINFO.RPTITEM.VIDITEM_1020();
            VID_1040_SoftRevision = new S6F11.RPTINFO.RPTITEM.VIDITEM_1040();
            VID_1050_TimeFormat = new S6F11.RPTINFO.RPTITEM.VIDITEM_1050();
            VID_1030_ModelName = new S6F11.RPTINFO.RPTITEM.VIDITEM_1030();
        }
        public string VH_ID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_51 VID_51_ActiveCarriers;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_52 VID_52_ActiveTransfers;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_53 VID_53_ActiveVehicles;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_01 VID_01_AlarmID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1060 VID_1060_AlarmText;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1070 VID_1070_AlarmLoc;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_03 VID_03_AlarmEnabled;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_04 VID_04_AlarmSet;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_310 VID_310_AllVehiclePassCountInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_302 VID_302_AssignMode;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_54 VID_54_CarrierID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_55 VID_55_CarrierInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_56 VID_56_CarrierLoc;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_05 VID_05_Clock;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_57 VID_57_CommandName;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_58 VID_58_CommandID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_59 VID_59_CommandInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_06 VID_06_ControlState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_74 VID_74_CurrentPortStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_75 VID_75_CurrentLoadStatus;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_76 VID_76_CurrentIOStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_60 VID_60_DestPort;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_304 VID_304_EmptyVehicleCount;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_07 VID_07_EventEnabled;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_305 VID_305_FrontEmptyVehicleCount;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_307 VID_307_FullVehicleCount;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_318 VID_318_IDResultCode;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_320 VID_320_IOInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_321 VID_321_IOData;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_322 VID_322_IOUnitID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_323 VID_323_IOUnitState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_311 VID_311_PortID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_312 VID_312_PortState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_313 VID_313_PortInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_314 VID_314_PortTransferState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_315 VID_315_PortLoadInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_316 VID_316_PortLoadState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_62 VID_62_Priority;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1210 VID_1210_PriviousControlState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1240 VID_1240_PriviousTSCState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_76 VID_76_ReadCarrierID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_317 VID_317_ReadIDInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_306 VID_306_RearEmptyVehicleCount;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_63 VID_63_Replace;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_64 VID_64_ResultCode;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_303 VID_303_SCUName;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_65 VID_65_SourcePort;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_66 VID_66_TransferCommand;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_67 VID_67_TransferInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_68 VID_68_TransferPort;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_69 VID_69_TransferPortList;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_201 VID_201_TSAvail;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_73 VID_73_TSCState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1230 VID_1230_TSAvailability;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_70 VID_70_VehicleID;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_71 VID_71_VehicleInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_72 VID_72_VehicleState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1650 VID_1650_VehicleAuto;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_308 VID_308_VehiclePassCountInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_309 VID_309_VehiclePassTime;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1001 VID_1001_AlarmLevel;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1002 VID_1002_FlagForAlarmReport;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1003 VID_1003_Classification;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_380 VID_380_CommandType;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_402 VID_402_EnrollActiveVehicles;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_401 VID_401_EnrollVehicleInfo;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_403 VID_403_VehicleForkStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_404 VID_404_VehicleDistance;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_405 VID_405_EnrollActiveCarrier;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_406 VID_406_EnrollVehicleCarrier;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_407 VID_407_VehiclePort;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1660 VID_1660_MainStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1670 VID_1670_CaplerStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1671 VID_1671_CaplerStatus1;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1672 VID_1672_CaplerStatus2;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1700 VID_1700_VehicleTXStatus;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1710 VID_1710_LoadStatus;

        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_61 VID_61_EqpName;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_2 VID_2_EstablishCommunicationTimeout;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1010 VID_1010_InitialCommunicationState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1020 VID_1020_InitialControlState;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1040 VID_1040_SoftRevision;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1050 VID_1050_TimeFormat;
        public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_1030 VID_1030_ModelName;












        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_60 VID_60_CommandType;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_62 VID_62_EnhancedCarriers;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_63 VID_63_EnhancedTransfers;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_64 VID_64_EqpName;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_65 VID_65_Priority;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_66 VID_66_Replace;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_67 VID_67_ResultCode;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_68 VID_68_SourcePort;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_69 VID_69_TransferCommand;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_70 VID_70_TransferInfo;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_71 VID_71_TransferPort;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_72 VID_72_TransferPortList;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_76 VID_76_VehicleState;

        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_301 VID_301_TransferCompleteInfo;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_303 VID_303_PortEvtState;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_304 VID_304_PortEventState;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_305 VID_305_RegisteredPorts;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_308 VID_308_VehiclePassCountInfo1;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_311 VID_311_PortID;

        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_902 VID_902_ChargerID;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_903 VID_903_ErrorCode;
        //public com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux.S6F11.RPTINFO.RPTITEM.VIDITEM_904 VID_904_UnitID;
    }

}
