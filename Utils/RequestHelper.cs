
using System;
using System.Web;

namespace RSuite.Infrastructure.Core.Helper
{
    public static class RequestHelper
    {
        public static string GetQueryStringParameter(string paramName)
        {
            HttpContext context = HttpContext.Current;

            if (context == null || context.Request == null)
                return null;

            HttpRequest request = context.Request;

            // Normal query string
            string value = request.QueryString[paramName];

            if (!string.IsNullOrEmpty(value))
                return value;

            // DynamicCallBack: ?Url=original-url
            string fullUrl = request.QueryString["Url"];

            if (string.IsNullOrEmpty(fullUrl))
                fullUrl = request.QueryString["url"];

            if (!string.IsNullOrEmpty(fullUrl))
            {
                int questionMarkIndex = fullUrl.IndexOf('?');

                if (questionMarkIndex >= 0)
                {
                    string queryString =
                        fullUrl.Substring(questionMarkIndex + 1);

                    value =
                        HttpUtility.ParseQueryString(queryString)[paramName];

                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return null;
        }

        public static int GetIntegerParameter(string paramName)
        {
            string value = GetQueryStringParameter(paramName);

            int result;

            if (int.TryParse(value, out result))
                return result;

            return 0;
        }
    }
}