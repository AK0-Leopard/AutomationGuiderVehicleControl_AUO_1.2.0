using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;

namespace com.mirle.ibg3k0.sc.Data.SECS.UMTC
{
    public class S2F49 : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        public string OBJSPEC;
        //[SecsElement(Index = 3, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        [SecsElement(Index = 3, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]//修改長度為20 MarkChou 20190329
        public string RCMD;
        [SecsElement(Index = 4)]
        public REPITEM[] REPITEMS;
        public class REPITEM : SXFY
        {
            //[SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
            //public string CPNAME;
            //[SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            //public string CPVAL;
        }

        public S2F49()
        {
            StreamFunction = "S2F49";
            StreamFunctionName = "Enhanced Remote Command Extension";
            W_Bit = 1;
        }
    }

    public class S2F49_STAGE : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        public string OBJSPEC;
        [SecsElement(Index = 3, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        public string RCMD;
        [SecsElement(Index = 4)]
        public REPITEM REPITEMS;
        public class REPITEM : SXFY
        {
            [SecsElement(Index = 1)]
            public REPITEM_STAGEINOF STAGEINOF;

            [SecsElement(Index = 2)]
            public REPITEM_TRANSFERINFO TRANSFERINFO;


            public class REPITEM_STAGEINOF : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2)]
                public REPITEM_STAGEINOF_CPVALUE CPVALUE;
                public class REPITEM_STAGEINOF_CPVALUE : SXFY
                {
                    [SecsElement(Index = 1)]
                    public REPITEM_ASCII STAGEID_CP;
                    [SecsElement(Index = 2)]
                    public REPITEM_U2 PRIORITY_CP;
                    [SecsElement(Index = 3)]
                    public REPITEM_U2 REPLACE_CP;
                    [SecsElement(Index = 4)]
                    public REPITEM_U2 EXPECTEDDURATION_CP;
                    [SecsElement(Index = 5)]
                    public REPITEM_U2 NOBLOCKINGTIME_CP;
                    [SecsElement(Index = 6)]
                    public REPITEM_U2 WAITTIMEOUT_CP;


                }
            }
            public class REPITEM_TRANSFERINFO : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2)]
                public REPITEM_TRANSFERINFO_CPVALUE CPVALUE;
                public class REPITEM_TRANSFERINFO_CPVALUE : SXFY
                {
                    [SecsElement(Index = 1)]
                    public REPITEM_ASCII CARRIERID_CP;
                    [SecsElement(Index = 2)]
                    public REPITEM_ASCII SOURCEPORT_CP;
                    [SecsElement(Index = 3)]
                    public REPITEM_ASCII DESTPORT_CP;
                }
            }

            public class REPITEM_ASCII : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME2;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPVAL_ASCII;
            }
            public class REPITEM_U2 : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME3;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                public string CPVAL_U2;
            }


        }



        public S2F49_STAGE()
        {
            StreamFunction = "S2F49";
            StreamFunctionName = "Enhanced Remote Command Extension";
            W_Bit = 1;
        }


    }

    public class S2F49_TRANSFER : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_4_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        public string OBJSPEC;
        [SecsElement(Index = 3, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        public string RCMD;
        [SecsElement(Index = 4)]
        public REPITEM REPITEMS;

        public S2F49_TRANSFER()
        {
            StreamFunction = "S2F49";
            StreamFunctionName = "Enhanced Remote Command Extension";
            W_Bit = 1;
        }

        public class REPITEM : SXFY
        {
            [SecsElement(Index = 1)]
            public COMM COMMINFO;
            [SecsElement(Index = 2)]
            public TRAN TRANINFO;

            public class COMM : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string COMMANDINFONAME;
                [SecsElement(Index = 2)]
                public COMMVALUE COMMAINFOVALUE;

                public class COMMVALUE : SXFY
                {
                    [SecsElement(Index = 1)]
                    public CP_ASCII COMMANDID;
                    [SecsElement(Index = 2)]
                    public CP_U2 PRIORITY;
                    [SecsElement(Index = 3)]
                    public CP_U2 REPLACE;
                }
            }
            public class TRAN : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string TRANSFERINFONAME;
                [SecsElement(Index = 2)]
                public TRANVALUE TRANSFERINFOVALUE;

                public class TRANVALUE : SXFY
                {
                    [SecsElement(Index = 1)]
                    public CP_ASCII CARRIERIDINFO;
                    [SecsElement(Index = 2)]
                    public CP_ASCII SOUINFO;
                    [SecsElement(Index = 3)]
                    public CP_ASCII DESTINFO;
                }
            }
            public class CP_ASCII : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)] //Modify Length 1 > 10 By Kevin
                public string CPVAL;
            }

            public class CP_U2 : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                public string CPVAL;
            }

        }
    }
}
