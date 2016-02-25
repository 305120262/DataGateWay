using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DataReviewer
{
    class ExportSHPDlg
    {
        SaveFileDialog m_dlg = null;

        public ExportSHPDlg() 
        {
            m_dlg = new SaveFileDialog();
            m_dlg.AddExtension = true;
            m_dlg.DefaultExt = "shp";
            m_dlg.Filter = "|*.shp";
            m_dlg.Title = "导出为ShapeFile文件..";            
        }

        public SaveFileDialog getDLG()
        {
            return m_dlg;
        }
    }
}
