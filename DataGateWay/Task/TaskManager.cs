using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using DataGateWay.Utilities;
using ESRI.ArcGIS.Geometry;

namespace DataGateWay.Task
{
    class TaskManager
    {
        private static TaskManager m_lock;

        private TaskManager()
        {
        }

        public static TaskManager GetInstance()
        {
            if (m_lock == null)
            {
                m_lock = new TaskManager();
            }
            return m_lock;
        }

        public const string CHECKOUT_STATUS="O";
        public const string CHECKIN_STATUS = "I";
        public const string AUTOCHECK_STATUS = "A";
        public const string MANUALCHECK_STATUS = "M";
        public const string FINISH_STATUS = "F";

        public void ChangeTasksStatus(string taskName,string status)
        {
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            IWorkspaceEdit wse = ws as IWorkspaceEdit;
            wse.StartEditing(false);
            if (rw != null)
            {
                wse.StartEditOperation();
                rw.set_Value(task_tbl.FindField("Status"), status);
                if (status == TaskManager.CHECKIN_STATUS)
                {
                    rw.set_Value(task_tbl.FindField("CheckInDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                else if(status ==TaskManager.AUTOCHECK_STATUS)
                {
                    rw.set_Value(task_tbl.FindField("AutoDataCheckDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                else if (status == TaskManager.MANUALCHECK_STATUS)
                {
                    rw.set_Value(task_tbl.FindField("ManualDataCheckDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                rw.Store();
                wse.StopEditOperation();
            }
            wse.StopEditing(true);
        }

        public void CreateTask(string taskName)
        {
            //IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            //ITable task_tbl = ws.OpenTable("TaskLog");
            //IRow rw = task_tbl.CreateRow();
            //rw.set_Value(task_tbl.FindField("TaskName"), taskName);
            //rw.Store();
        }

        /// <summary>
        /// 结束任务流程
        /// </summary>
        /// <param name="taskName"></param>
        public void FinishTask(string taskName)
        {
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IVersionedWorkspace vw = ws as IVersionedWorkspace;
            if (taskName.ToLower() == vw.DefaultVersion.VersionName.ToLower())
            {
                return;
            }
            IVersion ver = vw.FindVersion(taskName);
            IVersionEdit4 ve = ver as IVersionEdit4;
            IWorkspaceEdit wse = ver as IWorkspaceEdit;
            wse.StartEditing(true);
            ve.Reconcile4(vw.DefaultVersion.VersionName, true, true, true, true);
            if (ve.CanPost())
            {
                ve.Post(vw.DefaultVersion.VersionName);
            }
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            if (rw != null)
            {
                rw.set_Value(task_tbl.FindField("Status"),TaskManager.FINISH_STATUS);
                rw.set_Value(task_tbl.FindField("FinishDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                rw.Store();
            }
            wse.StopEditing(true);
            ver.Delete();
        }

        public void DeleteTask(string taskName)
        {
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            IWorkspaceEdit wse = ws as IWorkspaceEdit;
            
            if (rw != null)
            {
                string status = rw.get_Value(task_tbl.FindField("Status")) as string;
                if (status == TaskManager.CHECKOUT_STATUS)
                {
                    wse.StartEditing(true);
                    rw.Delete();
                    wse.StopEditing(true);
                }
                else if (status == TaskManager.CHECKIN_STATUS)
                {
                    IVersionedWorkspace vw = ws as IVersionedWorkspace;
                    IVersion ver = vw.FindVersion(taskName);
                    ver.Delete();

                    wse.StartEditing(true);
                    rw.Delete();
                    wse.StopEditing(true);

                }
                wse.StartEditing(false);
                ITable tgl_tbl = ws.OpenTable("TaskGridLog");
                IQueryFilter tgl_filter = new QueryFilterClass();
                tgl_filter.WhereClause = "TaskName = '" + taskName + "'";
                tgl_tbl.DeleteSearchedRows(tgl_filter);
                wse.StopEditing(true);

                IQueryFilter checkItem_filter = new QueryFilterClass();
                checkItem_filter.WhereClause = "VersionName = '" + taskName + "'";
                wse.StartEditing(false);
                ITable checkItemPtn_fc = ws.OpenTable("CheckItemPtn");
                checkItemPtn_fc.DeleteSearchedRows(checkItem_filter);

                ITable checkItemLn_fc = ws.OpenTable("CheckItemLn");
                checkItemLn_fc.DeleteSearchedRows(checkItem_filter);

                ITable checkItemPoly_fc = ws.OpenTable("CheckItemPoly");
                checkItemPoly_fc.DeleteSearchedRows(checkItem_filter);
                wse.StopEditing(true);
            }
           
        }

        public bool ExistTask(string taskName)
        {
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            if (rw != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IPolygon GetTaskLocation(string taskName)
        {
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            IFeatureClass task_fc = ws.OpenFeatureClass("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            IFeatureCursor cur = task_fc.Search(filter, false);
            IFeature task_fea = cur.NextFeature();
            if (task_fea != null)
            {
                return task_fea.Shape as IPolygon;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 查询任务列表
        /// </summary>
        /// <returns></returns>
        public List<string[]> GetTaskInfoList(string pTaskName,string pDept,string pStatus)
        {
            List<string[]> infos = new List<string[]>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            string whereStr = "";
            if (pTaskName != string.Empty)
            {
                whereStr = "TaskName like '%" + pTaskName + "%'";
            }
            if (pDept != string.Empty)
            {
                if (whereStr != string.Empty)
                {
                    whereStr += " and ";
                }
                whereStr += "Dept like '%" + pDept + "%'";
            }
            if (pStatus != string.Empty)
            {
                if (whereStr != string.Empty)
                {
                    whereStr += " and ";
                }
                whereStr += "Status='" + pStatus + "'";
            }
            filter.WhereClause = whereStr;
            IQueryFilterDefinition2 qfd = filter as IQueryFilterDefinition2;
            qfd.PostfixClause = "Order by CheckOutDate Desc";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            while (rw != null)
            {
                string[] values = new string[6];
                values[0] = rw.get_Value(task_tbl.FindField("TaskName")) as string;
                values[1] = rw.get_Value(task_tbl.FindField("Dept")) as string;
                values[2] = rw.get_Value(task_tbl.FindField("CheckOutDate")) as string;
                values[3] = rw.get_Value(task_tbl.FindField("CheckInDate")) as string;
                string status = rw.get_Value(task_tbl.FindField("Status")) as string;
                switch (status)
                {
                    case TaskManager.CHECKOUT_STATUS:
                        values[4] = "下发";
                        break;
                    case TaskManager.CHECKIN_STATUS:
                        values[4] = "上传";
                        break;
                    case TaskManager.FINISH_STATUS:
                        values[4] = "结束";
                        break;
                    case TaskManager.MANUALCHECK_STATUS:
                        values[4] = "监理已检查";
                        break;
                }
                values[5] = rw.get_Value(task_tbl.FindField("FinishDate")) as string;
                //values[6] = rw.get_Value(task_tbl.FindField("comment")) as string;
                //values[7] = rw.get_Value(task_tbl.FindField("totalUpdateItems")) as string;
                //values[8] = rw.get_Value(task_tbl.FindField("totalAddItems")) as string;
                //values[9] = rw.get_Value(task_tbl.FindField("totalDeleteItems")) as string;
                //values[10] = rw.get_Value(task_tbl.FindField("totalUpdateGrids")) as string;
                //values[11] = rw.get_Value(task_tbl.FindField("totalUpdateGridsArea")) as string;
                infos.Add(values);
                rw = cur.NextRow();
            }
            return infos;
        }

        public List<string> GetTaskInfoDetail(string taskName)
        {
            List<string> infos = new List<string>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_tbl.Search(filter, true);
            IRow rw = cur.NextRow();
            if (rw != null)
            {
                string pValue;
                pValue = rw.get_Value(task_tbl.FindField("TaskName")) as string;
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("Dept")) as string;
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("comment")) as string;
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("totalUpdateItems")).ToString();
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("totalAddItems")).ToString();
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("totalDeleteItems")).ToString();
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("totalGrids")).ToString();
                infos.Add(pValue);
                pValue = rw.get_Value(task_tbl.FindField("totalGridsArea")).ToString();
                infos.Add(pValue);
            }
            return infos;
        }

        public List<string[]> GetUpdateGridsInfoList(string taskName)
        {
            List<string[]> grids = new List<string[]>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_fc = ws.OpenTable("TaskGridLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "'";
            ICursor cur = task_fc.Search(filter, false);
            IRow task_fea = cur.NextRow();
            while (task_fea != null)
            {
                string[] values = new string[4];
                values[0] = task_fea.get_Value(task_fc.FindField("gridcode")) as string;
                string ispassed = task_fea.get_Value(task_fc.FindField("passed")).ToString();
                if (ispassed == "")
                {
                    values[1] = "否";
                }
                else
                {
                    values[1] = "是";
                }
                values[2] = task_fea.get_Value(task_fc.FindField("totaladditems")).ToString();
                values[3] = task_fea.get_Value(task_fc.FindField("totalupdateitems")).ToString();
                grids.Add(values);
                task_fea = cur.NextRow();
            }
            return grids;
        }

        public IPolygon GetGridLocation(string gridcode)
        {
            string codeField = Properties.Settings.Default.GridCodeField;
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            string gridLayer = Properties.Settings.Default.Grid;
            IFeatureClass task_fc = ws.OpenFeatureClass(gridLayer);
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = codeField + " = '" + gridcode + "'";
            IFeatureCursor cur = task_fc.Search(filter, false);
            IFeature grid_fea = cur.NextFeature();
            if (grid_fea != null)
            {
                return grid_fea.Shape as IPolygon;
            }
            else
            {
                return null;
            }
        }
       
        public string[] WaitCheckInTasks
        {
            get
            {
                return GetTaskName("O");
            }
        }
                
        public string[] WaitDataCheckTasks
        {
            get
            {
                return GetTaskName("I");
            }
        }

        private string[] GetTaskName(string status)
        {
            List<string> names = new List<string>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "Status = '"+status+"'";
            ICursor cur = task_tbl.Search(filter, false);
            IRow rw = cur.NextRow();
            while (rw != null)
            {
                string n = rw.get_Value(task_tbl.FindField("TaskName")) as string;
                names.Add(n);
                rw = cur.NextRow();
            }
            return names.ToArray();
        }

        //获得通过的更新网格总数
        public int GetPassedUpdateGrids(string taskName)
        {
            int total=0;
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_fc = ws.OpenTable("TaskGridLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TaskName = '" + taskName + "' and "+"passed = 1";
            filter.SubFields = "";
            ICursor cur = task_fc.Search(filter, false);
            IRow task_fea = cur.NextRow();
            while (task_fea != null)
            {
                total += 1;
                task_fea = cur.NextRow();
            }
            return total;
        }
    }


}
