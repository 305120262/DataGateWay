using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System.Text.RegularExpressions;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckBoundaryPointAnno : BaseChecker, IChecker
    {
        private string InputAnnoName;  //用作消息提示
        private string InputFCName;    //用作消息提示
        /// <summary>
        /// 检查注记与属性是否一致
        /// </summary>
        /// <param name="strParams">参数分别对应为注记要素名称、地物要素名称、参考字段名称（以，分割各字段）、字段以外可参考的字符、搜索容差</param>
        /// <returns></returns>
        public bool CheckData()
        {
            try
            {
                InputAnnoName = "界址点注记";
                InputFCName = "界址点";

                if (strParams == null || strParams.Length < 5)
                {
                    Message = InputAnnoName + "和" + InputFCName + "注记图层所需参数为空！请输入正确参数！";

                    return false;
                }

                string featAnnotName = strParams[0];
                string FeatName = strParams[1];
                string FieldNames = strParams[2];
                string otherString = strParams[3];
                string Tolerance = strParams[4];

                string strErrorMsg = "错误描述：" + InputAnnoName + "不匹配！";

                if (!Manager.IsCheckTaskData)
                {
                    CheckErrorList = CheckValue(featAnnotName, FeatName, FieldNames, otherString, Tolerance, strErrorMsg, null);
                }
                else
                {
                    List<int> idValueList = new List<int>();
                    bool bValue = Manager.CheckItems.TryGetValue(featAnnotName, out idValueList);
                    if (!bValue || idValueList.Count == 0)
                    {
                        Message = "图层名称： " + featAnnotName + "， 无检测数据！";

                        return false;
                    }
                    int[] idValues = idValueList.ToArray();
                    CheckErrorList = CheckValue(featAnnotName, FeatName, FieldNames, otherString, Tolerance, strErrorMsg, idValues);
                }
                return true;
            }
            catch (Exception err)
            {
                Message = "注记与属性是否一致检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }
        
        private ArrayList CheckValue(string featAnnotName, string FeatName, string CheckRule, string otherString, string Tolerance, string errorMsg, int[] idValues)
        {
            double tol = 0;
            ArrayList errorMsgList = new ArrayList(); //定义错误信息
            IFeatureClass AnnoFC = CheckerUtil.GetFeatureClassFromWorkspace(featAnnotName);
            IFeatureClass FeatFC = CheckerUtil.GetFeatureClassFromWorkspace(FeatName);
            if (AnnoFC == null || FeatFC == null)
            {
                string strMsg = "没能在数据库中找到对应的注记图层，请检查输入的图层名称是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return null;
            }
            try
            {
                tol = double.Parse(Tolerance);
            }
            catch
            {
                string strMsg = "输入的容差为非数值型，请检查输入的容差值是否正确";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                return errorMsgList;
            }

            string[] fieldsParm = CheckRule.Split(',');

            for (int i = 0; i < fieldsParm.Length; i++)
            {
                int fieldIndex = FeatFC.Fields.FindField(fieldsParm[i].ToString());
                if (fieldIndex == -1)
                {
                    string strMsg = "没能找到" + InputFCName + "图层中的 " + fieldsParm[i].ToString() + " 字段，请检查输入的字段名称是否正确";
                    errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
                    return errorMsgList;
                }
            }
            //开始检索所有的注记
            bool hasErr = false;
            IFeatureCursor pAnnoCursor;
            if (idValues == null)
            {
                pAnnoCursor = AnnoFC.Search(null, false);
            }
            else
            {
                pAnnoCursor = AnnoFC.GetFeatures(idValues, false);
            }
            IFeature pAnnoFeat = pAnnoCursor.NextFeature();
            while (pAnnoFeat != null)
            {
                string annoStr = pAnnoFeat.get_Value(pAnnoFeat.Fields.FindField("TextString")).ToString();  //Annotation的值
                //查找与之对应的所有相交的建筑物
                ISpatialFilter spFilter = new SpatialFilterClass();
                ITopologicalOperator topo = pAnnoFeat.Shape as ITopologicalOperator;
                topo.Buffer(tol);
                spFilter.Geometry = topo as IPolygon;
                spFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor featCursor = FeatFC.Search(spFilter, false);
                IFeature bFeat = featCursor.NextFeature();
                while (bFeat != null)
                {
                    string fieldsStr = "";
                    //ArrayList fieldsValue = new ArrayList();
                    for (int i = 0; i < fieldsParm.Length; i++)
                    {
                        string fStr = bFeat.get_Value(bFeat.Fields.FindField(fieldsParm[i])).ToString();
                        fieldsStr = fieldsStr + fStr;
                    }
                    string whole = fieldsStr + otherString;
                    string notFoundChar = "";
                    for (int i = 0; i < annoStr.Length; i++)
                    {
                        //假如没找到对应的字符，则写入错误
                        if (whole.IndexOf(annoStr[i]) == -1)
                        {
                            hasErr = true;
                            notFoundChar = notFoundChar + "'" + annoStr[i].ToString() + "' ";
                        }
                    }
                    if (hasErr)
                    {
                        List<IGeometry> pGeoList = new List<IGeometry>();
                        pGeoList.Add(pAnnoFeat.Shape);
                        pGeoList.Add(bFeat.Shape);   

                        string strMsg = "注记层：" + featAnnotName + ",要素ID号为： '" + pAnnoFeat.OID.ToString() + "' 与 对应要素层：" + FeatName + ", 要素ID号为： '" + bFeat.OID.ToString() + "'的属性不一致，没找到  " + notFoundChar + "关键字   " + errorMsg;
                        errorMsgList.Add(new CheckError { Description = strMsg, Locations = pGeoList });
                    }
                    bFeat = featCursor.NextFeature();
                }
                pAnnoFeat = pAnnoCursor.NextFeature();
            }
            if (hasErr == false)
            {
                string strMsg = "没发现任何不符合的要素";
                errorMsgList.Add(new CheckError { Description = strMsg, Locations = null });
            }
            return errorMsgList;
        }

        public override string Name
        {
            get
            {
                return "检查注记与属性是否一致";
            }
        }
    }
}
