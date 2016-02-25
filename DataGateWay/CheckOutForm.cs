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
using ESRI.ArcGIS.Geometry;
using DataGateWay.Task;

namespace DataGateWay
{
    public partial class CheckOutForm : Form
    {
        public IPolygon CheckOutArea;
        public CheckOutMode CheckMode;
        public CheckOutForm(CheckOutMode mode)
        {
            InitializeComponent();
            CheckMode = mode;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "mdb|*.mdb";
            DialogResult dlgret = dlg.ShowDialog();
            tbxExportpath.Text = dlg.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TaskManager tm = TaskManager.GetInstance();
            if (tm.ExistTask(tbxTaskName.Text))
            {
                MessageBox.Show("该任务名称已存在，请用其他名称！");
                return;
            }
            this.button1.Enabled = false;
            DataSyncAgent agent = new DataSyncAgent();
            string template = Properties.Settings.Default.DataTemplate;
            string dir = System.IO.Path.GetDirectoryName(tbxExportpath.Text);
            string dbname = System.IO.Path.GetFileNameWithoutExtension(tbxExportpath.Text);
            IPolygon area = null;
            if (CheckMode == CheckOutMode.Custom)
            {
                area = CheckOutArea;
            }
            else
            {
                area = agent.GetCheckOutArea(this.cbxArea.Text);
            }
            bool isSuccess = agent.CheckOut(Util.ServerWorkspace, dir, dbname, template, area,tbxTaskName.Text,tbxDept.Text);
            this.button1.Enabled = true;
            if (isSuccess)
            {
                MessageBox.Show("下发成功");
            }
            else
            {
                MessageBox.Show("下发失败");
            }
            this.Close();
        }

        private void CheckOutForm_Load(object sender, EventArgs e)
        {
            if (this.CheckMode == CheckOutMode.Custom)
            {
                cbxArea.Enabled = false;
            }
            else
            {
                cbxArea.DataSource = DataSyncAgent.GetAllCheckOutArea();
                cbxArea.Enabled = true;
            }
        }
    }

    public enum CheckOutMode
    {
        Area,
        Custom
    }
}
