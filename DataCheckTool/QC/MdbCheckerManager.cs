using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using DataGateWay.Utilities;
using DataGateWay.Checkers;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;


namespace DataGateWay.QC
{
    class MdbCheckerManager:BaseCheckerManager 
    {

        private IWorkspace m_checkingWS;

        static private MdbCheckerManager m_lock;
        private MdbCheckerManager()
        {
        }

        static public MdbCheckerManager GetInstance()
        {
            if (m_lock == null)
            {
                m_lock = new MdbCheckerManager();
                CheckerUtil.CheckerManager = m_lock;
            }
            return m_lock;
        }

        public void Check(bool isPartial)
        {
            m_checkingWS = Util.ServerWorkspace ;
            m_errors.Clear();
            m_log.Clear();
            m_log.Add("Start Check");
            IsCheckTaskData = isPartial;
            if (isPartial)
            {
                LoadCheckItems();
            }
            else
            {
                CheckItems.Clear();
            }
            foreach (var checker in Checkers)
            {
                m_log.Add("---------------------");
                m_log.Add("Checher：" + checker.GetType().ToString());
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

        private void LoadCheckItems()
        {
            CheckItems.Clear();
            CheckItemCount = 0;
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IDataset ds = ws as IDataset;
            List<String> fcNames = new List<string>();
            GetAllFeatureClassNames(ds, ref fcNames);
            foreach (string fcname in fcNames)
            {
                List<int> ids = new List<int>();
                IFeatureClass fc = ws.OpenFeatureClass(fcname);
                IQueryFilter filter = new QueryFilterClass();
                filter.WhereClause = "[SyncStatus]='A' OR [SyncStatus]='U'";
                IFeatureCursor cur = fc.Search(filter, false);
                IFeature fea = cur.NextFeature();
                while (fea != null)
                {
                    ids.Add(fea.OID);
                    fea = cur.NextFeature();
                }
                if (ids.Count > 0)
                {
                    CheckItems.Add(fcname, ids);
                    CheckItemCount += ids.Count;
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

        static void GetAllFeatureClassNames(IDataset ds, ref List<String> names)
        {
            IEnumDataset ds_list = ds.Subsets;
            IDataset sub_ds = ds_list.Next();
            while (sub_ds != null)
            {
                if (sub_ds is IFeatureClass)
                {
                    names.Add(sub_ds.Name);
                }
                else if (sub_ds is IFeatureDataset)
                {
                    GetAllFeatureClassNames(sub_ds, ref names);
                }
                sub_ds = ds_list.Next();
            }
        }

        static public string CheckerConfigsDir
        {
            get
            {
                return Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckerConfigs/";
            }
        }

        static public XElement CheckerMetaInfo
        {
            get
            {
                string filename = Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckersMetaInfo.xml";
                return XElement.Load(filename);

            }
        }
        
    }
}
