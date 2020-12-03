using Core.Exceptions;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Core
{
    public static class OptionSymbolUtils
    {
        public const string StandardDateFormat = "yyMMdd";
        private static Regex _optionSymbolRegex = new Regex(@"^([A-Z]{1,5})_(\d{6})([CP])(\d+(.\d)?)");
        private static Regex _callRegex = new Regex(@"^[A-Z]{1,5}[_ ]?\d{6}C\d+(.\d)?");
        private static Regex _putRegex = new Regex(@"^[A-Z]{1,5}[_ ]?\d{6}P\d+(.\d)?");

        public static bool IsOptionSymbol(string symbol) => _optionSymbolRegex.IsMatch(symbol);
        public static bool IsCall(string symbol) => _callRegex.IsMatch(symbol);
        public static bool IsPut(string symbol) => _putRegex.IsMatch(symbol);

        public static string ConvertToStandardDateFormat(string symbol, string fromFormat)
        {
            return ConvertDateFormat(symbol, fromFormat, StandardDateFormat);
        }

        public static string ConvertDateFormat(string symbol, string fromFormat, string toFormat)
        {
            ValidateDateIsFormatAndNotExpired(symbol, fromFormat);

            GroupCollection matchGroups = _optionSymbolRegex.Match(symbol).Groups;
            string date = DateTime.ParseExact(matchGroups[2].Value, fromFormat, CultureInfo.InvariantCulture).ToString(toFormat);

            return string.Format("{0}_{1}{2}{3}",
                matchGroups[1].Value,
                date,
                matchGroups[3].Value,
                matchGroups[4].Value);
        }

        public static void ValidateDateFormat(string symbol, string dateFormat)
        {
            bool isCorrectFormat = TryParseExactDate(symbol, dateFormat, out DateTime date);
            if (!isCorrectFormat)
            {
                throw new OptionParsingException("Option date not in correct format. Expected " + dateFormat, symbol);
            }
        }

        public static void ValidateDateIsFormatAndNotExpired(string symbol, string dateFormat)
        {
            bool isCorrectFormat = TryParseExactDate(symbol, dateFormat, out DateTime date);
            if (!isCorrectFormat)
            {
                throw new OptionParsingException("Option date not in correct format. Expected " + dateFormat, symbol);
            }
            else if (date < DateTime.Now)
            {
                throw new OptionParsingException("Option date is expired", symbol);
            }
        }

        public static bool TryParseExactDate(string symbol, string dateFormat, out DateTime date)
        {
            GroupCollection matchGroups = _optionSymbolRegex.Match(symbol).Groups;
            return DateTime.TryParseExact(matchGroups[2].Value, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
