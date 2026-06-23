using System;

namespace Report.Services
{
    public static class ServiceHelper
    {
        public static T ExecuteSave<T>(Func<T> action) where T : class, new()
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                var result = new T();
                var prop = typeof(T).GetProperty("Status");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(result, "ERROR:" + ex.Message);
                return result;
            }
        }

        public static T Execute<T>(Func<T> action, T defaultValue)
        {
            try
            {
                return action();
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string ExecuteDelete(Func<string> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                return "ERROR:" + ex.Message;
            }
        }
    }
}
