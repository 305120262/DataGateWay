using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using System.Xml.Linq;

namespace DataGateWay.QC
{
    abstract public class BaseCheckerManager
    {
        abstract public IWorkspace CheckingWorkspace
        {
            get;
        }
        public Dictionary<string, List<int>> CheckItems=new Dictionary<string,List<int>>();
        public bool IsCheckTaskData;
        public int CheckItemCount;

        private List<IChecker> m_checkers = new List<IChecker>();

        protected List<IChecker> Checkers
        {
            get { return m_checkers; }
            set { m_checkers = value; }
        }
        protected List<string> m_log = new List<string>();
        protected List<CheckError> m_errors = new List<CheckError>();

        public List<string> Log
        {
            get { return m_log; }
        }

        public List<CheckError> Errors
        {
            get { return m_errors; }
            set { m_errors = value; }
        }

        public void LoadConfig(string filename)
        {
            m_checkers.Clear();
            XElement doc = XElement.Load(filename);
            var checkers = from pn in doc.Descendants("Checker")
                           select pn;
            foreach (var c in checkers)
            {
                string typeName = c.Attribute("Type").Value + ",DataGateWayLibrary";
                BaseChecker checker = Activator.CreateInstance(System.Type.GetType(typeName)) as BaseChecker;
                checker.Manager = this;
                var parameters = from pn in c.Descendants("p")
                                 select pn.Value;
                List<string> ps = new List<string>();
                ps.AddRange(parameters);
                checker.Params = ps;
                IChecker ick = checker as IChecker;
                m_checkers.Add(ick);
            }
        }

    }
}
