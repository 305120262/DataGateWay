using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using DataGateWay.DataSync;
using DataGateWay.Utilities;
using DataGateWay.QC;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using DataGateWay.Task;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;

namespace DataGateWay
{
    public partial class MainForm : Form
    {
        private int idx;
        private IElement m_GridLocation;
        private IElement m_TaskLocation;
        private IElement m_ErrorLocation_ptn;
        private IElement m_ErrorLocation_ln;
        private IElement m_ErrorLocation_poly;

        public MainForm()
        {
            InitializeComponent();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("你是否确认退出应用程序?", "确认", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string mappath = Properties.Settings.Default.MapDoc;
            bool isMap = this.axMapControl1.CheckMxFile(mappath);
            if (isMap)
            {
                this.axMapControl1.LoadMxFile(mappath);
                this.axMapControl2.LoadMxFile(mappath);
            }
            for (int i = 0; i < axMapControl2.LayerCount; i++)
            {
                ILayer lyr = axMapControl2.get_Layer(i);
                IVersion ver = GetMapVersion(lyr);
                if(ver!=null)
                {
                    AppManager am = AppManager.GetInstance();
                    am.CurrentVersion = ver;
                    break;
                }
            }

            CheckOutTool tool = new CheckOutTool();
            idx = this.axToolbarControl1.AddItem(tool);
        }

        public void ChangeToDefaultVersion()
        {
            IVersionedWorkspace vws = Util.ServerWorkspace as IVersionedWorkspace;
            IVersion ver = vws.DefaultVersion;
            IMapAdmin3 ma = this.axMapControl2.Map as IMapAdmin3;
            IBasicMap bmap = this.axMapControl2.Map as IBasicMap;
            AppManager am = AppManager.GetInstance();
            IChangeDatabaseVersion cdv = new ChangeDatabaseVersionClass();
            cdv.Execute(am.CurrentVersion, ver, bmap);

            am.CurrentVersion = ver;
            am.TaskName = "";

            IGraphicsContainer gcon = this.axMapControl2.ActiveView.GraphicsContainer;
            gcon.DeleteAllElements();
            this.axMapControl2.Refresh(esriViewDrawPhase.esriViewGraphics,null,null);
            this.lstGrids.Items.Clear();
        }

        public void ChangeToCheckOutVerstion(string versionName)
        {
            this.superTabControl1.SelectedTabIndex = 1;
            IVersionedWorkspace vws = Util.ServerWorkspace as IVersionedWorkspace;
            IVersion ver = vws.DefaultVersion;
            IMapAdmin3 ma = this.axMapControl2.Map as IMapAdmin3;
            IBasicMap bmap = this.axMapControl2.Map as IBasicMap;
            AppManager am = AppManager.GetInstance();
            IChangeDatabaseVersion cdv = new ChangeDatabaseVersionClass();
            ma.FireChangeVersion(am.CurrentVersion, ver);
            cdv.Execute(am.CurrentVersion, ver, bmap);

            am.CurrentVersion = ver;
            am.TaskName = versionName;

            LocateTask(versionName);
            
        }

        public void ChangeToCheckInVersion(string versionName)
        {
            this.superTabControl1.SelectedTabIndex = 1;

            IVersionedWorkspace vws = Util.ServerWorkspace as IVersionedWorkspace;
            IVersion ver = vws.FindVersion(versionName);
            IMapAdmin3 ma = this.axMapControl2.Map as IMapAdmin3;
            IBasicMap bmap = this.axMapControl2.Map as IBasicMap;
            AppManager am = AppManager.GetInstance();
            IChangeDatabaseVersion cdv = new ChangeDatabaseVersionClass();
            //ma.FireChangeVersion(am.CurrentVersion, ver);
            //ILayer lyr = this.axMapControl2.get_Layer(2);

            cdv.Execute(am.CurrentVersion, ver, bmap);

            am.CurrentVersion = ver;
            am.TaskName = versionName;

            LocateTask(versionName);
            SetUpdateGridList(versionName);
            

        }

        public void ChangeToArchivedVersion(string taskName,string timeStamp)
        {
            this.superTabControl1.SelectedTabIndex = 1;
            IHistoricalWorkspace hws = Util.ServerWorkspace as IHistoricalWorkspace;
            IVersionedWorkspace vws = hws as IVersionedWorkspace;
            DateTime ts = DateTime.Parse(timeStamp);
            IHistoricalVersion ver = hws.FindHistoricalVersionByTimeStamp(ts);
            IMapAdmin3 ma = this.axMapControl2.Map as IMapAdmin3;
            IBasicMap bmap = this.axMapControl2.Map as IBasicMap;
            AppManager am = AppManager.GetInstance();
            IChangeDatabaseVersion cdv = new ChangeDatabaseVersionClass();
            if (am.CurrentVersion == null)
            {
                ma.FireChangeVersion(vws.DefaultVersion, ver as IVersion);
                cdv.Execute(vws.DefaultVersion, ver as IVersion, bmap);
            }
            else
            {
                ma.FireChangeVersion(am.CurrentVersion, ver as IVersion);
                cdv.Execute(am.CurrentVersion, ver as IVersion, bmap);
            }
            am.CurrentVersion = ver as IVersion;
            am.TaskName = taskName;

            LocateTask(taskName);
            SetUpdateGridList(taskName);
            

        }

        private void SetUpdateGridList(string taskName)
        {
            this.lstGrids.Items.Clear();
            TaskManager tm = TaskManager.GetInstance();
            List<string[]> infos = tm.GetUpdateGridsInfoList(taskName);
            foreach (var info in infos)
            {
                ListViewItem item = new ListViewItem(info);
                this.lstGrids.Items.Add(item);
            }
            this.FilterLayers();
        }

        private void LocateTask(string taskName)
        {
            TaskManager tm = TaskManager.GetInstance();
            IPolygon location = tm.GetTaskLocation(taskName);
            if (location != null)
            {
                this.axMapControl2.Extent = location.Envelope;

                IGraphicsContainer gcon = this.axMapControl2.ActiveView.GraphicsContainer;
                if (m_TaskLocation == null)
                {
                    IRgbColor color = new RgbColorClass();
                    color.Blue = 0;
                    color.Green = 255;
                    color.Red = 0;
                    ISimpleLineSymbol slsym = new SimpleLineSymbolClass();
                    slsym.Color = color as IColor;

                    ISimpleFillSymbol sfsym = new SimpleFillSymbolClass();
                    sfsym.Outline = slsym as ILineSymbol;
                    sfsym.Style = esriSimpleFillStyle.esriSFSNull;

                    m_TaskLocation = new PolygonElementClass();
                    IFillShapeElement pelem = m_TaskLocation as IFillShapeElement;
                    pelem.Symbol = sfsym as IFillSymbol;
                    m_TaskLocation.Geometry = location as IGeometry;
                    gcon.AddElement(m_TaskLocation, 0);
                }
                else
                {
                    m_TaskLocation.Geometry = location as IGeometry;

                    gcon.UpdateElement(m_TaskLocation);
                }
               
            }
        }

        public void LocateCheckError(IGeometry location)
        {
            IGeometry area=location;
            if (location.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                ITopologicalOperator topo = location as ITopologicalOperator;
                IGeometry buffer = topo.Buffer(10);
                area = buffer.Envelope;
                this.axMapControl2.Extent = buffer.Envelope;
            }
            else
            {
                this.axMapControl2.Extent = location.Envelope;
            }
            IGraphicsContainer gcon = this.axMapControl2.ActiveView.GraphicsContainer;
            if (m_ErrorLocation_ptn != null)
            {
                gcon.DeleteElement(m_ErrorLocation_ptn);
            }
            if (m_ErrorLocation_ln != null)
            {
                gcon.DeleteElement(m_ErrorLocation_ln);
            }
            if (m_ErrorLocation_poly != null)
            {
                gcon.DeleteElement(m_ErrorLocation_poly);
            }

            if (location.GeometryType==esriGeometryType.esriGeometryPoint)
            {
                IRgbColor color = new RgbColorClass();
                color.Blue = 0;
                color.Green = 0;
                color.Red = 255;
                ISimpleMarkerSymbol smsym = new SimpleMarkerSymbolClass();
                smsym.Color = color as IColor;
                smsym.Size = 10;

                m_ErrorLocation_ptn = new MarkerElementClass();
                IMarkerElement pelem = m_ErrorLocation_ptn as IMarkerElement;
                pelem.Symbol = smsym as ISimpleMarkerSymbol;
                m_ErrorLocation_ptn.Geometry = location as IGeometry;

                gcon.AddElement(m_ErrorLocation_ptn, 0);
            }
            else if (location.GeometryType == esriGeometryType.esriGeometryPolyline)
            {
                IRgbColor color = new RgbColorClass();
                color.Blue = 0;
                color.Green = 0;
                color.Red = 255;
                ISimpleLineSymbol slsym = new SimpleLineSymbolClass();
                slsym.Color = color as IColor;
                slsym.Width = 4;

                m_ErrorLocation_ln = new LineElementClass();
                ILineElement pelem = m_ErrorLocation_ln as ILineElement;
                pelem.Symbol = slsym as ISimpleLineSymbol;
                m_ErrorLocation_ln.Geometry = location as IGeometry;

                gcon.AddElement(m_ErrorLocation_ln, 0);
            }
            else if (location.GeometryType == esriGeometryType.esriGeometryPolygon)
            {
                IRgbColor color = new RgbColorClass();
                color.Blue = 0;
                color.Green = 0;
                color.Red = 255;
                ISimpleLineSymbol slsym = new SimpleLineSymbolClass();
                slsym.Color = color as IColor;

                ISimpleFillSymbol sfsym = new SimpleFillSymbolClass();
                sfsym.Outline = slsym as ILineSymbol;
                sfsym.Style = esriSimpleFillStyle.esriSFSNull;

                m_ErrorLocation_poly = new PolygonElementClass();
                IFillShapeElement pelem = m_ErrorLocation_poly as IFillShapeElement;
                pelem.Symbol = sfsym as IFillSymbol;
                m_ErrorLocation_poly.Geometry = location as IGeometry;

                gcon.AddElement(m_ErrorLocation_poly, 0);
            }

            

        }

        public void SetCurrentTaskStatusLabel(string name)
        {
            this.statusLabel.Text = "数据库版本名称："+name;
        }

        public void SetCurrentTaskNameLabel(string name)
        {
            this.taskNameLabel.Text = "当前任务名称："+name;
        }

        private void lstGrids_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lstGrids.SelectedItems != null)
            {
                TaskManager tm = TaskManager.GetInstance();
                IPolygon location = tm.GetGridLocation(lstGrids.SelectedItems[0].SubItems[0].Text);
                if (location != null)
                {
                    ITopologicalOperator topo = location as ITopologicalOperator;
                    IGeometry buffer = topo.Buffer(10);
                    this.axMapControl2.Extent = buffer.Envelope;

                    IGraphicsContainer gcon = this.axMapControl2.ActiveView.GraphicsContainer;
                    if (m_GridLocation == null)
                    {
                        IRgbColor color = new RgbColorClass();
                        color.Blue = 255;
                        color.Green = 0;
                        color.Red = 0;
                        ISimpleLineSymbol slsym = new SimpleLineSymbolClass();
                        slsym.Color = color as IColor;

                        ISimpleFillSymbol sfsym = new SimpleFillSymbolClass();
                        sfsym.Outline = slsym as ILineSymbol;
                        sfsym.Style = esriSimpleFillStyle.esriSFSNull;

                        m_GridLocation=new PolygonElementClass();
                        IFillShapeElement pelem = m_GridLocation as IFillShapeElement;
                        pelem.Symbol = sfsym as IFillSymbol;
                        gcon.AddElement(m_GridLocation,0);
                    }
                    m_GridLocation.Geometry = location as IGeometry;
                    
                    gcon.UpdateElement(m_GridLocation);
                }
            }
        }

        //配置
        private void buttonItem10_Click(object sender, EventArgs e)
        {
            ConfigForm form = new ConfigForm();
            form.ShowDialog();
        }

        //自定义区域下发
        private void buttonItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请在地图上圈选需要导出的区域");
            ITool tool = this.axToolbarControl1.GetItem(idx).Command as ITool;
            this.axMapControl1.CurrentTool = tool;
            this.superTabControl1.SelectedTabIndex = 0;
        }

        //按作业区域下发
        private void buttonItem2_Click(object sender, EventArgs e)
        {
            CheckOutForm form = new CheckOutForm(CheckOutMode.Area);
            form.ShowDialog();
        }

        //上传
        private void buttonItem3_Click(object sender, EventArgs e)
        {
            CheckInForm form = new CheckInForm();
            form.ShowDialog();
            
        }

        //上传结果查看
        private void buttonItem4_Click(object sender, EventArgs e)
        {
            ViewCheckInDataForm form = new ViewCheckInDataForm();
            form.ShowDialog(this);
        }

        //运行质检
        private void buttonItem5_Click(object sender, EventArgs e)
        {
            DataCheckForm form = new DataCheckForm();
            form.Show();
        }

        //质检方案配置
        private void buttonItem6_Click(object sender, EventArgs e)
        {
            CheckerConfigForm form = new CheckerConfigForm();
            form.Show();
        }

        //任务列表
        private void buttonItem7_Click(object sender, EventArgs e)
        {
            ViewTaskForm form = new ViewTaskForm();
            form.Show();
        }

        //监理报告
        private void buttonItem8_Click(object sender, EventArgs e)
        {
            QCReportForm form = new QCReportForm();
            form.Show();
        }

        //提交
        private void buttonItem9_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("是否提交任务", "任务提交", MessageBoxButtons.YesNo);
            if(res==DialogResult.Yes)
            {
                this.buttonItem9.Enabled = false;
                AppManager am = AppManager.GetInstance();
                string postVersionName = am.CurrentVersion.VersionName;
                string[] versionfullName = postVersionName.Split('.');
                this.ChangeToDefaultVersion();
                TaskManager tm = TaskManager.GetInstance();
                tm.FinishTask(versionfullName[versionfullName.Length - 1]);
                this.buttonItem9.Enabled = true;
                MessageBox.Show("提交成功！");
            }
        }

        private void FilterLayers()
        {
            for (int i = 0; i < axMapControl2.LayerCount; i++)
            {
                ILayer lyr = axMapControl2.get_Layer(i);
                SetLayerDefinition(lyr);
            }
        }

        private void SetLayerDefinition(ILayer lyr)
        {
            if (lyr is ICompositeLayer)
            {
                ICompositeLayer comlyr = lyr as ICompositeLayer;
                for (int j = 0; j < comlyr.Count; j++)
                {
                    ILayer lyr2 = comlyr.get_Layer(j);
                    SetLayerDefinition(lyr2);
                }
            }
            else
            {
                switch (lyr.Name.Trim().ToLower())
                {
                    case "checkitemptn":
                    case "checkitemln":
                    case "checkitempoly":
                    case "checkarea":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "VersionName = '" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    case "passedgrid":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "passed =1 and TaskName='" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    case "updategrid":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "passed is null and TaskName='" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private IVersion GetMapVersion(ILayer lyr)
        {
            if (lyr is ICompositeLayer)
            {
                ICompositeLayer comlyr = lyr as ICompositeLayer;
                for (int j = 0; j < comlyr.Count; j++)
                {
                    ILayer lyr2 = comlyr.get_Layer(j);
                    IVersion ver = GetMapVersion(lyr2);
                    if (ver != null)
                    {
                        return ver;
                    }
                }
            }
            else
            {
                IFeatureLayer flyr = lyr as IFeatureLayer;
                if (flyr != null)
                {
                    IDataset dst = flyr.FeatureClass as IDataset;
                    IDataset ds_ws = dst.Workspace as IDataset;
                    IVersion source_ver = dst.Workspace as IVersion;
                    return source_ver;

                }
            }
            return null;
        }
    }
}
