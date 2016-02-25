using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Geodatabase;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckPointOnLine : BaseChecker, IChecker
    {
        /// <summary>
        /// 点是否在线上检查
        /// </summary>
        /// <param name="strParams">参数数组包含：点图层名称、线图层名称</param>
        /// <returns>返回检查到的错误信息数组</returns>
        public bool CheckData()
        {
            try
            {
                if (strParams == null || strParams.Length < 4)
                {
                    Message = "点是否在线上检查所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string layerName = strParams[0];
                string layerName2 = strParams[1];
                if (layerName == null || layerName == string.Empty || layerName2 == null || layerName2 == string.Empty)
                {
                    Message = "点是否在线上检查所需图层名为空！请输入正确图层名参数！";
                    
                    return false;
                }
                string strSQL = strParams[2];
                if (strSQL == null || strSQL == string.Empty)
                    strSQL = "1=1";
                string strSQL2 = strParams[3];
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
                string strErrorMsg = "错误描述：点不在线上现象！";

                CheckErrorList = CheckerUtil.CheckPointsIsOnLines(layerName, layerName2, strSQL, strSQL2, false, esriSpatialRelEnum.esriSpatialRelIntersects, "T**T*****", strErrorMsg);
                
                return true;
            }
            catch (Exception err)
            {
                Message = "点不在线上检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }
    }
}
