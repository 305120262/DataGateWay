using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataGateWay.Task;

namespace DataGateWay
{
    public partial class QCReportForm : Form
    {
        public QCReportForm()
        {
            InitializeComponent();
        }

        private void QCReportForm_Load(object sender, EventArgs e)
        {
            AppManager am = AppManager.GetInstance();
            string taskName = am.TaskName;
            TaskManager tm = TaskManager.GetInstance();
            List<string> infos = tm.GetTaskInfoDetail(taskName);
            if (infos.Count!=0)
            {
                this.tbxTaskName.Text = infos[0];
                this.tbxDept.Text = infos[1];
                this.tbxCheckComment.Text = infos[2];
                this.tbxUpdateItems.Text = infos[3];
                this.tbxAddItems.Text = infos[4];
                this.tbxDeleteItems.Text = infos[5];
                this.tbxUpdateGrids.Text = infos[6];
                this.tbxUpdateArea.Text = infos[7];
                this.tbxCheckedGrids.Text = tm.GetPassedUpdateGrids(taskName).ToString();
            }
        }
    }
}
