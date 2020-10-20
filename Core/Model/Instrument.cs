using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class Instrument
    {
        public string assetType;
        public string cusip;
        public string symbol;
        public string description;
        public string putCall;
    }
}

public class AssetType
{
    public const string EQUITY = nameof(EQUITY);
    public const string OPTION = nameof(OPTION);
}

//enum AssetType
//{
//    EQUITY,
//    OPTION,
//    INDEX,
//    MUTUAL_FUND,
//    CASH_EQUIVALENT,
//    FIXED_INCOME,
//    CURRENCY
//}

enum PutCall
{
    PUT,
    CALL
}
