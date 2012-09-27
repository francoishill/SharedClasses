using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;
using System.Globalization;
using System.Diagnostics;
using SharedClasses;
#if WINFORMS
using System.Windows.Forms;
#endif

public class Logging
{
#if WINFORMS
	public static NotifyIcon staticNotifyIcon = null;

	public static void appendLogTextbox_OfPassedTextbox(TextBox messagesTextbox, string str, NotifyIcon notifyicon = null)
	{
		NotifyIcon notifyIconToUse = null;
		if (notifyicon != null) notifyIconToUse = notifyicon;
		else if (staticNotifyIcon != null) notifyIconToUse = staticNotifyIcon;
		//label1.Text = str;
		Control topParent = messagesTextbox;
		while (topParent != null && topParent != messagesTextbox.Parent) topParent = messagesTextbox.Parent;
		if (staticNotifyIcon != null) staticNotifyIcon.ShowBalloonTip(3000, "Message in textbox", str, ToolTipIcon.Info);
		
		ThreadingInterop.UpdateGuiFromThread(messagesTextbox, () =>
		{
			messagesTextbox.Text = str + (messagesTextbox.Text.Length > 0 ? Environment.NewLine : "") + messagesTextbox.Text;
		});
		Application.DoEvents();
	}
#endif

	public static void LogErrorShowInNotepad(string errorMessage)
	{
		string tmpFile = Path.GetTempFileName();
		File.WriteAllText(tmpFile, errorMessage);
		System.Diagnostics.Process.Start("notepad", "\"" + tmpFile + "\"");
	}

	private static string GetDateStringFromUsingReportingFrequency(ReportingFrequencies reportingFrequency, DateTime? date = null)
	{
		string dateFormat = "yyyy_MM_dd HH_mm_ss";
		switch (reportingFrequency)
		{
			case ReportingFrequencies.Secondly:
				dateFormat = "yyyy_MM_dd HH_mm_ss";
				break;
			case ReportingFrequencies.Minutely:
				dateFormat = "yyyy_MM_dd HH_mm";
				break;
			case ReportingFrequencies.Hourly:
				dateFormat = "yyyy_MM_dd HH";
				break;
			case ReportingFrequencies.Daily:
				dateFormat = "yyyy_MM_dd";
				break;
		}
		return (date ?? DateTime.Now).ToString(dateFormat);
	}

	#region LogAnyMessages

	[DebuggerDisplay("{Type} at {Date}: {Message}")]
	public class LogMessage
	{
		public DateTime Date;
		public LogTypes Type;
		public string Message;
		public LogMessage(DateTime Date, LogTypes Type, string Message)
		{
			this.Date = Date;
			this.Type = Type;
			this.Message = Message;
		}
		public string GetLineToWriteInFile()
		{
			return string.Format("{0} | {1} | {2}", Date.ToString(cLogMessageDateFormat), Type.ToString().ToLower(), Message);
		}
	}

	public const string cLogMessageDateFormat = "yyyy-MM-dd HH:mm:ss.fff";
	private struct MessageInQueue { public string Filepath; public IEnumerable<LogMessage> Messages; }
	private static ConcurrentQueue<MessageInQueue> messagesToStillLog = new ConcurrentQueue<MessageInQueue>();
	public enum LogTypes { Success, Info, Warning, Error };
	public enum ReportingFrequencies { Secondly, Minutely, Hourly, Daily };
	private static bool isBusyWriting = false;
	public static string LogMessageToFile(List<string> logMessages, LogTypes logType, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		if (logMessages == null || logMessages.Count == 0)
			return null;
		
		DateTime now = DateTime.Now;

		if (SubfolderNameInApplication == null) SubfolderNameInApplication = "";
		SubfolderNameInApplication += (SubfolderNameInApplication.Length > 0 ? "\\" : "") + now.ToString("yyyy MM dd");
		var filename = GetDateStringFromUsingReportingFrequency(reportingFrequency, now) + ".txt";
		var filepath = SettingsInterop.GetFullFilePathInLocalAppdata(filename, ApplicationName, SubfolderNameInApplication, CompanyName, true);

		var fullMessages = logMessages.Select(m => new LogMessage(now, logType, m));
		if (isBusyWriting)
		{
			messagesToStillLog.Enqueue(new MessageInQueue() { Filepath = filepath, Messages = fullMessages });
			return filepath;
		}

		isBusyWriting = true;
		File.AppendAllLines(
			filepath,
			fullMessages.Select(m => m.GetLineToWriteInFile()));

		while (messagesToStillLog.Count > 0)
		{
			MessageInQueue msg;
			while (!messagesToStillLog.TryDequeue(out msg))
			{
				System.Threading.Thread.Sleep(300);
			}
			File.AppendAllLines(msg.Filepath, msg.Messages.Select(m => m.GetLineToWriteInFile()));
		}
		isBusyWriting = false;

		return filepath;
	}
	public static string LogMessageToFile(string logMessage, LogTypes logType, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{
		return LogMessageToFile(new List<string>() { logMessage }, logType, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName);
	}
	#endregion LogAnyMessages

	#region LogSpecificMessages
	public static string LogSuccessToFile(List<string> successMessages, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(successMessages, LogTypes.Success, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }
	public static string LogSuccessToFile(string successMessage, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(successMessage, LogTypes.Success, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }

	public static string LogInfoToFile(List<string> infoMessages, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(infoMessages, LogTypes.Info, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }
	public static string LogInfoToFile(string infoMessage, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(infoMessage, LogTypes.Info, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }

	public static string LogWarningToFile(List<string> warningMessages, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(warningMessages, LogTypes.Warning, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }
	public static string LogWarningToFile(string warningMessages, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(warningMessages, LogTypes.Warning, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }

	public static string LogErrorToFile(List<string> errorMessages, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(errorMessages, LogTypes.Error, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }
	public static string LogErrorToFile(string errorMessage, ReportingFrequencies reportingFrequency, string ApplicationName, string SubfolderNameInApplication = null, string CompanyName = "FJH")
	{ return LogMessageToFile(errorMessage, LogTypes.Error, reportingFrequency, ApplicationName, SubfolderNameInApplication, CompanyName); }
	#endregion LogSpecificMessages

	public static List<LogMessage> GetLoggedMessagesFromFile(string filePath, out List<string> UnparseableLines)
	{
		List<LogMessage> result = new List<LogMessage>();
		UnparseableLines = new List<string>();

		var lines = File.ReadAllLines(filePath);
		foreach (string l in lines)
		{
			int firstPipe = l.IndexOf('|');
			int secondPipe = l.IndexOf('|', firstPipe + 1);
			string chunk1_date = l.Substring(0, firstPipe).Trim();
			string chunk2_logtype = l.Substring(firstPipe + 1, secondPipe - firstPipe - 1).Trim();
			string chunk3_message = l.Substring(secondPipe + 1, l.Length - secondPipe - 1).Trim();

			DateTime datetime;
			Logging.LogTypes logtype;
			if (!DateTime.TryParseExact(chunk1_date, Logging.cLogMessageDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out datetime))
				UnparseableLines.Add(l);
			else if (!Enum.TryParse<Logging.LogTypes>(chunk2_logtype, true, out logtype))
				UnparseableLines.Add(l);
			else
				//Successfully parsed the line
				result.Add(new LogMessage(datetime, logtype, chunk3_message));
		}
		if (UnparseableLines.Count == 0)
			UnparseableLines = null;
		return result;
	}
}