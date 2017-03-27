using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
	public class Logger
	{
		static StreamWriter swLog = null;
		static public bool DebugEnabled = true;

		static public void EnableUnicode()
		{
			Console.OutputEncoding = Encoding.Unicode;
			//ConsoleFontEx.SetConsoleFont("SimHei");
		}

		static public void SetLogFile(string filename)
		{
			try
			{
				swLog = File.AppendText(filename);
				swLog.AutoFlush = true;
			}
			catch (Exception e)
			{
				if (swLog != null) swLog.Dispose();
				swLog = null;
				Log("Failed to write to log file: {0} ({1})", filename, e.Message);
			}
		}

		static void LogToFile(string sMsg)
		{
			if (swLog == null) return;

			lock (swLog)
			{
				try
				{
					swLog.WriteLine(sMsg);
				}
				catch (Exception e)
				{
					swLog.Dispose();
					swLog = null;
					Log("Failed to write to log file: {0}", e.Message);
				}
			}
		}

		static void LogWithColor(ConsoleColor textColor, string sFormat, params Object[] args)
		{
			DateTime now = DateTime.Now;
			string sMsg = string.Format(sFormat, args);
			string sTime = now.ToString("yyyy-MM-dd HH.mm.ss.fff");
			string sLog = string.Format("[{0}]  {1}", sTime, sMsg);

			ConsoleColor currColor = Console.ForegroundColor;
			Console.ForegroundColor = textColor;
			Console.WriteLine(sLog);
			Console.ForegroundColor = currColor;
			LogToFile(sLog);
		}

		static public void Log(string sFormat, params Object[] args)
		{
			// INFO level logging
			LogWithColor(ConsoleColor.Green, sFormat, args);
		}

		static public void LogError(string sFormat, params Object[] args)
		{
			// ERROR logging
			LogWithColor(ConsoleColor.Red, sFormat, args);
		}

		static public void LogDebug(string sFormat, params Object[] args)
		{
			if (!DebugEnabled) return;

			// DEBUG level logging
			LogWithColor(ConsoleColor.White, sFormat, args);
		}
	}
}
