using Core.Model.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Order
    {
        public Order(string symbol, float quantity, string transactionType, string orderType, float limit)
        {
            if (orderType == Core.Model.Constants.OrderType.LIMIT && limit <= 0)
            {
                Exception ex = new ArgumentException("limit cannot be unassigned for a limit order");
                Log.Error(ex, "Limit cannot be unassigned for a limit order");
                throw ex;
            }

            Symbol = symbol;
            Quantity = quantity;
            TransactionType = transactionType;
            OrderType = orderType;
            Limit = limit;
        }

        public string Symbol { get; }
        public float Quantity { get; }
        public string TransactionType { get; }
        public string OrderType { get; }
        public float Limit { get; }
    }
}
