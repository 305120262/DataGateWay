using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace DataGateWay.QC
{
    public class CheckError
    {
        private string m_Description;
        private List<IGeometry> m_Locations=new List<IGeometry>();

        public List<IGeometry> Locations
        {
            get { return m_Locations; }
            set { m_Locations = value; }
        }


        public override string ToString()
        {
            return m_Description;
        }

        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }
    }
}
