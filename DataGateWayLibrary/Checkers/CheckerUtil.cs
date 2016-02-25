using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckerUtil
    {
        static public List<int> m_idValues = null;
        /// <summary>
        /// 检查图层中的重叠的点，线和面要素
        /// 检查图层中的要素相交（如：线/线，面/面）
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="SRDesc"></param>
        /// <returns>返回错误信息数组</returns>
        static public ArrayList CheckOverlapFeaturesInLayer(string strLayerName, string strSQL, esriSpatialRelEnum eSREnum, string SRDesc, string errorMessage)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            
            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            if (pFeatCursor == null)
            {
                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                IFeatureCursor pFCursor = pFeatClass.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        if (pOverlayerFeature.OID != pFeature.OID)
                        {
                            List<IGeometry> pGeoList = new List<IGeometry>();
                            pGeoList.Add(pFeature.Shape);
                            pGeoList.Add(pOverlayerFeature.Shape);
                            string strMsg = "图层名称：" + strLayerName + ", 当前要素ID号为： '" + pFeature.OID.ToString() + "' ,  与要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！" + errorMessage;
                            errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                        }

                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查图层中的冗余线（相同编码/不同编码）
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="fieldName">编码字段名称</param>
        /// <param name="bEqual">编码是否相同</param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <returns></returns>
        static public ArrayList CheckRedundantFeaturesInLayer(string strLayerName, string strLayerName2, string fieldName, string strSQL, string strSQL2, bool bEqual, esriSpatialRelEnum eSREnum, string SRDesc)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            
            //set query filter            
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            if (pFeatCursor == null)
            {
                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }           

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                int fieldIndex = pFeature.Fields.FindField(fieldName);
                if (fieldIndex == -1)
                {
                    string strMsg = "没能找到值域字段，请检查输入的值域字段名称是否正确";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                    return errorMsgList;
                }
                object fieldValue = pFeature.get_Value(fieldIndex);
                IGeometry pGeo = pFeature.Shape;

                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    
                    while (pOverlayerFeature != null)
                    {
                        int iIndex = pOverlayerFeature.Fields.FindField(fieldName);
                        object tmpValue = pOverlayerFeature.get_Value(iIndex);
                        if (bEqual)
                        {
                            if (pOverlayerFeature.OID != pFeature.OID && tmpValue.Equals(fieldValue))
                            {
                                List<IGeometry> pGeoList = new List<IGeometry>();
                                pGeoList.Add(pFeature.Shape);
                                pGeoList.Add(pOverlayerFeature.Shape);
                                string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！错误描述：相同编码的要素存在冗余重叠部分";
                                errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                            }
                        }
                        else
                        {
                            if (pOverlayerFeature.OID != pFeature.OID && !tmpValue.Equals(fieldValue))
                            {
                                List<IGeometry> pGeoList = new List<IGeometry>();
                                pGeoList.Add(pFeature.Shape);
                                pGeoList.Add(pOverlayerFeature.Shape);
                                string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！错误描述：不同编码的要素存在冗余重叠部分";
                                errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                            }
                        }

                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查点/线，点/面，线/线，线/面，面/面几何关系（如：点在面内，点在线上，线与面相交，面与面叠加）
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="nType"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <param name="errorMsg"></param>
        /// <returns>返回错误信息数组</returns>
        static public ArrayList CheckOverlapFeaturesInLayers(string strLayerName, string strLayerName2, string strSQL, string strSQL2, int nType, esriSpatialRelEnum eSREnum, string SRDesc, string errorMsg)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            
            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }             

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                bool bRelation = false;
                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {                    
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        if((pOverlayerFeature.Class.AliasName != pFeature.Class.AliasName) || (pOverlayerFeature.OID != pFeature.OID))
                            bRelation = true;

                        if (nType == 22 || nType==34 || nType == 23 )
                        {//22:线线相交,34:面叠盖相交,23:线穿越面
                            List<IGeometry> pGeoList = new List<IGeometry>();
                            pGeoList.Add(pFeature.Shape);
                            pGeoList.Add(pOverlayerFeature.Shape);
                            string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！" + errorMsg;
                            errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                        }

                        if (bRelation)
                            break;

                        pOverlayerFeature = pFCursor.NextFeature();
                    }

                    if (nType == 13 || nType == 33 || nType == 24 || nType == 32)
                    {//13：点与面,33：面1是否在面2内,24：线是否在面边界上,32:面的边界是否被线压盖
                        if (!bRelation)
                        {
                            List<IGeometry> pGeoList = new List<IGeometry>();
                            pGeoList.Add(pFeature.Shape);
                            //pGeoList.Add(pOverlayerFeature.Shape);
                            string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "', 存在错误！" + errorMsg;
                            errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                        }
                    }                    
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查图层中的碎线/面
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strType"></param>
        /// <param name="dValue"></param>
        /// <returns>返回错误信息数组</returns>
        static public ArrayList CheckBrokenFeaturesInLayer(string strLayerName, string strSQL, string strType, double dValue = 0.1)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;
            
            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();
                pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }
            if (pFeatCursor == null)
            {
                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            
            IField pField = null;
            string typeMsg = "";
            if (strType.ToLower() == "line")
            {
                pField = pFeatClass.LengthField;
                typeMsg = "碎线";
            }
            else
            {
                pField = pFeatClass.AreaField;
                typeMsg = "碎面";
            }
            if (pField == null)
            {
                string strMsg = "图层中没有对应的字段，请检查图层字段是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                int iIndex = pFeature.Fields.FindField(pField.Name);
                object fieldValue = pFeature.get_Value(iIndex);
                double dFieldValue = (double)fieldValue;
                if (dFieldValue <= dValue)
                {
                    List<IGeometry> pGeoList = new List<IGeometry>();
                    pGeoList.Add(pFeature.Shape);
                    
                    string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素为" + typeMsg;
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查图层中自相交的要素
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <returns></returns>
        static public ArrayList CheckSelfIntersectFeaturesInLayer(string strLayerName, string strSQL)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }
            if (pFeatCursor == null)
            {
                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
                        
            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.ShapeCopy;
                
                ITopologicalOperator2 pTopOper = (ITopologicalOperator2)pGeo;
                pTopOper.IsKnownSimple_2 = false;
                bool bol = pTopOper.IsSimple;
                bol = pTopOper.IsSimple;

                if (!bol)
                {
                    List<IGeometry> pGeoList = new List<IGeometry>();
                    pGeoList.Add(pFeature.Shape);

                    string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素为自相交";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 折返线检查(当两段线间夹角小于给定值)
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="dAngle"></param>
        /// <returns></returns>
        static public ArrayList CheckLineAngle(string strLayerName, string strSQL, double dAngle = 0.0)
        {
            ArrayList errorMsgList = new ArrayList();
            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }
            if (pFeatCursor == null)
            {
                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }  

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.ShapeCopy;
                ISegmentCollection pSegColl = pGeo as ISegmentCollection;
                int nCount = pSegColl.SegmentCount;
                for (int i = 0; i < nCount-1;i++ )
                {
                    ISegment pSeg = pSegColl.get_Segment(i);
                    ISegment pSeg2 = pSegColl.get_Segment(i + 1);

                    ILine pLine = pSeg as ILine;                    
                    ILine pLine2 = pSeg2 as ILine;                   
                    //计算两线段之间的夹角
                    double dVtxAng = CalcAngleBetweenLines(pLine, pLine2, true);
                    
                    if (dVtxAng <= dAngle)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素为折返线";
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });

                        break;
                    }
                }                

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 面闭合检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        static public ArrayList CheckClosedFeaturesInLayer(string strLayerName, string strSQL)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            if (pFeatClass.ShapeType != esriGeometryType.esriGeometryPolygon && pFeatClass.ShapeType != esriGeometryType.esriGeometryRing
                && pFeatClass.ShapeType != esriGeometryType.esriGeometryEnvelope)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();
                pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }           

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {                
                IGeometry pGeo = pFeature.ShapeCopy;
                IPolygon pPg = pGeo as IPolygon;
                                                
                if (!pPg.IsClosed)
                {
                    List<IGeometry> pGeoList = new List<IGeometry>();
                    pGeoList.Add(pFeature.Shape);

                    string strMsg = "图层名称：" + strLayerName + "，ID号：'" + pFeature.OID.ToString() + "' 的要素为未闭合面！";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 复合线节点间距检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strSQL"></param>
        /// <param name="dLenght">指定值</param>
        /// <returns></returns>
        static public ArrayList CheckCompositeNodesDistance(string strLayerName, string strSQL, double dLenght=0.1)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
           
            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }              

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.ShapeCopy;

                ISegmentCollection pSegColl = pGeo as ISegmentCollection;
                int nCount = pSegColl.SegmentCount;
                for (int i = 0; i < nCount; i++)
                {
                    ISegment pSeg = pSegColl.get_Segment(i);
                    
                    ILine pLine = pSeg as ILine;
                    double dLen = pLine.Length;

                    if (dLen <= dLenght)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素，存在错误：复合线节点间距小于指定值！";
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });

                        break;
                    }
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查面的构成是顺时针还是逆时针
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        static public ArrayList CheckPolygonIsClockwise(string strLayerName, string strSQL, bool bClockwise)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.ShapeCopy;
                IPointCollection pts = pGeo as IPointCollection;

                //根据面的坐标顺序判断是否顺时针还是逆时针
                bool bCW = isClockwise(pts);
                
                if (bClockwise)
                {
                    if (!bCW)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素为逆时针构成的面！";
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }
                }
                else
                {
                    if (bCW)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 的要素为顺时针构成的面！";
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查点是否在线上（在线端点上还是在线边上）
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="bOnEndPoint"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        static public ArrayList CheckPointsIsOnLines(string strLayerName, string strLayerName2, string strSQL, string strSQL2, bool bOnEndPoint, esriSpatialRelEnum eSREnum, string SRDesc, string errorMsg)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }
                        
            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                IPoint pt = pGeo as IPoint;

                bool bRelation = false;
                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        IPointCollection pts = pOverlayerFeature.Shape as IPointCollection;
                        if (bOnEndPoint)
                        {                            
                            bRelation = pointIsOnPointCollection(pt, pts, bOnEndPoint);
                        }
                        else
                            bRelation = true;

                        if (bRelation)
                            break;
                        
                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                    if (!bRelation)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);
                        //pGeoList.Add(pOverlayerFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "', 存在错误！" + errorMsg;
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }                    
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 检查线上是否有点（线端点上是否有点）
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        static public ArrayList CheckLinesHasPoints(string strLayerName, string strLayerName2, string strSQL, string strSQL2, bool bOnEndPoint, esriSpatialRelEnum eSREnum, string SRDesc, string errorMsg)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                IPointCollection pts = pGeo as IPointCollection;

                bool bRelation = false;
                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        IPoint pt = pOverlayerFeature.Shape as IPoint;
                        bRelation = pointIsOnPointCollection(pt, pts, bOnEndPoint);
                        if (bRelation)
                            break;
                        
                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                    if (!bRelation)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pFeature.Shape);

                        string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "', 存在错误！" + errorMsg;
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 线上重复点检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strSQL"></param>
        /// <returns></returns>
        static public ArrayList CheckLineOverlapPointsInLayer(string strLayerName, string strSQL, string strErrorMsg, double dDistance=0.0001)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            if (pFeatClass == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }                 

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.ShapeCopy;
                IPointCollection pts = pGeo as IPointCollection;

                bool bOverlap = false;
                for (int i = 0; i < pts.PointCount - 1; i++)
                {
                    IPoint pt1 = pts.get_Point(i);
                    IPoint pt2 = pts.get_Point(i + 1);
                    double dX = Math.Abs(pt1.X - pt2.X);
                    double dY = Math.Abs(pt1.Y - pt2.Y);

                    if ((dX < dDistance && dY < dDistance))
                    {
                        bOverlap = true;
                        break;
                    }                    
                }

                if (bOverlap)
                {
                    List<IGeometry> pGeoList = new List<IGeometry>();
                    pGeoList.Add(pFeature.Shape);

                    string strMsg = "图层名称：" + strLayerName + "，要素ID号：'" + pFeature.OID.ToString() + "' 存在错误！ " + strErrorMsg;
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                }

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 线伪结点检查/面悬挂检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        static public ArrayList CheckPseudoNodesLines(string strLayerName, string strLayerName2, string strSQL, string strSQL2, esriSpatialRelEnum eSREnum, string SRDesc, string errorMsg)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                IPointCollection pts = pGeo as IPointCollection;

                bool bRelation = false;
                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        IPointCollection pts2 = pOverlayerFeature.Shape as IPointCollection;
                        for (int i = 0; i < pts2.PointCount; i++)
                        {
                            IPoint pt = pts2.get_Point(i);
                            bRelation = pointIsOnPointCollection(pt, pts, false);
                            if (bRelation)
                                break;
                        }
                        if (bRelation)
                        {
                            List<IGeometry> pGeoList = new List<IGeometry>();
                            pGeoList.Add(pFeature.Shape);
                            pGeoList.Add(pOverlayerFeature.Shape);

                            string strMsg = "图层名称：" + strLayerName + ",要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！ " + errorMsg;
                            errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                        }

                        pOverlayerFeature = pFCursor.NextFeature();
                    }                    
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 面悬挂检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        static public ArrayList CheckSuspensionNodesOnPolygon(string strLayerName, string strLayerName2, string strSQL, string strSQL2, esriSpatialRelEnum eSREnum, string SRDesc, string errorMsg)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }

            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;

                IPointCollection pts = pGeo as IPointCollection;
                ISegmentCollection pSegColl = pGeo as ISegmentCollection;
                IRelationalOperator relationalOperator = pSegColl as IRelationalOperator;
                                
                //要素对象之间的空间关系判断查询
                ISpatialFilter pSF = SetSpatialRelation(pGeo, eSREnum, SRDesc);
                IQueryFilter pQF = pSF;
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        IGeometry pGeo2 = pOverlayerFeature.Shape;
                        IPointCollection pts2 = pGeo2 as IPointCollection;
                        ISegmentCollection pSegColl2 = pGeo2 as ISegmentCollection;
                        IRelationalOperator relationalOperator2 = pSegColl2 as IRelationalOperator;

                        if (relationalOperator != null)
                        {
                            int nCount = 0;
                            bool bPoints = false;
                            for (int i = 0; i < pts2.PointCount - 1; i++)
                            {
                                IPoint pt = pts2.get_Point(i);
                                //判断面上节点是否重叠在一起
                                bPoints = pointIsOnPointCollection(pt, pts, false);
                                if (bPoints)
                                    break;
                                //判断点是否在面上
                                bool bRelOp = relationalOperator.Touches(pt);
                                if (bRelOp)
                                {
                                    nCount++;
                                }
                            }

                            if ((nCount == 1) && !bPoints )
                            {
                                List<IGeometry> pGeoList = new List<IGeometry>();
                                pGeoList.Add(pFeature.Shape);
                                pGeoList.Add(pOverlayerFeature.Shape);

                                string strMsg = "图层名称：" + strLayerName + ", 要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！ " + errorMsg;
                                errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                            }
                        }

                        if (relationalOperator2 != null)
                        {
                            int nCount = 0;
                            bool bPoints = false;
                            for (int j = 0; j < pts.PointCount - 1; j++)
                            {
                                IPoint pt = pts.get_Point(j);
                                //判断面上节点是否重叠在一起
                                bPoints = pointIsOnPointCollection(pt, pts2, false);
                                if (bPoints)
                                    break;

                                //判断点是否在面上
                                bool bRelOp = relationalOperator2.Touches(pt);
                                if (bRelOp)
                                {
                                    nCount++;
                                }
                            }

                            if ((nCount == 1) && !bPoints)
                            {
                                List<IGeometry> pGeoList = new List<IGeometry>();
                                pGeoList.Add(pFeature.Shape);
                                pGeoList.Add(pOverlayerFeature.Shape);

                                string strMsg = "图层名称：" + strLayerName + ", 要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！ " + errorMsg;
                                errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                            }
                        }
                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        /// <summary>
        /// 面缝隙检查
        /// </summary>
        /// <param name="strLayerName"></param>
        /// <param name="strLayerName2"></param>
        /// <param name="strSQL"></param>
        /// <param name="strSQL2"></param>
        /// <param name="errorMsg"></param>
        /// <param name="dGap"></param>
        /// <returns></returns>
        static public ArrayList CheckPolygonsHasGap(string strLayerName, string strLayerName2, string strSQL, string strSQL2, string errorMsg, double dGap=0.01)
        {
            ArrayList errorMsgList = new ArrayList();

            IFeatureClass pFeatClass = GetFeatureClassFromWorkspace(strLayerName);
            IFeatureClass pFeatClass2 = GetFeatureClassFromWorkspace(strLayerName2);
            if (pFeatClass == null || pFeatClass2 == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确！";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            //set query filter
            //IQueryFilter pQueryFilter = new QueryFilter();
            //pQueryFilter.WhereClause = strSQL;

            IFeatureCursor pFeatCursor;
            if (m_idValues == null)
            {
                pFeatCursor = pFeatClass.Search(null, false);
            }
            else
            {
                int[] idValues = m_idValues.ToArray();
                IGeoDatabaseBridge geodatabaseBridge = new GeoDatabaseHelperClass();pFeatCursor = geodatabaseBridge.GetFeatures(pFeatClass,ref idValues, false);
            }
                        
            IFeature pFeature = pFeatCursor.NextFeature();
            if (pFeature == null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
                System.GC.Collect();

                string strMsg = "没有需要检测的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            while (pFeature != null)
            {
                IGeometry pGeo = pFeature.Shape;
                ISegmentCollection pSegColl = pGeo as ISegmentCollection;
                IRelationalOperator relationalOperator = pSegColl as IRelationalOperator;
                                
                bool bRelation = false;
                
                //要素对象之间的空间关系判断查询             
                IQueryFilter pQF = new QueryFilter();
                pQF.WhereClause = strSQL2;
                IFeatureCursor pFCursor = pFeatClass2.Search(pQF, false);

                if (pFCursor != null)
                {
                    IFeature pOverlayerFeature = pFCursor.NextFeature();
                    while (pOverlayerFeature != null)
                    {
                        IGeometry pGeo2 = pOverlayerFeature.Shape;
                        bool bOverlaps = relationalOperator.Overlaps(pGeo2);
                        if (bOverlaps)
                        {//要素之间有重叠部分
                            ITopologicalOperator2 pTopOper = (ITopologicalOperator2)pGeo;                            
                            IGeometry tmpGeo2 = pTopOper.Union(pGeo2);
                            List<IGeometryCollection> exteriorRingGeoList = null;
                            List<IGeometryCollection> interiorRingGeoList = null;
                            //获取多边形外环和内环部分几何
                            GetPolygonRings(tmpGeo2, out exteriorRingGeoList, out interiorRingGeoList);
                            if (interiorRingGeoList != null && interiorRingGeoList.Count > 0)
                            {
                                for (int i = 0; i < interiorRingGeoList.Count; i++)
                                {
                                    IGeometryCollection interiorRingGeometryCollection = interiorRingGeoList[i];
                                    for (int j = 0; j < interiorRingGeometryCollection.GeometryCount; j++)
                                    {//计算内环面积
                                        IGeometry interRingGeo = interiorRingGeometryCollection.get_Geometry(j);
                                        IArea pArea = interRingGeo as IArea;
                                        if (Math.Abs(pArea.Area) > dGap)
                                        {
                                            bRelation = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        
                            if (bRelation)
                            {
                                List<IGeometry> pGeoList = new List<IGeometry>();
                                pGeoList.Add(pFeature.Shape);
                                pGeoList.Add(pOverlayerFeature.Shape);

                                string strMsg = "图层名称：" + strLayerName + ", 要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！ " + errorMsg;
                                errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                            }
                        }
                        else
                        {
                            bool bDisjoint = relationalOperator.Disjoint(pGeo2);
                            if (bDisjoint)
                            {//要素之间相离，不存在重叠部分
                                ITopologicalOperator2 pTopOper = (ITopologicalOperator2)pGeo;
                                IGeometry tmpGeo = pTopOper.Buffer(dGap);
                                
                                ISegmentCollection pSegColl2 = tmpGeo as ISegmentCollection;
                                IRelationalOperator relationalOperator2 = pSegColl2 as IRelationalOperator;
                                bool bOverlaps2 = relationalOperator2.Overlaps(pGeo2);
                                if (bOverlaps2)
                                {
                                    List<IGeometry> pGeoList = new List<IGeometry>();
                                    pGeoList.Add(pFeature.Shape);
                                    pGeoList.Add(pOverlayerFeature.Shape);

                                    string strMsg = "图层名称：" + strLayerName + ", 要素ID号为： '" + pFeature.OID.ToString() + "' 与 图层名称：" + strLayerName2 + ", 要素ID号为： '" + pOverlayerFeature.OID.ToString() + "' 存在错误！ " + errorMsg;
                                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                                }
                            }
                        }
                        
                        pOverlayerFeature = pFCursor.NextFeature();
                    }
                }

                System.Runtime.InteropServices.Marshal.ReleaseComObject(pFCursor);
                System.GC.Collect();       

                pFeature = pFeatCursor.NextFeature();
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pFeatCursor);
            System.GC.Collect();

            return errorMsgList;
        }

        #region "私有方法"

        /// <summary>
        /// 从数据源中获取图层数据
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        static public IFeatureClass GetFeatureClassFromWorkspace(string layerName)
        {
            try
            {
                IFeatureWorkspace fws = CheckerManager.CheckingWorkspace as IFeatureWorkspace;
                return fws.OpenFeatureClass(layerName);
            }
            catch
            {
                return null;
            }
        }

        static public BaseCheckerManager CheckerManager;

        /// <summary>
        /// 设置空间关系
        /// </summary>
        /// <param name="pGeo"></param>
        /// <param name="eSREnum"></param>
        /// <param name="SRDesc"></param>
        /// <returns></returns>
        static private ISpatialFilter SetSpatialRelation(IGeometry pGeo, esriSpatialRelEnum eSREnum, string SRDesc)
        {
            ISpatialFilter pSF = new SpatialFilter();
            pSF.Geometry = pGeo;
            pSF.SpatialRel = eSREnum;
            pSF.SpatialRelDescription = SRDesc;

            return pSF;
        }
        /// <summary>
        /// 计算两线段之间的夹角
        /// </summary>
        /// <param name="pLine"></param>
        /// <param name="pLine2"></param>
        /// <param name="bAsDegrees">是否转为度</param>
        /// <returns></returns>
        static private double CalcAngleBetweenLines(ILine pLine, ILine pLine2, bool bAsDegrees)
        {
            pLine.ReverseOrientation();
            double dLAngle = pLine.Angle;
            if (dLAngle < 0)
                dLAngle = 2 * Math.PI + dLAngle;
            pLine.ReverseOrientation();
            
            double dLAngle2 = pLine2.Angle;
            if (dLAngle2 < 0)
                dLAngle2 = 2 * Math.PI + dLAngle2;
            double dVtxAng = dLAngle - dLAngle2;
            if (dVtxAng < 0)
                dVtxAng = 2 * Math.PI + dVtxAng;

            if (bAsDegrees)
            {
                dVtxAng = dVtxAng * 180 / Math.PI;
                if (dVtxAng > 180)
                    dVtxAng = 360 - dVtxAng;
            }

            return dVtxAng;
        }

        /// <summary>
        /// 判断点集的顺序是否按照顺时针方向构建
        /// </summary>
        /// <param name="pts"></param>
        /// <returns>true:为顺时针；false:逆时针</returns>
        static private bool isClockwise(IPointCollection pts)
        {
            if (pts == null || pts.PointCount == 0)
                return false;

            double dArea = 0.0;
            for (int i = 0; i < pts.PointCount -1;i++ )
            {
                IPoint pt1 = pts.get_Point(i);
                IPoint pt2 = pts.get_Point(i+1);
                double tmpArea = ((pt2.X - pt1.X) * (pt2.Y + pt1.Y)) / 2;
                dArea = dArea + tmpArea;
            }

            return dArea > 0;
        }

        /// <summary>
        /// 判断点是否在点集合中
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="pts"></param>
        /// <param name="bOnEndPoint">是否在端点上</param>
        /// <returns></returns>
        static private bool pointIsOnPointCollection(IPoint pt, IPointCollection pts, bool bOnEndPoint,double dDistance=0.0001)
        {
            bool bOnPoints = false;

            int nCount = pts.PointCount;
            if (bOnEndPoint)
            {
                IPoint startPoint = pts.get_Point(0);
                IPoint endPoint = pts.get_Point(nCount - 1);

                double dX = Math.Abs(pt.X - startPoint.X);
                double dY = Math.Abs(pt.Y - startPoint.Y);
                double dX2 = Math.Abs(pt.X - endPoint.X);
                double dY2 = Math.Abs(pt.Y - endPoint.Y);

                if ((dX < dDistance && dY < dDistance) || (dX2 < dDistance && dY2 < dDistance))
                {
                    bOnPoints = true;
                }
                else
                    bOnPoints = false;
            }
            else
            {
                for (int i = 0; i < nCount; i++)
                {
                    IPoint tmpPoint = pts.get_Point(i);
                    double dX = Math.Abs(pt.X - tmpPoint.X);
                    double dY = Math.Abs(pt.Y - tmpPoint.Y);

                    if ((dX < dDistance && dY < dDistance))
                    {
                        bOnPoints = true;
                    }
                    else
                        bOnPoints = false;
                }
            }

            return bOnPoints;
        }

        /// <summary>
        /// 获取多边形的外环和内环
        /// </summary>
        /// <param name="pGeo"></param>
        /// <param name="exteriorRingGeoList"></param>
        /// <param name="interiorRingGeoList"></param>
        static private void GetPolygonRings(IGeometry pGeo, out List<IGeometryCollection> exteriorRingGeoList, out List<IGeometryCollection> interiorRingGeoList)
        {
            exteriorRingGeoList = new List<IGeometryCollection>();
            interiorRingGeoList = new List<IGeometryCollection>();

            IPolygon4 polygon = pGeo as IPolygon4;
            IGeometryBag exteriorRingGeometryBag = polygon.ExteriorRingBag;

            IGeometryCollection exteriorRingGeometryCollection = exteriorRingGeometryBag as IGeometryCollection;
            exteriorRingGeoList.Add(exteriorRingGeometryCollection);

            for (int i = 0; i < exteriorRingGeometryCollection.GeometryCount; i++)
            {
                IGeometry exteriorRingGeometry = exteriorRingGeometryCollection.get_Geometry(i);

                IPointCollection exteriorRingPointCollection = exteriorRingGeometry as IPointCollection;

                IGeometryBag interiorRingGeometryBag = polygon.get_InteriorRingBag(exteriorRingGeometry as IRing);

                IGeometryCollection interiorRingGeometryCollection = interiorRingGeometryBag as IGeometryCollection;
                interiorRingGeoList.Add(interiorRingGeometryCollection);
                //for (int k = 0; k < interiorRingGeometryCollection.GeometryCount; k++)
                //{
                //    IGeometry interiorRingGeometry = interiorRingGeometryCollection.get_Geometry(k);
                //}
            }
        }
        #endregion
    }
}
