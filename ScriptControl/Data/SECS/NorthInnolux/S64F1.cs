// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="S64F1.cs" company="">
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
    /// Class S64F1.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.stc.Data.SecsData.SXFY" />
    public class S64F1 : SXFY
    {
        [SecsElement(Index = 1, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 16)]
        public string CLOCK;
        [SecsElement(Index = 2, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
        public string COMMANDID;
        [SecsElement(Index = 3, ListSpreadOut = true, Type = SecsElement.SecsElementType.TYPE_ASCII, Length = 64)]
        public string CARRIERID;
        /// <summary>
        /// Initializes a new instance of the <see cref="S64F1"/> class.
        /// </summary>
        public S64F1()
        {
            StreamFunction = "S64F1";
            StreamFunctionName = "Destination Change Request";
            W_Bit = 1;
        }
    }
}
