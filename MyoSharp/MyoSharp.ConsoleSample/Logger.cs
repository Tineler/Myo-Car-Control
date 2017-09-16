using System;
using System.Configuration;

namespace MyoSharp.ConsoleSample
{
    public class Logger
    {
        private readonly bool _isDebug;
        public Logger()
        {
            _isDebug = ConfigurationManager.AppSettings["debugMode"] == "True";
        }

        public void Debug(string message)
        {
            if (_isDebug)
            {
                Console.WriteLine(message);
            }
        }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }
    }
}
