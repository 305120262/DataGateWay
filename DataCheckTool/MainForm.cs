using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using DataGateWay.Utilities;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using DataGateWay.QC;
using System.IO;

namespace DataGateWay
{
    public partial class MainForm : Form
    {
        private ICommandPool m_cpool = new CommandPoolClass();

        public MainForm()
        {
            InitializeComponent();
            ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
            pool.SetHook(this.axMapControl1.Object);
        }

        public void LocateCheckError(IGeometry location)
        {
            if (location.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                //ITopologicalOperator topo = location as ITopologicalOperator;
                //IGeometry buffer = topo.Buffer(10);
                IPoint ptn = location as IPoint;
                IEnvelope env= this.axMapControl1.Extent;
                env.CenterAt(ptn);
                this.axMapControl1.Extent =env;
            }
            else
            {
                this.axMapControl1.Extent = location.Envelope;
            }
        }

        public void LoadLayers(List<string> fcnames)
        {
            this.axMapControl1.ClearLayers();
            IFeatureWorkspace ws = Util.ServerWorkspace as IFeatureWorkspace;
            foreach (string fcname in fcnames)
            {
                IFeatureClass fc = ws.OpenFeatureClass(fcname);
                IFeatureLayer flyr = new FeatureLayerClass();
                flyr.FeatureClass = fc;
                ILayer lyr = flyr as ILayer;
                lyr.Name = fcname;
                IGeoFeatureLayer gflyr = flyr as IGeoFeatureLayer;
                ISimpleRenderer render = new SimpleRendererClass();
                ISymbol sym=null;
                if (fc.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    sym = new SimpleFillSymbolClass();
                }
                render.Symbol = sym;
                gflyr.Renderer = render as IFeatureRenderer;
                this.axMapControl1.AddLayer(lyr);
            }
            this.axMapControl1.Extent = this.axMapControl1.FullExtent;
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void buttonItem3_Click(object sender, EventArgs e)
        {
            CheckerConfigForm form = new CheckerConfigForm();
            form.Show();
        }

        private void buttonItem4_Click(object sender, EventArgs e)
        {
            DataCheckForm form = new DataCheckForm();
            form.Show();
        }

        private void buttonItem5_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsMapZoomInTool";
            ICommand cmd=this.m_cpool.FindByUID(uID);
            if (cmd ==null)
            {
                cmd = new ControlsMapZoomInToolClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            ITool tool = cmd as ITool;
            this.axMapControl1.CurrentTool = tool;
        }

        private void buttonItem6_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsMapZoomOutTool";
            ICommand cmd = this.m_cpool.FindByUID(uID);
            if (cmd == null)
            {
                cmd = new ControlsMapZoomOutToolClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            ITool tool = cmd as ITool;
            this.axMapControl1.CurrentTool = tool;
        }

        private void buttonItem7_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsMapPanTool";
            ICommand cmd = this.m_cpool.FindByUID(uID);
            if (cmd == null)
            {
                cmd = new ControlsMapPanToolClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            ITool tool = cmd as ITool;
            this.axMapControl1.CurrentTool = tool;
        }

        private void buttonItem8_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsMapFullExtentCommand";
            ICommand cmd = this.m_cpool.FindByUID(uID);
            if (cmd == null)
            {
                cmd = new ControlsMapFullExtentCommandClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            cmd.OnClick();
        }

        private void buttonItem9_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsAddDataCommand";
            ICommand cmd = this.m_cpool.FindByUID(uID);
            if (cmd == null)
            {
                cmd = new ControlsAddDataCommandClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            cmd.OnClick();
        }

        private void buttonItem10_Click(object sender, EventArgs e)
        {
            UID uID = new UIDClass();
            uID.Value = "esriControls.ControlsMapIdentifyTool";
            ICommand cmd = this.m_cpool.FindByUID(uID);
            if (cmd == null)
            {
                cmd = new ControlsMapIdentifyToolClass();
                ICommandPoolEdit pool = this.m_cpool as ICommandPoolEdit;
                pool.AddCommand(cmd, uID);
                pool.CallOnCreate(cmd);
            }
            ITool tool = cmd as ITool;
            this.axMapControl1.CurrentTool = tool;
        }

        private void buttonItem11_Click(object sender, EventArgs e)
        {
            MdbCheckerManager cm = MdbCheckerManager.GetInstance();
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "txt|*.txt";
            DialogResult dlgret = dlg.ShowDialog();
            if (dlgret == DialogResult.OK)
            {
                StreamWriter sw = File.CreateText(dlg.FileName);
                foreach (string s in cm.Log)
                {
                    sw.WriteLine(s);
                }
                sw.Flush();
                sw.Close();
            }
        }
    }
}
