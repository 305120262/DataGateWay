using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using DevComponents.DotNetBar;
using System.Collections;
using DataReviewer.Task;

namespace DataReviewer
{
    public partial class MainForm : Form
    {

        #region members
        IWorkspace m_GlobalWorkspace;
        IPolygon m_NewPolygon;
        private IElement m_GridLocation;
        INewPolygonFeedback m_NewPolygonFeedback;
        INewRectangleFeedback m_RecFeedback = null;
        IWorkspaceEdit2 m_EditWorkspace;
        IMapControl4 m_MapCtrls;  
        string m_sketchshape = "";
        #endregion

        public MainForm()
        {
            AEInitializer.Initialize();
            InitializeComponent();
            
            m_MapCtrls = axMapControl1.Object as IMapControl4;
        }

        private void ribbonClientPanel1_Click(object sender, EventArgs e)
        {

        }

        private void superTabControlPanel1_Click(object sender, EventArgs e)
        {

        }

        
        /// <summary>
        ///添加检查区
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItem9_Click(object sender, EventArgs e)
        {
            //打开或关闭面板：
            if (this.panelEx1.Visible == false) this.panelEx1.Visible = true;
            else this.panelEx1.Visible = false;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItem1_Click(object sender, EventArgs e)
        {
            ViewTaskForm form = new ViewTaskForm();
            form.Show();
        }

        /// <summary>
        /// 开始检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItem8_Click(object sender, EventArgs e)
        {
            ////开始编辑：
            //IVersionedWorkspace3 versionWorkspace = m_GlobalWorkspace as IVersionedWorkspace3;
            //m_EditWorkspace = versionWorkspace as IWorkspaceEdit2;
            ////m_EditWorkspace = m_GlobalWorkspace as IWorkspaceEdit2;
            ////if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(true);

            //this.buttonItem8.Enabled = false;
            //this.buttonItem9.Enabled = true;
            //this.buttonItem11.Enabled = true;

            ////判断是否存在未检查要素：
            //if(this.itemPanel1.Items.Count>3)
            //this.btnSetGridPass.Enabled = true;

        }

        /// <summary>
        /// 切换到任务
        /// </summary>
        /// <param name="versionName"></param>
        public void ChangeToCheckInVersion(string versionName)
        {
            IVersionedWorkspace vws = Util.ServerWorkspace as IVersionedWorkspace;
            IVersion ver = vws.FindVersion(versionName);
            IMapAdmin3 ma = this.axMapControl1.Map as IMapAdmin3;
            IBasicMap bmap = this.axMapControl1.Map as IBasicMap;
            AppManager am = AppManager.GetInstance();
            IChangeDatabaseVersion cdv = new ChangeDatabaseVersionClass();
            if (am.CurrentVersion == null)
            {
                ma.FireChangeVersion(vws.DefaultVersion, ver);
                cdv.Execute(vws.DefaultVersion, ver, bmap);
            }
            else
            {
                ma.FireChangeVersion(am.CurrentVersion, ver);
                cdv.Execute(am.CurrentVersion, ver, bmap);
            }
            am.CurrentVersion = ver;
            am.TaskName = versionName;

            m_EditWorkspace = ver as IWorkspaceEdit2;
            FilterLayers();
            SetUpdateGridList(versionName);
            SetStatusLabel(versionName);
        }

        private void SetStatusLabel(string taskName)
        {
            TaskManager tm = TaskManager.GetInstance();
            List<string> infos = tm.GetTaskInfoDetail(taskName);
            string status = string.Format("任务名称：{0} 作业单位：{1} 总更新图幅数：{2} 总更新面积：{3} 新增要素总数：{4} 更新要素总数：{5} 删除要素总数:{6}",infos[0],infos[1],infos[6],infos[7],infos[4],infos[3],infos[5]);
            this.toolStripStatusLabel1.Text = status;
        }

        private void FilterLayers()
        {
            for (int i = 0; i < axMapControl1.LayerCount; i++)
            {
                ILayer lyr = axMapControl1.get_Layer(i);
                switch (lyr.Name.Trim())
                {
                    case "CheckItemPtn":
                    case "CheckItemLn":
                    case "CheckItemPoly":
                    case "CheckArea":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "VersionName = '" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    case "PassedGrid":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "passed=1 and TaskName='" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    case "UpdateGrid":
                        if (lyr is IFeatureLayerDefinition2)
                        {
                            IFeatureLayerDefinition2 flyrd = lyr as IFeatureLayerDefinition2;
                            flyrd.DefinitionExpression = "sde.sde.TaskGridLog.passed is null and sde.sde.TaskGridLog.TaskName='" + AppManager.GetInstance().TaskName + "'";
                        }
                        break;
                    default:
                        continue;
                }
            }
        }

        //private void InitialCatalogPanel() 
        //{
        //   //根据所选versionname列出所有相关网格的code
        //    this.itemPanel3.Items.Clear();
        //    //添加标题:
        //    LabelItem labelitem = new LabelItem("待检查图幅", "待检查图幅");
        //    this.itemPanel3.Items.Add(labelitem as BaseItem);

        //    //添加图幅列表：
        //    IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
        //    ITable table = checkWorkspace.OpenTable("TaskGridLog");

        //    ICursor cursor = CheckFeatueEditor.UncheckGridCode(table, AppManager.GetInstance().TaskName);
        //    if (cursor != null)
        //    {
        //        IRow row = cursor.NextRow();
        //        while (row != null)
        //        {
        //            int rowindex = row.Fields.FindFieldByAliasName("图幅号");
        //            string text = row.get_Value(rowindex) as string;
        //            ButtonItem buttonitem = new ButtonItem(text, text);
        //            this.itemPanel3.Items.Add(buttonitem as BaseItem);
        //            row = cursor.NextRow();
        //        }
        //    }
        //    this.itemPanel3.Refresh();
        //}

        private void InitialItemPanel() 
        {
            this.itemPanel1.Items.Clear();
            this.itemPanel2.Items.Clear();

            LabelItem labelitem = new LabelItem("待检查面要素","待检查面要素");
            this.itemPanel1.Items.Add(labelitem as BaseItem);
            labelitem = new LabelItem("已检查面要素", "已检查面要素");
            this.itemPanel2.Items.Add(labelitem as BaseItem);
            LoadAllUncheckedItems("CheckItemPoly");
            LoadAllCheckedItems("CheckItemPoly");

            labelitem = new LabelItem("待检查线要素", "待检查线要素");
            this.itemPanel1.Items.Add(labelitem as BaseItem);
            labelitem = new LabelItem("已检查线要素", "已检查线要素");
            this.itemPanel2.Items.Add(labelitem as BaseItem);
            LoadAllUncheckedItems("CheckItemLn");
            LoadAllCheckedItems("CheckItemLn");

            labelitem = new LabelItem("待检查点要素", "待检查点要素");
            this.itemPanel1.Items.Add(labelitem as BaseItem);
            labelitem = new LabelItem("已检查点要素", "已检查点要素");
            this.itemPanel2.Items.Add(labelitem as BaseItem);
            LoadAllUncheckedItems("CheckItemPtn");
            LoadAllCheckedItems("CheckItemPtn");

            itemPanel1.Refresh();
            itemPanel2.Refresh();
        }

        private void LoadAllUncheckedItems(string pFeatureClassName) 
        {
            //遍历更新的图层要素：
            IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
            IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass(pFeatureClassName);
            IFeatureCursor cursor = CheckFeatueEditor.UncheckFeaturesGDB(checkFeatureClass,AppManager.GetInstance().TaskName);
            //IFeatureCursor cursor = CheckFeatueEditor.UncheckFeaturesGDB(checkFeatureClass);
            if (cursor != null)
            {
                IFeature feature = cursor.NextFeature();
                while (feature != null)
                {
                    int namefieldindex = feature.Fields.FindFieldByAliasName("ObjectClass");
                    int fidfieldindex = feature.Fields.FindFieldByAliasName("FeatureID");
                    string text = feature.get_Value(fidfieldindex).ToString() + "." + feature.get_Value(namefieldindex).ToString();
                    //string text = feature.OID.ToString() + "." + feature.Class.AliasName;
                    ButtonItem buttonitem = new ButtonItem(text, text);
                    buttonitem.Tooltip = feature.OID.ToString() + "." + feature.Class.AliasName;
                    this.itemPanel1.Items.Add(buttonitem as BaseItem);
                    feature = cursor.NextFeature();
                }
            }          
        }

        private void LoadAllCheckedItems(string pFeatureClassName)
        {
            //遍历更新的图层要素：
            IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
            IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass(pFeatureClassName);
            IFeatureCursor cursor = CheckFeatueEditor.CheckFeaturesGDB(checkFeatureClass,AppManager.GetInstance().TaskName);
            //IFeatureCursor cursor = CheckFeatueEditor.CheckFeaturesGDB(checkFeatureClass);
            if (cursor != null)
            {
                IFeature feature = cursor.NextFeature();
                while (feature != null)
                {
                    int namefieldindex = feature.Fields.FindFieldByAliasName("ObjectClass");
                    int fidfieldindex = feature.Fields.FindFieldByAliasName("FeatureID");
                    string text = feature.get_Value(fidfieldindex).ToString() + "." + feature.get_Value(namefieldindex).ToString();
                    //string text = feature.OID.ToString() + "." + feature.Class.AliasName;
                    ButtonItem buttonitem = new ButtonItem(text, text);
                    buttonitem.Tooltip = feature.OID.ToString() + "." + feature.Class.AliasName;
                    this.itemPanel2.Items.Add(buttonitem as BaseItem);
                    feature = cursor.NextFeature();
                }
            }
        }

        private void buttonItem20_Click(object sender, EventArgs e)
        {
            m_sketchshape = "polygon";
            m_NewPolygonFeedback = null;
            m_RecFeedback = null;
            m_MapCtrls.CurrentTool = null;
            m_MapCtrls.ActiveView.Refresh();
        }

        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            
            #region 处理各种交互情况
            switch (m_sketchshape)
            {
                case "polygon":
                    {
                        IScreenDisplay iSDisplay = m_MapCtrls.ActiveView.ScreenDisplay;

                        IPoint newPoint = new PointClass();
                        newPoint = iSDisplay.DisplayTransformation.ToMapPoint(e.x, e.y); 

                        if (e.button == 1)
                        {
                            //左单击画多边形添加点：
                            if (m_NewPolygonFeedback == null)
                            {
                                m_NewPolygonFeedback = new NewPolygonFeedbackClass();

                                m_NewPolygonFeedback.Display = iSDisplay;
                                m_NewPolygonFeedback.Start(newPoint);
                            }
                            else m_NewPolygonFeedback.AddPoint(newPoint);

                        }
                        if (e.button == 2)
                        {
                            if (m_NewPolygonFeedback == null) return;
                            //右键结束绘制：
                            m_NewPolygonFeedback.AddPoint(newPoint);
                            m_NewPolygon = m_NewPolygonFeedback.Stop();

                            //若是逆时针创建，反转一下，变为顺时针：
                            IArea feedBackArea = m_NewPolygon as IArea;
                            if (feedBackArea.Area < 0) m_NewPolygon.ReverseOrientation();

                            IGeometry newGeometry = m_NewPolygon as IGeometry;
                            IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
                            IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass("CheckArea");


                            if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(false);

                            m_EditWorkspace.StartEditOperation();
                            int OID = CheckFeatueEditor.InsertNewFeature(newGeometry, checkFeatureClass,AppManager.GetInstance().TaskName);
                            m_EditWorkspace.StopEditOperation();
                            //ISelectionEnvironment selectionEnv = new SelectionEnvironmentClass();
                            //selectionEnv.AreaSelectionMethod = esriSpatialRelEnum.esriSpatialRelOverlaps;
                            //selectionEnv.CombinationMethod = esriSelectionResultEnum.esriSelectionResultNew;
                            //this.m_MapCtrls.ActiveView.FocusMap.SelectByShape(feature.Shape, null, true);
                            //this.m_MapCtrls.ActiveView.FocusMap.SelectByShape(feature.Shape, selectionEnv, true);
                            //m_MapCtrls.Map.SelectByShape(m_NewPolygon, null, true);
                            ILayer iLayer = m_MapCtrls.Layer[0];
                            IFeature iFeature = checkFeatureClass.GetFeature(OID);

                            clearFeatureSelection();

                            m_MapCtrls.Map.SelectFeature(iLayer, iFeature);
                            m_NewPolygonFeedback = null;
                            m_MapCtrls.ActiveView.Refresh();
                        }
                        break;
                    }

                case "rectangle":
                    {
                        IScreenDisplay iSDisplay = m_MapCtrls.ActiveView.ScreenDisplay;

                        IPoint newPoint = new PointClass();
                        newPoint = iSDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
                        if (e.button == 1)
                        {
                            if (m_RecFeedback == null)
                            {
                                m_RecFeedback = new NewRectangleFeedbackClass();
                                m_RecFeedback.Display = iSDisplay;
                                m_RecFeedback.Angle = 90;
                                m_RecFeedback.Start(newPoint);

                                newPoint.Y = newPoint.Y + 20;

                                m_RecFeedback.SetPoint(newPoint);

                            }
                            else m_RecFeedback.SetPoint(newPoint);
                        }
                        if (e.button == 2)
                        {
                            if (m_RecFeedback != null)
                            {
                                m_NewPolygon = m_RecFeedback.Stop(newPoint) as IPolygon;

                                //若是逆时针创建，反转一下，变为顺时针：
                                IArea feedBackArea = m_NewPolygon as IArea;
                                if (feedBackArea.Area < 0) m_NewPolygon.ReverseOrientation();

                                IGeometry newGeometry = m_NewPolygon as IGeometry;
                                IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
                                IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass("CheckArea");


                                if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(false);

                                m_EditWorkspace.StartEditOperation();
                                int OID = CheckFeatueEditor.InsertNewFeature(newGeometry, checkFeatureClass,AppManager.GetInstance().TaskName);
                                m_EditWorkspace.StopEditOperation();

                                ILayer iLayer = m_MapCtrls.Layer[0];
                                IFeature iFeature = checkFeatureClass.GetFeature(OID);

                                clearFeatureSelection();

                                m_MapCtrls.Map.SelectFeature(iLayer,iFeature);
                                m_MapCtrls.ActiveView.Refresh();
                                m_RecFeedback = null;
                            }

                        }

                        break;
                    }

            }
            #endregion
        }

        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            switch (m_sketchshape) 
            {
                case "polygon": 
                    {
                        if (m_NewPolygonFeedback != null)
                        {
                            IPoint iPoint = new PointClass();
                            iPoint = m_MapCtrls.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);

                            m_NewPolygonFeedback.MoveTo(iPoint);
                        }
                    }
                    break;
                case "rectangle": 
                    {
                        IScreenDisplay iSDisplay = m_MapCtrls.ActiveView.ScreenDisplay;

                        IPoint newPoint = new PointClass();
                        newPoint = iSDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);

                        if (m_RecFeedback != null) m_RecFeedback.MoveTo(newPoint);
                    }
                    break;
            }
           
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            //提交注释：
            if (this.textBoxX1.Text.Trim() == "") return;

            string textinput = this.textBoxX1.Text;
            //根据当前所选要素进行更新注释；
            IFeatureLayer iCheckLayer = m_MapCtrls.Map.get_Layer(0) as IFeatureLayer;
            IFeatureSelection iCheckSelection = iCheckLayer as IFeatureSelection;
            int CheckFeatureOID = 0;
            if(iCheckSelection.SelectionSet.Count == 1) CheckFeatureOID = iCheckSelection.SelectionSet.IDs.Next();

            if(CheckFeatureOID > 0)
            {
                IVersionedWorkspace3 versionWorkspace = m_GlobalWorkspace as IVersionedWorkspace3;
                IFeatureWorkspace checkWorkspace = versionWorkspace as IFeatureWorkspace;
                IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass("CheckArea");
                if (checkFeatureClass != null) 
                {
                    if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(false);

                    m_EditWorkspace.StartEditOperation();
                    int res = CheckFeatueEditor.UpdateComment(CheckFeatureOID, checkFeatureClass, textinput);
                    m_EditWorkspace.StopEditOperation();

                    if (res > 0) 
                    {
                        MessageBox.Show("提交成功！");
                        m_MapCtrls.ActiveView.Refresh();
                    }
                    else MessageBox.Show("提交失败！");
                }
            } 
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            //清除textbox里的文字：
            this.textBoxX1.Clear();
        }

        private void buttonItem21_Click(object sender, EventArgs e)
        {
            m_sketchshape = "featureselect";
            m_NewPolygonFeedback = null;
            ESRI.ArcGIS.SystemUI.ICommand selectCommand = new ControlsSelectFeaturesToolClass();
            selectCommand.OnCreate(m_MapCtrls.Object);
            m_MapCtrls.CurrentTool = selectCommand as ITool;
        }

        private void buttonItem22_Click(object sender, EventArgs e)
        {
            //删除所选要素(单选)：
            m_sketchshape = "featuredelete";
            m_NewPolygonFeedback = null;
            IFeatureLayer iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(0) as IFeatureLayer;
            IFeatureSelection CurrentSelection = iCheckLayer as IFeatureSelection;
            int OID = 0;
            if (CurrentSelection.SelectionSet.Count == 1) OID = CurrentSelection.SelectionSet.IDs.Next();
            if (OID > 0) 
            {
                IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
                IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass("CheckArea");
                if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(false);

                m_EditWorkspace.StartEditOperation();
                CheckFeatueEditor.DeleteCheckArea(OID, checkFeatureClass);
                m_EditWorkspace.StopEditOperation();

                MessageBox.Show("删除成功！");
                //清除选择要素：
                CurrentSelection.Clear();
                m_MapCtrls.ActiveView.Refresh();
                
            }
        }

        private void axMapControl1_OnSelectionChanged(object sender, EventArgs e)
        {
            this.textBoxX1.Clear();

            IFeatureLayer iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(0) as IFeatureLayer;
            IFeatureSelection CurrentSelection = iCheckLayer as IFeatureSelection;
            int OID = 0;
            if (CurrentSelection.SelectionSet.Count == 1) OID = CurrentSelection.SelectionSet.IDs.Next();
            if (OID > 0) 
            {
                IFeatureWorkspace checkWorkspace = m_GlobalWorkspace as IFeatureWorkspace;
                IFeatureClass checkFeatureClass = checkWorkspace.OpenFeatureClass("CheckArea");
                this.textBoxX1.Text = CheckFeatueEditor.GetComment(OID, checkFeatureClass);
                //this.textBoxX1.Text = checkFeatureClass.GetFeature(OID).get_Value(5).ToString();
            }
            
        }

        private void buttonItem6_Click(object sender, EventArgs e)
        {
            //获取全图范围：
            m_MapCtrls.ActiveView.Extent = m_MapCtrls.ActiveView.FullExtent;
            m_MapCtrls.ActiveView.Refresh();
        }

        private void buttonItem7_Click(object sender, EventArgs e)
        {
            //设置为移动Icommand，PAN:
            m_sketchshape = "pan";
            m_NewPolygonFeedback = null;
            ESRI.ArcGIS.SystemUI.ICommand panCommand = new ControlsMapPanToolClass();
            panCommand.OnCreate(m_MapCtrls.Object);
            m_MapCtrls.CurrentTool = panCommand as ITool;
        }

        private void buttonItem10_Click(object sender, EventArgs e)
        {
            //设置为放大Icommand，Zoom in:
            m_sketchshape = "zoomin";
            m_NewPolygonFeedback = null;
            ESRI.ArcGIS.SystemUI.ICommand panCommand = new ControlsMapZoomInToolClass();
            panCommand.OnCreate(m_MapCtrls.Object);
            m_MapCtrls.CurrentTool = panCommand as ITool;
        }

        private void buttonItem27_Click(object sender, EventArgs e)
        {
            //设置为缩小Icommand，Zoom out:
            m_sketchshape = "zoomout";
            m_NewPolygonFeedback = null;
            ESRI.ArcGIS.SystemUI.ICommand panCommand = new ControlsMapZoomOutToolClass();
            panCommand.OnCreate(m_MapCtrls.Object);
            m_MapCtrls.CurrentTool = panCommand as ITool;
        }

        private void buttonItem23_Click(object sender, EventArgs e)
        {
            //清除选择要素：
            IFeatureLayer iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(0) as IFeatureLayer;
            IFeatureSelection CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(3) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(2) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(1) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();


            m_MapCtrls.ActiveView.Refresh();
        }

        private void itemPanel1_ItemDoubleClick(object sender, MouseEventArgs e)
        {
            //若是左建双击缩放至要素：
            if (e.Clicks == 2) 
            {
                ButtonItem item = new ButtonItem();
                item = sender as ButtonItem;
                if (item == null) return;
                string FeatureClassName = item.Tooltip.Substring(item.Tooltip.IndexOf('.') + 1);
                string strOID = item.Tooltip.Substring(0, item.Tooltip.IndexOf('.'));
                int OID = Convert.ToInt32(strOID);

                IFeature feature = DBOperator.getFeatureFrom(OID, FeatureClassName, m_GlobalWorkspace);
                IGeometry5 geometry = feature.Shape as IGeometry5;
                this.m_MapCtrls.CenterAt(geometry.CentroidEx);
                this.m_MapCtrls.ActiveView.Extent = geometry.Envelope;
                ILayer iLayer = null;

                if (FeatureClassName.Contains("CheckItemPtn"))
                {
                    iLayer = m_MapCtrls.Map.Layer[1];
                }
                else if (FeatureClassName.Contains("CheckItemPtn"))
                {
                    iLayer = m_MapCtrls.Map.Layer[2];
                }
                else
                {
                    iLayer = m_MapCtrls.Map.Layer[3];
                }

                clearFeatureSelection();

                m_MapCtrls.Map.SelectFeature(iLayer, feature);
                //this.m_MapCtrls.ActiveView.FocusMap.SelectByShape(feature.Shape, null, true);
                //this.m_MapCtrls.ActiveView.FocusMap.SelectByShape(feature.Shape, selectionEnv, true);

            }
        }

        private void itemPanel1_ItemClick(object sender, EventArgs e)
        {
            

        }

        /// <summary>
        /// 确认网格已经检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItem12_Click(object sender, EventArgs e)
        {
            if (lstGrids.SelectedItems != null)
            {
                if (lstGrids.SelectedItems.Count > 0)
                {
                    TaskManager tm = TaskManager.GetInstance();
                    AppManager am = AppManager.GetInstance();
                    string gridcode = lstGrids.SelectedItems[0].SubItems[0].Text;
                    tm.SetGridPassed(am.TaskName, gridcode);
                    this.SetUpdateGridList(am.TaskName);
                }
            }
            
            ////设为已查，更改Passed字段值为1，刷新ItemPanel1、2
            //IFeatureLayer iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(0) as IFeatureLayer;
            //IFeatureSelection CurrentSelection = iCheckLayer as IFeatureSelection;
            //if (CurrentSelection.SelectionSet.Count > 0) return;
            //ISelection iSelection;
            //iSelection = m_MapCtrls.ActiveView.FocusMap.FeatureSelection;
            
            ////CurrentSelection = m_MapCtrls.ActiveView.FocusMap.FeatureSelection as IFeatureSelection;
            
            //IEnumFeature features = iSelection as IEnumFeature;
            
            //if (!(m_EditWorkspace.IsBeingEdited())) m_EditWorkspace.StartEditing(false);
            //IFeature feature = features.Next();
            //if (feature == null) return;
            //while(feature != null)
            //{               
            //    m_EditWorkspace.StartEditOperation();
            //    CheckFeatueEditor.Check(feature, m_GlobalWorkspace,AppManager.GetInstance().TaskName);
            //    //CheckFeatueEditor.Check(feature, m_GlobalWorkspace);
            //    m_EditWorkspace.StopEditOperation();

            //    feature = features.Next();
            //}
           
            //if (m_EditWorkspace.IsBeingEdited()) m_EditWorkspace.StopEditing(true);        

            //MessageBox.Show("设置成功！");

            //InitialItemPanel();

            //if(this.itemPanel1.Items.Count == 3) this.buttonItem12.Enabled = false;
            
        }

        private void itemPanel2_ItemClick(object sender, EventArgs e)
        {

        }

        private void itemPanel2_ItemDoubleClick(object sender, MouseEventArgs e)
        {
            //若是左建双击缩放至要素：
            if (e.Clicks == 2)
            {
                ButtonItem item = new ButtonItem();
                item = sender as ButtonItem;
                if (item == null) return;
                string FeatureClassName = item.Tooltip.Substring(item.Tooltip.IndexOf('.') + 1);
                string strOID = item.Tooltip.Substring(0, item.Tooltip.IndexOf('.'));
                int OID = Convert.ToInt32(strOID);

                IFeature feature = DBOperator.getFeatureFrom(OID, FeatureClassName, m_GlobalWorkspace);
                IGeometry5 geometry = feature.Shape as IGeometry5;
                this.m_MapCtrls.CenterAt(geometry.CentroidEx);
                this.m_MapCtrls.ActiveView.Extent = geometry.Envelope;
                ILayer iLayer = null;

                if(FeatureClassName.Contains("CheckItemPtn"))
                {
                    iLayer = m_MapCtrls.Map.Layer[1];
                }
                else if(FeatureClassName.Contains("CheckItemPtn"))
                {
                    iLayer = m_MapCtrls.Map.Layer[2];
                }
                else
                {
                    iLayer = m_MapCtrls.Map.Layer[3];
                }

                clearFeatureSelection();

                m_MapCtrls.Map.SelectFeature(iLayer, feature);
            }
        }

        private void itemPanel1_ItemRemoved(object sender, ItemRemovedEventArgs e)
        {

        }

        private void superTabControl2_SelectedTabChanged(object sender, SuperTabStripSelectedTabChangedEventArgs e)
        {

        }

        private void buttonItem3_Click(object sender, EventArgs e)
        {
            //点击identify工具：
            m_sketchshape = "identify";
            m_NewPolygonFeedback = null;
            ESRI.ArcGIS.SystemUI.ICommand panCommand = new ControlsMapIdentifyToolClass();
            panCommand.OnCreate(m_MapCtrls.Object);
            m_MapCtrls.CurrentTool = panCommand as ITool;
        }

        private void buttonItem19_Click(object sender, EventArgs e)
        {
            m_sketchshape = "rectangle";
            m_NewPolygonFeedback = null;
            m_RecFeedback = null;
            m_MapCtrls.CurrentTool = null;
            m_MapCtrls.ActiveView.Refresh();
        }

        public void clearFeatureSelection() 
        {
            //清除选择要素：
            IFeatureLayer iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(0) as IFeatureLayer;
            IFeatureSelection CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(3) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(2) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();

            iCheckLayer = m_MapCtrls.ActiveView.FocusMap.get_Layer(1) as IFeatureLayer;
            CurrentSelection = iCheckLayer as IFeatureSelection;
            CurrentSelection.Clear();


            m_MapCtrls.ActiveView.Refresh();
        }

        private void btn_export_Click(object sender, EventArgs e)
        {

        }

        private void buttonItem4_Click(object sender, EventArgs e)
        {
            MapCatalogRelateForm frm = new MapCatalogRelateForm(Properties.Settings.Default.Grid, Properties.Settings.Default.GridCodeField);
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
            {
                Properties.Settings.Default.Grid = frm.getFeatureClassName();
                Properties.Settings.Default.GridCodeField = frm.getCodeFieldName();
            } 
        }

        private void itemPanel3_ItemDoubleClick(object sender, MouseEventArgs e)
        {
            //双击，且不是点击非button item;
            if (e.Clicks == 2) 
            {
                ButtonItem item = new ButtonItem();
                item = sender as ButtonItem;
                if (item == null) return;
                IFeature feature = DBOperator.getFeatureFrom(item.Name, Properties.Settings.Default.Grid, Properties.Settings.Default.GridCodeField, m_GlobalWorkspace);
                if (feature == null) return;
                IGeometry5 geometry = feature.Shape as IGeometry5;
                this.m_MapCtrls.CenterAt(geometry.CentroidEx);
                this.m_MapCtrls.ActiveView.Extent = geometry.Envelope;
                
            }

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string mappath = Properties.Settings.Default.strMxdPath;
            bool isMap = this.axMapControl1.CheckMxFile(mappath);
            if (isMap)
            {
                this.axMapControl1.LoadMxFile(mappath);
            }
            AppManager am = AppManager.GetInstance();
            am.CurrentVersion = Util.ServerWorkspace as IVersion;
            m_GlobalWorkspace = Util.ServerWorkspace;
            m_EditWorkspace = m_GlobalWorkspace as IWorkspaceEdit2;
        }

        private void SetUpdateGridList(string taskName,string code="")
        {
            this.lstGrids.Items.Clear();
            TaskManager tm = TaskManager.GetInstance();
            string codeFieldName = Properties.Settings.Default.GridCodeField;
            List<string[]> infos = tm.GetUpdateGridsInfoList(taskName,code);
            foreach (var info in infos)
            {
                ListViewItem item = new ListViewItem(info);
                this.lstGrids.Items.Add(item);
            }
            this.axMapControl1.Refresh(esriViewDrawPhase.esriViewGeography, null, null);
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
                    this.axMapControl1.Extent = buffer.Envelope;

                    IGraphicsContainer gcon = this.axMapControl1.ActiveView.GraphicsContainer;
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

                        m_GridLocation = new PolygonElementClass();
                        IFillShapeElement pelem = m_GridLocation as IFillShapeElement;
                        pelem.Symbol = sfsym as IFillSymbol;
                        gcon.AddElement(m_GridLocation, 0);
                    }
                    m_GridLocation.Geometry = location as IGeometry;

                    gcon.UpdateElement(m_GridLocation);
                }
            }
        }

        private void buttonItem8_Click_1(object sender, EventArgs e)
        {
            //弹出保存shp框：
            ExportSHPDlg dlg = new ExportSHPDlg();
            if (dlg.getDLG().ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string strSHPPath = dlg.getDLG().FileName;
                bool res = CheckFeatueEditor.ExportSHP(strSHPPath, m_GlobalWorkspace);
                if (res == true) MessageBox.Show("导出成功！");
                else MessageBox.Show("导出失败！");
            }
        }

        /// <summary>
        /// 提交任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItem2_Click(object sender, EventArgs e)
        {
            CommentForm frm = new CommentForm();
            frm.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetUpdateGridList(AppManager.GetInstance().TaskName,this.tbxGridCode.Text);
        }



    }
}