using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataReviewer.Task;

namespace DataReviewer
{
    public partial class ViewTaskForm : Form
    {
        public ViewTaskForm()
        {
            InitializeComponent();
        }

        private void ViewTaskForm_Load(object sender, EventArgs e)
        {
            LoadTasks("","",TaskManager.CHECKIN_STATUS);
        }

        private void LoadTasks(string pTaskName, string pDept, string pStatus)
        {
            this.listViewEx1.Items.Clear();
            TaskManager tm = TaskManager.GetInstance();
            List<string[]> infos = tm.GetTaskInfoList(pTaskName,pDept,pStatus);
            foreach (var info in infos)
            {
                ListViewItem item = new ListViewItem(info);
                this.listViewEx1.Items.Add(item) ;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.listViewEx1.SelectedItems.Count > 0)
            {
                ListViewItem item = this.listViewEx1.SelectedItems[0];
                AppManager am = AppManager.GetInstance();
                MainForm mf = am.AppForm;
                if (item.SubItems[4].Text == "上传")
                {
                    mf.ChangeToCheckInVersion(item.SubItems[0].Text);
                }
            }
            this.Close();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            FillterTasks();
        }

        private void FillterTasks()
        {
            LoadTasks(this.tbxTaskName.Text, tbxDept.Text, TaskManager.CHECKIN_STATUS);
        }
    }
}
