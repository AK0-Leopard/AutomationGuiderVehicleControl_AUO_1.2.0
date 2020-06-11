// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="S2F42.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
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
    /// Class S2F42.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.stc.Data.SecsData.SXFY" />
    public class S2F42 : SXFY
    {


        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        public string HCACK;
        /// <summary>
        /// The hcack
        /// </summary>
        [SecsElement(Index = 2)]
        public RPYITEM ITEM;
        public S2F42()
        {
            StreamFunction = "S2F42";
            W_Bit = 0;
        }

        public class RPYITEM : SXFY
        {
            //[SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 1)]
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]//修改長度為64 MarkChou 20190329
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
            public string CPACK;

        }


    }
    public class S2F42_CARRIER_CANCEL_ABORT : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        public string HCACK;
        /// <summary>
        /// The hcack
        /// </summary>
        [SecsElement(Index = 2)]
        public RPYITEM ITEM;
        /// <summary>
        /// Initializes a new instance of the <see cref="S2F42"/> class.
        /// </summary>
        public S2F42_CARRIER_CANCEL_ABORT()
        {
            StreamFunction = "S2F42";
            W_Bit = 0;
        }

        public class RPYITEM : SXFY
        {

            [SecsElement(Index = 1)]
            public COMMNADINFO CMDINFO;
            [SecsElement(Index = 2)]
            public CARRIERINFO CAINFO;
            //[SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 1)]

            public class COMMNADINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                public string CPACK;
            }
            public class CARRIERINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
                public string CPACK;
            }
        }



    }

    public class S2F42_RENAME : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_BINARY, Length = 1)]
        public string HCACK;
        /// <summary>
        /// The hcack
        /// </summary>
        [SecsElement(Index = 2)]
        public RPYITEM ITEM;
        /// <summary>
        /// Initializes a new instance of the <see cref="S2F42"/> class.
        /// </summary>
        public S2F42_RENAME()
        {
            StreamFunction = "S2F42";
            W_Bit = 0;
        }

        public class RPYITEM : SXFY
        {

            [SecsElement(Index = 1)]
            public COMMNADINFO CMDINFO;
            [SecsElement(Index = 2)]
            public CARRIERINFO PRECAINFO;
            [SecsElement(Index = 3)]
            public CARRIERINFO NEWCAINFO;
            [SecsElement(Index = 4)]
            public CARRIERLOCINFO CLINFO;
            //[SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 1)]

            public class COMMNADINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                public string CPVAL;
            }
            public class CARRIERINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 12)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                public string CPVAL;
            }
            public class CARRIERLOCINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 10)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
                public string CPVAL;
            }
        }



    }
}
