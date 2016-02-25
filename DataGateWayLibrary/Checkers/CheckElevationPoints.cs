using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckElevationPoints : BaseChecker, IChecker
    {
        /// <summary>
        /// 高程点注记检查
        /// </summary>
        /// <param name="strParams">参数数组包含：高程点注记名称、高程点名称、高程点字段名称、检测容差</param>
        /// <returns>返回检查到的错误信息数组</returns>
        public bool CheckData()
        {
            try
            {
                if (strParams == null || strParams.Length < 4)
                {
                    Message = "高程点注记检查所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string ElevatAnnotName = strParams[0];
                string ElevatFeatName = strParams[1];
                string ElevatFieldName = strParams[2];
                string Tolerance = strParams[3];
                if (ElevatAnnotName == null || ElevatAnnotName == string.Empty || ElevatFeatName == null || ElevatFeatName == string.Empty)
                {
                    Message = "高程点注记检查所需图层名为空！请输入正确图层名参数！";
                    
                    return false;
                }
                if (ElevatFieldName == null || ElevatFieldName == string.Empty)
                {
                    Message = "高程点字段名称为空，请输入正确的高程点字段参数";
                    
                    return false;
                }
                if (Tolerance == null || Tolerance == string.Empty)
                {
                    Message = "检测容差没有设置，请输入容差参数";
                    
                    return false;
                }
                string strErrorMsg = "错误描述：高程点注记不匹配！";

                //检查图层是否有更新信息
                if (!Manager.IsCheckTaskData)
                {
                    CheckErrorList = CheckValue(ElevatAnnotName, ElevatFeatName, ElevatFieldName, Tolerance, strErrorMsg, null);
                }
                else
                {
                    List<int> idValueList = new List<int>();
                    bool bValue = Manager.CheckItems.TryGetValue(ElevatAnnotName, out idValueList);
                    if (!bValue || idValueList.Count == 0)
                    {
                        Message = "图层名称： " + ElevatAnnotName + "， 无检测数据！";

                        return false;
                    }
                    int[] idValues = idValueList.ToArray();
                    CheckErrorList = CheckValue(ElevatAnnotName, ElevatFeatName, ElevatFieldName, Tolerance, strErrorMsg, idValues);
                }
                return true;
            }
            catch (Exception err)
            {
                Message = "高程点注记检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }



        /// <summary>
        /// 检测高程点值是否标对
        /// </summary>
        /// <param name="ElevatFieldName">高程点字段名称</param>
        public ArrayList CheckValue(string ElevatAnnotName, string ElevatFeatName, string ElevatFieldName,string Tolerance,string errorMsg, int[]idValues)
        {
            double tol=0;
            ArrayList errorMsgList = new ArrayList(); //定义错误信息
            IFeatureClass AnnoFC = CheckerUtil.GetFeatureClassFromWorkspace(ElevatAnnotName);
            IFeatureClass ElevateFC = CheckerUtil.GetFeatureClassFromWorkspace(ElevatFeatName);
            if (AnnoFC == null || ElevateFC == null)
            {
                string strMsg = "没能在数据库中找到对应的高程点注记图层，请检查输入的图层名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return null;
            }
            int fieldIndex = ElevateFC.Fields.FindField(ElevatFieldName);
            if (fieldIndex == -1)
            {
                string strMsg = "没能找到高程点字段，请检查输入的高程点字段名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            try
            {
                tol = double.Parse(Tolerance);
            }
            catch{
                string strMsg = "输入的容差为非数值型，请检查输入的容差值是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return null;
            }
            IFeature TempElvAnnoFeat = null;
            IFeatureCursor pCursor;
            if (idValues == null)
            {
                pCursor = AnnoFC.Search(null, false);
            }
            else
            {
                pCursor = AnnoFC.GetFeatures(idValues, false);
            }

            //遍历所有的高程点注记
            TempElvAnnoFeat = pCursor.NextFeature();
            while (TempElvAnnoFeat != null)
            {
                //获取高程点注记的值
                string AnnoText = TempElvAnnoFeat.get_Value(TempElvAnnoFeat.Fields.FindField("TextString")).ToString();
                IPolygon annoPolygon = TempElvAnnoFeat.Shape as IPolygon;
                //缓冲一个范围作为查找
                ITopologicalOperator topo = annoPolygon as ITopologicalOperator;
                topo.Buffer(tol);
                //查找压盖的高程点
                ISpatialFilter spFilter = new SpatialFilterClass();
                spFilter.Geometry = topo as IPolygon ;
                spFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor ElevateCusor = ElevateFC.Search(spFilter, false);

                IFeature ElevatFeat = ElevateCusor.NextFeature();
                //当某一个注记没能找到对应的高程点的时候，将其没有对应的注记的ID记录下来（针对某些注记可能单独存在的情况，需确认这种情况下注记是否多余）
                if (ElevatFeat == null)
                {
                    string strMsg = "高程点注记：" + ElevatAnnotName + ",要素ID号为： '" + TempElvAnnoFeat.OID.ToString() + "'  未能找到任何的高程点与之对应";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                }
                while (ElevatFeat != null)
                {
                    //获取到高程点的值
                    string contValue = ElevatFeat.get_Value(ElevatFeat.Fields.FindField(ElevatFieldName)).ToString();
                    if (contValue != AnnoText)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(TempElvAnnoFeat.Shape);
                        pGeoList.Add(ElevatFeat.Shape);  

                        string strMsg = "高程点注记：" + ElevatAnnotName + ",要素ID号为： '" + TempElvAnnoFeat.OID.ToString() + "' 与 高程点名称：" + ElevatFeatName + ", 要素ID号为： '" + ElevatFeat.OID.ToString() + "'数值不一致  " + errorMsg;
                        //MessageBox.Show(strMsg);
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }


                    ElevatFeat = ElevateCusor.NextFeature();
                }

                TempElvAnnoFeat = pCursor.NextFeature();
            }

            return errorMsgList;
        }
    }
}
