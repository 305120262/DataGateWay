using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckSphereValue : BaseChecker, IChecker
    {
        /// <summary>
        /// 检查是否符合值域范围
        /// </summary>
        /// <param name="strParams">参数对应为图层名称、要检查字段的名称、检测值范围（用-隔开）</param>
        /// <returns></returns>
        public bool CheckData()
        {
            try
            {
                string strErrorMsg = "错误描述：数值不在值域范围内！";
                
                if (strParams == null || strParams.Length < 3)
                {
                    Message = "要检查图层所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string featFCName = strParams[0];
                string FieldName = strParams[1];
                string StrSphere = strParams[2];

                if (featFCName == string.Empty || FieldName == string.Empty || StrSphere == string.Empty)
                {
                    Message = "要检查图层所需参数为空！请输入正确参数！";

                    return false;
                }

                //检查图层是否有更新信息
                if (!Manager.IsCheckTaskData)
                {
                    CheckErrorList = CheckValue(featFCName, FieldName, StrSphere, strErrorMsg, null);
                }
                else
                {
                    List<int> idValueList = new List<int>();
                    bool bValue = Manager.CheckItems.TryGetValue(featFCName, out idValueList);
                    if (!bValue || idValueList.Count == 0)
                    {
                        Message = "图层名称： " + featFCName + "， 无检测数据！";

                        return false;
                    }
                    int[] idValues = idValueList.ToArray();

                    CheckErrorList = CheckValue(featFCName, FieldName, StrSphere, strErrorMsg, idValues);
                }
                return true;
            }
            catch (Exception err)
            {
                Message = "（线）值域检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }


        public ArrayList CheckValue(string featFCName, string FieldName, string StrSphere, string errorMsg, int[] idValues)
        {
            
            ArrayList errorMsgList = new ArrayList(); //定义错误信息
            string[] Sphere = StrSphere.Split('-');
            string from = Sphere[0];
            string to = Sphere[1];

            double dMin = 0;
            double dMax = 0;
            try
            {
                dMin = double.Parse(from);
                dMax = double.Parse(to);
            }
            catch
            {
                string strMsg = "输入的范围为非数值型，范围输入请用'-'隔开，如‘10-20’。请检查输入的范围值是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            IFeatureClass FeatFC = CheckerUtil.GetFeatureClassFromWorkspace(featFCName);
            if (FeatFC == null)
            {
                string strMsg = "没能在数据库中找到对应的图层，请检查输入的图层名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            int fieldIndex = FeatFC.Fields.FindField(FieldName);
            if (fieldIndex == -1)
            {
                string strMsg = "没能找到值域字段，请检查输入的值域字段名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }
            //IFeatureCursor featCursor = FeatFC.Search(null, false);
            IFeatureCursor featCursor = FeatFC.GetFeatures(idValues, false);
            IFeature bFeat = featCursor.NextFeature();
            while (bFeat != null)
            {
                double fValue = (double)bFeat.get_Value(bFeat.Fields.FindField(FieldName));
                if (fValue > dMax || fValue < dMin)
                {
                    List<IGeometry> pGeoList = new List<IGeometry>();
                    pGeoList.Add(bFeat.Shape);                    

                    string strMsg = featFCName + "图层中,要素ID号为： '" + bFeat.OID.ToString() + "字段数值为 " + fValue.ToString() + " 不在设定的值域范围内 " + errorMsg;
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                }
                bFeat = featCursor.NextFeature();
            }                           
            return errorMsgList;
        }
    }
}
