using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace DataReviewer
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

        public const string CHECKOUT_STATUS = "O";
        public const string CHECKIN_STATUS = "I";
        public const string AUTOCHECK_STATUS = "A";
        public const string MANUALCHECK_STATUS = "M";
        public const string FINISH_STATUS = "F";

        public void ChangeTasksStatus(string taskName, string status)
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
                else if (status == TaskManager.AUTOCHECK_STATUS)
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
                rw.set_Value(task_tbl.FindField("Status"), TaskManager.FINISH_STATUS);
                rw.set_Value(task_tbl.FindField("FinishDate"), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                rw.Store();
            }
            wse.StopEditing(true);
            ver.Delete();
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

        public List<string[]> GetTaskInfoList()
        {
            List<string[]> infos = new List<string[]>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            ICursor cur = task_tbl.Search(null, false);
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
                }
                values[5] = rw.get_Value(task_tbl.FindField("FinishDate")) as string;
                infos.Add(values);
                rw = cur.NextRow();
            }
            return infos;
        }

        public string[] WaitCheckInTasks
        {
            get
            {
                return GetTasks("O");
            }
        }

        /// <summary>
        /// 获得所有等待数据检测的任务
        /// </summary>
        public string[] WaitDataCheckTasks
        {
            get
            {
                return GetTasks("I");
            }
        }

        private string[] GetTasks(string status)
        {
            List<string> names = new List<string>();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            ITable task_tbl = ws.OpenTable("TaskLog");
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "Status = '" + status + "'";
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
    }
}
