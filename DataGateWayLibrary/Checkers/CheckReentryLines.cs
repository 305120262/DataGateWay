using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.Geodatabase;
using DataGateWay.QC;

namespace DataGateWay.Checkers
{
    public class CheckReentryLines : BaseChecker, IChecker
    {
        /// <summary>
        /// 折返线检查(当两段线间夹角小于给定值)
        /// </summary>
        /// <param name="paramsValue">参数数组包含：图层名称和夹角角度值</param>
        /// <returns>返回检查到的错误信息数组</returns>
        public bool CheckData()
        {
            try
            {
                if (strParams == null || strParams.Length < 3)
                {
                    Message = "折返线检查所需参数为空！请输入正确参数！";
                    
                    return false;
                }

                string layerName = strParams[0];
                if (layerName == null || layerName == string.Empty)
                {
                    Message = "折返线检查所需图层名为空！请输入正确图层名参数！";
                    
                    return false;
                }
                string strAngle = strParams[1];
                string strSQL = strParams[2];
                if (strSQL == null || strSQL == string.Empty)
                    strSQL = "1=1";

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
                if (strAngle == null || strAngle == string.Empty)
                {
                    CheckErrorList = CheckerUtil.CheckLineAngle(layerName, strSQL);
                }
                else
                {
                    double dAngle = Convert.ToDouble(strAngle);
                    CheckErrorList = CheckerUtil.CheckLineAngle(layerName, strSQL, dAngle);
                }
                
                return true;
            }
            catch (Exception err)
            {
                Message = "折返线检查出现异常，错误原因：" + err.Message;
                return false;
            }
        }
    }
}
