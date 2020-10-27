using System;
using System.Collections.Generic;
using Core;
using Core.Model;
using LiteDB;

namespace Database
{
    public class PaperTradeBrokerClient : PositionDB, IBrokerClient
    {
        public PaperTradeBrokerClient(string dbPath) : base(dbPath) { }
    }
}
