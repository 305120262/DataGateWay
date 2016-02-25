using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Collections;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckContourAnnotation : BaseChecker, IChecker
    {
       
        /// <summary>
        /// 等高线注记检查
        /// </summary>
        /// <param name="strParams">参数数组包含：等高线注记名称、等高线名称、等高线字段名称</param>
        /// <returns>返回检查到的错误信息数组</returns>
        public bool CheckData()
        {
            try
            {
                if (strParams == null || strParams.Length < 3 )
                {
                    Message = "等高线注记检查所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string ContAnnotName = strParams[0];
                string ContFeatName = strParams[1];
                string ContFieldName = strParams[2];
                string strErrorMsg = "错误描述：等高线注记不匹配！";

                if (ContAnnotName == null || ContAnnotName == string.Empty || ContFeatName == null || ContFeatName == string.Empty)
                {
                    Message = "等高线注记检查所需图层名为空！请输入正确图层名参数！";
                    
                    return false;
                }
                if (ContFieldName == null || ContFieldName == string.Empty)
                {
                    Message = "等高线字段名称为空，请输入正确的等高线字段参数";
                    
                    return false;
                }

                //检查图层是否有更新信息
                if (!Manager.IsCheckTaskData)
                {
                    CheckErrorList = CheckValue(ContAnnotName, ContFeatName, ContFieldName, strErrorMsg, null);
                }
                else
                {
                    List<int> idValueList = new List<int>();
                    bool bValue = Manager.CheckItems.TryGetValue(ContAnnotName, out idValueList);
                    if (!bValue || idValueList.Count == 0)
                    {
                        Message = "图层名称： " + ContAnnotName + "， 无检测数据！";
                        return false;
                    }
                    int[] idValues = idValueList.ToArray();
                    CheckErrorList = CheckValue(ContAnnotName, ContFeatName, ContFieldName, strErrorMsg, idValues);

                }
                return true;
            }
            catch (Exception err)
            {
                Message = "等高线注记检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }



        /// <summary>
        /// 检测等高线值是否标对
        /// </summary>
        /// <param name="ContourFieldName">等高线字段名称</param>
        public ArrayList CheckValue(string ContAnnotName, string ContFeatName, string ContourFieldName, string errorMsg, int[] idValues)
        {
            ArrayList errorMsgList = new ArrayList(); //定义错误信息
            IFeatureClass AnnoFC =CheckerUtil.GetFeatureClassFromWorkspace(ContAnnotName);
            IFeatureClass ContourFC = CheckerUtil.GetFeatureClassFromWorkspace(ContFeatName);
            if (AnnoFC == null || ContourFC == null)
            {
                string strMsg = "没能在数据库中找到对应的等高线注记图层，请检查输入的图层名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            int fieldIndex = ContourFC.Fields.FindField(ContourFieldName);
            if (fieldIndex == -1)
            {
                string strMsg = "没能找到等高线字段，请检查输入的等高线字段名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            IFeature TempContAnnoFeat = null;
            IFeatureCursor pCursor;
            if (idValues == null)
            {
                pCursor = AnnoFC.Search(null, false);
            }
            else
            {
                pCursor = AnnoFC.GetFeatures(idValues, false);
            }
            //遍历所有的等高线注记
            TempContAnnoFeat = pCursor.NextFeature();
            while (TempContAnnoFeat != null)
            {
                //获取等高线注记的值
                string AnnoText = TempContAnnoFeat.get_Value(TempContAnnoFeat.Fields.FindField("TextString")).ToString();
                IPolygon annoPolygon = TempContAnnoFeat.Shape as IPolygon;
                //查找压盖的等高线
                ISpatialFilter spFilter = new SpatialFilterClass();
                spFilter.Geometry = annoPolygon;
                spFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor contCusor = ContourFC.Search(spFilter, false);
                
                IFeature contFeat = contCusor.NextFeature();
                //当某一个注记没能找到对应的等高线的时候，将其没有对应的注记的ID记录下来（针对某些注记可能单独存在的情况，需确认这种情况下注记是否多余）
                if (contFeat == null)
                {
                    string strMsg = "等高线注记：" + ContAnnotName + ",要素ID号为： '" + TempContAnnoFeat.OID.ToString() + "'  未能找到任何的等高线与之对应";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = null }); 
                }
                while (contFeat != null)
                {
                    //获取到等高线的值
                    string contValue =contFeat.get_Value(contFeat.Fields.FindField(ContourFieldName)).ToString();
                    if (contValue != AnnoText)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(TempContAnnoFeat.Shape);
                        pGeoList.Add(contFeat.Shape);

                        string strMsg = "等高线注记：" + ContAnnotName + ",要素ID号为： '" + TempContAnnoFeat.OID.ToString() + "' 与 等高线名称：" + ContFeatName + ", 要素ID号为： '" + contFeat.OID.ToString() + "'数值不一致  " + errorMsg;
                        //MessageBox.Show(strMsg);
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList }); 
                    }


                    contFeat = contCusor.NextFeature();
                }

                TempContAnnoFeat = pCursor.NextFeature();
            }
            
            return errorMsgList;
        }
    }
}
