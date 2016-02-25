using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Geodatabase;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckPolygonsHasGap : BaseChecker, IChecker
    {
        /// <summary>
        /// 面缝隙检查
        /// </summary>
        /// <param name="paramsValue">参数数组包含：图层名称1和图层名称2</param>
        /// <returns>返回检查到的错误信息数组</returns>
        public bool CheckData()
        {
            try
            {
                if (strParams == null || strParams.Length < 5)
                {
                    Message = "面缝隙检查所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string layerName = strParams[0];
                string layerName2 = strParams[1];
                if (layerName == null || layerName == string.Empty || layerName2 == null || layerName2 == string.Empty)
                {
                    Message = "面缝隙检查所需图层名为空！请输入正确图层名参数！";
                    
                    return false;
                }

                string strSQL = strParams[3];
                if (strSQL == null || strSQL == string.Empty)
                    strSQL = "1=1";
                string strSQL2 = strParams[4];
                if (strSQL2 == null || strSQL2 == string.Empty)
                    strSQL2 = "1=1";

                //检查图层是否有更新信息
                if (!Manager.IsCheckTaskData)
                {
                    CheckerUtil.m_idValues = null;
                }
                else
                {
                    List<int> idValueList = new List<int>();
                    bool bValue = Manager.CheckItems.TryGetValue(layerName, out idValueList);
                    if (!bValue || idValueList.Count == 0)
                    {
                        Message = "图层名称： " + layerName + "， 无检测数据！";

                        return false;
                    }
                    CheckerUtil.m_idValues = idValueList;
                }
                string strErrorMsg = "错误描述：面与面之间存在缝隙现象！";

                string strValue = strParams[2];
                if (strValue == null || strValue == string.Empty)
                {
                    CheckErrorList = CheckerUtil.CheckPolygonsHasGap(layerName, layerName2, strSQL, strSQL2, strErrorMsg);
                }
                else
                {
                    double dGap = Convert.ToDouble(strValue);
                    CheckErrorList = CheckerUtil.CheckPolygonsHasGap(layerName, layerName2, strSQL, strSQL2, strErrorMsg, dGap);
                }
                
                return true;
            }
            catch (Exception err)
            {
                Message = "面缝隙检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }
    }
}
