using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace QnABot
{
    public class Utils
    {
        public static string GetAppSetting(string key)
        {
#if DEBUG
            return ConfigurationManager.AppSettings[key];
#else
            return Microsoft.Bot.Builder.Azure.Utils.GetAppSetting(key);
#endif
        }
    }
}