using RestSharp;
using Serilog;
using System;

namespace TDAmeritrade
{
    internal static class APIExceptions
    {
        public static void ThrowAPIException(IRestResponse response)
        {
            Exception ex;
            if (response.IsSuccessful)
            {
                ex = new GeneralException("Tried to throw APIException from valid response");
                Log.Error(ex, "Tried to throw unnecessary error");
                throw ex;
            }

            switch ((int)response.StatusCode)
            {
                case 400:
                    ex = new NotNullException(response.Content);
                    break;
                case 401:
                    ex = new TknExpException(response.Content);
                    break;
                case 403:
                    ex = new ForbidException(response.Content);
                    break;
                case 404:
                    ex = new NotFndException(response.Content);
                    break;
                case 429:
                    ex = new ExdLmtException(response.Content);
                    break;
                case 500:
                case 503:
                    ex = new ServerException(response.Content);
                    break;
                default:
                    ex = new GeneralException(response.Content);
                    break;
            }
            Log.Error(ex, "API Exception encountered");
            throw ex;
        }
    }

    internal class NotNullException : Exception
    {
        public NotNullException(string message) : base(message) { }
    }

    internal class TknExpException : Exception
    {
        public TknExpException(string message) : base(message) { }
    }

    internal class ForbidException : Exception
    {
        public ForbidException(string message) : base(message) { }
    }

    internal class NotFndException : Exception
    {
        public NotFndException(string message) : base(message) { }
    }

    internal class ExdLmtException : Exception
    {
        public ExdLmtException(string message) : base(message) { }
    }

    internal class ServerException : Exception
    {
        public ServerException(string message) : base(message) { }
    }

    internal class GeneralException : Exception
    {
        public GeneralException(string message) : base(message) { }
    }
}
