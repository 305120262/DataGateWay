using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace DataCheckToolAuxiliary
{
    public partial class setting2 : Form
    {
        public setting2()
        {
            InitializeComponent();
        }

        private void btn_browse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "mdb|*.mdb";
            dlg.ShowDialog();

            targetDbName_txt.Text = dlg.FileName;
            DataCheckToolAuxiliary.Form1.sourceDbName = dlg.FileName;
        }

        private void btnBrowse2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "mdb|*.mdb";
            dlg.ShowDialog();

            db_txt.Text = dlg.FileName;
            DataCheckToolAuxiliary.Form1.targetDbName = dlg.FileName;
        }

        private void btn_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pTargetFile = DataCheckToolAuxiliary.Form1.targetDbName;
            string pSourceFile = DataCheckToolAuxiliary.Form1.sourceDbName;
            DialogResult result = MessageBox.Show(this, "确定要合并数据库" + pTargetFile + " 和 " + pSourceFile + " 吗？", "提示", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                //打开目标数据库
                IWorkspaceName pWorkspaceName = new WorkspaceName() as IWorkspaceName;
                pWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesGDB.AccessWorkspaceFactory";
                pWorkspaceName.PathName = pTargetFile;
                IName pName;
                pName = pWorkspaceName as IName;
                IWorkspace workspace = (IWorkspace)pName.Open();
                IFeatureWorkspace target_ws = workspace as IFeatureWorkspace;
                IWorkspaceEdit wse = target_ws as IWorkspaceEdit;

                //打开源数据
                pWorkspaceName.PathName = pSourceFile;
                pName = pWorkspaceName as IName;
                IFeatureWorkspace source_ws = pName.Open() as IFeatureWorkspace;


                IDataset ds = workspace as IDataset;
                List<String> fcNames = new List<string>();
                Form1.GetAllFeatureClassNames(ds, ref fcNames);

                //遍历地图控件中的每个图层,进行数据合并
                foreach (string fcname in fcNames)
                {
                    //打开目标图层
                    IFeatureClass target_fc = target_ws.OpenFeatureClass(fcname);
                    IFeatureClass source_fc = source_ws.OpenFeatureClass(fcname);


                    //设置查询过滤关系
                    IQueryFilter pQuerFileter = new QueryFilter();
                    pQuerFileter.WhereClause = "";

                    wse.StartEditing(false);
                    IFeatureCursor target_cur = target_fc.Insert(true);
                    IFeatureBuffer buffer = target_fc.CreateFeatureBuffer();

                    IFeatureCursor source_cur = source_fc.Search(pQuerFileter, true);
                    IFeature pFeature = source_cur.NextFeature();

                    while (pFeature != null)
                    {
                        //只有可编辑情况下，才合并数据
                        if (pFeature.get_Value(source_fc.FindField("Editable")).ToString() == "1")
                        {
                            for (int n = 0; n < source_fc.Fields.FieldCount; n++)
                            {
                                IField source_field = source_fc.Fields.get_Field(n);
                                if (source_field.Name != source_fc.OIDFieldName && source_field.Name != "SHAPE_Area" && source_field.Name != "SHAPE_Length" && source_field.Name != "Editable")
                                {
                                    int target_field_index = source_fc.FindField(source_field.Name);
                                    if (target_field_index != -1)
                                    {
                                        object source_value = pFeature.get_Value(source_fc.FindField(source_field.Name));
                                        buffer.set_Value(target_field_index, source_value);
                                    }
                                }
                            }
                            target_cur.InsertFeature(buffer);   //合并追加一条记录
                        }
                        pFeature = source_cur.NextFeature();

                    }
                    target_cur.Flush();
                    wse.StopEditing(true);

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(source_cur);   //释放资源
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(target_cur);
                }
                System.Runtime.InteropServices.Marshal.ReleaseComObject(source_ws);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(target_ws);
                MessageBox.Show("数据合并成功！", "提示", MessageBoxButtons.OK);
            }
        }

    }
}
