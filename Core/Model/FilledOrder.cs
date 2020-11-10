using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class FilledOrder
    {
        public FilledOrder(string symbol, float price, string instruction, string orderType, float limit, int quantity, DateTime time)
        {
            if (orderType == Core.Model.Constants.OrderType.LIMIT && limit <= 0)
            {
                Exception ex = new ArgumentException("limit cannot be unassigned for a limit order");
                Log.Error(ex, "Limit cannot be unassigned for a limit order");
                throw ex;
            }

            Symbol = symbol;
            Price = price;
            Instruction = instruction;
            OrderType = orderType;
            Limit = limit;
            Quantity = quantity;
            Time = time;
        }

        public string Symbol { get; }
        public float Price { get; }
        public string Instruction { get; }
        public string OrderType { get; }
        public float Limit { get; }
        public int Quantity { get; }
        public DateTime Time { get; }
    }
}
