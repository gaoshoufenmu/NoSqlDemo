using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ElasticsearchIO
{
    class Log
    {
        private const string _info = "Info";
        private const string _warn = "Warn";
        private const string _error = "Error";
        private const string _fatal = "Fatal";


        public static void Log_Info(string message)
        {
            LogManager.GetLogger(_info).Info(message);
        }
        public static void Log_Warn(string message)
        {
            LogManager.GetLogger(_warn).Warn(message);
        }
        public static void Log_Error(string message)
        {
            LogManager.GetLogger(_error).Error(message);
        }
        public static void Log_Fatal(string message)
        {
            LogManager.GetLogger(_fatal).Fatal(message);
        }
    }
}
