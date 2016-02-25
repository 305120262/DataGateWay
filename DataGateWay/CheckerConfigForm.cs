using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using DataGateWay.QC;

namespace DataGateWay
{
    public partial class CheckerConfigForm : Form
    {
        private XElement m_schema;
        private string m_filepath;

        public CheckerConfigForm()
        {
            InitializeComponent();
        }

        private void CheckerConfigForm_Load(object sender, EventArgs e)
        {
            ReadSchemas();
            LoadSchema();
        }

        private void ReadSchemas()
        {
            string dir = SDECheckerManager.CheckerConfigsDir;
            string[] configs = Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < configs.Length; i++)
            {
                string f = configs[i];
                configs[i] = Path.GetFileNameWithoutExtension(f);
            }
            this.cbxSolution.DataSource = configs;
        }

        private void LoadSchema()
        {
            if (this.cbxSolution.Text != null)
            {
                lstCheckers.Items.Clear();
                m_filepath = SDECheckerManager.CheckerConfigsDir + this.cbxSolution.Text + ".xml";
                m_schema = XElement.Load(m_filepath);
                var checkers = from pn in m_schema.Descendants("Checker")
                               select pn;
                foreach (XElement c in checkers)
                {
                    lstCheckers.Items.Add(c.Attribute("Description").Value);
                }
            }
        }

        private void lstCheckers_SelectedIndexChanged(object sender, EventArgs e)
        {
            

            tbxDescription.Text = "";
            tbxType.Text = "";

            if (lstCheckers.SelectedItem != null)
            {
                LoadCheckerParameters(lstCheckers.Text);
            }
        }

        private void LoadCheckerParameters(string checkerName)
        {
            DataTable dt = new DataTable();
            DataColumn dc1 = new DataColumn("name");
            DataColumn dc2 = new DataColumn("value");
            dt.Columns.Add(dc1);
            dt.Columns.Add(dc2);
            dgParameters.DataSource = dt;

            var searched = from pn in m_schema.Descendants("Checker")
                           where pn.Attribute("Description").Value == checkerName
                           select pn;
            XElement checker = searched.First();
            this.tbxDescription.Text = checker.Attribute("Description").Value;
            this.tbxType.Text = checker.Attribute("Type").Value;
            var values = from v in checker.Descendants("p")
                         select v;

            var templates = from c in SDECheckerManager.CheckerMetaInfo.Descendants("Checker")
                            where c.Attribute("Type").Value == checker.Attribute("Type").Value
                            select c;
            XElement template = templates.First();

            var parameters = from p in templates.Descendants("p")
                             select p;

            int i = 0;
            foreach (var p in parameters)
            {
                try
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = p.Value;
                    if (values.Count() == 0)
                    {
                        dr[1] = "";
                    }
                    else
                    {
                        dr[1] = values.ElementAt(i).Value;
                    }
                    dt.Rows.Add(dr);
                }
                catch
                {
                }
                i++;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string newSchema = this.cbxSolution.Text;
            m_filepath = SDECheckerManager.CheckerConfigsDir + newSchema + ".xml";
            if (File.Exists(m_filepath))
            {
                MessageBox.Show("已经存在同名方案！");
                return;
            }
            XElement root = new XElement("Checkers");
            XDocument doc = new XDocument();
            doc.Declaration = new XDeclaration("1.0", "utf-8", "");
            doc.Add(root);
            doc.Save(m_filepath);
            m_schema = XElement.Load(m_filepath);
            ReadSchemas();
            this.cbxSolution.Text = newSchema;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (lstCheckers.SelectedItem != null)
            {
                var searched = from pn in m_schema.Descendants("Checker")
                               where pn.Attribute("Description").Value == lstCheckers.Text
                               select pn;
                XElement checker = searched.First();

                var templates = from c in SDECheckerManager.CheckerMetaInfo.Descendants("Checker")
                                where c.Attribute("Type").Value == checker.Attribute("Type").Value
                                select c;
                XElement template = templates.First();

                var parameters = from p in templates.Descendants("p")
                                 select p;

                XElement paras = checker.Descendants("parameters").First();
                paras.RemoveNodes();

                DataTable dt = dgParameters.DataSource as DataTable;
                foreach (DataRow dr in dt.Rows)
                {
                    paras.Add(new XElement("p", Convert.ToString(dr[1])));
                }
                m_schema.Save(m_filepath);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (lstCheckers.SelectedItem != null)
            {
                var searched = from pn in m_schema.Descendants("Checker")
                               where pn.Attribute("Description").Value == lstCheckers.Text
                               select pn;
                XElement checker = searched.First();
                checker.Remove();
                lstCheckers.Items.Remove(lstCheckers.SelectedItem);
            }
            m_schema.Save(m_filepath);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            NewCheckerForm form = new NewCheckerForm();
            DialogResult dlg = form.ShowDialog();
            if (dlg == DialogResult.OK)
            {
                lstCheckers.Items.Add(form.Description);

                XElement checker = new XElement("Checker");
                checker.SetAttributeValue("Type", form.CheckerType);
                checker.SetAttributeValue("Description", form.Description);
                checker.Add(new XElement("parameters"));
                m_schema.Add(checker);
                m_schema.Save(m_filepath);
                LoadCheckerParameters(form.Description);
            }
        }

        private void cbxSolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSchema();
        }
    }
}
