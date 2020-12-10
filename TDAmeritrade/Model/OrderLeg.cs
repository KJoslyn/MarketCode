using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDAmeritrade.Model
{
    internal class OrderLeg
    {
        public OrderLeg (string instruction, float quantity, Instrument instrument) 
        {
            Instruction = instruction;
            Quantity = quantity;
            Instrument = instrument;
        }

        public string Instruction { get; init; }
        public float Quantity { get; init; }
        public Instrument Instrument { get; init; }
    }
}
