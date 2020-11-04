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
        public Order(string symbol, int quantity, string instruction, string orderType, float limit)
        {
            if (orderType == Core.Model.Constants.OrderType.LIMIT && limit <= 0)
            {
                Exception ex = new ArgumentException("limit cannot be unassigned for a limit order");
                Log.Error(ex, "Limit cannot be unassigned for a limit order");
                throw ex;
            }

            Symbol = symbol;
            Quantity = quantity;
            Instruction = instruction;
            OrderType = orderType;
            Limit = limit;
        }

        public string Symbol { get; }
        public int Quantity { get; }
        public string Instruction { get; }
        public string OrderType { get; }
        public float Limit { get; }
    }
}
