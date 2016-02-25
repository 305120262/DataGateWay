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
    public partial class CommentForm : Form
    {
        public CommentForm()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TaskManager tm = TaskManager.GetInstance();
            AppManager am = AppManager.GetInstance();
            tm.ChangeTasksStatus(am.TaskName, TaskManager.MANUALCHECK_STATUS, comment: tbxComment.Text);
            MessageBox.Show("提交成功");
            this.Close();
        }

        private void CommentForm_Load(object sender, EventArgs e)
        {
            TaskManager tm = TaskManager.GetInstance();
            AppManager am = AppManager.GetInstance();
            int passed = tm.GetPassedUpdateGrids(am.TaskName);
            this.lblGrids.Text = passed.ToString();
        }
    }
}
