using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Carto;


namespace DataReviewer
{
    class MapDocOperator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pDocPath"></param>
        /// <returns></returns>
        static public IMapDocument openMapDoc(string pDocPath) 
        {
            IMapDocument mapDoc = new MapDocument();
            if (!mapDoc.get_IsPresent(pDocPath)) return null;
            if (!mapDoc.get_IsMapDocument(pDocPath)) return null;
            if (mapDoc.get_IsReadOnly(pDocPath)) return null;
            if (mapDoc.get_IsRestricted(pDocPath)) return null;
            else 
            {
                mapDoc.Open(pDocPath,"");
                return mapDoc;
            }
                
            
        }
    }
}
