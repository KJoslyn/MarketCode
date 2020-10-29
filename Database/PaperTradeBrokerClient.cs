using Core;

namespace Database
{
    public class PaperTradeBrokerClient : PositionDB, IBrokerClient
    {
        public PaperTradeBrokerClient(string dbPath) : base(dbPath) { }
    }
}
