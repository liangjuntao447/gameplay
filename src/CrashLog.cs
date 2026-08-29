using System;
using System.IO;

namespace TouchCloudPad
{
    /// <summary>
    /// Writes unhandled-exception details to crash.log next to the exe so the
    /// user (or developer) can see what went wrong even when the window dies.
    /// </summary>
    internal static class CrashLog
    {
        private static string Path
        {
            get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"); }
        }

        public static void Write(Exception ex)
        {
            try
            {
                File.AppendAllText(Path,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]\r\n" +
                    (ex == null ? "(null exception)" : ex.ToString()) +
                    "\r\n----------------------\r\n");
            }
            catch { }
        }
    }
}
