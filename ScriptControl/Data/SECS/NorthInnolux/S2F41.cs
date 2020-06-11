// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="S2F41.cs" company="">
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
    /// Class S2F41.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.stc.Data.SecsData.SXFY" />
    public class S2F41 : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
        public string RCMD;

        //[SecsElement(Index = 2)]
        //public REPITEM[] REPITEMS;

        /// <summary>
        /// Initializes a new instance of the <see cref="S2F41"/> class.
        /// </summary>
        public S2F41()
        {
            StreamFunction = "S2F41";
            IsBaseType = true;
        }
        //public class REPITEM : SXFY
        //{
        //    //[SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
        //    [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]//修改長度為64 MarkChou 20190329
        //    public string CPNAME;
        //    [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
        //    public string CPVAL;
        //}
    }
    public class S2F41_CANCEL_ABORT_TRANSFER : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 6)]
        public string RCMD;

        [SecsElement(Index = 2)]
        public REPITEM ITEM;

        /// <summary>
        /// Initializes a new instance of the <see cref="S2F41"/> class.
        /// </summary>
        public S2F41_CANCEL_ABORT_TRANSFER()
        {
            StreamFunction = "S2F41";
            IsBaseType = true;
        }
        public class REPITEM : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            public string CPVAL;
        }
    }

    public class S2F41_CARRIER_CANCEL_ABORT_TRANSFER : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 13)]
        public string RCMD;

        [SecsElement(Index = 2)]
        public COMMANDINFO CMDITEM;
        [SecsElement(Index = 3)]
        public CARRIERINFO CAITEM;
        /// <summary>
        /// Initializes a new instance of the <see cref="S2F41"/> class.
        /// </summary>
        public S2F41_CARRIER_CANCEL_ABORT_TRANSFER()
        {
            StreamFunction = "S2F41";
            IsBaseType = true;
        }
        public class COMMANDINFO : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            public string CPVAL;
        }
        public class CARRIERINFO : SXFY
        {
            [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
            public string CPNAME;
            [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
            public string CPVAL;
        }
    }

    public class S2F41_RENAME : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 13)]
        public string RCMD;
        [SecsElement(Index = 2)]
        public RPTITEM ITEM;

        /// <summary>
        /// Initializes a new instance of the <see cref="S2F41"/> class.
        /// </summary>
        public S2F41_RENAME()
        {
            StreamFunction = "S2F41";
            IsBaseType = true;
        }
        public class RPTITEM : SXFY
        {
            [SecsElement(Index = 1)]
            public COMMANDINFO CMDITEM;
            [SecsElement(Index = 2)]
            public CARRIERINFO PRECAITEM;
            [SecsElement(Index = 3)]
            public CARRIERINFO NEWCAITEM;
            [SecsElement(Index = 4)]
            public CARRIERLOCINFO CLITEM;
            public class COMMANDINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 9)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
                public string CPVAL;
            }
            public class CARRIERINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 12)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
                public string CPVAL;
            }
            public class CARRIERLOCINFO : SXFY
            {
                [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 10)]
                public string CPNAME;
                [SecsElement(Index = 2, Type = SecsElement.SecsElementType.TYPE_UNKNOWN, Length = 64)]
                public string CPVAL;
            }
        }

    }
    public class S2F41_PriorityUpdate : SXFY
    {
        /// <summary>
        /// The RCMD
        /// </summary>
        [SecsElement(Index = 1, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 20)]
        public string RCMD;

        [SecsElement(Index = 2)]
        public REPITEM REPITEMS;

        /// <summary>
        /// Initializes a new instance of the <see cref="S2F41"/> class.
        /// </summary>
        public S2F41_PriorityUpdate()
        {
            StreamFunction = "S2F41";
            IsBaseType = true;
        }
        public class REPITEM : SXFY
        {
            [SecsElement(Index = 1)]
            public REPITEM_ASCII CommandID_CP;
            [SecsElement(Index = 2)]
            public REPITEM_U2 PRIORITY_CP;

            public class REPITEM_ASCII : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPVAL_ASCII;
            }
            public class REPITEM_U2 : SXFY
            {
                [SecsElement(Index = 1, ListElementType = SecsElement.SecsElementType.TYPE_ASCII, Length = 40)]
                public string CPNAME;
                [SecsElement(Index = 2, ListElementType = SecsElement.SecsElementType.TYPE_2_BYTE_UNSIGNED_INTEGER, Length = 1)]
                public string CPVAL_U2;
            }


        }
    }
}
