using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;

namespace DataReviewer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppManager am = AppManager.GetInstance();
            MainForm mf = new MainForm();
            am.AppForm = mf;
            Application.Run(am.AppForm);
            //Application.Run(new rectfeedback());
        }
    }
}
