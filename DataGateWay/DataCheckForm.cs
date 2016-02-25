using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataGateWay.QC;
using System.IO;
using DataGateWay.Task;
using DevComponents.DotNetBar;

namespace DataGateWay
{
    public partial class DataCheckForm : Form
    {
        private string m_taskName;
        private string[] m_log;

        public DataCheckForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            SDECheckerManager cm = SDECheckerManager.GetInstance();
            cm.LoadConfig(Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckerConfigs/"+cbxSolution.Text+".xml");
            this.progressBar1.Visible = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private void DataCheckForm_Load(object sender, EventArgs e)
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath) + @"/CheckerConfigs/";
            string[] files = Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories);
            for(int i=0;i<files.Length;i++)
            {
                string f = files[i];
                files[i] = Path.GetFileNameWithoutExtension(f);
            }
            this.cbxSolution.DataSource = files;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            SDECheckerManager cm = SDECheckerManager.GetInstance();
            AppManager am = AppManager.GetInstance();
            cm.Check(am.TaskName, cbxCheckPartial.Checked); 
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.button1.Enabled = true;
            SDECheckerManager cm = SDECheckerManager.GetInstance();
            lstErrors.DataSource = null;
            lstErrors.Items.Clear();
            lstErrors.DataSource = cm.Errors;
            lblProblemCount.Text = cm.Errors.Count.ToString();
            lblRecordCount.Text = cm.CheckItemCount.ToString();
            tbxLog.Lines = cm.Log.ToArray();

            this.progressBar1.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "txt|*.txt";
            DialogResult dlgret = dlg.ShowDialog();
            if (dlgret == DialogResult.OK)
            {
                StreamWriter sw = File.CreateText(dlg.FileName);
                for (int i = 0; i < m_log.Length; i++)
                {
                    sw.WriteLine(m_log[i]);
                }
                sw.Flush();
                sw.Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lstErrors.SelectedItem != null)
            {
                CheckError error = lstErrors.SelectedItem as CheckError;
                if (error.Locations!=null && error.Locations.Count > 0)
                {
                    AppManager am = AppManager.GetInstance();
                    am.AppForm.LocateCheckError(error.Locations[0]);
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }


    }
}
