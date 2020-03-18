using QuickFix;
using QuickFix.Fields;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;

using System.Threading;

using ToyMatchingEngine.Helper;
using ToyMatchingEngine.Model;
using ToyMatchingEngine.Connection;

namespace ToyMatchingEngine.Logic
{
    public class OrderAcceptor
    {
        private class OrderMatcherQueueItem
        {
            public string Symbol { get; set; }
            public string OrderID { get; set; }
            public decimal OrderQty { get; set; }
            public decimal Price { get; set; }
            public Side Side { get; set; }
            public msgtype msgtype { get;set; }
            public OrderMatcherQueueItem(string symbol,string orderID,decimal orderQty,decimal price,Side side,msgtype msgtype)
            {
                Symbol = symbol;
                OrderID = orderID;
                OrderQty = orderQty;
                Price = price;
                Side = side;
                this.msgtype = msgtype;
            }
        }
        ConcurrentQueue<OrderMatcherQueueItem> OrderMatcherQueue = new ConcurrentQueue<OrderMatcherQueueItem>();
        private OrderAcceptor() 
        {
            new Thread(() =>
            {
                Matcher matcher = Matcher.GetInstance();
                while(true)
                {
                    while (OrderMatcherQueue.TryDequeue(out OrderMatcherQueueItem item))
                        matcher.AddMessage(item.Symbol, item.OrderID, item.OrderQty, item.Price, item.Side, item.msgtype);
                    Thread.Sleep(10);
                }
                
            }).Start();
        }
        private static OrderAcceptor instance;
        public static OrderAcceptor GetInstance()
        {
            if(instance == null)
            {
                instance = new OrderAcceptor();
            }
            return instance;
        }
        private int execIdCounter;
        private string ExecIDCounter
        {
            get
            {
                return Util.GetTodayString() + (execIdCounter++).ToString();
            }
        }
        private int trdMatchIDCounter;
        private string TrdMatchIDCounter
        {
            get
            {
                return Util.GetTodayString() + (trdMatchIDCounter++).ToString();
            }
        }
        public Dictionary<string, Order> Orders { get; set; } = new Dictionary<string, Order>();
        public Dictionary<string, Order> ClOrdIdMap { get; set; } = new Dictionary<string, Order>();
        public Message AddNewMessage(Message request,SessionID sessionID)
        {
            bool fail = false;
            string rejectMessage = "";
            if (!request.IsSetField(Tags.OrdType))
            {
                fail = true;
                rejectMessage = "OrdType not set";
            }
            string ordType = request.GetField(Tags.OrdType);
            if(ordType[0] != OrdType.LIMIT)
            {
                fail = true;
                rejectMessage = $"OrdType ({ordType}) is not supported";
            }
            if (!request.IsSetField(Tags.ClOrdID))
            {
                fail = true;
                rejectMessage = "ClOrdID not set";
            }
            if (!request.IsSetField(Tags.Symbol))
            {
                fail = true;
                rejectMessage = "Symbol not set";
            }
            if (!request.IsSetField(Tags.Price))
            {
                fail = true;
                rejectMessage = "Price not set";
            }
            if (!request.IsSetField(Tags.OrderQty))
            {
                fail = true;
                rejectMessage = "OrderQty not set";
            }
            if (!request.IsSetField(Tags.Side))
            {
                fail = true;
                rejectMessage = "Side not set";
            }
            if (!request.IsSetField(Tags.Account))
            {
                fail = true;
                rejectMessage = "Account not set";
            }

            //for field checks
            if (fail)
            {
                Message executionReject = PrepareNewReject(rejectMessage, (request.IsSetField(Tags.ClOrdID) ? request.GetField(Tags.ClOrdID) : null));
                return executionReject;
            }
            string clOrdID = request.GetField(Tags.ClOrdID);
            if (ClOrdIdMap.ContainsKey(clOrdID))
            {
                fail = true;
                rejectMessage = "Duplicate ClOrdID";
            }
            string orderQtyS = request.GetField(Tags.OrderQty);
            string priceS = request.GetField(Tags.Price);
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
            string symbol = request.GetField(Tags.Symbol);
            char sideS = request.GetField(Tags.Side)[0];
            if (!Util.ValidateSide(sideS))
            {
                fail = true;
                rejectMessage = "Invalid side";
            }
            
            if (fail)
            {
                Message executionReject = PrepareNewReject(rejectMessage, clOrdID);
                return executionReject;
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
                order.SessionID = sessionID;
                order.ClOrdID = clOrdID;
                order.OrdStatus = new OrdStatus(OrdStatus.NEW);
                Orders[order.OrderID] = order;
                ClOrdIdMap[clOrdID] = order;
                
                QuickFix.FIX50SP2.ExecutionReport executionReport = new QuickFix.FIX50SP2.ExecutionReport();
                executionReport.SetField(new ExecID(ExecIDCounter));
                executionReport.SetField(new ClOrdID(clOrdID));
                executionReport.SetField(new ExecType(ExecType.NEW));
                executionReport.SetField(new OrderID(order.OrderID));
                executionReport.SetField(new OrdStatus(OrdStatus.NEW));
                executionReport.SetField(new Account(request.GetField(Tags.Account)));
                executionReport.SetField(new Symbol(request.GetField(Tags.Symbol)));
                executionReport.SetField(side);
                executionReport.SetField(new OrderQty(orderQty));
                executionReport.SetField(new Price(price));
                executionReport.SetField(new OrdType(request.GetField(Tags.OrdType)[0]));
                executionReport.SetField(new TimeInForce(request.GetField(Tags.TimeInForce)[0]));
                executionReport.SetField(new LeavesQty(orderQty));
                executionReport.SetField(new CumQty(0));
                executionReport.SetField(new AvgPx(order.AvgPx));
                executionReport.SetField(new TransactTime(DateTime.Now));
                OrderMatcherQueue.Enqueue(new OrderMatcherQueueItem(order.Symbol.getValue(), order.OrderID, order.OrderQty, order.Price, order.Side, msgtype.New));
                return executionReport;
            }
        }



        private Message PrepareNewReject(string rejectMessage,string clOrdID = null)
        {
            QuickFix.FIX50SP2.ExecutionReport executionReject = new QuickFix.FIX50SP2.ExecutionReport();
            executionReject.SetField(new ExecID(ExecIDCounter));
            if(!string.IsNullOrEmpty(clOrdID))
                executionReject.SetField(new ClOrdID(clOrdID));
            executionReject.SetField(new ExecType(ExecType.REJECTED));
            executionReject.SetField(new OrdStatus(OrdStatus.REJECTED));
            executionReject.SetField(new OrderID("NONE"));
            executionReject.SetField(new Text(rejectMessage));
            executionReject.SetField(new TransactTime(DateTime.Now));
            return executionReject;
        }

        private Message PrepareOrderCancelReject(string rejectMessage,OrdStatus ordStatus,string clOrdID = null,string origClOrdID = null)
        {
            QuickFix.FIX50SP2.OrderCancelReject response = new QuickFix.FIX50SP2.OrderCancelReject();
            if (!string.IsNullOrEmpty(clOrdID))
            {
                response.SetField(new ClOrdID(clOrdID));
            }
            if (!string.IsNullOrEmpty(origClOrdID))
            {
                response.SetField(new OrigClOrdID(origClOrdID));
            }
            response.SetField(new CxlRejResponseTo('1'));
            response.SetField(new Text(rejectMessage));
            response.SetField(new OrderID("NONE"));
            response.SetField(ordStatus);
            response.SetField(new TransactTime(DateTime.Now));
            return response;
        }
        public Message AddReplaceMessage(Message request)
        {
            bool fail = false;
            string rejectMessage = "";
            if (!request.IsSetField(Tags.OrdType))
            {
                fail = true;
                rejectMessage = "OrdType not set";
            }
            string ordType = request.GetField(Tags.OrdType);
            if (ordType[0] != OrdType.LIMIT)
            {
                fail = true;
                rejectMessage = $"OrdType ({ordType}) is not supported";
            }
            if (!request.IsSetField(Tags.ClOrdID))
            {
                fail = true;
                rejectMessage = "ClOrdID not set";
            }
            if (!request.IsSetField(Tags.OrigClOrdID))
            {
                fail = true;
                rejectMessage = "OrigClOrdID not set";
            }
            if (!request.IsSetField(Tags.Symbol))
            {
                fail = true;
                rejectMessage = "Symbol not set";
            }
            if (!request.IsSetField(Tags.OrderQty))
            {
                fail = true;
                rejectMessage = "OrderQty not set";
            }
            if (!request.IsSetField(Tags.Side))
            {
                fail = true;
                rejectMessage = "Side not set";
            }
            if (!request.IsSetField(Tags.Account))
            {
                fail = true;
                rejectMessage = "Account not set";
            }
            //field check return message
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    new OrdStatus(OrdStatus.REJECTED),
                    (request.IsSetField(Tags.ClOrdID) ? request.GetField(Tags.ClOrdID) : null),
                    (request.IsSetField(Tags.OrigClOrdID) ? request.GetField(Tags.OrigClOrdID) : null));
            }
            OrdStatus rejectOrdStatus = new OrdStatus(OrdStatus.REJECTED);
            string origClOrdID = request.GetField(Tags.OrigClOrdID);
            string clOrdID = request.GetField(Tags.ClOrdID);
            if (!ClOrdIdMap.ContainsKey(origClOrdID))
            {
                fail = true;
                rejectMessage = "Unknown OrigClOrdID";
                origClOrdID = "NONE";
            }
            else
            {
                rejectOrdStatus = ClOrdIdMap[origClOrdID].OrdStatus;
                if (ClOrdIdMap.ContainsKey(clOrdID))
                {
                    fail = true;
                    rejectMessage = "Duplicate ClOrdID";
                    
                }
            }
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    rejectOrdStatus,
                    clOrdID,
                    origClOrdID);
            }
            Order order = ClOrdIdMap[origClOrdID];
            if (order.OrdStatus.getValue() == OrdStatus.FILLED)
            {
                fail = true;
                rejectMessage = "Order already filled";
            }
            if (order.OrdStatus.getValue() == OrdStatus.CANCELED)
            {
                fail = true;
                rejectMessage = "Order already canceled";
            }
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    rejectOrdStatus,
                    clOrdID,
                    origClOrdID);
            }
            if (request.IsSetField(Tags.Price))
            {
                string priceS = request.GetField(Tags.Price);
                if (!Util.ValidateStringDecimal(priceS))
                {
                    fail = true;
                    rejectMessage = "Invalid Price format";
                }
            }
            string orderQtyS = request.GetField(Tags.OrderQty);
            if (!Util.ValidateStringInteger(orderQtyS))
            {
                fail = true;
                rejectMessage = "Invalid OrderQty format";
            }
            string symbol = request.GetField(Tags.Symbol);
            char sideS = request.GetField(Tags.Side)[0];
            if (!Util.ValidateSide(sideS))
            {
                fail = true;
                rejectMessage = "Invalid side";
            }
            
            
            bool priceChanged = true;
            decimal price = 0m;
            if (request.IsSetField(Tags.Price))
            {
                price = decimal.Parse(request.GetField(Tags.Price), CultureInfo.InvariantCulture);
                if(order.Price == price)
                {
                    priceChanged = false;
                }
            }
            bool orderQtyChanged = true;
            decimal orderQty = decimal.Parse(request.GetField(Tags.OrderQty), CultureInfo.InvariantCulture);
            if (order.OrderQty == orderQty)
            {
                orderQtyChanged = false;
            }
            if(!priceChanged && !orderQtyChanged)
            {
                fail = true;
                rejectMessage = "Price or OrderQty should change";
            }
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    rejectOrdStatus,
                    clOrdID, origClOrdID);
            }
            else
            {
                ClOrdIdMap[clOrdID] = order;
                QuickFix.FIX50SP2.ExecutionReport executionReport = new QuickFix.FIX50SP2.ExecutionReport();
                executionReport.SetField(new ClOrdID(clOrdID));
                order.ClOrdID = clOrdID;
                order.OrigClOrdID = origClOrdID;
                executionReport.SetField(new OrigClOrdID(origClOrdID));
                executionReport.SetField(new ExecID(ExecIDCounter));
                if (request.IsSetField(Tags.Price))
                {
                    executionReport.SetField(new Price(price));
                    order.Price = price;
                }
                executionReport.SetField(new OrderQty(orderQty));
                order.OrderQty = orderQty;

                

                executionReport.SetField(new ExecType(ExecType.REPLACED));
                executionReport.SetField(new OrderID(order.OrderID));
                executionReport.SetField(order.OrdStatus);
                executionReport.SetField(new Account(request.GetField(Tags.Account)));
                executionReport.SetField(order.Symbol);
                executionReport.SetField(order.Side);
                executionReport.SetField(new OrderQty(orderQty));
                executionReport.SetField(new Price(price));
                executionReport.SetField(new OrdType(ordType[0]));
                executionReport.SetField(new LeavesQty(order.LeavesQty));
                executionReport.SetField(new CumQty(order.CumQty));
                executionReport.SetField(new AvgPx(order.AvgPx));
                executionReport.SetField(new TransactTime(DateTime.Now));
                OrderMatcherQueue.Enqueue(new OrderMatcherQueueItem(order.Symbol.getValue(), order.OrderID, order.OrderQty - order.CumQty, order.Price, order.Side, msgtype.Replace));
                return executionReport;
            }
        }

        public Message AddCancelMessage(Message request)
        {
            bool fail = false;
            string rejectMessage = "";
            if (!request.IsSetField(Tags.ClOrdID))
            {
                fail = true;
                rejectMessage = "ClOrdID not set";
            }
            if (!request.IsSetField(Tags.OrigClOrdID))
            {
                fail = true;
                rejectMessage = "OrigClOrdID not set";
            }
            //field check return message
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    new OrdStatus(OrdStatus.REJECTED),
                    (request.IsSetField(Tags.ClOrdID) ? request.GetField(Tags.ClOrdID) : null),
                    (request.IsSetField(Tags.OrigClOrdID) ? request.GetField(Tags.OrigClOrdID) : null));
            }
            OrdStatus rejectOrdStatus = new OrdStatus(OrdStatus.REJECTED);
            string origClOrdID = request.GetField(Tags.OrigClOrdID);
            string clOrdID = request.GetField(Tags.ClOrdID);
            if (!ClOrdIdMap.ContainsKey(origClOrdID))
            {
                fail = true;
                rejectMessage = "Unknown OrigClOrdID";
                origClOrdID = "NONE";
            }
            else
            {
                rejectOrdStatus = ClOrdIdMap[origClOrdID].OrdStatus;
                if (ClOrdIdMap.ContainsKey(clOrdID))
                {
                    fail = true;
                    rejectMessage = "Duplicate ClOrdID";

                }
            }
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    rejectOrdStatus,
                    clOrdID,
                    origClOrdID);
            }
            Order order = ClOrdIdMap[origClOrdID];
            if(order.OrdStatus.getValue() == OrdStatus.FILLED)
            {
                fail = true;
                rejectMessage = "Order already filled";
            }
            if (order.OrdStatus.getValue() == OrdStatus.CANCELED)
            {
                fail = true;
                rejectMessage = "Order already canceled";
            }
            if (fail)
            {
                return PrepareOrderCancelReject(rejectMessage,
                    rejectOrdStatus,
                    clOrdID,
                    origClOrdID);
            }
            else
            {
                QuickFix.FIX50SP2.ExecutionReport cancelAck = new QuickFix.FIX50SP2.ExecutionReport();
                cancelAck.SetField(new ClOrdID(clOrdID));
                cancelAck.SetField(new OrigClOrdID(origClOrdID));
                cancelAck.SetField(new ExecID(ExecIDCounter));
                cancelAck.SetField(new ExecType(ExecType.CANCELED));
                cancelAck.SetField(new OrdStatus(OrdStatus.CANCELED));
                order.OrdStatus = new OrdStatus(OrdStatus.CANCELED);
                cancelAck.SetField(order.Symbol);
                cancelAck.SetField(order.Side);
                order.OrderQty = order.CumQty;
                cancelAck.SetField(new OrderQty(order.OrderQty));
                cancelAck.SetField(new Price(order.Price));
                cancelAck.SetField(new LeavesQty(0));
                order.OrderQty = order.CumQty;
                cancelAck.SetField(new CumQty(order.CumQty));
                cancelAck.SetField(new AvgPx(order.AvgPx));
                cancelAck.SetField(new OrderID(order.OrderID));
                cancelAck.SetField(new TransactTime(DateTime.Now));
                OrderMatcherQueue.Enqueue(new OrderMatcherQueueItem(order.Symbol.getValue(), order.OrderID, order.OrderQty, order.Price, order.Side, msgtype.Cancel));
                return cancelAck;
            }
        }

        public void PublishExecutionTrade(Order order)
        {
            QuickFix.FIX50SP2.ExecutionReport tradeExecution = new QuickFix.FIX50SP2.ExecutionReport();
            tradeExecution.SetField(new ClOrdID(order.ClOrdID));
            if(!string.IsNullOrWhiteSpace(order.OrigClOrdID))
                tradeExecution.SetField(new OrigClOrdID(order.OrigClOrdID));
            tradeExecution.SetField(new OrderID(order.OrderID));
            tradeExecution.SetField(new Price(order.Price));
            tradeExecution.SetField(new OrderQty(order.OrderQty));
            tradeExecution.SetField(new TrdMatchID(TrdMatchIDCounter));
            tradeExecution.SetField(new ExecID(ExecIDCounter));
            tradeExecution.SetField(new ExecType(ExecType.TRADE));
            tradeExecution.SetField(order.OrdStatus);
            tradeExecution.SetField(order.Symbol);
            tradeExecution.SetField(order.Side);
            tradeExecution.SetField(new LastQty(order.LastQty));
            tradeExecution.SetField(new LastPx(order.LastPx));
            tradeExecution.SetField(new LeavesQty(order.LeavesQty));
            tradeExecution.SetField(new CumQty(order.CumQty));
            tradeExecution.SetField(new AvgPx(order.AvgPx));
            tradeExecution.SetField(new TransactTime(DateTime.Now));
            QuickFixAcceptor.GetInstance().PublishMessage(tradeExecution, order.SessionID);
        }
    }
}
