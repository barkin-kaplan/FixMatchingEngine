using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using QuickFix;
using QuickFix.Fields;

using ToyMatchingEngine.Helper;

using ToyMatchingEngine.Model;

namespace ToyMatchingEngine.Logic
{
    public class OrderAcceptor
    {
        private OrderAcceptor() { }
        private static OrderAcceptor instance;
        public static OrderAcceptor GetInstance()
        {
            if(instance == null)
            {
                instance = new OrderAcceptor();
            }
            return instance;
        }
        private int ExecutionIdCounter { get; set; }
        public Dictionary<string, Order> Orders { get; set; } = new Dictionary<string, Order>();
        public Dictionary<string, Order> ClOrdIdMap { get; set; } = new Dictionary<string, Order>();
        public Message AddNewOrder(Message m)
        {
            bool fail = false;
            string rejectMessage = "";
            string ordType = m.GetField(Tags.OrdType);
            if(ordType[0] != OrdType.LIMIT)
            {
                fail = true;
                rejectMessage = $"OrdType ({ordType}) is not supported";
            }
            if (!m.IsSetField(Tags.ClOrdID))
            {
                fail = true;
                rejectMessage = "ClOrdID not set";
            }
            if (!m.IsSetField(Tags.Symbol))
            {
                fail = true;
                rejectMessage = "Symbol not set";
            }
            if (!m.IsSetField(Tags.Price))
            {
                fail = true;
                rejectMessage = "Price not set";
            }
            if (!m.IsSetField(Tags.Side))
            {
                fail = true;
                rejectMessage = "Side not set";
            }
            if (!m.IsSetField(Tags.Account))
            {
                fail = true;
                rejectMessage = "Account not set";
            }
            string clOrdId = m.GetField(Tags.ClOrdID);
            if (!ClOrdIdMap.ContainsKey(clOrdId))
            {
                fail = true;
                rejectMessage = "Duplicate ClOrdID";
            }
            string orderQtyS = m.GetField(Tags.OrderQty);
            string priceS = m.GetField(Tags.Price);
            if (!Util.ValidateStringDecimal(priceS))
            {
                fail = true;
                rejectMessage = "Invalid Price format";
            }
            if (!Util.ValidateStringInteger(orderQtyS))
            {
                fail = true;
                rejectMessage = "Invalid OrderQty format";
            }
            string symbol = m.GetField(Tags.Symbol);
            char sideS = m.GetField(Tags.Side)[0];
            if (!Util.ValidateSide(sideS))
            {
                fail = true;
                rejectMessage = "Invalid side";
            }
            QuickFix.FIX50SP2.ExecutionReport executionReport = new QuickFix.FIX50SP2.ExecutionReport();
            executionReport.SetField(new ExecID(Util.GetTodayString() + (ExecutionIdCounter++).ToString()));
            executionReport.SetField(new TransactTime(DateTime.Now));
            executionReport.SetField(new ClOrdID(clOrdId));
            if (fail)
            {
                executionReport.SetField(new ExecType(ExecType.REJECTED));
                executionReport.SetField(new OrdStatus(OrdStatus.REJECTED));
                executionReport.SetField(new OrderID("NONE"));
                executionReport.SetField(new Text(rejectMessage));
            }
            else
            {
                decimal orderQty = decimal.Parse(orderQtyS, CultureInfo.InvariantCulture);
                decimal price = decimal.Parse(priceS, CultureInfo.InvariantCulture);
                Side side = new Side(sideS);
                Order order = new Order(orderQty,
                symbol,
                price,
                side
                );
                Orders[order.OrderID] = order;
                ClOrdIdMap[clOrdId] = order;
                Matcher matcher = Matcher.GetInstance();
                matcher.AddMessage(order.Symbol, order.OrderID, order.OrderQty, order.Price, order.Side, msgtype.New);

                executionReport.SetField(new ExecType(ExecType.NEW));
                executionReport.SetField(new Account(m.GetField(Tags.Account)));
                executionReport.SetField(new Symbol(m.GetField(Tags.Symbol)));
                executionReport.SetField(side);
                executionReport.SetField(new OrderQty(orderQty));
                executionReport.SetField(new Price(price));
                executionReport.SetField(new OrdType(m.GetField(Tags.OrdType)[0]));

                return (true, order.OrderID);
            }
            
        } 

    }
}
