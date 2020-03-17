using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QuickFix;
using QuickFix.Fields;

using ToyMatchingEngine.Logic;

namespace ToyMatchingEngine.Connection
{
    public class QuickFixAcceptor : IApplication
    {
        public void FromAdmin(Message message, SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void FromApp(Message m, SessionID sessionID)
        {
            string msgType = m.Header.GetField(Tags.MsgType);
            if(msgType == MsgType.NEWORDERSINGLE)
            {
                bool result;string responseOrId;
                (result, responseOrId) = OrderAcceptor.GetInstance().AddNewOrder(m);
                if (result)
                {
                    QuickFix.FIX50SP2.ExecutionReport executionReport = new QuickFix.FIX50SP2.ExecutionReport();
                    executionReport.SetField(new ClOrdID(m.GetField(Tags.ClOrdID)));
                    executionReport.SetField(new OrderID(responseOrId));
                    executionReport.SetField(new OrderQty(m.GetField(Tags.OrderQty)));
                }
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void OnLogon(SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void OnLogout(SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            throw new NotImplementedException();
        }
    }
}
