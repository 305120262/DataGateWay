using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace DataGateWay.QC
{
    public class BaseChecker
    {
        private List<string> m_Params;

        protected string m_Message;
        protected ArrayList m_CheckErrorList;

        public BaseCheckerManager Manager;

        protected string[] strParams
        {
            get
            {
                return m_Params.ToArray();
            }
        }

        public virtual string Name
        {
            get
            {
                return "基础检测类";
            }
        }

        public List<string> Params
        {
            get
            {
                return m_Params;
            }
            set
            {
                m_Params = value;
            }
        }

        public string Message
        {
            get
            {
                return m_Message;
            }

            set
            {
                m_Message = value;
            }
        }

        public ArrayList CheckErrorList
        {
            get
            {
                return m_CheckErrorList;
            }

            set
            {
                m_CheckErrorList = value;
            }
        }

        public string GetCheckingTarget()
        {
            return m_Params[0];
        }
    }
}
