using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using DataGateWay.Utilities;
using DataGateWay.Checkers;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using ESRI.ArcGIS.Geometry;
using DataGateWay.Task;


namespace DataGateWay.QC
{
    class SDECheckerManager:BaseCheckerManager 
    {

        private IWorkspace m_checkingWS;

        static private SDECheckerManager m_lock;
        private SDECheckerManager()
        {
        }

        static public SDECheckerManager GetInstance()
        {
            if (m_lock == null)
            {
                m_lock = new SDECheckerManager();
                CheckerUtil.CheckerManager = m_lock;
            }
            return m_lock;
        }

        public void Check(string taskName,bool isPartial)
        {
            IVersionedWorkspace vws = Util.ServerWorkspace as IVersionedWorkspace;
            m_checkingWS = vws.FindVersion(taskName) as IWorkspace;
            m_errors.Clear();
            m_log.Clear();
            m_log.Add("Start Check");
            IsCheckTaskData = true;
            LoadCheckItems(taskName, isPartial);
            foreach (var checker in Checkers)
            {
                m_log.Add("---------------------");
                m_log.Add("Checker：" + checker.GetType().ToString());
                BaseChecker bc = checker as BaseChecker;
                if (checker.CheckData())
                {
                    if (bc.CheckErrorList != null)
                    {
                        foreach (CheckError err in bc.CheckErrorList)
                        {
                            m_log.Add(err.Description);
                        }
                        m_errors.AddRange(bc.CheckErrorList.OfType<CheckError>());
                    }
                    m_log.Add("Check Successfully");
                }
                else
                {
                    m_log.Add("Check Failed");
                    m_log.Add(bc.Message);
                }
                m_log.Add("---------------------");
            }
            m_log.Add("End Check");
        }

        private void LoadCheckItems(string taskName, bool isPartial)
        {
            CheckItems.Clear();
            CheckItemCount = 0;
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IWorkspace2 ws2 = ws as IWorkspace2;
            if (isPartial)
            {
                //Check point items
                IFeatureClass fc = ws.OpenFeatureClass("CheckItemPtn");
                IQueryFilter filter = new QueryFilterClass();
                filter.WhereClause = "VersionName ='" + taskName + "'";
                IFeatureCursor cur = fc.Search(filter, false);
                IFeature fea = cur.NextFeature();
                while (fea != null)
                {
                    string fc_name = fea.get_Value(fc.FindField("FeatureClassName")) as string;
                    int fc_id = (int)fea.get_Value(fc.FindField("FeatureID"));
                    if (CheckItems.ContainsKey(fc_name))
                    {
                        List<int> ids;
                        CheckItems.TryGetValue(fc_name, out ids);
                        ids.Add(fc_id);
                        CheckItems[fc_name] = ids;
                    }
                    else
                    {
                        List<int> ids = new List<int>();
                        ids.Add(fc_id);
                        CheckItems.Add(fc_name, ids);
                    }
                    CheckItemCount++;
                    fea = cur.NextFeature();
                }
                //Check line items
                fc = ws.OpenFeatureClass("CheckItemLn");
                cur = fc.Search(filter, false);
                fea = cur.NextFeature();
                while (fea != null)
                {
                    string fc_name = fea.get_Value(fc.FindField("FeatureClassName")) as string;
                    int fc_id = (int)fea.get_Value(fc.FindField("FeatureID"));
                    if (CheckItems.ContainsKey(fc_name))
                    {
                        List<int> ids;
                        CheckItems.TryGetValue(fc_name, out ids);
                        ids.Add(fc_id);
                        CheckItems[fc_name] = ids;
                    }
                    else
                    {
                        List<int> ids = new List<int>();
                        ids.Add(fc_id);
                        CheckItems.Add(fc_name, ids);
                    }
                    CheckItemCount++;
                    fea = cur.NextFeature();
                }
                //Check polygon items
                fc = ws.OpenFeatureClass("CheckItemPoly");
                cur = fc.Search(filter, false);
                fea = cur.NextFeature();
                while (fea != null)
                {
                    string fc_name = fea.get_Value(fc.FindField("FeatureClassName")) as string;
                    int fc_id = (int)fea.get_Value(fc.FindField("FeatureID"));
                    if (CheckItems.ContainsKey(fc_name))
                    {
                        List<int> ids;
                        CheckItems.TryGetValue(fc_name, out ids);
                        ids.Add(fc_id);
                        CheckItems[fc_name] = ids;
                    }
                    else
                    {
                        List<int> ids = new List<int>();
                        ids.Add(fc_id);
                        CheckItems.Add(fc_name, ids);
                    }
                    CheckItemCount++;
                    fea = cur.NextFeature();
                }
            }
            else
            {
                HashSet<string> targets = new HashSet<string>();
                foreach (BaseChecker checker in Checkers)
                {
                    targets.Add(checker.GetCheckingTarget());
                }
                TaskManager tm = TaskManager.GetInstance();
                IPolygon area = tm.GetTaskLocation(taskName);
                ISpatialFilter filter = new SpatialFilter();
                filter.Geometry = area;
                //filter.GeometryField = "Shape";
                filter.SubFields = "";
                filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                foreach (string target in targets)
                {
                    if (!ws2.get_NameExists(esriDatasetType.esriDTFeatureClass, target))
                    {
                        continue;
                    }
                    IFeatureClass fc = ws.OpenFeatureClass(target);
                    IFeatureCursor cur = fc.Search(filter, true);
                    IFeature fea = cur.NextFeature();
                    while (fea != null)
                    {
                        int fc_id = fea.OID;
                        if (CheckItems.ContainsKey(target))
                        {
                            List<int> ids;
                            CheckItems.TryGetValue(target, out ids);
                            ids.Add(fc_id);
                            CheckItems[target] = ids;
                        }
                        else
                        {
                            List<int> ids = new List<int>();
                            ids.Add(fc_id);
                            CheckItems.Add(target, ids);
                        }
                        CheckItemCount++;
                        fea = cur.NextFeature();
                    }

                }
            }
        }

        public override IWorkspace CheckingWorkspace
        {
            get
            {
                return m_checkingWS;
            }
        }

        static public string CheckerConfigsDir
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckerConfigs/";
            }
        }

        static public XElement CheckerMetaInfo
        {
            get
            {
                string filename = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckersMetaInfo.xml";
                return XElement.Load(filename);
 
            }
        }
    }
}
