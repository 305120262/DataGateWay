using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;

namespace DataGateWay
{
    class AppManager
    {
        static private AppManager m_lock;
        private MainForm m_appForm;
        private IVersion m_currentVersion;

        public IVersion CurrentVersion
        {
            get { return m_currentVersion; }
            set { m_currentVersion = value; m_appForm.SetCurrentTaskStatusLabel(value.VersionName); }
        }

        private string m_TaskName;

        public string TaskName
        {
            get { return m_TaskName; }
            set { m_TaskName = value; m_appForm.SetCurrentTaskNameLabel(value); }
        }
        
        private AppManager()
        {
        }

        static public AppManager GetInstance()
        {
            if (m_lock == null)
            {
                m_lock = new AppManager();
            }
            return m_lock;
        }

        public MainForm AppForm
        {
            get { return m_appForm; }
            set { m_appForm = value; }
        }
    }
}
