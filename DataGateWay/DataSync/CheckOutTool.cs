using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Controls;
using System.Windows.Forms;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using DataGateWay.Utilities;

namespace DataGateWay.DataSync
{
    /// <summary>
    /// Summary description for CheckOutTool.
    /// </summary>
    [Guid("40aa888e-4d31-44e2-888e-4e6844ebe2da")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("DataGateWay.DataSync.CheckOutTool")]
    public sealed class CheckOutTool : BaseTool
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

        public CheckOutTool()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "DataGateWay"; //localizable text 
            base.m_caption = "CheckOutTool";  //localizable text 
            base.m_message = "This should work in ArcMap/MapControl/PageLayoutControl";  //localizable text
            base.m_toolTip = "CheckOutTool";  //localizable text
            base.m_name = "CheckOutTool";   //unique id, non-localizable (e.g. "MyCategory_MyTool")
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
        #region Member Field
        private IPolygon area;
        private bool isSketching = false;
        private INewPolygonFeedback fb;
        private object missing = Type.Missing;
        #endregion

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
            // TODO: Add CheckOutTool.OnClick implementation
            if (isSketching!=true)
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
            // TODO:  Add CheckOutTool.OnMouseDown implementation
            IPoint ptn = m_hookHelper.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
            if (this.isSketching!=true)
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
            IPoint ptn = m_hookHelper.ActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y);
            this.fb.MoveTo(ptn);
        }

        public override void OnDblClick()
        {
            area = fb.Stop();
            this.isSketching = false;
            CheckOutForm form = new CheckOutForm(CheckOutMode.Custom);
            form.CheckOutArea = area;
            form.Show();
        }

        #endregion
    }
}
