

using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace DataCheckToolAuxiliary
{
    /// <summary>
    /// Summary description for Tool1.
    /// </summary>
    [Guid("a8e3d096-7c25-4d16-a9b3-663619d3cd68")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ProgId("DataCheckToolAuxiliary.Tool1")]
    public sealed class Tool1 : BaseTool
    {
        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);
            ControlsCommands.Register(regKey);
        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);
            ControlsCommands.Unregister(regKey);
        }

        #endregion
        #endregion

        private IHookHelper m_hookHelper = null;
        IMap pMap;
        IActiveView pActiveView;
        private IPolygon area;
        private bool isSketching = false;
        private INewPolygonFeedback fb;
        private object missing = Type.Missing;

        public Tool1()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = ""; //localizable text 
            base.m_caption = "多边形选择";  //localizable text 
            base.m_message = "This should work in ArcMap/MapControl/PageLayoutControl";  //localizable text
            base.m_toolTip = "多边形选择分割范围";  //localizable text
            base.m_name = "";   //unique id, non-localizable (e.g. "MyCategory_MyTool")
            try
            {
                //
                // TODO: change resource name if necessary
                //
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
                base.m_cursor = new System.Windows.Forms.Cursor(GetType(), GetType().Name + ".cur");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        #region Overridden Class Methods

        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            try
            {
                m_hookHelper = new HookHelperClass();
                m_hookHelper.Hook = hook;
                if (m_hookHelper.ActiveView == null)
                {
                    m_hookHelper = null;
                }
            }
            catch
            {
                m_hookHelper = null;
            }

            if (m_hookHelper == null)
                base.m_enabled = false;
            else
                base.m_enabled = true;

            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            pMap = m_hookHelper.FocusMap;
            pActiveView = m_hookHelper.ActiveView;
            pMap.ClearSelection();
            pActiveView.Refresh();

            // TODO: Add Tool1.OnClick implementation
            if (isSketching != true)
            {
                this.fb = new NewPolygonFeedbackClass();
                this.fb.Display = m_hookHelper.ActiveView.ScreenDisplay;

                ISimpleFillSymbol sym = new SimpleFillSymbolClass();
                IRgbColor color = new RgbColorClass();
                color.Red = 0;
                color.Green = 0;
                color.Blue = 255;
                ISimpleLineSymbol ln_sym = new SimpleLineSymbolClass();
                ln_sym.Color = color as IColor;
                ln_sym.Width = 2;
                sym.Outline = ln_sym as ILineSymbol;
                sym.Style = esriSimpleFillStyle.esriSFSNull;
                this.fb.Symbol = sym as ISymbol;
            }
            else
            {
                this.fb.Refresh(m_hookHelper.ActiveView.ScreenDisplay.hDC);
            }
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            IPoint ptn = m_hookHelper.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
            if (this.isSketching != true)
            {
                this.isSketching = true;
                this.fb.Start(ptn);
            }
            else
            {
                this.fb.AddPoint(ptn);
            }

        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add Tool1.OnMouseMove implementation
            IPoint ptn = m_hookHelper.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
            this.fb.MoveTo(ptn);
        }

        public override void OnDblClick()
        {
            area = fb.Stop();
            this.isSketching = false;

            DataCheckToolAuxiliary.Form1.SelectArea = area;
            DataCheckToolAuxiliary.Form1.pIsSelect = true;
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add Tool1.OnMouseUp implementation
        }
        #endregion
    }
}
