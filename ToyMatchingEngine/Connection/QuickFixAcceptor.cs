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
        private static QuickFixAcceptor instance { get; set; }
        private QuickFixAcceptor() { }
        public static QuickFixAcceptor GetInstance()
        {
            if(instance == null)
            {
                instance = new QuickFixAcceptor();
            }
            return instance;
        }
        public void FromAdmin(Message message, SessionID sessionID)
        {
            
        }

        public void FromApp(Message m, SessionID sessionID)
        {
            string msgType = m.Header.GetField(Tags.MsgType);
            OrderAcceptor acceptor = OrderAcceptor.GetInstance();
            Message response;
            switch (msgType)
            {
                case MsgType.NEWORDERSINGLE:
                    response = acceptor.AddNewMessage(m,sessionID);                    
                    break;
                case MsgType.ORDERCANCELREPLACEREQUEST:
                    response = acceptor.AddReplaceMessage(m);
                    break;
                case MsgType.ORDERCANCELREQUEST:
                    response = acceptor.AddCancelMessage(m);
                    break;
                default:
                    response = new QuickFix.FIX50SP2.BusinessMessageReject();
                    response.SetField(new Text($"Unsopported message type {msgType}"));
                    break;
            }
            Session.LookupSession(sessionID).Send(response);
        }

        public void OnCreate(SessionID sessionID)
        {
            Console.WriteLine($"Created Session : {sessionID}");
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine($"Logon Session : {sessionID}");
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine($"Logout Session : {sessionID}");
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            
        }

        public void PublishMessage(Message m,SessionID sessionID)
        {
            Session.LookupSession(sessionID).Send(m);
        }
    }
}
