using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;

namespace com.mirle.ibg3k0.sc.Data.SECS.NorthInnolux
{
    public class S2F50 : SXFY
    {
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        public string HCACK;
        [SecsElement(Index = 2)]
        public SXFY[] CEPCOLLECT;

        public S2F50()
        {
            StreamFunction = "S2F50";
            StreamFunctionName = "Enhanced Remote Command Ack";
            W_Bit = 0;
        }

        //public class REPITEM : SXFY
        //{
        //    //[SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        //    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]//修改長度為64 MarkChou 20190329
        //    public string CPNAME;
        //    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        //    public string CPACK;
        //}


        public class COMMANDINFO : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 11)]
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
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
                public class PRIORITY : SXFY
                {
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
                public class REPLACE : SXFY
                {
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
            }

        }
        public class TRANSFERINFO : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 12)]
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
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
                public class SOURCEPORT : SXFY
                {
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 10)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
                public class DESTPORT : SXFY
                {
                    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 8)]
                    public string CPNAME;
                    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                    public string CPVAL;
                }
            }

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
            public string CPVAL_A;
        }

        public class CP_Unknown : SXFY
        {
            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            public string CPVAL_U;
        }

        public class CEPITEM : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
            public string NAME;
            [SecsElement(Index = 2)]
            public CP_U1[] CPINFO;
        }

        public class CEPITEMS : SXFY
        {
            [SecsElement(Index = 1, ListSpreadOut = true)]
            public CEPITEM[] CEPINFO;
        }

        public class CP_U1 : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_1_BYTE_UNSIGNED_INTEGER, Length = 1)]
            public string CEPACK;
        }


        //public class CEPCOLLECT : SXFY
        //{
        //    [SecsElement(Index = 1, ListSpreadOut = true)]
        //    public CEP[] CEPS;
        //    public class CEP : SXFY
        //    {
        //        [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        //        public string NAME;
        //        [SecsElement(Index = 2)]
        //        public VALUE CPINFO;
        //        public class VALUE : SXFY
        //        {
        //            [SecsElement(Index = 1, ListSpreadOut = true)]
        //            public CP_U1[] CPS;
        //        }
        //        public class CP_U1 : SXFY
        //        {
        //            [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
        //            public string CPNAME;
        //            [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_1_BYTE_UNSIGNED_INTEGER, Length = 64)]
        //            public string CEPACK;
        //        }

        //    }


        //}

    }
}
