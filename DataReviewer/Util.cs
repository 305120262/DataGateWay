using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;

namespace DataReviewer
{
    class Util
    {
        static IWorkspace m_serverws;

        public static IWorkspace ServerWorkspace
        {
            get
            {
                if (m_serverws == null)
                {
                    Type factype = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
                    IWorkspaceFactory wsf = (IWorkspaceFactory)Activator.CreateInstance(factype);
                    IPropertySet props = new PropertySetClass();
                    props.SetProperty("server", Properties.Settings.Default.strSdeServer);
                    props.SetProperty("instance", Properties.Settings.Default.strSdePort);
                    props.SetProperty("user", Properties.Settings.Default.strSdeUser);
                    props.SetProperty("password", Properties.Settings.Default.strSdePswd);
                    props.SetProperty("database", Properties.Settings.Default.strSdeDB);
                    props.SetProperty("version", Properties.Settings.Default.strSdeVersion);
                    props.SetProperty("AUTHENTICATION_MODE", "dbms");
                    m_serverws = wsf.Open(props, 0);
                }
                return m_serverws;
            }
        }

    }
}
