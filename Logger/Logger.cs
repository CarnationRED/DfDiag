using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Logger
{
    public static class Logger
    {
        private static readonly Timer zipOldLogsTimer;
        public static readonly Thread thread;
        private static int lastLogDay;
        private static string lastLogFileName;
        private static string lastLogFileFullName;
        private static string logMainDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        private static Queue<string> logs = new Queue<string>();
        private static FileVersionInfo mainVerInfo;
        private static FileInfo mainInfo;
        public static string MainExecutable
        {
            get
            {
                var n1 = Process.GetCurrentProcess().MainModule.FileName;
                if (n1.EndsWith("dotnet.exe"))
                {
                    n1 = Environment.GetCommandLineArgs()[0];
                    if (n1.EndsWith("dotnet.exe"))
                        n1 = Assembly.GetEntryAssembly().Location;
                }
                return n1;
            }
        }
        public static FileVersionInfo MainExecutableVerInfo
        {
            get
            {
                if (mainVerInfo == null)
                {
                    mainVerInfo = FileVersionInfo.GetVersionInfo(MainExecutable);
                }
                return mainVerInfo;
            }
        }
        public static FileInfo MainExecutableInfo
        {
            get
            {
                if (mainInfo == null)
                {
                    mainInfo = new FileInfo(MainExecutable);
                }
                return mainInfo;
            }
        }

        static Logger()
        {
            thread = new Thread(Run) { IsBackground = true };
            thread.IsBackground = true;
            thread.Start();

            zipOldLogsTimer = new Timer((a) =>
            {
                var currentMonthStr = DateTime.Now.ToString("yyyy-MM");
                foreach (var group
                    in
                    new DirectoryInfo(logMainDir)
                    .EnumerateFiles("????-??-??.txt")
                    .GroupBy(f => f.Name.Substring(0, 7))
                    .Where(g => g.Key != currentMonthStr))
                {
                    try
                    {
                        var zipFn = Path.Combine(logMainDir, group.Key + ".zip");
                        if (File.Exists(zipFn))
                            File.Delete(zipFn);
                        using (var zip = ZipFile.Open(zipFn, ZipArchiveMode.Create))
                        {
                            foreach (var file in group)
                                zip.CreateEntryFromFile(file.FullName, file.Name);
                        }
                        foreach (var file in group)
                            file.Delete();
                    }
                    catch (Exception ex)
                    {
                        ex.LogToFile();
                    }
                }

            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            zipOldLogsTimer.Change(TimeSpan.Zero, new TimeSpan(24, 0, 0));
        }
        public static void Log(string log)
        {
            logs.Enqueue($"{DateTime.Now:HH:mm:ss.ff}\t{log}");
        }
        public static void LogToFile(this string log) => Log(log);
        public static void LogToFile(this Exception ex, [CallerMemberName] string caller = null)
        {
            if (ex is AggregateException ae)
                Log($" {caller}: {ae}");
            else
                Log($" {caller}: {ex.Message}\n\t{ex.InnerException}");
        }
        /// <summary>
        /// 
        /// </summary>
        static void Run()
        {
            while (true)
            {
                if (logs.Count > 0)
                {
                    FileStream fs;
                    try
                    {
                        if (!Directory.Exists(logMainDir))
                            Directory.CreateDirectory(logMainDir);
                        var d = DateTime.Now;
                        if (d.Day != lastLogDay || !File.Exists(lastLogFileFullName))
                        {
                            lastLogDay = d.Day;
                            lastLogFileName = $"{d:yyyy-MM-dd}.txt";
                            lastLogFileFullName = Path.Combine(logMainDir, lastLogFileName);
                            if (!File.Exists(lastLogFileFullName))
                            {
                                fs = File.Create(lastLogFileFullName);
                                var info = $"{Path.GetFileNameWithoutExtension(MainExecutableInfo.Name)} log file\n" +
                                    $" Main Executable: {MainExecutable}\n" +
                                    $" Modified: {MainExecutableInfo.CreationTimeUtc}\n" +
                                    $" Product Version: {MainExecutableVerInfo.ProductVersion}\n" +
                                    $" Log Date: {d:yyyy-MM-dd}\n";
                                info += $" OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\n";

                                using (StreamWriter streamWriter = new StreamWriter(fs, Encoding.UTF8))
                                {
                                    streamWriter.WriteLine(info);
                                }
                                fs = File.Open(lastLogFileFullName, FileMode.Append);
                            }
                            else
                                fs = File.Open(lastLogFileFullName, FileMode.Append);
                        }
                        else
                            fs = File.Open(lastLogFileFullName, FileMode.Append);
                        using (var tr = new StreamWriter(fs, Encoding.UTF8))
                        {
                            while (logs.Count > 0)
                                tr.WriteLine(logs.Dequeue());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                Thread.Sleep(3000);
            }
        }
    }
}
