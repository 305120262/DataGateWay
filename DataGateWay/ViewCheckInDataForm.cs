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
    public partial class ViewCheckInDataForm : Form
    {
        public ViewCheckInDataForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MainForm mf = this.Owner as MainForm;
            mf.ChangeToCheckInVersion(this.cbxTask.Text);
            this.Close();
        }

        private void ViewCheckInDataForm_Load(object sender, EventArgs e)
        {
            TaskManager tm = TaskManager.GetInstance();
            this.cbxTask.DataSource = tm.WaitDataCheckTasks;
        }
    }
}
