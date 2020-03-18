using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ToyMatchingEngine.Helper;
using ToyMatchingEngine.Logic;
using QuickFix;
using QuickFix.Fields;

namespace ToyMatchingEngine.Model
{
    public class Order
    {
        private static int OrderIdCounter { get; set; }
        public decimal OrderQty { get; set; }
        private decimal cumQty;
        public decimal CumQty 
        {
            get
            {
                return cumQty;
            }
            set
            {
                cumQty = value;
                if(cumQty == 0)
                {
                    OrdStatus = new OrdStatus(OrdStatus.NEW);
                }
                else if (cumQty == OrderQty)
                {
                    OrdStatus = new OrdStatus(OrdStatus.FILLED);
                }
                else
                {
                    OrdStatus = new OrdStatus(OrdStatus.PARTIALLY_FILLED);
                }
            }

        }
        public decimal LeavesQty
        {
            get { return OrderQty - CumQty; }
        }
        public decimal Price { get; set; }
        public decimal LastQty { get; set; }
        public Symbol Symbol { get; set; }
        public decimal AvgPx { get; set; }
        public decimal LastPx { get; set; }
        public OrdStatus OrdStatus { get; set; }
        public Side Side { get; set; }
        public string OrderID { get; set; }
        public SessionID SessionID { get; set; }
        public string ClOrdID { get; set; }
        public string OrigClOrdID { get; set; }

        public Order( decimal orderQty,string symbol, decimal price,Side side)
        {
            OrderID = Util.GetTodayString() + (OrderIdCounter++).ToString();
            OrderQty = orderQty;
            Symbol = new Symbol(symbol);
            Price = price;
            Side = side;
        }

        public void AddExecution(decimal price,decimal qty)
        {
            AvgPx = ((CumQty * AvgPx) + (qty * price)) / (qty + CumQty);
            CumQty += qty;
            LastPx = price;
            LastQty = qty;
            OrderAcceptor.GetInstance().PublishExecutionTrade(this);
        }
    }
}
