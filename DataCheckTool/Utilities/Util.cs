using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace DataGateWay.Utilities
{
    class Util
    {
        static IWorkspace m_serverws;
        static string m_mdb;

        public static IWorkspace ServerWorkspace
        {
            get
            {
                if (m_serverws == null)
                {
                    IWorkspaceFactory wsf = new AccessWorkspaceFactoryClass();
                    m_serverws = wsf.OpenFromFile(m_mdb, 0);
                }
                return m_serverws;
            }
        }

        public static String MdbFileName
        {
            set
            {
                m_mdb = value;
            }
        }
    }
}
