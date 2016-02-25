using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using System.IO;

namespace DataReviewer
{
    class CheckFeatueEditor
    {
        public static int InsertNewFeature(IGeometry pShape, IFeatureClass pFeatureClass,string pTaskName)
        {
            //插入新图形，返回OID;
            IFeature newFeature = pFeatureClass.CreateFeature();
            if (newFeature == null) return -1;
            newFeature.Shape = pShape;
            newFeature.set_Value(pFeatureClass.FindField("VersionName"), pTaskName);
            newFeature.Store();
            return newFeature.OID;
            
        }

        public static int UpdateComment(int pOID, IFeatureClass pFeatureClass,string pText) 
        {
            //更新注释信息；
            IFeature updateFeature = pFeatureClass.GetFeature(pOID);
            if (updateFeature == null) return 0;
            int fieldindex = updateFeature.Fields.FindField("note");
            //IField filed = updateFeature.Fields.Field[4];
            //string fieldname = updateFeature.Fields.Field[4].Name;
            //int fieldindex = pFeatureClass.FindField("COMMENT");
            updateFeature.set_Value(fieldindex,pText);
            updateFeature.Store();
            return 1;
        }

        public static string GetComment(int pOID, IFeatureClass pFeatureClass) 
        {
            //根据OID获取注释信息：
            string comment = "";
            IFeature feature = pFeatureClass.GetFeature(pOID);
            //comment = pFeatureClass.GetFeature(pOID).get_Value(pFeatureClass.FindField("Comment")).ToString();
            comment = feature.get_Value(feature.Fields.FindField("note")).ToString();
            return comment;
        }

        public static void DeleteCheckArea(int pOID,IFeatureClass pFeatureClass) 
        {
            //删除指定要素：
            if (pFeatureClass.GetFeature(pOID) == null) return;

            pFeatureClass.GetFeature(pOID).Delete();

        }

        public static IFeatureCursor UncheckFeaturesGDB(IFeatureClass pFeatureClass) 
        {
            //找出要检查的更新区域：
            IQueryFilter filter = new QueryFilterClass();
            //filter.WhereClause = "Passed = 0 AND VersionName = 'xuy'";
            filter.WhereClause = "Passed = 0";
            IFeatureCursor cursor = pFeatureClass.Search(filter, true);
            return cursor;
        }
        public static IFeatureCursor UncheckFeaturesGDB(IFeatureClass pFeatureClass,string pVersionName)
        {
            //找出要检查的更新区域：
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "Passed = 0 AND VersionName = '"+pVersionName+"'";
            IFeatureCursor cursor = pFeatureClass.Search(filter, true);
            return cursor;
        }
        public static IFeatureCursor CheckFeaturesGDB(IFeatureClass pFeatureClass)
        {
            //找出已检查的更新区域：
            IQueryFilter filter = new QueryFilterClass();
            //filter.WhereClause = "Passed = 1 AND VersionName = 'xuy'";
            filter.WhereClause = "Passed = 1";
            IFeatureCursor cursor = pFeatureClass.Search(filter, true);
            return cursor;
        }

        public static IFeatureCursor CheckFeaturesGDB(IFeatureClass pFeatureClass, string pVersionName)
        {
            //找出已检查的更新区域：
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "Passed = 1 AND VersionName = '" + pVersionName + "'";
            IFeatureCursor cursor = pFeatureClass.Search(filter, true);
            return cursor;
        }

        //public static ICursor UncheckGridCode(ITable pTable,string pVersionName) 
        //{
        //    //根据表格数据获取grid code：
        //    IQueryFilter filter = new QueryFilterClass();
        //    filter.WhereClause = "TaskName = '" + pVersionName + "'";
        //    ICursor cursor = pTable.Search(filter, true);
        //    return cursor; 
        //}

        //public static void Check(IFeature pFeature, IWorkspace pWorkspace,string pVersionName) 
        //{
        //    //更新检查状态：
        //    int ii = 1;
        //    int fieldindex = pFeature.Fields.FindField("passed");
        //    if (pFeature.get_Value(pFeature.Fields.FindField("VersionName")).ToString() == pVersionName) 
        //    {
        //        pFeature.set_Value(fieldindex, ii);
        //        pFeature.Store();
        //    }

        //}

        //public static void Check(IFeature pFeature, IWorkspace pWorkspace)
        //{
        //    //更新检查状态：
        //    int ii = 1;
        //    int fieldindex = pFeature.Fields.FindFieldByAliasName("Pass");
         
        //        pFeature.set_Value(fieldindex, ii);
        //        pFeature.Store();

        //}

        public static bool ExportSHP(string pDestPath,IWorkspace pWorkspace) 
        {

            IWorkspace sourceWorkspace = pWorkspace;
            IFeatureClass sourceFeatureClass = DBOperator.getFeatureClass(pWorkspace, "CheckArea");
            string shpFolderPath = pDestPath.Substring(0, pDestPath.LastIndexOf("\\"));
            string shpFileName = pDestPath.Substring(pDestPath.LastIndexOf("\\") + 1);
            shpFileName = shpFileName.Substring(0, shpFileName.Length-4);

            IWorkspace targetWorkspace = DBOperator.openShapeFileWorkspace(shpFolderPath);
            if (File.Exists(pDestPath) == true) 
            {
                DBOperator.DeleteFeatureClass(targetWorkspace, shpFileName);
            }
            
            IDataset sourcedataset = sourceWorkspace as IDataset;
            IWorkspaceName sourceWorkspaceName = sourcedataset.FullName as IWorkspaceName;
            IDataset targetdataset = targetWorkspace as IDataset;
            IWorkspaceName targetWorkspaceName = targetdataset.FullName as IWorkspaceName;

            sourcedataset = sourceFeatureClass as IDataset;
            IFeatureClassName sourceFeatureClassName = sourcedataset.FullName as IFeatureClassName;

            // Create a name object for the target dataset.
            IFeatureClassName targetFeatureClassName = new FeatureClassNameClass();
            IDatasetName targetDatasetName = (IDatasetName)targetFeatureClassName;
            targetDatasetName.Name = shpFileName;
            targetDatasetName.WorkspaceName = targetWorkspaceName;

            // Create the objects and references necessary for field validation.
            IFieldChecker fieldChecker = new FieldCheckerClass();
            IFields sourceFields = sourceFeatureClass.Fields;
            IFields targetFields = null;
            IEnumFieldError enumFieldError = null;

            // Validate the fields and check for errors.
            fieldChecker.Validate(sourceFields, out enumFieldError, out targetFields);
            if (enumFieldError != null)
            {
                IFieldError error = enumFieldError.Next();
                while (error != null)
                {
                    // Handle the errors in a way appropriate to your application.
                    Console.WriteLine("Errors were encountered during field validation.");
                    error = enumFieldError.Next();

                }
            } 

            // Find the shape field.
            String shapeFieldName = sourceFeatureClass.ShapeFieldName;
            int shapeFieldIndex = sourceFeatureClass.FindField(shapeFieldName);
            IField shapeField = sourceFields.get_Field(shapeFieldIndex);

            // Get the geometry definition from the shape field and clone it.
            IGeometryDef geometryDef = shapeField.GeometryDef;
            IClone geometryDefClone = (IClone)geometryDef;
            IClone targetGeometryDefClone = geometryDefClone.Clone();
            IGeometryDef targetGeometryDef = (IGeometryDef)targetGeometryDefClone;

            // Cast the IGeometryDef to the IGeometryDefEdit interface.
            IGeometryDefEdit targetGeometryDefEdit = (IGeometryDefEdit)targetGeometryDef;

            // Set the IGeometryDefEdit properties.
            targetGeometryDefEdit.GridCount_2 = 1;
            targetGeometryDefEdit.set_GridSize(0, 0.75);

            // Set the required properties for the IFieldChecker interface.
            fieldChecker.InputWorkspace = sourceWorkspace;
            fieldChecker.ValidateWorkspace = targetWorkspace;

            // Create the converter and run the conversion.
            IFeatureDataConverter2 featureDataConverter = new FeatureDataConverterClass();
            
            IDatasetName sourceFClassName = sourceFeatureClassName as IDatasetName;
            IEnumInvalidObject enumInvalidObject = featureDataConverter.ConvertFeatureClass
               (sourceFClassName, null, null,null, targetFeatureClassName,
               targetGeometryDef, targetFields, "", 1000, 0);
            // Check for errors.
            IInvalidObjectInfo invalidObjectInfo = null;
            enumInvalidObject.Reset();
            while ((invalidObjectInfo = enumInvalidObject.Next()) != null)
            {
                // Handle the errors in a way appropriate to the application.
                Console.WriteLine("Errors occurred for the following feature: {0}",
                    invalidObjectInfo.InvalidObjectID);
                return false;
            }
            return true;
        }
    }
}
