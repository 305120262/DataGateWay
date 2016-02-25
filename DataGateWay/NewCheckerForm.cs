using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataGateWay.QC;

namespace DataGateWay
{
    public partial class NewCheckerForm : Form
    {
        public string Description;
        public string CheckerType;

        public NewCheckerForm()
        {
            InitializeComponent();
        }

        private void NewCheckerForm_Load(object sender, EventArgs e)
        {
            cbxType.Items.Clear();
            var templates = from c in SDECheckerManager.CheckerMetaInfo.Descendants("Checker")
                            select c;
            foreach (var temp in templates)
            {
                cbxType.Items.Add(new CheckerItem { Description = temp.Attribute("Description").Value, CheckerType = temp.Attribute("Type").Value });
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Description = this.tbxDescription.Text;
            CheckerItem item =this.cbxType.SelectedItem as CheckerItem;
            this.CheckerType = item.CheckerType;
        }

    }

    class CheckerItem
    {
        public string Description;
        public string CheckerType;

        public override string ToString()
        {
            return Description;
        }
    }
}
