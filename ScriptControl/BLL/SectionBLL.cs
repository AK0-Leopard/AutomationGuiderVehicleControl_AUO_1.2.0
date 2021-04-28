using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.sc.RouteKit;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class SectionBLL
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public SCApplication scApp;
        public Database dataBase { get; private set; }
        public Cache cache { get; private set; }
        public SectionBLL()
        {
        }
        public void start(SCApplication _app)
        {
            scApp = _app;
            dataBase = new Database(scApp.SectionDao);
            cache = new Cache(scApp.getCommObjCacheManager());
            dataBase.SetConnectedSections();
        }
        public class Database
        {
            SectionDao SectionDao = null;
            public Database(SectionDao dao)
            {
                SectionDao = dao;
            }
            public List<ASECTION> loadAllSection()
            {
                List<ASECTION> sections = null;
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    sections = SectionDao.loadAll(con);
                }
                return sections;
            }
            public void SetConnectedSections()
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    List<ASECTION> sections = SectionDao.loadAll(con);
                    foreach (ASECTION sec in sections)
                    {
                        int count1 = 0;
                        int count2 = 0;
                        foreach (ASECTION s in sections)
                        {
                            if (SCUtility.isMatche(sec.SEC_ID, s.SEC_ID))
                            {
                                continue;
                            }
                            else
                            {
                                if (SCUtility.isMatche(sec.FROM_ADR_ID, s.FROM_ADR_ID) || SCUtility.isMatche(sec.FROM_ADR_ID, s.TO_ADR_ID))
                                {
                                    count1++;
                                    if (count1 == 1)
                                    {
                                        sec.ADR1_CHG_SEC_ID_1 = s.SEC_ID;
                                    }
                                    else if (count1 == 2)
                                    {
                                        sec.ADR1_CHG_SEC_ID_2 = s.SEC_ID;
                                    }
                                    else if (count1 == 3)
                                    {
                                        sec.ADR1_CHG_SEC_ID_3 = s.SEC_ID;
                                    }
                                    else
                                    {
                                        //do nothing
                                    }
                                }
                                if (SCUtility.isMatche(sec.TO_ADR_ID, s.FROM_ADR_ID) || SCUtility.isMatche(sec.TO_ADR_ID, s.TO_ADR_ID))
                                {
                                    count2++;
                                    if (count2 == 1)
                                    {
                                        sec.ADR2_CHG_SEC_ID_1 = s.SEC_ID;
                                    }
                                    else if (count2 == 2)
                                    {
                                        sec.ADR2_CHG_SEC_ID_2 = s.SEC_ID;
                                    }
                                    else if (count2 == 3)
                                    {
                                        sec.ADR2_CHG_SEC_ID_3 = s.SEC_ID;
                                    }
                                    else
                                    {
                                        //do nothing
                                    }
                                }
                            }
                        }

                    }


                    foreach (ASECTION sec in sections)
                    {
                        SectionDao.update(con, sec);
                    }
                }

            }

            public ASECTION DisableSection(string secNum)
            {
                ASECTION section = null;
                try
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        section = SectionDao.getByID(con, secNum);
                        if (section != null)
                        {
                            section.PRE_DISABLE_FLAG = false;
                            section.PRE_DISABLE_TIME = null;
                            section.DISABLE_TIME = DateTime.Now;
                            section.STATUS = E_SEG_STATUS.Closed;
                            SectionDao.update(con, section);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                return section;
            }
            public ASECTION EnableSection(string secNum)
            {
                ASECTION section = null;
                try
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        section = SectionDao.getByID(con, secNum);
                        if (section != null)
                        {
                            section.PRE_DISABLE_FLAG = false;
                            section.PRE_DISABLE_TIME = null;
                            section.DISABLE_TIME = null;
                            section.STATUS = E_SEG_STATUS.Active;
                        }
                        SectionDao.update(con, section);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                return section;
            }
        }
        public class Cache
        {
            CommObjCacheManager CommObjCacheManager = null;
            public Cache(CommObjCacheManager commObjCacheManager)
            {
                CommObjCacheManager = commObjCacheManager;
            }
            public ASECTION GetSection(string id)
            {
                return CommObjCacheManager.getSection(id);
            }
            public List<ASECTION> GetSectionBySegmentID(string id)
            {
                return CommObjCacheManager.getSections().
                       Where(sec => sec.SEG_NUM.Trim() == id.Trim()).
                       ToList();
            }

            public ASECTION GetSection(string adr1, string adr2)
            {
                return CommObjCacheManager.getSection(adr1, adr2);
            }
            public List<ASECTION> GetSections()
            {
                return CommObjCacheManager.getSections();
            }
            public List<ASECTION> GetSections(List<string> ids)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => ids.Contains(sec.SEC_ID.Trim())).
                                                 ToList();
                return result_sections;
            }
            public List<ASECTION> GetSectionsByFromAddress(string adr)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => sec.FROM_ADR_ID.Trim() == adr.Trim()).
                                                 ToList();
                return result_sections;
            }

            public List<ASECTION> GetSectionsByToAddress(string adr)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => sec.TO_ADR_ID.Trim() == adr.Trim()).
                                                 ToList();
                return result_sections;
            }

            public List<ASECTION> GetSectionsByAddress(string adr_id)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => SCUtility.isMatche(sec.FROM_ADR_ID, adr_id) ||
                                                 SCUtility.isMatche(sec.TO_ADR_ID, adr_id)).
                                                 ToList();
                return result_sections;
            }
            public List<ASECTION> GetSectionsByAddresses(List<string> adrIDs)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => adrIDs.Contains(SCUtility.Trim(sec.FROM_ADR_ID)) ||
                                                              adrIDs.Contains(SCUtility.Trim(sec.TO_ADR_ID))).
                                                 ToList();
                return result_sections;
            }

            public double GetSectionsDistance(List<string> sectionIDs)
            {
                List<ASECTION> result_sections = CommObjCacheManager.getSections().
                                                 Where(sec => sectionIDs.Contains(sec.SEC_ID.Trim())).
                                                 ToList();
                return result_sections.Sum(sec => sec.SEC_DIS);
            }
            public void EnableSection(string secID)
            {
                ASECTION section = CommObjCacheManager.getSections().Where(s => s.SEC_ID.Trim() == secID.Trim())
                                                  .FirstOrDefault();
                section.PRE_DISABLE_FLAG = false;
                section.PRE_DISABLE_TIME = null;
                section.DISABLE_TIME = null;
                section.STATUS = E_SEG_STATUS.Active;
            }

            public void DisableSection(string secID)
            {
                ASECTION section = CommObjCacheManager.getSections().Where(s => s.SEC_ID.Trim() == secID.Trim())
                                                  .FirstOrDefault();
                section.PRE_DISABLE_FLAG = false;
                section.PRE_DISABLE_TIME = null;
                section.DISABLE_TIME = DateTime.Now;
                section.STATUS = E_SEG_STATUS.Closed;
            }

        }
    }
}
