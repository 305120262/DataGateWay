using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.GeoDatabaseDistributed;

namespace DataCheckToolAuxiliary
{
    public partial class Form1 : Form
    {
        public static IPolygon SelectArea;
        IMap pMap;
        IActiveView pActiveView;
        public static Boolean pIsSelect;                      //判断是否选择好分割范围
        private System.Object m_FillSymbol;    // 在MapControl上绘制范围使用的符号
        private IRgbColor pRGBColor;
        private int idx;
        public static string sourceDbName;    //需处理的个人数据库路径和名称
        public static string targetDbName;    //需合并的个人数据库
        public static string dbPath;          //需处理的个人数据库路径
        public static string dbName;          //新创建的数据库名称
        public static double pBufferDistance;         //缓冲区半径

        public Form1()
        {
            InitializeComponent();
            CreateOverviewSymbol();
            
           
        }

        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {

                
        }
          

        //样式设置
        private void CreateOverviewSymbol()
        {
            // 获取IRGBColor接口
            IRgbColor color = new RgbColor();

            // 设置颜色属性
            color.RGB = 255;
            pRGBColor = new RgbColor();
            pRGBColor.Red = 100;
            pRGBColor.Green = 250;
            pRGBColor.Blue = 0;          

            // 获取ILine符号接口
            ILineSymbol outline = new SimpleLineSymbol();

            // 设置线符号属性
            outline.Width = 1.5;
            outline.Color = color;

            // 获取IFillSymbol接口
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbol();
            //ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();

            // 设置填充符号属性

            simpleFillSymbol.Outline = outline;
            //simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSCross;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSHollow;
            m_FillSymbol = simpleFillSymbol;

        }


        //导出xml工作空间文档
        private void ExportWS_Schema(string pGDB, string XmlFile)
        {
            IWorkspaceFactory pWSF = new AccessWorkspaceFactory();
            IWorkspace pWS;
            pWS = pWSF.OpenFromFile(pGDB, 0);
            IGdbXmlExport pExporter = new GdbExporter();
            pExporter.ExportWorkspaceSchema(pWS, XmlFile, false, true);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWSF);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWS);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pExporter);
        }

        //导入xml工作空间
        private void ImportWS(string dbPath,string pGDB, string XmlFile)
        {
            IWorkspaceFactory pWSF = new AccessWorkspaceFactory();
            IWorkspaceName pWSN = pWSF.Create(dbPath, pGDB, null, 0);
            IName pName = (IName)pWSN;
            IWorkspace pWS = (IWorkspace)pName.Open();
            
            //导入库结构
            IGdbXmlImport pImporter = new GdbImporter();
            IEnumNameMapping pEnumName=null;
            pImporter.GenerateNameMapping(XmlFile, pWS, out pEnumName);
            pImporter.ImportWorkspace(XmlFile, pEnumName, pWS, true);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(pWS);



        }


        public static void GetAllFeatureClassNames(IDataset ds, ref List<String> names)
        {
            IEnumDataset ds_list = ds.Subsets;
            IDataset sub_ds = ds_list.Next();
            while (sub_ds != null)
            {
                if (sub_ds is IFeatureClass)
                {
                    names.Add(sub_ds.Name);
                }
                else if (sub_ds is IFeatureDataset)
                {
                    GetAllFeatureClassNames(sub_ds, ref names);
                }
                sub_ds = ds_list.Next();
            }
        }

        //数据合并
        private void 数据合并ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setting2 setForm = new setting2();
            setForm.Show(this);
        }

        //数据分割
        private void 数据分割ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Tool1 tool = new Tool1();
            idx = axToolbarControl1.AddItem(tool);
        }

        private void 选择范围ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请在地图上选择需要分割的区域!", "提示");
            ITool tool = this.axToolbarControl1.GetItem(idx).Command as ITool;
            this.axMapControl1.CurrentTool = tool;
        }


        private void 设置参数ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            setting setForm = new setting();
            setForm.Show();
        }

        private void 分割数据ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pIsSelect == false)
            {
                MessageBox.Show("请先进行地图分割范围选择", "提示");
            }
            else if (pIsSelect == true)
            {
                pActiveView = this.axMapControl1.ActiveView;
                pMap = pActiveView.FocusMap;


                ////先清除已有选择图形
                //pMap.ClearSelection();
                //pActiveView.Refresh();

                if (sourceDbName == "")
                {
                    MessageBox.Show("请选择需分割的个人数据库路径");
                    return;
                }
                else if (pBufferDistance == 0)
                {
                    MessageBox.Show("请设置缓冲区半径！");
                    return;
                }

                //数据分割之前先导出数据框架
                string XmlFile = System.Windows.Forms.Application.StartupPath + "\\WS_Schema.xml";   // 配置文件放置在应用程序的可执行文件的路径下。
                ExportWS_Schema(sourceDbName, XmlFile);

                //检查文件是否已经存在
                bool isExist = File.Exists(dbPath + dbName);
                if (isExist == true) File.Delete(dbPath + dbName);
                ImportWS(dbPath, dbName, XmlFile);

                //********************************************


                //IGeometry pGeometry = this.axMapControl1.TrackPolygon();
                IGeometry pGeometry = (IGeometry)SelectArea;
                IGeometry pBufferGeometry; //缓冲区几何图形

                axMapControl1.DrawShape(pGeometry, ref m_FillSymbol);

                //空间查询

                ILayer pLayer;
                IFeatureLayer pFeatureLayer;
                IFeatureClass pFeatureClass;
                ISpatialFilter pSpatialFilter = new SpatialFilter();
                IFeatureCursor pFeatureCursor;
                IFeature pFeature;
                int m;
                string pFileGDB;

                //遍历地图控件中的每个图层,进行多边形选择
                for (int i = 0; i < axMapControl1.Map.LayerCount; i++)
                {
                    pLayer = pMap.get_Layer(i);
                    pFeatureLayer = (IFeatureLayer)pLayer;
                    pFeatureClass = pFeatureLayer.FeatureClass;
                    string str = pFeatureClass.AliasName;
                    pSpatialFilter.set_GeometryEx(pGeometry, false);
                    //设置空间过滤关系为包含
                    pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                    pFeatureCursor = pFeatureClass.Search(pSpatialFilter, false);
                    pFeature = pFeatureCursor.NextFeature();

                    ////遍历所有符合要求的要素，使用IMap：：SelectFeature方法将他们添加到图层的选择要素层中去
                    while (pFeature != null)
                    {
                        pMap.SelectFeature(pFeatureLayer, pFeature);
                        pFeature = pFeatureCursor.NextFeature();
                    }
                    pActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                    m = this.axMapControl1.Map.SelectionCount;	// 选择要素的个数

                }

                DialogResult result = MessageBox.Show(this, "是否按此范围进行数据分割？", "提示", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    //pFileGDB = Properties.Settings.Default.targetPersonalGDB;//此处是写死的参数，可以修改
                    pFileGDB = dbPath + dbName;
                    //缓冲区
                    pGeometry.SpatialReference = this.axMapControl1.SpatialReference;
                    ITopologicalOperator topologicalOperator = pGeometry as ITopologicalOperator;

                    pBufferGeometry = topologicalOperator.Buffer(pBufferDistance);


                    //打开目标数据库
                    IWorkspaceName pWorkspaceName = new WorkspaceName() as IWorkspaceName;
                    pWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesGDB.AccessWorkspaceFactory";
                    pWorkspaceName.PathName = pFileGDB;
                    IName pName = pWorkspaceName as IName;
                    IFeatureWorkspace target_ws = pName.Open() as IFeatureWorkspace;
                    IWorkspaceEdit wse = target_ws as IWorkspaceEdit;

                    //遍历地图控件中的每个图层,进行数据分割
                    for (int i = 0; i < axMapControl1.Map.LayerCount; i++)
                    {
                        pLayer = pMap.get_Layer(i);
                        pFeatureLayer = (IFeatureLayer)pLayer;
                        pFeatureClass = pFeatureLayer.FeatureClass;
                        string pFeatureClassName = pFeatureClass.AliasName;

                        //打开目标图层
                        IFeatureClass target_fc = target_ws.OpenFeatureClass(pFeatureClassName);

                        //*********设置空间过滤关系为包含，选择框内的要素剪切到第二个个人数据库里，标识为可编辑状态******************
                        pSpatialFilter.set_GeometryEx(pGeometry, false);
                        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;

                        wse.StartEditing(false);
                        IFeatureCursor target_cur = target_fc.Insert(true);
                        IFeatureBuffer buffer = target_fc.CreateFeatureBuffer();

                        pFeatureCursor = pFeatureClass.Update(pSpatialFilter, true);
                        pFeature = pFeatureCursor.NextFeature();

                        while (pFeature != null)
                        {

                            for (int n = 0; n < pFeatureClass.Fields.FieldCount; n++)
                            {
                                IField source_field = pFeatureClass.Fields.get_Field(n);
                                if (source_field.Name != pFeatureClass.OIDFieldName && source_field.Name != "SHAPE_Area" && source_field.Name != "SHAPE_Length")
                                {
                                    int target_field_index = pFeatureClass.FindField(source_field.Name);
                                    if (target_field_index != -1)
                                    {
                                        if (source_field.Name == "Editable")
                                        {

                                            buffer.set_Value(target_field_index, 1);//选择框内的要素设定为可编辑

                                        }
                                        else
                                        {
                                            object source_value = pFeature.get_Value(pFeatureClass.FindField(source_field.Name));
                                            buffer.set_Value(target_field_index, source_value);
                                        }
                                    }
                                }
                            }
                            target_cur.InsertFeature(buffer);   //新的mdb图层中增加一条记录
                            pFeatureCursor.DeleteFeature();     //删除原数据库中的要素，保证每一个可编辑要素有并且只存在一个数据库中
                            pFeature = pFeatureCursor.NextFeature();

                        }

                        System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);   //释放资源


                        //***************设置空间过滤关系为相交，缓冲区内的要素复制到第二个个人数据库里，但标识为不可编辑状态****************
                        pSpatialFilter.set_GeometryEx(pBufferGeometry, false);
                        pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIndexIntersects;


                        pFeatureCursor = pFeatureClass.Search(pSpatialFilter, true);
                        pFeature = pFeatureCursor.NextFeature();

                        while (pFeature != null)
                        {

                            for (int n = 0; n < pFeatureClass.Fields.FieldCount; n++)
                            {
                                IField source_field = pFeatureClass.Fields.get_Field(n);
                                if (source_field.Name != pFeatureClass.OIDFieldName && source_field.Name != "SHAPE_Area" && source_field.Name != "SHAPE_Length")
                                {
                                    int target_field_index = pFeatureClass.FindField(source_field.Name);
                                    if (target_field_index != -1)
                                    {
                                        if (source_field.Name == "Editable")
                                        {

                                            buffer.set_Value(target_field_index, 0);//缓冲区内的要素设定为不可编辑

                                        }
                                        else
                                        {
                                            object source_value = pFeature.get_Value(pFeatureClass.FindField(source_field.Name));
                                            buffer.set_Value(target_field_index, source_value);
                                        }
                                    }
                                }
                            }
                            target_cur.InsertFeature(buffer);   //新的mdb图层中增加一条记录
                            pFeature = pFeatureCursor.NextFeature();

                        }
                        target_cur.Flush();
                        wse.StopEditing(true);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatureCursor);   //释放资源
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(target_cur);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(buffer);

                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(target_ws);
                    MessageBox.Show("数据分割成功！", "提示", MessageBoxButtons.OK);

                }
                pIsSelect = false;
                //*****************************************
            }
        }

    }
}
