using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DataReviewer
{
    public partial class MapCatalogRelateForm : Form
    {

        string m_FeatureClassName = "";
        string m_CodeFiledName = "";

        public MapCatalogRelateForm()
        {
            InitializeComponent();
        }

        public MapCatalogRelateForm(string pFeatureClassName,string pCodeFieldName)
        {
            InitializeComponent();
            this.textBoxX1.Text = pFeatureClassName;
            this.textBoxX2.Text = pCodeFieldName;
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            if (this.textBoxX1.Text.Trim() == "" || this.textBoxX2.Text.Trim() == "") 
            {
                MessageBox.Show("输入信息不完整！");
                return;
            }

            m_FeatureClassName = this.textBoxX1.Text.Trim();
            m_CodeFiledName = this.textBoxX2.Text.Trim();

            this.Close();

        }

        public string getFeatureClassName() 
        {
            return m_FeatureClassName;
        }

        public string getCodeFieldName() 
        {
            return m_CodeFiledName;
        }

    }
}
