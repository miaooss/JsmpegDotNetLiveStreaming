
using System;

namespace LiveStreamingWebRTC.Logger
{
    public class Logger : ILogger
    {
        public void Error(string message)
        {
            Console.WriteLine($"Error::{message}");
        }

        public void Info(string message)
        {
            Console.WriteLine($"Info::{message}");
        }
    }
}
