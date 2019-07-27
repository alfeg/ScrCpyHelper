using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DeviceManager
{
    public class ConsoleExeRunner
    {
        public static string[] Execute(string cmd, string args)
        {
            var info = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var p = Process.Start(info);
            var result = ReadLines(p.StandardOutput).ToArray();
            p.WaitForExit(5000);

            return result;
        }

        public static IEnumerable<string> ReadLines(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}