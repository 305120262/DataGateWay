using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DataGateWay
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DataTemplate = this.tbxTemplate.Text;
            Properties.Settings.Default.CheckOutArea= this.tbxCheckOutArea.Text ;
            Properties.Settings.Default.CheckOutAreaID =this.tbxCheckOutAreaID.Text ;
            Properties.Settings.Default.MapDoc = this.tbxMapDoc.Text;
            Properties.Settings.Default.SDEDB=this.tbxSDEDB.Text;
            Properties.Settings.Default.SDEPassword=this.tbxSDEPassword.Text;
            Properties.Settings.Default.SDEServer=this.tbxSDEServer.Text;
            Properties.Settings.Default.SDEService=this.tbxSDEService.Text;
            Properties.Settings.Default.SDEUser=this.tbxSDEUser.Text ;
            Properties.Settings.Default.SDEVersion=this.tbxSDEVersion.Text;
            Properties.Settings.Default.Grid = this.tbxGrid.Text;
            Properties.Settings.Default.GridCodeField = this.tbxGridCodeField.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            this.tbxTemplate.Text = Properties.Settings.Default.DataTemplate;
            this.tbxCheckOutArea.Text = Properties.Settings.Default.CheckOutArea;
            this.tbxCheckOutAreaID.Text = Properties.Settings.Default.CheckOutAreaID;
            this.tbxMapDoc.Text = Properties.Settings.Default.MapDoc;
            this.tbxSDEDB.Text = Properties.Settings.Default.SDEDB;
            this.tbxSDEPassword.Text = Properties.Settings.Default.SDEPassword;
            this.tbxSDEServer.Text = Properties.Settings.Default.SDEServer;
            this.tbxSDEService.Text = Properties.Settings.Default.SDEService;
            this.tbxSDEUser.Text = Properties.Settings.Default.SDEUser;
            this.tbxSDEVersion.Text = Properties.Settings.Default.SDEVersion;
            this.tbxGrid.Text = Properties.Settings.Default.Grid;
            this.tbxGridCodeField.Text = Properties.Settings.Default.GridCodeField;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "xml|*.xml";
            DialogResult dlgret = dlg.ShowDialog();
            tbxTemplate.Text = dlg.FileName;
        }
    }
}
