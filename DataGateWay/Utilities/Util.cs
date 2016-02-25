using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF;

namespace DataGateWay.Utilities
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
                    IWorkspaceFactory wsf = new SdeWorkspaceFactoryClass();
                    IPropertySet props = new PropertySetClass();
                    props.SetProperty("server", Properties.Settings.Default.SDEServer);
                    props.SetProperty("instance", Properties.Settings.Default.SDEService);
                    props.SetProperty("user", Properties.Settings.Default.SDEUser);
                    props.SetProperty("password", Properties.Settings.Default.SDEPassword);
                    props.SetProperty("database", Properties.Settings.Default.SDEDB);
                    props.SetProperty("version", Properties.Settings.Default.SDEVersion);
                    props.SetProperty("AUTHENTICATION_MODE","dbms");
                    m_serverws = wsf.Open(props, 0);
                }
                return m_serverws;
            }
        }


        public static void GetAllFeatureClassNames(IDataset ds, ref List<String> names)
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
    }
}
