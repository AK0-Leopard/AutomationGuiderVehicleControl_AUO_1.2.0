using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;

namespace com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux
{
    public class S2F49 : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 0)]
        public string OBJSPEC;
        [SecsElement(Index = 3, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
        public string RCMD;
        [SecsElement(Index = 4)]
        public REPITEM ITEM;

        public class REPITEM : SXFY
        {
            [SecsElement(Index = 1)]
            public COMMANDINFO CommandInfo;
            [SecsElement(Index = 2)]
            public TRANSFERINFO TranferInfo;
        }
        public class COMMANDINFO : SXFY
        {
            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 11)]
            public string CPNAME;
            [SecsElement(Index = 2)]
            public INFO Info;


            public class INFO : SXFY
            {
                [SecsElement(Index = 1)]
                public COMMANDID CommandID;
                [SecsElement(Index = 2)]
                public PRIORITY Priority;
                [SecsElement(Index = 3)]
                public REPLACE Replace;
                public class COMMANDID : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CPVAL;
                }
                public class PRIORITY : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string CPVAL;
                }
                public class REPLACE : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                    public string CPVAL;
                }
            }

        }
        public class TRANSFERINFO : SXFY
        {
            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 12)]
            public string CPNAME;
            [SecsElement(Index = 2)]
            public INFO Info;

            public class INFO : SXFY
            {
                [SecsElement(Index = 1)]
                public CARRIERID CarrierID;
                [SecsElement(Index = 2)]
                public SOURCEPORT SourcePort;
                [SecsElement(Index = 3)]
                public DESTPORT DestPort;
                public class CARRIERID : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CPVAL;
                }
                public class SOURCEPORT : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 10)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CPVAL;
                }
                public class DESTPORT : SXFY
                {
                    [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                    public string CPVAL;
                }
            }

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
        [SecsElement(Index = 1, ListSpreadOut = true, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
        public string DATAID;
        [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 0)]
        public string OBJSPEC;
        [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
        public string RCMD;
        [SecsElement(Index = 4)]
        public COMM[] COMMS;

        public S2F49_TRANSFER()
        {
            StreamFunction = "S2F49";
            StreamFunctionName = "Enhanced Remote Command Extension";
            W_Bit = 1;
        }


        public class COMM : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
            public string COMMANDINFONAME;
            [SecsElement(Index = 2)]
            public COMMVALUE COMMAINFOVALUE;

            public class COMMVALUE : SXFY
            {
                [SecsElement(Index = 1)]
                public CP_ASCII CPVAL1;
                [SecsElement(Index = 2)]
                public CP_Unknown CPVAL2;
                [SecsElement(Index = 3)]
                public CP_Unknown CPVAL3;
            }
        }
        public class CP_ASCII : SXFY
        {
            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)] //Modify Length 1 > 10 By Kevin
            public string CPVAL;
        }

        public class CP_Unknown : SXFY
        {
            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            public string CPVAL;
        }

    }
}
