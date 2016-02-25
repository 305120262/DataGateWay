using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;


namespace DataCheckToolAuxiliary
{
    public partial class setting : Form
    {
        public int i;
        public setting()
        {
            InitializeComponent();
        }

        private void browse_btn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "mdb|*.mdb";
            dlg.ShowDialog();

            sourceDbName_txt.Text = dlg.FileName;
            DataCheckToolAuxiliary.Form1.sourceDbName = dlg.FileName;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void setting_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void setting_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sourceDbName_txt.Text == "")
            {
                MessageBox.Show("请选择需分割的个人数据库路径");
                e.Cancel = true;
                return;
            }
            else if (bufferDistance_txt.Text == "0")
            {
                MessageBox.Show("请设置缓冲区半径！");
                e.Cancel = true;
                return;
            }

            FileInfo fi = new FileInfo(DataCheckToolAuxiliary.Form1.sourceDbName);
            DataCheckToolAuxiliary.Form1.dbPath = DataCheckToolAuxiliary.Form1.sourceDbName.Substring(0, DataCheckToolAuxiliary.Form1.sourceDbName.Length - fi.Name.Length);
            DataCheckToolAuxiliary.Form1.dbName = fi.Name.Substring(0, fi.Name.Length - 4) + "2.mdb";
            DataCheckToolAuxiliary.Form1.pBufferDistance = Double.Parse(bufferDistance_txt.Text);
        }

    }
}
