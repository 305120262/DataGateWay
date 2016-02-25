using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataGateWay
{
    class AppManager
    {
        static private AppManager m_lock;
        private MainForm m_appForm;

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
