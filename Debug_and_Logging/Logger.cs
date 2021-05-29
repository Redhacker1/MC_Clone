using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using File = System.IO.File;
using Path = System.IO.Path;
using Thread = System.Threading.Thread;

namespace MinecraftClone.Debug_and_Logging
{
    public enum LogLevel
    {
        None,
        Info,
        Debug,
        Warning,
        Error,
        Fatal
    }

    public struct LogOutputInfo
    {
        public LogLevel Level;
        public DateTime Time;
        public string Message;
        public bool ForcePrint;
    }
    public class Logger
    {
        readonly object _messageMutex = new object();
        readonly List<LogOutputInfo> _messageQueue = new List<LogOutputInfo>();
        LogLevel _consolePrintLevel;
        readonly Action<string> _printConsoleCallback;
        readonly string _workingFile;

        bool _exitThread;



        public Logger(string filepath,string fileName, Action<string> consolePrintFunction = null)
        {
            SetPrintLevel(LogLevel.Warning);
            _printConsoleCallback = consolePrintFunction ?? GD_print;

            string creationTime = DateTime.Now.ToString(CultureInfo.CurrentCulture).Replace("/", "_").Replace(':', '.');
            GD.Print(creationTime);

            _workingFile = Path.Combine(filepath, fileName +"_"+creationTime+".log");
            File.Create(_workingFile).Close();

            Thread loggingThread = new Thread(ThreadLoop);
            loggingThread.Start();
               
        }

        public void SetPrintLevel(LogLevel minLevel)
        {
            _consolePrintLevel = minLevel;
        }

        public void CloseLogger()
        {
            _exitThread = true;
        }

        void ThreadLoop()
        {
            while(!_exitThread)
            {
                if (_messageQueue.Count == 0) continue;
                LogOutputInfo message;
                lock (_messageMutex)
                {
                    message = _messageQueue[0];
                    _messageQueue.RemoveAt(0);
                }

                StreamWriter file = new StreamWriter(_workingFile, true);

                if (message.Level != LogLevel.None)
                {
                    file.Write($"{message.Time} {message.Level}: {message.Message}\n");
                    file.Close();   
                }
                else
                {
                    file.Write($"{message.Time}: {message.Message}\n");
                }
                if ((int)message.Level >= (int)_consolePrintLevel || message.ForcePrint)
                {
                    _printConsoleCallback(message.Level != LogLevel.None
                        ? $"{message.Time}: {message.Level}: {message.Message}"
                        : $"{message.Time}: {message.Message}");
                }

            }
        }

        public void Log(string message, LogLevel level, bool forcePrintToConsole = false)
        {
            DateTime time = DateTime.Now;
            LogOutputInfo loggerInfo = new LogOutputInfo
            {
                Level = level,
                Message = message,
                Time = time,
                ForcePrint = forcePrintToConsole
            };
            lock(_messageMutex)
            {
                _messageQueue.Add(loggerInfo);
            }

        }


        static void GD_print(string printVal)
        {
            GD.Print(printVal);
        }
    }
}
