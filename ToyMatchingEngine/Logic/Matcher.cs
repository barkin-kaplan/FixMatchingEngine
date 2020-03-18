using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFix.Fields;
using QuickFix;

namespace ToyMatchingEngine.Logic
{
    public enum msgtype
    {
        New,Replace,Cancel
    }
    public class Matcher
    {
        
        private class PassiveOrder
        {
            public decimal qty;
            public decimal price;
            public string symbol;
            public string orderId;
            public Side side;
        }
        private class SymbolMarket
        {
            
            public List<PassiveOrder> buys = new List<PassiveOrder>();
            public List<PassiveOrder> sells = new List<PassiveOrder>();
            public string symbol;
            /// <summary>
            /// Add new message to market and return matching quantity
            /// </summary>
            /// <param name="orderId"></param>
            /// <param name="initialQty"></param>
            /// <param name="price"></param>
            /// <param name="side"></param>
            /// <returns>Execution quantity for this new order</returns>
            public decimal AddNewMessage(string orderId,decimal initialQty,decimal price,Side side)
            {
                OrderAcceptor acceptor = OrderAcceptor.GetInstance();
                decimal qty = initialQty;
                decimal matchQty;
                decimal lastPx;
                if (side.getValue() == Side.BUY)
                {
                    while(qty > 0 && sells.Count > 0 && sells[0].price <= price)
                    {
                        PassiveOrder nextPassive = sells[0];
                        lastPx = nextPassive.price;
                        if (qty >= nextPassive.qty)
                        {
                            matchQty = nextPassive.qty;
                            qty -= matchQty;
                            sells.RemoveAt(0);
                            
                        }
                        else
                        {
                            matchQty = qty;
                            nextPassive.qty -= matchQty;
                            qty = 0;                            
                        }
                        acceptor.Orders[nextPassive.orderId].AddExecution(lastPx,matchQty);
                        acceptor.Orders[orderId].AddExecution(lastPx, matchQty);
                    }
                }
                else
                {
                    while (qty > 0 && buys.Count > 0 && buys[0].price >= price)
                    {
                        PassiveOrder nextPassive = buys[0];
                        lastPx = nextPassive.price;
                        if (qty >= nextPassive.qty)
                        {
                            matchQty = nextPassive.qty;
                            qty -= matchQty;
                            buys.RemoveAt(0);
                        }
                        else
                        {
                            matchQty = qty;
                            nextPassive.qty -= matchQty;
                            qty = 0;
                        }
                        acceptor.Orders[nextPassive.orderId].AddExecution(lastPx, matchQty);
                        acceptor.Orders[orderId].AddExecution(lastPx, matchQty);
                    }
                }
                if(qty > 0)
                {
                    PassiveOrder newPassive = new PassiveOrder { orderId = orderId, side = side, symbol = symbol, qty = qty, price = price };
                    AddPassiveOrder(newPassive);
                }
                return initialQty - qty;
            }

            private void AddPassiveOrder(PassiveOrder newPassive)
            {
                List<PassiveOrder> dummySides;
                if(newPassive.side.getValue() == Side.BUY)
                {
                    dummySides = buys;
                }
                else
                {
                    dummySides = sells;
                }

                int index = 0;
                while (index < dummySides.Count && newPassive.price <= dummySides[index].price)
                {
                    index++;
                }
                dummySides.Insert(index, newPassive);
            }
            /// <summary>
            /// takes replace message arguements. qty is the quantity that is to be seen on the market.
            /// </summary>
            /// <param name="orderId"></param>
            /// <param name="qty"></param>
            /// <param name="price"></param>
            /// <param name="side"></param>
            /// <returns></returns>
            public decimal AddReplaceOrder(string orderId, decimal qty, decimal price, Side side)
            {
                
                AddCancelMessage(orderId, side);
                return AddNewMessage(orderId, qty, price, side);
            }

            public void AddCancelMessage(string orderId,Side side)
            {
                if (side.getValue() == Side.BUY)
                {
                    int index = buys.FindIndex(new Predicate<PassiveOrder>((o) => o.orderId == orderId));
                    if(index != -1)
                    {
                        buys.RemoveAt(index);
                    }
                }
                else
                {
                    int index = sells.FindIndex(new Predicate<PassiveOrder>((o) => o.orderId == orderId));
                    if (index != -1)
                    {
                        sells.RemoveAt(index);
                    }
                }
            }

            
        }

        
        private static Matcher instance;
        public static Matcher GetInstance()
        {
            if (instance == null)
            {
                instance = new Matcher();
            }
            return instance;
        }
        private Dictionary<string, SymbolMarket> marketMap = new Dictionary<string, SymbolMarket>();
        private Matcher() { }
        
        public decimal AddMessage(string symbol,string orderId,decimal orderQty,decimal price,Side side,msgtype msgtype)
        {
            if (marketMap.TryGetValue(symbol, out SymbolMarket market))
            {

            }
            else
            {
                market = new SymbolMarket();
                marketMap[symbol] = market;
            }

            switch (msgtype)
            {
                case msgtype.New:
                    return market.AddNewMessage(orderId, orderQty, price, side);
                case msgtype.Replace:
                    return market.AddReplaceOrder(orderId, orderQty, price, side);
                case msgtype.Cancel:
                    market.AddCancelMessage(orderId, side);
                    return 0;
                default:
                    throw new Exception($"Not gonna happen exception");
            }
        }
    }

    
}
