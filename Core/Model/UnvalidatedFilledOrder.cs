using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class UnvalidatedFilledOrder : HasSymbolInStandardFormat, HasTime
    {
        public UnvalidatedFilledOrder(string symbol, float price, string instruction, string orderType, float limit, int quantity, DateTime time)
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

        public UnvalidatedFilledOrder() { }

        public string Id { get => Symbol + Time.ToString() + Instruction + OrderType + Quantity.ToString() + Quantity.ToString() + Price.ToString("0.00"); }
        public float Price { get; init; }
        public string Instruction { get; init; }
        public string OrderType { get; init; }
        public float Limit { get; init; }
        public int Quantity { get; init; }
        public DateTime Time { get; init; }

        public bool EqualsIgnoreTime(FilledOrder other)
        {
            return Symbol == other.Symbol &&
                Price == other.Price &&
                Instruction == other.Instruction &&
                OrderType == other.OrderType &&
                Limit == other.Limit &&
                Quantity == other.Quantity;
        }

        public bool StrictEquals(FilledOrder other)
        {
            return EqualsIgnoreTime(other) &&
                Time.Equals(other.Time);
        }
    }
}
