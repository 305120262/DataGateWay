using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesGDB;
using System.Collections;

namespace DataReviewer
{
    class DBOperator
    {
        static public IWorkspace openSDEWorkspace(string p_server,string p_instance,string p_database, string p_user, string p_pswd, string p_version) 
        {
            // For example, server = "Kona".// Database = "SDE" or "" if Oracle.// Instance = "5151".// User = "vtest".// Password = "go".// Version = "SDE.DEFAULT"

            IPropertySet propertySet = new PropertySet();
            propertySet.SetProperty("SERVER", p_server);
            propertySet.SetProperty("INSTANCE", p_instance);
            propertySet.SetProperty("DATABASE", p_database);
            propertySet.SetProperty("USER", p_user);
            propertySet.SetProperty("PASSWORD", p_pswd);
            propertySet.SetProperty("VERSION", p_version);

            Type factype = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
            IWorkspaceFactory iworkspacefac = (IWorkspaceFactory)Activator.CreateInstance(factype);
            try
            {
                return iworkspacefac.Open(propertySet, 0);
            }
            catch(Exception ex) 
            {
                //MessageBox.Show(ex.ToString());
                return null;
            }
        }

        static public IWorkspace openFileGDBWorkspace(string p_pathGDB) 
        {
            // For example, path = @"C:\XiZangData.gdb"

            Type factype = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            IWorkspaceFactory iworkspacefac = (IWorkspaceFactory)Activator.CreateInstance(factype);

            //IWorkspaceFactory iworkspacefac = new FileGDBWorkspaceFactory();

            try 
            {
                return iworkspacefac.OpenFromFile(p_pathGDB, 0);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return null;
            }
        }

        static public IWorkspace openAccessGDBWorkspace(string p_pathMDB)
        {
            // For example, path = @"C:\XiZangData.mdb"

            Type factype = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            IWorkspaceFactory iworkspacefac = (IWorkspaceFactory)Activator.CreateInstance(factype);

            //IWorkspaceFactory iworkspacefac = new FileGDBWorkspaceFactory();

            try
            {
                return iworkspacefac.OpenFromFile(p_pathMDB, 0);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return null;
            }
        }

        static public IWorkspace openShapeFileWorkspace(string pPath) 
        {
            Type factype = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            IWorkspaceFactory iworkspacefac = (IWorkspaceFactory)Activator.CreateInstance(factype);

            try 
            {
                return iworkspacefac.OpenFromFile(pPath, 0);
            }

            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return null;
            }
        }

        public static IFeature getFeatureFrom(int pOID, string pFeatureClassName, IWorkspace pWorkspace)
        {
            IFeatureClass featureclass = getFeatureClass(pWorkspace, pFeatureClassName);
            if (featureclass == null) return null;
            IFeature feature = featureclass.GetFeature(pOID);
            return feature;
        }

        public static IFeature getFeatureFrom(string pGridCodeNum, string pFeatureClassName, string pCodeFieldName,IWorkspace pWorkspace)
        {
            IFeatureClass featureclass = getFeatureClass(pWorkspace, pFeatureClassName);
            if (featureclass == null) return null;
            IQueryFilter filter = new QueryFilterClass();
            string strWhereClause = "";
            strWhereClause += pCodeFieldName;
            strWhereClause += "='";
            strWhereClause += pGridCodeNum;
            strWhereClause += "'";
            //filter.WhereClause = '+pCodeFieldName+' = '"+pGridCodeNum+"'";
            //filter.WhereClause = "GRIDCODENUM = '310030'";
            filter.WhereClause = strWhereClause;
            IFeatureCursor cursor = featureclass.Search(filter, true);
            if (cursor == null) return null;
            IFeature feature = cursor.NextFeature();
            return feature;
        }

        public static IFeatureClass getFeatureClass(IWorkspace pWorkspace, string pFeaturClassName)
        {
            IFeatureClass featureclass = null;
            if (pWorkspace is IFeatureWorkspace)
            {
                IFeatureWorkspace featureworkspace = pWorkspace as IFeatureWorkspace;
                featureclass = featureworkspace.OpenFeatureClass(pFeaturClassName);
            }
            return featureclass;
        }

        public static ArrayList getVerionNameList(IWorkspace pWorkspace) 
        {
            if (pWorkspace == null) return null;
            IFeatureWorkspace FeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IVersionedWorkspace3 VersionedWorkspace = FeatureWorkspace as IVersionedWorkspace3;
            ArrayList VersionNames = new ArrayList();
            IEnumVersionInfo MulVersionInfo = VersionedWorkspace.Versions;
            IVersionInfo SiglVersionInfo = MulVersionInfo.Next();
            while (SiglVersionInfo != null) 
            {
                VersionNames.Add(SiglVersionInfo.VersionName);
                SiglVersionInfo = MulVersionInfo.Next();
            }
            return VersionNames;
            
        }

        public static void DeleteFeatureClass(IWorkspace pWorkspace, string pFeatureClassName) 
        {
            IFeatureClass featureClass = DBOperator.getFeatureClass(pWorkspace, pFeatureClassName);
            IDataset dataSet = featureClass as IDataset;
            dataSet.Delete(); 
        }
    }
}