using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.sc.RouteKit;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class GuideBLL
    {
        protected SCApplication scApp;
        protected Logger logger = LogManager.GetCurrentClassLogger();

        public void start(SCApplication _scApp)
        {
            scApp = _scApp;
        }
        //public (RouteInfo stratFromRouteInfo, RouteInfo fromToRouteInfo) getGuideInfo(string startaddress, string sourceAddress, string destAddress)
        //{
        //    int.TryParse(startaddress, out int i_start_address);
        //    int.TryParse(sourceAddress, out int i_source_address);
        //    int.TryParse(destAddress, out int i_dest_address);
        //    (List<RouteInfo> stratFromRouteInfoList, List<RouteInfo> fromToRouteInfoList) =
        //        scApp.NewRouteGuide.getStartFromThenFromToRoutesAddrToAddrToAddr(i_start_address, i_source_address, i_dest_address);
        //    return (stratFromRouteInfoList.First(), fromToRouteInfoList.First());
        //}
        public virtual (bool isSuccess, List<string> guideSegmentIds, List<string> guideSectionIds, List<string> guideAddressIds, int totalCost)
            getGuideInfo(string startAddress, string targetAddress, List<string> byPassSectionIDs = null, string startSection = null)
        {
            if (SCUtility.isMatche(startAddress, targetAddress))
            {
                return (true, new List<string>(), new List<string>(), new List<string>(), 0);
            }

            bool is_success = false;
            int.TryParse(startAddress, out int i_start_address);
            int.TryParse(targetAddress, out int i_target_address);

            List<RouteInfo> stratFromRouteInfoList = null;
            if (byPassSectionIDs == null || byPassSectionIDs.Count == 0)
            {
                stratFromRouteInfoList = scApp.NewRouteGuide.getFromToRoutesAddrToAddr(i_start_address, i_target_address);
            }
            else
            {
                stratFromRouteInfoList = scApp.NewRouteGuide.getFromToRoutesAddrToAddr(i_start_address, i_target_address, byPassSectionIDs);
            }
            RouteInfo min_stratFromRouteInfo = null;
            if (stratFromRouteInfoList != null && stratFromRouteInfoList.Count > 0)
            {
                min_stratFromRouteInfo = stratFromRouteInfoList.First();
                is_success = true;
            }
            else
            {
                return (false, null, null, null, int.MaxValue);
            }

            return (is_success, null, min_stratFromRouteInfo.GetSectionIDs(), min_stratFromRouteInfo.GetAddressesIDs(), min_stratFromRouteInfo.total_cost);
        }


        public (bool isSuccess, List<string> guideSegmentIds, List<string> guideSectionIds, List<string> guideAddressIds, int totalCost)
        getGuideInfo_New2(string startAddress, string targetAddress, List<string> byPassSectionIDs = null, string startSection = null)
        {
            AADDRESS start_adr = scApp.getCommObjCacheManager().getAddress(startAddress);
            AADDRESS target_adr = scApp.getCommObjCacheManager().getAddress(targetAddress);
            string start_node = startAddress;
            string target_node = targetAddress;
            //bool is_node_adr_start = start_adr.IsSegment;
            //bool is_node_adr_target = target_adr.IsSegment;
            bool is_node_adr_start = scApp.SegmentBLL.cache.IsSegmentAddress(startAddress);
            bool is_node_adr_target = scApp.SegmentBLL.cache.IsSegmentAddress(targetAddress);

            if (SCUtility.isMatche(startAddress, targetAddress))
            {
                return (true, new List<string>(), new List<string>(), new List<string>(), 0);
            }
            if (!is_node_adr_start)
            {
                string start_section_adr_id = null;
                ASECTION start_adr_section = null;

                //if (!start_adr.IsSection)
                if (start_adr.IsControl)
                {
                    var nearSectionAddress = scApp.AddressesBLL.cache.GetAddress(start_adr.NODE_ID);
                    if (nearSectionAddress == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Start address:{startAddress} is a nonsection address,but node id:{start_adr.NODE_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                    else
                    {
                        start_section_adr_id = nearSectionAddress.ADR_ID;
                    }
                    start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                    if (start_adr_section == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Start address:{startAddress} is a nonsection address,but section id:{start_adr.SEC_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                }
                else
                {
                    start_section_adr_id = start_adr.ADR_ID;
                    start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_section_adr_id).First();
                }
                ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                if (start_adr_segment.STATUS != E_SEG_STATUS.Active)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"Segment ID:{start_adr_segment.SEG_ID} is disable, can't find guide info");
                    return (false, null, null, null, int.MaxValue);
                }
                start_node = start_adr_segment.getNearSegmentAddress(start_section_adr_id);
            }
            if (!is_node_adr_target)
            {
                string target_section_adr_id = null;
                ASECTION target_adr_section = null;
                //if (!target_adr.IsSection)
                if (target_adr.IsControl)
                {
                    var nearSectionAddress = scApp.AddressesBLL.cache.GetAddress(target_adr.NODE_ID);
                    if (nearSectionAddress == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Target address:{startAddress} is a nonsection address,but node id:{target_adr.NODE_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                    else
                    {
                        target_section_adr_id = nearSectionAddress.ADR_ID;
                    }
                    target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                    if (target_adr_section == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Target address:{startAddress} is a nonsection address,but section id:{target_adr.SEC_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                }
                else
                {
                    target_section_adr_id = target_adr.ADR_ID;
                    target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_section_adr_id).First();
                }
                ASEGMENT target_adr_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                if (target_adr_segment.STATUS != E_SEG_STATUS.Active)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"Segment ID:{target_adr_segment.SEG_ID} is disable, can't find guide info");
                    return (false, null, null, null, int.MaxValue);
                }
                target_node = target_adr_segment.getNearSegmentAddress(target_section_adr_id);
            }

            if (SCUtility.isMatche(start_node, target_node))
            {
                HashSet<string> guideSegmentIds = new HashSet<string>();
                List<string> guideSectionIds = new List<string>();
                List<string> guideAddressIds = new List<string>();

                ASECTION start_adr_section = null;
                ASECTION target_adr_section = null;
                if (start_adr.IsControl)
                {
                    start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                }
                else
                {
                    if (!SCUtility.isEmpty(startSection))
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSection(startSection);
                    }
                    else
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(start_adr.ADR_ID).FirstOrDefault();
                        if (start_adr_section == null)
                            start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_adr.ADR_ID).FirstOrDefault();
                    }
                }
                if (target_adr.IsControl)
                {
                    target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                }
                else
                {
                    target_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(target_adr.ADR_ID).FirstOrDefault();
                    if (target_adr_section == null)
                        target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_adr.ADR_ID).FirstOrDefault();
                }

                ASEGMENT start_adr_seg = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                ASEGMENT target_adr_seg = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                if (SCUtility.isMatche(start_adr_seg.SEG_ID, target_adr_seg.SEG_ID))
                {
                    if (SCUtility.isMatche(start_adr_section.SEC_ID, target_adr_section.SEC_ID))
                    {
                        if (SCUtility.isMatche(start_adr_section.FROM_ADR_ID, start_adr.ADR_ID))
                        {
                            guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        }
                        else if (SCUtility.isMatche(start_adr_section.TO_ADR_ID, start_adr.ADR_ID))
                        {

                            guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        }
                        else
                        {
                            if (start_adr.DISTANCE > target_adr.DISTANCE)
                            {
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            }
                        }
                        guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    }
                    else
                    {
                        int start_sec_index = start_adr_seg.GetSectionIndex(start_adr_section);
                        int target_sec_index = start_adr_seg.GetSectionIndex(target_adr_section);
                        if (start_adr.IsControl)
                        {
                            if (start_sec_index < target_sec_index)
                            {
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            }
                            //guideAddressIds.Add(start_adr.NODE_ID);
                        }
                        else
                        {
                            guideAddressIds.Add(start_adr.ADR_ID);
                        }
                        if (target_adr.IsControl)
                        {
                            if (target_sec_index < start_sec_index)
                            {
                                guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                            }
                            //guideAddressIds.Add(target_adr.NODE_ID);
                        }
                        else
                        {
                            guideAddressIds.Add(target_adr.ADR_ID);
                        }
                        guideSegmentIds.Add(start_adr_section.SEG_NUM);
                        //int index_start_adr_section = start_adr_seg.Sections.IndexOf(start_adr_section);
                        //int index_target_adr_section = start_adr_seg.Sections.IndexOf(target_adr_section);

                        //if (index_start_adr_section < index_target_adr_section)
                        //{
                        //    guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        //    guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                        //}
                        //else
                        //{
                        //    guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        //    guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                        //}
                        //guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    }
                }
                else
                {
                    //if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealFromAddress))
                    //{
                    //    guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                    //    guideAddressIds.Add(start_adr_seg.RealToAddress);
                    //    guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    //}
                    //else
                    //{
                    //    guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                    //    guideAddressIds.Add(start_adr_seg.RealFromAddress);
                    //    guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    //}
                    //guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    //guideSegmentIds.Add(target_adr_section.SEG_NUM);
                    if (SCUtility.isMatche(start_adr_seg.RealFromAddress, target_adr_seg.RealFromAddress))
                    {
                        guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealFromAddress);
                        guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    }
                    else if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealToAddress))
                    {
                        guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealToAddress);
                        guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    }
                    else if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealFromAddress))
                    {
                        guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealToAddress);
                        guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    }
                    else
                    {
                        guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealFromAddress);
                        guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    }
                    guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    guideSegmentIds.Add(target_adr_section.SEG_NUM);
                }


                var guideSectionInfos = RouteInfo.GetSectionInfos(scApp.SegmentBLL, guideSegmentIds.ToList(), guideAddressIds);
                return (true, guideSegmentIds.ToList(), guideSectionInfos.guideSections, guideSectionInfos.guideAddresses, 0);
            }
            else
            {
                int.TryParse(start_node, out int i_start_address);
                int.TryParse(target_node, out int i_target_address);

                List<RouteInfo> stratFromRouteInfoList = scApp.NewRouteGuide.getFromToRoutesAddrToAddr(i_start_address, i_target_address);
                if (stratFromRouteInfoList == null || stratFromRouteInfoList.Count == 0)
                {
                    int[] current_ban_route = scApp.NewRouteGuide.getAllBanDirectArray();
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"start node:{start_node}({i_start_address}) and target node:{target_node}({i_target_address})," +
                             $"can't find the path. Current ban route:{string.Join(",", current_ban_route)}");
                    return (false, null, null, null, int.MaxValue);
                }
                RouteInfo min_stratFromRouteInfo = stratFromRouteInfoList.First();
                List<string> guideSegmentIds = min_stratFromRouteInfo.GetSectionIDs();
                List<string> guideAddressIds = min_stratFromRouteInfo.GetAddressesIDs();
                if (!is_node_adr_start)
                {
                    ASECTION start_adr_section = null;
                    //if (!start_adr.IsSection)
                    if (start_adr.IsControl)
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                    }
                    else
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(start_adr.ADR_ID).FirstOrDefault();
                        if (start_adr_section == null)
                            start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_adr.ADR_ID).FirstOrDefault();
                    }

                    if (SCUtility.isMatche(start_adr_section.SEG_NUM, guideSegmentIds.First()))
                    {
                        //if (!start_adr.IsSection)

                        if (start_adr.IsControl)
                        {
                            ////Not thing
                            ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds[0], start_adr_segment.RealFromAddress))
                            {
                                guideAddressIds[0] = start_adr_section.FROM_ADR_ID;
                            }
                            else
                            {
                                guideAddressIds[0] = start_adr_section.TO_ADR_ID;
                            }

                            //guideAddressIds[0] = start_adr.NODE_ID.Trim();
                        }
                        else
                        {
                            guideAddressIds[0] = start_adr.ADR_ID.Trim();
                        }
                    }
                    else
                    {
                        guideSegmentIds.Insert(0, start_adr_section.SEG_NUM.Trim());
                        //   if (!start_adr.IsSection)
                        if (start_adr.IsControl)
                        {
                            ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds[0], start_adr_segment.RealFromAddress))
                            {
                                guideAddressIds.Insert(0, start_adr_section.TO_ADR_ID.Trim());
                            }
                            else
                            {
                                guideAddressIds.Insert(0, start_adr_section.FROM_ADR_ID.Trim());
                            }
                        }
                        else
                        {
                            guideAddressIds.Insert(0, start_adr.ADR_ID.Trim());
                        }
                    }
                }
                if (!is_node_adr_target)
                {
                    ASECTION target_adr_section = null;
                    //if (!target_adr.IsSection)
                    if (target_adr.IsControl)
                    {
                        target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                    }
                    else
                    {
                        target_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(target_adr.ADR_ID).FirstOrDefault();
                        if (target_adr_section == null)
                            target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_adr.ADR_ID).FirstOrDefault();
                    }

                    if (SCUtility.isMatche(target_adr_section.SEG_NUM, guideSegmentIds.Last()))
                    {
                        //guideAddressIds[guideAddressIds.Count - 1] = target_adr.ADR_ID.Trim();
                        if (target_adr.IsControl)
                        {
                            ASEGMENT target_adr_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds.Last(), target_adr_segment.RealFromAddress))
                            {
                                guideAddressIds[guideAddressIds.Count - 1] = target_adr_section.FROM_ADR_ID.Trim();
                                //guideAddressIds[0] = target_adr_section.FROM_ADR_ID;
                            }
                            else
                            {
                                guideAddressIds[guideAddressIds.Count - 1] = target_adr_section.TO_ADR_ID.Trim();
                            }

                            //guideAddressIds[guideAddressIds.Count - 1] = target_adr.NODE_ID.Trim();
                        }
                        else
                        {
                            guideAddressIds[guideAddressIds.Count - 1] = target_adr.ADR_ID.Trim();
                        }
                    }
                    else
                    {
                        guideSegmentIds.Add(target_adr_section.SEG_NUM.Trim());
                        ASEGMENT target_adr_on_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                        //if (!target_adr.IsSection)
                        if (target_adr.IsControl)
                        {

                            //if (SCUtility.isMatche(guideAddressIds.Last(), target_adr_section.FROM_ADR_ID))
                            //{
                            //    guideAddressIds.Add(target_adr_section.TO_ADR_ID.Trim());
                            //}
                            //else
                            //{
                            //    guideAddressIds.Add(target_adr_section.FROM_ADR_ID.Trim());
                            //}
                            if (SCUtility.isMatche(guideAddressIds.Last(), target_adr_on_segment.RealFromAddress))
                            {
                                guideAddressIds.Add(target_adr_section.TO_ADR_ID.Trim());
                            }
                            else
                            {
                                guideAddressIds.Add(target_adr_section.FROM_ADR_ID.Trim());
                            }
                        }
                        else
                        {
                            guideAddressIds.Add(target_adr.ADR_ID.Trim());
                        }
                    }
                }
                var guideSectionInfos = RouteInfo.GetSectionInfos(scApp.SegmentBLL, guideSegmentIds, guideAddressIds);

                return (true, guideSegmentIds, guideSectionInfos.guideSections, guideSectionInfos.guideAddresses, min_stratFromRouteInfo.total_cost);
            }
        }


        public bool IsRoadWalkable(string startAddress, string targetAddress)
        {
            try
            {
                if (SCUtility.isMatche(startAddress, targetAddress))
                    return true;

                var guide_info = getGuideInfo(startAddress, targetAddress);
                //if ((guide_info.guideAddressIds != null && guide_info.guideAddressIds.Count != 0) &&
                //    ((guide_info.guideSectionIds != null && guide_info.guideSectionIds.Count != 0)))
                if (guide_info.isSuccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool IsBanRoute(List<ReserveInfo> reserveInfos)
        {
            int[] all_ban_DirectArray = scApp.NewRouteGuide.getAllBanDirectArray();
            foreach (var reserve_info in reserveInfos)
            {
                ASECTION sec_reserve = scApp.SectionBLL.cache.GetSection(reserve_info.ReserveSectionID);
                int from_adr_id = 0;
                int to_adr_id = 0;
                switch (reserve_info.DriveDirction)
                {
                    case DriveDirction.DriveDirForward:
                        int.TryParse(sec_reserve.REAL_FROM_ADR_ID, out from_adr_id);
                        int.TryParse(sec_reserve.REAL_TO_ADR_ID, out to_adr_id);
                        break;
                    case DriveDirction.DriveDirReverse:
                        int.TryParse(sec_reserve.REAL_TO_ADR_ID, out from_adr_id);
                        int.TryParse(sec_reserve.REAL_FROM_ADR_ID, out to_adr_id);
                        break;
                }
                for (int i = 0; i < all_ban_DirectArray.Length - 1; i++)
                {
                    int adr1 = all_ban_DirectArray[i];
                    int adr2 = all_ban_DirectArray[i + 1];
                    if (from_adr_id == adr1 && from_adr_id == adr2)
                        return true;
                }
            }
            return false;
        }

        public virtual void initialUnbanRoute()
        {
            scApp.NewRouteGuide.resetBanRoute();
            var segments = scApp.SegmentBLL.cache.GetSegments();
            foreach (ASEGMENT seg in segments)
            {
                if (seg.STATUS == E_SEG_STATUS.Closed)
                {
                    foreach (var sec in seg.Sections)
                        scApp.NewRouteGuide.banRouteTwoDirect(sec.SEC_ID);//由於目前AGV的圖資資料Section=Segment，之後會將牠們分開，
                                                                          //因此先將Segment作為Sectiong使用
                }
            }
        }

        public virtual ASEGMENT unbanRouteTwoDirect(string segmentID)
        {
            ASEGMENT segment_do = null;
            ASEGMENT segment_vo = scApp.SegmentBLL.cache.GetSegment(segmentID);
            if (segment_vo != null)
            {
                foreach (var sec in segment_vo.Sections)
                    scApp.NewRouteGuide.unbanRouteTwoDirect(sec.SEC_ID);
            }
            segment_do = scApp.MapBLL.EnableSegment(segmentID);
            return segment_do;
        }
        public virtual ASEGMENT banRouteTwoDirect(string segmentID)
        {
            ASEGMENT segment = null;
            ASEGMENT segment_vo = scApp.SegmentBLL.cache.GetSegment(segmentID);
            if (segment_vo != null)
            {
                foreach (var sec in segment_vo.Sections)
                    scApp.NewRouteGuide.banRouteTwoDirect(sec.SEC_ID);
            }
            segment = scApp.MapBLL.DisableSegment(segmentID);
            return segment;
        }




        public (List<string> replaceSegmentIds, List<string> replaceSectionIds, List<string> replaceAddressIds) getReplaceRoad(string start_node, string target_node, string willReplaceSegment)
        {
            int.TryParse(start_node, out int i_start_address);
            int.TryParse(target_node, out int i_target_address);
            List<RouteInfo> stratFromRouteInfoList = scApp.NewRouteGuide.getFromToRoutesAddrToAddr(i_start_address, i_target_address);
            foreach (var route_info in stratFromRouteInfoList)
            {
                List<string> segment_ids = route_info.GetSectionIDs();
                if (!segment_ids.Contains(willReplaceSegment.Trim()))
                {
                    List<string> seg_address_ids = route_info.GetAddressesIDs();
                    var guideSectionInfos = RouteInfo.GetSectionInfos(scApp.SegmentBLL, segment_ids, seg_address_ids);
                    return (segment_ids, guideSectionInfos.guideSections, guideSectionInfos.guideAddresses);

                }
            }
            return (null, null, null);
        }
        public bool isOneDirectPathSection(string section_id)
        {
            return scApp.getCommObjCacheManager().oneDirectPathDic.ContainsKey(section_id);
        }
        public List<ASECTION> getOneDirectPathBySectionID(string section_id)
        {
            List<ASECTION> oneDirectPath = new List<ASECTION>();
            if (scApp.getCommObjCacheManager().oneDirectPathDic.TryGetValue(section_id, out oneDirectPath))
            {
                return oneDirectPath;
            }
            else
            {
                return new List<ASECTION>();
            }
        }
    }


    public class GuldeBLLForFloy : GuideBLL
    {
        public override (bool isSuccess, List<string> guideSegmentIds, List<string> guideSectionIds, List<string> guideAddressIds, int totalCost)
        getGuideInfo(string startAddress, string targetAddress, List<string> byPassSectionIDs = null, string startSection = null)
        {
            AADDRESS start_adr = scApp.getCommObjCacheManager().getAddress(startAddress);
            AADDRESS target_adr = scApp.getCommObjCacheManager().getAddress(targetAddress);
            string start_node = startAddress;
            string target_node = targetAddress;
            //bool is_node_adr_start = start_adr.IsSegment;
            //bool is_node_adr_target = target_adr.IsSegment;
            bool is_node_adr_start = scApp.SegmentBLL.cache.IsSegmentAddress(startAddress);
            bool is_node_adr_target = scApp.SegmentBLL.cache.IsSegmentAddress(targetAddress);

            if (SCUtility.isMatche(startAddress, targetAddress))
            {
                return (true, new List<string>(), new List<string>(), new List<string>(), 0);
            }
            if (!is_node_adr_start)
            {
                string start_section_adr_id = null;
                ASECTION start_adr_section = null;

                //if (!start_adr.IsSection)
                if (start_adr.IsControl)
                {
                    var nearSectionAddress = scApp.AddressesBLL.cache.GetAddress(start_adr.NODE_ID);
                    if (nearSectionAddress == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Start address:{startAddress} is a nonsection address,but node id:{start_adr.NODE_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                    else
                    {
                        start_section_adr_id = nearSectionAddress.ADR_ID;
                    }
                    start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                    if (start_adr_section == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Start address:{startAddress} is a nonsection address,but section id:{start_adr.SEC_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                }
                else
                {
                    start_section_adr_id = start_adr.ADR_ID;
                    start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_section_adr_id).First();
                }
                ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                if (start_adr_segment.STATUS != E_SEG_STATUS.Active)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"Segment ID:{start_adr_segment.SEG_ID} is disable, can't find guide info");
                    return (false, null, null, null, int.MaxValue);
                }
                start_node = start_adr_segment.getNearSegmentAddress(start_section_adr_id);
            }
            if (!is_node_adr_target)
            {
                string target_section_adr_id = null;
                ASECTION target_adr_section = null;
                //if (!target_adr.IsSection)
                if (target_adr.IsControl)
                {
                    var nearSectionAddress = scApp.AddressesBLL.cache.GetAddress(target_adr.NODE_ID);
                    if (nearSectionAddress == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Target address:{startAddress} is a nonsection address,but node id:{target_adr.NODE_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                    else
                    {
                        target_section_adr_id = nearSectionAddress.ADR_ID;
                    }
                    target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                    if (target_adr_section == null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(GuideBLL), Device: "OHxC",
                           Data: $"Target address:{startAddress} is a nonsection address,but section id:{target_adr.SEC_ID} not exist.");
                        return (false, null, null, null, int.MaxValue);
                    }
                }
                else
                {
                    target_section_adr_id = target_adr.ADR_ID;
                    target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_section_adr_id).First();
                }
                ASEGMENT target_adr_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                if (target_adr_segment.STATUS != E_SEG_STATUS.Active)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"Segment ID:{target_adr_segment.SEG_ID} is disable, can't find guide info");
                    return (false, null, null, null, int.MaxValue);
                }
                target_node = target_adr_segment.getNearSegmentAddress(target_section_adr_id);
            }

            if (SCUtility.isMatche(start_node, target_node))
            {
                HashSet<string> guideSegmentIds = new HashSet<string>();
                List<string> guideSectionIds = new List<string>();
                List<string> guideAddressIds = new List<string>();

                ASECTION start_adr_section = null;
                ASECTION target_adr_section = null;
                if (start_adr.IsControl)
                {
                    start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                }
                else
                {
                    if (!SCUtility.isEmpty(startSection))
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSection(startSection);
                    }
                    else
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(start_adr.ADR_ID).FirstOrDefault();
                        if (start_adr_section == null)
                            start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_adr.ADR_ID).FirstOrDefault();
                    }
                }
                if (target_adr.IsControl)
                {
                    target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                }
                else
                {
                    target_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(target_adr.ADR_ID).FirstOrDefault();
                    if (target_adr_section == null)
                        target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_adr.ADR_ID).FirstOrDefault();
                }

                ASEGMENT start_adr_seg = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                ASEGMENT target_adr_seg = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                if (SCUtility.isMatche(start_adr_seg.SEG_ID, target_adr_seg.SEG_ID))
                {
                    if (SCUtility.isMatche(start_adr_section.SEC_ID, target_adr_section.SEC_ID))
                    {
                        if (SCUtility.isMatche(start_adr_section.FROM_ADR_ID, start_adr.ADR_ID))
                        {
                            guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        }
                        else if (SCUtility.isMatche(start_adr_section.TO_ADR_ID, start_adr.ADR_ID))
                        {

                            guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        }
                        else
                        {
                            if (start_adr.DISTANCE > target_adr.DISTANCE)
                            {
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            }
                        }
                        guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    }
                    else
                    {
                        int start_sec_index = start_adr_seg.GetSectionIndex(start_adr_section);
                        int target_sec_index = start_adr_seg.GetSectionIndex(target_adr_section);
                        if (start_adr.IsControl)
                        {
                            if (start_sec_index < target_sec_index)
                            {
                                guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                            }
                            //guideAddressIds.Add(start_adr.NODE_ID);
                        }
                        else
                        {
                            guideAddressIds.Add(start_adr.ADR_ID);
                        }
                        if (target_adr.IsControl)
                        {
                            if (target_sec_index < start_sec_index)
                            {
                                guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                            }
                            else
                            {
                                guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                            }
                            //guideAddressIds.Add(target_adr.NODE_ID);
                        }
                        else
                        {
                            guideAddressIds.Add(target_adr.ADR_ID);
                        }
                        guideSegmentIds.Add(start_adr_section.SEG_NUM);
                        //int index_start_adr_section = start_adr_seg.Sections.IndexOf(start_adr_section);
                        //int index_target_adr_section = start_adr_seg.Sections.IndexOf(target_adr_section);

                        //if (index_start_adr_section < index_target_adr_section)
                        //{
                        //    guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        //    guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                        //}
                        //else
                        //{
                        //    guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        //    guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                        //}
                        //guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    }
                }
                else
                {
                    //if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealFromAddress))
                    //{
                    //    guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                    //    guideAddressIds.Add(start_adr_seg.RealToAddress);
                    //    guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    //}
                    //else
                    //{
                    //    guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                    //    guideAddressIds.Add(start_adr_seg.RealFromAddress);
                    //    guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    //}
                    //guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    //guideSegmentIds.Add(target_adr_section.SEG_NUM);
                    if (SCUtility.isMatche(start_adr_seg.RealFromAddress, target_adr_seg.RealFromAddress))
                    {
                        guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealFromAddress);
                        guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    }
                    else if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealToAddress))
                    {
                        guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealToAddress);
                        guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    }
                    else if (SCUtility.isMatche(start_adr_seg.RealToAddress, target_adr_seg.RealFromAddress))
                    {
                        guideAddressIds.Add(start_adr_section.FROM_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealToAddress);
                        guideAddressIds.Add(target_adr_section.TO_ADR_ID);
                    }
                    else
                    {
                        guideAddressIds.Add(start_adr_section.TO_ADR_ID);
                        guideAddressIds.Add(start_adr_seg.RealFromAddress);
                        guideAddressIds.Add(target_adr_section.FROM_ADR_ID);
                    }
                    guideSegmentIds.Add(start_adr_section.SEG_NUM);
                    guideSegmentIds.Add(target_adr_section.SEG_NUM);
                }


                var guideSectionInfos = RouteInfo.GetSectionInfos(scApp.SegmentBLL, guideSegmentIds.ToList(), guideAddressIds);
                return (true, guideSegmentIds.ToList(), guideSectionInfos.guideSections, guideSectionInfos.guideAddresses, 0);
            }
            else
            {
                int.TryParse(start_node, out int i_start_address);
                int.TryParse(target_node, out int i_target_address);

                List<RouteInfo> stratFromRouteInfoList = scApp.NewRouteGuide.getFromToRoutesAddrToAddr(i_start_address, i_target_address);
                if (stratFromRouteInfoList == null || stratFromRouteInfoList.Count == 0)
                {
                    int[] current_ban_route = scApp.NewRouteGuide.getAllBanDirectArray();
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(GuideBLL), Device: "OHxC",
                       Data: $"start node:{start_node}({i_start_address}) and target node:{target_node}({i_target_address})," +
                             $"can't find the path. Current ban route:{string.Join(",", current_ban_route)}");
                    return (false, null, null, null, int.MaxValue);
                }
                RouteInfo min_stratFromRouteInfo = stratFromRouteInfoList.First();
                List<string> guideSegmentIds = min_stratFromRouteInfo.GetSectionIDs();
                List<string> guideAddressIds = min_stratFromRouteInfo.GetAddressesIDs();
                if (!is_node_adr_start)
                {
                    ASECTION start_adr_section = null;
                    //if (!start_adr.IsSection)
                    if (start_adr.IsControl)
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSection(start_adr.SEC_ID);
                    }
                    else
                    {
                        start_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(start_adr.ADR_ID).FirstOrDefault();
                        if (start_adr_section == null)
                            start_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(start_adr.ADR_ID).FirstOrDefault();
                    }

                    if (SCUtility.isMatche(start_adr_section.SEG_NUM, guideSegmentIds.First()))
                    {
                        //if (!start_adr.IsSection)

                        if (start_adr.IsControl)
                        {
                            ////Not thing
                            ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds[0], start_adr_segment.RealFromAddress))
                            {
                                guideAddressIds[0] = start_adr_section.FROM_ADR_ID;
                            }
                            else
                            {
                                guideAddressIds[0] = start_adr_section.TO_ADR_ID;
                            }

                            //guideAddressIds[0] = start_adr.NODE_ID.Trim();
                        }
                        else
                        {
                            guideAddressIds[0] = start_adr.ADR_ID.Trim();
                        }
                    }
                    else
                    {
                        guideSegmentIds.Insert(0, start_adr_section.SEG_NUM.Trim());
                        //   if (!start_adr.IsSection)
                        if (start_adr.IsControl)
                        {
                            ASEGMENT start_adr_segment = scApp.SegmentBLL.cache.GetSegment(start_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds[0], start_adr_segment.RealFromAddress))
                            {
                                guideAddressIds.Insert(0, start_adr_section.TO_ADR_ID.Trim());
                            }
                            else
                            {
                                guideAddressIds.Insert(0, start_adr_section.FROM_ADR_ID.Trim());
                            }
                        }
                        else
                        {
                            guideAddressIds.Insert(0, start_adr.ADR_ID.Trim());
                        }
                    }
                }
                if (!is_node_adr_target)
                {
                    ASECTION target_adr_section = null;
                    //if (!target_adr.IsSection)
                    if (target_adr.IsControl)
                    {
                        target_adr_section = scApp.SectionBLL.cache.GetSection(target_adr.SEC_ID);
                    }
                    else
                    {
                        target_adr_section = scApp.SectionBLL.cache.GetSectionsByToAddress(target_adr.ADR_ID).FirstOrDefault();
                        if (target_adr_section == null)
                            target_adr_section = scApp.SectionBLL.cache.GetSectionsByFromAddress(target_adr.ADR_ID).FirstOrDefault();
                    }

                    if (SCUtility.isMatche(target_adr_section.SEG_NUM, guideSegmentIds.Last()))
                    {
                        //guideAddressIds[guideAddressIds.Count - 1] = target_adr.ADR_ID.Trim();
                        if (target_adr.IsControl)
                        {
                            ASEGMENT target_adr_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                            if (SCUtility.isMatche(guideAddressIds.Last(), target_adr_segment.RealFromAddress))
                            {
                                guideAddressIds[guideAddressIds.Count - 1] = target_adr_section.FROM_ADR_ID.Trim();
                                //guideAddressIds[0] = target_adr_section.FROM_ADR_ID;
                            }
                            else
                            {
                                guideAddressIds[guideAddressIds.Count - 1] = target_adr_section.TO_ADR_ID.Trim();
                            }

                            //guideAddressIds[guideAddressIds.Count - 1] = target_adr.NODE_ID.Trim();
                        }
                        else
                        {
                            guideAddressIds[guideAddressIds.Count - 1] = target_adr.ADR_ID.Trim();
                        }
                    }
                    else
                    {
                        guideSegmentIds.Add(target_adr_section.SEG_NUM.Trim());
                        ASEGMENT target_adr_on_segment = scApp.SegmentBLL.cache.GetSegment(target_adr_section.SEG_NUM);
                        //if (!target_adr.IsSection)
                        if (target_adr.IsControl)
                        {

                            if (SCUtility.isMatche(guideAddressIds.Last(), target_adr_on_segment.RealFromAddress))
                            {
                                guideAddressIds.Add(target_adr_section.TO_ADR_ID.Trim());
                            }
                            else
                            {
                                guideAddressIds.Add(target_adr_section.FROM_ADR_ID.Trim());
                            }
                        }
                        else
                        {
                            guideAddressIds.Add(target_adr.ADR_ID.Trim());
                        }
                    }
                }
                var guideSectionInfos = RouteInfo.GetSectionInfos(scApp.SegmentBLL, guideSegmentIds, guideAddressIds);

                return (true, guideSegmentIds, guideSectionInfos.guideSections, guideSectionInfos.guideAddresses, min_stratFromRouteInfo.total_cost);
            }
        }
        public override ASEGMENT unbanRouteTwoDirect(string segmentID)
        {
            ASEGMENT segment = null;
            scApp.NewRouteGuide.unbanRouteTwoDirect(segmentID);
            segment = scApp.MapBLL.EnableSegment(segmentID);
            return segment;
        }
        public override ASEGMENT banRouteTwoDirect(string segmentID)
        {
            ASEGMENT segment = null;
            scApp.NewRouteGuide.banRouteTwoDirect(segmentID);
            segment = scApp.MapBLL.DisableSegment(segmentID);
            return segment;
        }

        public override void initialUnbanRoute()
        {
            scApp.NewRouteGuide.resetBanRoute();
            var segments = scApp.SegmentBLL.cache.GetSegments();
            foreach (ASEGMENT seg in segments)
            {
                if (seg.STATUS == E_SEG_STATUS.Closed)
                {
                    scApp.NewRouteGuide.banRouteTwoDirect(seg.SEG_ID);//由於目前AGV的圖資資料Section=Segment，之後會將牠們分開，
                }
            }
        }

    }
}

