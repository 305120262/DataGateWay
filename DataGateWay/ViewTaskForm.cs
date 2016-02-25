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
    public partial class ViewTaskForm : Form
    {
        public ViewTaskForm()
        {
            InitializeComponent();
        }

        private void ViewTaskForm_Load(object sender, EventArgs e)
        {
            LoadTasks("","","");
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
                else if (item.SubItems[4].Text == "下发")
                {
                    mf.ChangeToCheckOutVerstion(item.SubItems[0].Text);
                }
                else if (item.SubItems[4].Text == "监理已检查")
                {
                    mf.ChangeToCheckInVersion(item.SubItems[0].Text);
                }
                else
                {
                    mf.ChangeToArchivedVersion(item.SubItems[0].Text, item.SubItems[5].Text);
                }
                this.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.listViewEx1.SelectedItems.Count > 0)
            {
                ListViewItem item = this.listViewEx1.SelectedItems[0];
                DialogResult dres = MessageBox.Show("是否确定取消选中任务", "取消任务", MessageBoxButtons.OKCancel);
                if (dres == DialogResult.OK)
                {
                    string taskName = item.SubItems[0].Text;
                    AppManager am = AppManager.GetInstance();
                    if (am.CurrentVersion.VersionName == taskName)
                    {
                        MainForm mf = am.AppForm;
                        mf.ChangeToDefaultVersion();
                    }
                    TaskManager tm = TaskManager.GetInstance();
                    tm.DeleteTask(item.SubItems[0].Text);
                    am.TaskName = "";
                    FillterTasks();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FillterTasks();
        }

        private void FillterTasks()
        {
            string status = "";
            if (this.radioButton1.Checked)
            {
                status = "";
            }
            else if (this.radioButton2.Checked)
            {
                status = TaskManager.CHECKOUT_STATUS;
            }
            else if (this.radioButton3.Checked)
            {
                status = TaskManager.CHECKIN_STATUS;
            }
            else if (this.radioButton4.Checked)
            {
                status = TaskManager.FINISH_STATUS;
            }
            else if (this.radioButton5.Checked)
            {
                status = TaskManager.MANUALCHECK_STATUS;
            }
            LoadTasks(this.tbxTaskName.Text, tbxDept.Text, status);
        }
    }
}
