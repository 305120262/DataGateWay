using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseDistributed;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using System.IO;
using ESRI.ArcGIS.Geometry;
using DataGateWay.Utilities;
using ESRI.ArcGIS.ADF;
using DataGateWay.Task;

namespace DataGateWay.DataSync
{
    class DataSyncAgent
    {
        private String m_message;
        public String Message
        {
            get
            {
                return m_message;
            }
            set
            {
                m_message = value;
            }
        }
        public bool CheckOut(IWorkspace source,string dir,string dbname,string template,IPolygon area,string taskName,string dept)
        {
            
            m_message = "";
            //检查文件是否已经存在
            string mdbpath = dir + @"\" + dbname + ".mdb";
            bool isExist = File.Exists(mdbpath);

            if (isExist)
            {
                File.Delete(mdbpath);
            }

            Type factoryType = Type.GetTypeFromProgID(
    "esriDataSourcesGDB.AccessWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance
                (factoryType);
            
            IWorkspaceName workspaceName = workspaceFactory.Create(dir, dbname,
    null, 0);

            IWorkspace workspace = workspaceFactory.OpenFromFile(mdbpath, 0);

            //导入库结构
            IGdbXmlImport importer = new GdbImporterClass();
            IEnumNameMapping mapping = null;
            bool isConflict = false;
            isConflict = importer.GenerateNameMapping(template, workspace, out mapping);
            importer.ImportWorkspace(template, mapping, workspace, true);


            IDataset ds = workspace as IDataset;
            List<String> fcNames = new List<string>();
            Util.GetAllFeatureClassNames(ds, ref fcNames);

            IFeatureWorkspace source_ws = source as IFeatureWorkspace;
            IFeatureWorkspace target_ws = workspace as IFeatureWorkspace;
            IWorkspaceEdit wse = target_ws as IWorkspaceEdit;

            foreach (string fcname in fcNames)
            {
                IWorkspace2 source_ws2=source_ws as IWorkspace2;
                if (!source_ws2.get_NameExists(esriDatasetType.esriDTFeatureClass,fcname))
                {
                    continue;
                }
                IFeatureClass source_fc = source_ws.OpenFeatureClass(fcname);
                IFeatureClass target_fc = target_ws.OpenFeatureClass(fcname);
                AddSyncFields(target_fc);

                ISpatialFilter filter = new SpatialFilterClass();
                filter.Geometry = area as IGeometry;
                filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                wse.StartEditing(false);
                IFeatureCursor target_cur = target_fc.Insert(true);
                IFeatureBuffer buffer = target_fc.CreateFeatureBuffer();
                IFeatureCursor source_cur = source_fc.Search(filter,true);
                IFeature source_fea = source_cur.NextFeature();
                while (source_fea!=null)
                {
                    buffer.set_Value(target_fc.FindField("SyncID"), source_fea.OID);
                    for (int i = 0; i < source_fc.Fields.FieldCount; i++)
                    {
                        IField source_field = source_fc.Fields.get_Field(i);
                        if (source_field.Name != source_fc.OIDFieldName)
                        {
                            int target_field_index = target_fc.FindField(source_field.Name);
                            if (target_field_index != -1)
                            {
                                object source_value= source_fea.get_Value(source_fc.FindField(source_field.Name));
                                buffer.set_Value(target_field_index,source_value);
                            }
                        }
                    }
                    target_cur.InsertFeature(buffer);
                    source_fea = source_cur.NextFeature();
                }
                target_cur.Flush();
                wse.StopEditing(true);
            }

            IFeatureClass log_fc = source_ws.OpenFeatureClass("TaskLog");
            
            IWorkspaceEdit source_wse = source_ws as IWorkspaceEdit;
            source_wse.StartEditing(false);
            IFeature log_fea = log_fc.CreateFeature();
            log_fea.set_Value(log_fc.FindField("CheckOutDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            log_fea.set_Value(log_fc.FindField("TaskName"), taskName);
            log_fea.set_Value(log_fc.FindField("Dept"), dept);
            log_fea.set_Value(log_fc.FindField("Status"), Task.TaskManager.CHECKOUT_STATUS);
            log_fea.Shape = area;
            log_fea.Store();
            source_wse.StopEditing(true);

            return true;
        }

        static void AddSyncFields(IFeatureClass fc)
        {
            IFieldEdit fe = new FieldClass();
            fe.Name_2 = "SyncID";
            fe.Type_2 = esriFieldType.esriFieldTypeInteger;
            fe.IsNullable_2 = true;

            fc.AddField(fe as IField);

            fe = new FieldClass();
            fe.Name_2 = "SyncTimeStamp";
            fe.Type_2 = esriFieldType.esriFieldTypeDate;
            fe.IsNullable_2 = true;

            fc.AddField(fe as IField);

            fe = new FieldClass();
            fe.Name_2 = "SyncStatus";
            fe.Type_2 = esriFieldType.esriFieldTypeString;
            fe.Length_2 = 4;
            fe.IsNullable_2 = true;

            fc.AddField(fe as IField);

            fe = new FieldClass();
            fe.Name_2 = "SyncEditable";
            fe.Type_2 = esriFieldType.esriFieldTypeString;
            fe.Length_2 = 1;
            fe.IsNullable_2 = true;
            fe.DefaultValue_2 = "T";

            fc.AddField(fe as IField);
        }

        public bool CheckIn(IWorkspace store,string versionName,string dbpath,string gridFeatureClass,string gridCodeFieldName)
        {
            //创建子版本
            IVersion ver_store = store as IVersion;
            IVersion new_version = ver_store.CreateVersion(versionName);
            new_version.Access = esriVersionAccess.esriVersionAccessPublic;
            IFeatureWorkspace target_ws = new_version as IFeatureWorkspace;
            IWorkspaceEdit2 wse = target_ws as IWorkspaceEdit2;
            //删除TaskGridLog
            ITable grid_tbl = target_ws.OpenTable("TaskGridLog");
            IQueryFilter grid_filter = new QueryFilterClass();
            grid_filter.WhereClause = "TaskName = '"+versionName+"'";
            wse.StartEditing(false);
            grid_tbl.DeleteSearchedRows(grid_filter);
            wse.StopEditing(true);
            //删除CheckItem
            IQueryFilter checkItems_filter = new QueryFilterClass();
            checkItems_filter.WhereClause = "versionName = '" + versionName + "'";
            ITable checkItems = target_ws.OpenTable("CheckItemPtn");
            checkItems.DeleteSearchedRows(checkItems_filter);
            checkItems = target_ws.OpenTable("CheckItemLn");
            checkItems.DeleteSearchedRows(checkItems_filter);
            checkItems = target_ws.OpenTable("CheckItemPoly");
            checkItems.DeleteSearchedRows(checkItems_filter);

            IFeatureClass grid_fc = target_ws.OpenFeatureClass(gridFeatureClass);
            int gridCodeFld_idx = grid_fc.FindField(gridCodeFieldName);
            Dictionary<string, int[]> updateGridCodes = new Dictionary<string, int[]>();
            ISpatialFilter gridFilter = new SpatialFilter();
            gridFilter.GeometryField = grid_fc.ShapeFieldName;
            gridFilter.AddField(gridCodeFieldName);
            gridFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            //总更新网格面积
            double totalUpdateGridsArea = 0;
            double totalUpdateGrids = 0;
            double totalUpdateItems = 0;
            double totalAddItems = 0;
            double totalDeleteItems = 0;

            wse.StartEditing(true);

            try
            {
                
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
                IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance
                    (factoryType);

                IWorkspace source = workspaceFactory.OpenFromFile(dbpath, 0);
                IDataset ds = source as IDataset;
                List<String> fcNames = new List<string>();
                Util.GetAllFeatureClassNames(ds, ref fcNames);
                IFeatureWorkspace source_ws = source as IFeatureWorkspace;
                foreach (string fcname in fcNames)
                {
                    IWorkspace2 target_ws2 = target_ws as IWorkspace2;
                    if (!target_ws2.get_NameExists(esriDatasetType.esriDTFeatureClass, fcname))
                    {
                        continue;
                    }
                    IFeatureClass source_fc = source_ws.OpenFeatureClass(fcname);
                    IFeatureClass target_fc = target_ws.OpenFeatureClass(fcname);

                    int syncid_idx = source_fc.FindField("SyncID");
                    Dictionary<int, int> field_idxs = new Dictionary<int, int>();
                    for (int i = 0; i < source_fc.Fields.FieldCount; i++)
                    {
                        IField source_field = source_fc.Fields.get_Field(i);
                        if (source_field.Name == "SyncID")
                        {
                            continue;
                        }
                        if (source_field.Name == "SyncTimeStamp")
                        {
                            continue;
                        }
                        if (source_field.Name == "SyncStatus")
                        {
                            continue;
                        }
                        if (source_field.Name == "SyncEditable")
                        {
                            continue;
                        }
                        if (source_field.Name == source_fc.OIDFieldName)
                        {
                            continue;
                        }
                        int j = target_fc.FindField(source_field.Name);
                        if (j != -1)
                        {
                            field_idxs.Add(i, j);
                        }
                    }

                    string checkItemName;
                    switch (target_fc.ShapeType)
                    {
                        case esriGeometryType.esriGeometryPoint:
                            checkItemName = "CheckItemPtn";
                            break;
                        case esriGeometryType.esriGeometryPolyline:
                            checkItemName = "CheckItemLn";
                            break;
                        case esriGeometryType.esriGeometryPolygon:
                            checkItemName = "CheckItemPoly";
                            break;
                        default:
                            checkItemName = "CheckItemPoly";
                            break;
                    }
                    IFeatureClass checkItem_fc = target_ws.OpenFeatureClass(checkItemName);
                    int checkItem_f1 = checkItem_fc.FindField("FeatureClassName");
                    int checkItem_f2 = checkItem_fc.FindField("FeatureID");
                    int checkItem_f3 = checkItem_fc.FindField("VersionName");
                    int checkItem_f4 = checkItem_fc.FindField(checkItem_fc.ShapeFieldName);
                    IGeoDataset checkItem_gds = checkItem_fc as IGeoDataset;

                    //同步更新
                    wse.StartEditOperation();
                    IQueryFilter filter = new QueryFilterClass();
                    filter.WhereClause = "[SyncStatus]='U'";

                    IFeatureCursor source_cur = source_fc.Search(filter, false);
                    IFeature source_fea = source_cur.NextFeature();
                    while (source_fea != null)
                    {
                        int id = Convert.ToInt32(source_fea.get_Value(syncid_idx));
                        IFeature target_fea = target_fc.GetFeature(id);
                        foreach (KeyValuePair<int, int> field_idx in field_idxs)
                        {
                            target_fea.set_Value(field_idx.Value, source_fea.get_Value(field_idx.Key));
                        }
                        target_fea.Store();
                        //添加check item
                        IFeature checkItem_fea = checkItem_fc.CreateFeature();
                        checkItem_fea.set_Value(checkItem_f1, fcname);
                        checkItem_fea.set_Value(checkItem_f2, id);
                        checkItem_fea.set_Value(checkItem_f3, versionName);
                        IGeometry shape = target_fea.ShapeCopy;
                        shape.Project(checkItem_gds.SpatialReference);
                        checkItem_fea.set_Value(checkItem_f4, shape);
                        checkItem_fea.Store();
                        //添加TaskGridLog
                        gridFilter.Geometry = target_fea.Shape;
                        IFeatureCursor grid_cur = grid_fc.Search(gridFilter, true);
                        IFeature grid_fea = grid_cur.NextFeature();
                        while (grid_fea != null)
                        {
                            string gridid = grid_fea.get_Value(gridCodeFld_idx).ToString();
                            if (updateGridCodes.ContainsKey(gridid))
                            {
                                updateGridCodes[gridid][0] += 1;
                            }
                            else
                            {
                                int[] c = new int[2] { 1, 0 };
                                updateGridCodes.Add(gridid, c);
                            }
                            IArea area = grid_fea.Shape as IArea;
                            totalUpdateGridsArea += area.Area;
                            grid_fea = grid_cur.NextFeature();
                        }
                        totalUpdateItems += 1;
                        source_fea = source_cur.NextFeature();
                    }
                    wse.StopEditOperation();

                    //同步删除
                    wse.StartEditOperation();
                    List<int> lst_del = new List<int>();
                    TaskManager tm = TaskManager.GetInstance();
                    ISpatialFilter target_filter = new SpatialFilterClass();
                    target_filter.Geometry = tm.GetTaskLocation(versionName);
                    target_filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                    target_filter.SubFields = "";
                    using (ComReleaser comReleaser = new ComReleaser())
                    {
                        IFeatureCursor target_fea_cur = target_fc.Search(target_filter, false);
                        comReleaser.ManageLifetime(target_fea_cur);
                        IFeature target_fea_del = target_fea_cur.NextFeature();
                        while (target_fea_del != null)
                        {
                            filter = new QueryFilterClass();
                            filter.WhereClause = "[SyncID]=" + target_fea_del.OID.ToString();
                            using (ComReleaser comReleaser2 = new ComReleaser())
                            {
                                IFeatureCursor source_cur_del = source_fc.Search(filter, true);
                                comReleaser2.ManageLifetime(source_cur_del);
                                source_fea = source_cur_del.NextFeature();
                                if (source_fea == null)
                                {
                                    lst_del.Add(target_fea_del.OID);
                                    totalDeleteItems += 1;
                                }
                            }
                            target_fea_del = target_fea_cur.NextFeature();
                        }
                    }
                    foreach (int id in lst_del)
                    {
                        IFeature target_fea_del = target_fc.GetFeature(id);
                        target_fea_del.Delete();
                    }
                    wse.StopEditOperation();

                    //同步新建
                    wse.StartEditOperation();
                    filter = new QueryFilterClass();
                    filter.WhereClause = "[SyncID] is null";
                    source_cur = source_fc.Search(filter, false);
                    source_fea = source_cur.NextFeature();
                    while (source_fea != null)
                    {
                        IFeature target_fea = target_fc.CreateFeature();
                        foreach (KeyValuePair<int, int> field_idx in field_idxs)
                        {
                            target_fea.set_Value(field_idx.Value, source_fea.get_Value(field_idx.Key));
                        }
                        target_fea.Store();
                        //添加check item
                        IFeature checkItem_fea = checkItem_fc.CreateFeature();
                        checkItem_fea.set_Value(checkItem_f1, fcname);
                        checkItem_fea.set_Value(checkItem_f2, target_fea.OID);
                        checkItem_fea.set_Value(checkItem_f3, versionName);
                        IGeometry shape = target_fea.ShapeCopy;
                        shape.Project(checkItem_gds.SpatialReference);
                        checkItem_fea.set_Value(checkItem_f4, shape);
                        checkItem_fea.Store();
                        //添加TaskGridLog
                        gridFilter.Geometry = target_fea.Shape;
                        IFeatureCursor grid_cur = grid_fc.Search(gridFilter, true);
                        IFeature grid_fea = grid_cur.NextFeature();
                        while (grid_fea != null)
                        {
                            string gridid = grid_fea.get_Value(gridCodeFld_idx).ToString();
                            if (updateGridCodes.ContainsKey(gridid))
                            {
                                updateGridCodes[gridid][1] += 1;
                            }
                            else
                            {
                                int[] c = new int[2] { 0, 1 };
                                updateGridCodes.Add(gridid, c);
                            }
                            IArea area = grid_fea.Shape as IArea;
                            totalUpdateGridsArea += area.Area;
                            grid_fea = grid_cur.NextFeature();
                        }
                        totalAddItems += 1;
                        source_fea = source_cur.NextFeature();
                    }
                    wse.StopEditOperation();
                }

                
                //添加TaskGridLog
                wse.StartEditOperation();              
                using (ComReleaser comR = new ComReleaser())
                {
                    ICursor tgl_cur = grid_tbl.Insert(true);
                    IRowBuffer tgl_rowBF = grid_tbl.CreateRowBuffer();
                    comR.ManageLifetime(tgl_rowBF);
                    comR.ManageLifetime(tgl_cur);
                    foreach (string gridcode in updateGridCodes.Keys)
                    {
                        tgl_rowBF.set_Value(1, versionName);
                        tgl_rowBF.set_Value(2, gridcode);
                        tgl_rowBF.set_Value(4, updateGridCodes[gridcode][1]);
                        tgl_rowBF.set_Value(5, updateGridCodes[gridcode][0]);
                        tgl_cur.InsertRow(tgl_rowBF);
                    }
                    tgl_cur.Flush();
                }
                wse.StopEditOperation();
               
                //设置Task的内容更新信息
                totalUpdateGrids = updateGridCodes.Keys.Count;
                wse.StartEditOperation();
                IFeatureClass task_fc = target_ws.OpenFeatureClass("TaskLog");
                IQueryFilter task_filter = new QueryFilterClass();
                task_filter.WhereClause = "TaskName = '" + versionName + "'";
                IFeatureCursor cur = task_fc.Update(task_filter, true);
                IFeature task_fea = cur.NextFeature();
                if (task_fea != null)
                {
                    task_fea.set_Value(task_fc.FindField("totalAddItems"),totalAddItems);
                    task_fea.set_Value(task_fc.FindField("totalUpdateItems"), totalUpdateItems);
                    task_fea.set_Value(task_fc.FindField("totalDeleteItems"), totalDeleteItems);
                    task_fea.set_Value(task_fc.FindField("totalGrids"), totalUpdateGrids);
                    task_fea.set_Value(task_fc.FindField("totalGridsArea"), totalUpdateGridsArea);
                    task_fea.Store();
                }            
                wse.StopEditOperation();
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                wse.StopEditing(true);
            }
            return true;
        }

        public bool ExistVersion(IWorkspace workspace, string versionName)
        {
            IVersionedWorkspace vw = workspace as IVersionedWorkspace;
            try
            {
                IVersion ver = vw.FindVersion(versionName);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public void DeleteVersion(IWorkspace workspace, string versionName)
        {
            IVersionedWorkspace vw = workspace as IVersionedWorkspace;
            IVersion ver = vw.FindVersion(versionName);
            AppManager am = AppManager.GetInstance();
            if (am.CurrentVersion.VersionName == versionName)
            {
                MainForm mf = am.AppForm;
                mf.ChangeToDefaultVersion();
            }
            ver.Delete();
        }

        public IPolygon GetCheckOutArea(string id)
        {
            IPolygon area = null;
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IFeatureClass fc = ws.OpenFeatureClass(Properties.Settings.Default.CheckOutArea);
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = Properties.Settings.Default.CheckOutAreaID +" = '" + id + "'";
            IFeatureCursor cur = fc.Search(filter, false);
            IFeature fea = cur.NextFeature();
            if (fea != null)
            {
                area = fea.Shape as IPolygon;
            }
            return area;
        }

        static public List<string> GetAllCheckOutArea()
        {
            List<string> ids = new List<string>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IFeatureClass fc = ws.OpenFeatureClass(Properties.Settings.Default.CheckOutArea);
            IFeatureCursor cur = fc.Search(null, false);
            IFeature fea = cur.NextFeature();
            while (fea != null)
            {
                string id = fea.get_Value(fc.FindField(Properties.Settings.Default.CheckOutAreaID)) as string;
                ids.Add(id);
                fea = cur.NextFeature();
            }
            return ids;
        }

        public List<string> CheckDataSchema(string template, string dbpath)
        {
            List<string> msgs = new List<string>();
            try
            {
                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
                IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance
                    (factoryType);

                IFeatureWorkspace source_fws = workspaceFactory.OpenFromFile(dbpath, 0) as IFeatureWorkspace;

                IScratchWorkspaceFactory target_wsf = new FileGDBScratchWorkspaceFactoryClass();
                IWorkspace target_ws = target_wsf.CreateNewScratchWorkspace();

                IGdbXmlImport importer = new GdbImporterClass();
                IEnumNameMapping mapping = null;
                bool isConflict = false;
                isConflict = importer.GenerateNameMapping(template, target_ws, out mapping);
                importer.ImportWorkspace(template, mapping, target_ws, true);

                IDataset ds = target_ws as IDataset;
                List<String> fcNames = new List<string>();
                Util.GetAllFeatureClassNames(ds, ref fcNames);

                IFeatureWorkspace target_fws = target_ws as IFeatureWorkspace;
                IWorkspace2 source_ws = source_fws as IWorkspace2;
                foreach (string fcName in fcNames)
                {
                    if(!source_ws.get_NameExists(esriDatasetType.esriDTFeatureClass,fcName))
                    {
                        msgs.Add(string.Format("数据源中丢失图层：{0}", fcName));
                        continue;
                    }
                    IFeatureClass source_fc = source_fws.OpenFeatureClass(fcName);
                    IFeatureClass target_fc = target_fws.OpenFeatureClass(fcName);
                    for (int i = 0; i < target_fc.Fields.FieldCount; i++)
                    {
                        IField fld = target_fc.Fields.get_Field(i);
                        if (fcName == target_fc.OIDFieldName)
                        {
                            continue;
                        }
                        int source_fld_idx = source_fc.FindField(fld.Name);
                        if (source_fld_idx == -1)
                        {
                            msgs.Add(string.Format("数据源中丢失字段{0}", fld.Name));
                        }
                        else
                        {
                            IField source_fld = source_fc.Fields.get_Field(source_fld_idx);
                            if (source_fld.Type != fld.Type)
                            {
                                msgs.Add(string.Format("数据源中字段{0}类型不正确", fld.Name));
                            }
                            if (source_fld.Length != fld.Length)
                            {
                                msgs.Add(string.Format("数据源中字段{0}长度不正确", fld.Name));
                            }
                            if (source_fld.Precision != fld.Precision)
                            {
                                msgs.Add(string.Format("数据源中字段{0}精度不正确", fld.Name));
                            }
                        }

                    }
                    if (source_fc.FindField("SyncID") == -1)
                    {
                        msgs.Add(@"数据源中丢失系统字段SyncID");
                    }
                    if (source_fc.FindField("SyncTimeStamp") == -1)
                    {
                        msgs.Add(@"数据源中丢失系统字段SyncTimeStamp");
                    }
                    if (source_fc.FindField("SyncStatus") == -1)
                    {
                        msgs.Add(@"数据源中丢失系统字段SyncStatus");
                    }
                    if (source_fc.FindField("SyncEditable") == -1)
                    {
                        msgs.Add(@"数据源中丢失系统字段SyncEditable");
                    }
                }
            }
            catch
            { }
            return msgs;
        }
    }
}
