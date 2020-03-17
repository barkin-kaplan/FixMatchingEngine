using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ToyMatchingEngine.Helper;
using QuickFix.Fields;

namespace ToyMatchingEngine.Model
{
    public class Order
    {
        private static int OrderIdCounter { get; set; }
        public decimal OrderQty { get; set; }
        public decimal CumulativeQty { get; set; }
        public decimal OpenQty
        {
            get { return OrderQty - CumulativeQty; }
        }
        public decimal Price { get; set; }
        public decimal LastQty { get; set; }
        public string Symbol { get; set; }
        public decimal AvgPx { get; set; }
        public decimal LastPx { get; set; }
        public OrdStatus OrdStatus { get; set; }
        public Side Side { get; set; }
        public string OrderID { get; set; }

        public Order( decimal orderQty,string symbol, decimal price,Side side)
        {
            OrderID = Util.GetTodayString() + (OrderIdCounter++).ToString();
            OrderQty = orderQty;
            Symbol = symbol;
            Price = price;
            Side = side;
        }
    }
}
