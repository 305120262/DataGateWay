using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataGateWay.DataSync;
using DataGateWay.Utilities;
using DataGateWay.Task;

namespace DataGateWay
{
    public partial class CheckInForm : Form
    {
        public CheckInForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "mdb|*.mdb";
            dlg.ShowDialog();
            tbxFile.Text = dlg.FileName;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;
            this.lstCheckInMsgs.DataSource = null;
            this.lstCheckInMsgs.Items.Clear();
            string versionName = cbxTask.Text;
            if (versionName == string.Empty)
            {
                return;
            }
            DataSyncAgent agent = new DataSyncAgent();
            List<string> msgs = agent.CheckDataSchema(Properties.Settings.Default.DataTemplate, tbxFile.Text);
            this.lstCheckInMsgs.DataSource = msgs.ToArray();
            if (agent.ExistVersion(Util.ServerWorkspace, versionName))
            {
                DialogResult dlgret = MessageBox.Show("数据版本已经存在，是否覆盖？", "", MessageBoxButtons.YesNo);
                if (dlgret == DialogResult.No)
                {
                    return;
                }
                else
                {
                    agent.DeleteVersion(Util.ServerWorkspace, versionName);
                }
            }
            bool isSuccess = agent.CheckIn(Util.ServerWorkspace, versionName, tbxFile.Text, Properties.Settings.Default.Grid, Properties.Settings.Default.GridCodeField);
            if (isSuccess)
            {
                TaskManager tm = TaskManager.GetInstance();
                tm.ChangeTasksStatus(versionName, TaskManager.CHECKIN_STATUS);
                MessageBox.Show("导入成功");
                this.Close();
            }
            else
            {
                MessageBox.Show("导入失败");
                this.button2.Enabled = true;
            }
            
        }

        private void CheckInForm_Load(object sender, EventArgs e)
        {
            TaskManager tm = TaskManager.GetInstance();
            this.cbxTask.DataSource = tm.WaitCheckInTasks;
        }
    }
}
