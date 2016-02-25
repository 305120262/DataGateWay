using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using DevComponents.DotNetBar;

namespace DataReviewer
{
    public partial class VersionSelectForm : Form
    {
        public string clickVersionName = "";
        
        public VersionSelectForm()
        {
            InitializeComponent();
        }

        public VersionSelectForm(ArrayList pVersions) 
        {
            InitializeComponent();
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            //初始化version列表:
            InitialVersionList(pVersions);
        }

        public VersionSelectForm(string[] pVersions) 
        {
            InitializeComponent();
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            //初始化version列表:
            InitialVersionList(pVersions);
        }

        public void InitialVersionList(ArrayList pVersions)
        {
            if (pVersions.Count == 0) return;
            for (int ii = 0; ii < pVersions.Count; ii++) 
            {
                ButtonItem buttonitem = new ButtonItem(pVersions[ii].ToString(),pVersions[ii].ToString());
                this.itemPanel1.Items.Add(buttonitem);

            }
            this.itemPanel1.Refresh();
        }

        public void InitialVersionList(string[] pVersions) 
        {
            if (pVersions.Length == 0) return;
            for (int ii = 0; ii < pVersions.Length; ii++) 
            {
                ButtonItem buttonitem = new ButtonItem(pVersions[ii].ToString(), pVersions[ii].ToString());
                this.itemPanel1.Items.Add(buttonitem);
            }
        }

        private void itemPanel1_ItemDoubleClick(object sender, MouseEventArgs e)
        {
            //双击确定，关闭窗口
            if (e.Clicks == 2) 
            {
                ButtonItem item = new ButtonItem();
                item = sender as ButtonItem;
                if (item == null) return;

                clickVersionName = item.Name;
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
 
        }
    }
}
