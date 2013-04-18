using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;

namespace SharedClasses
{
	public static class ResourceUsageTracker
	{
		//private static readonly TimeSpan cDelayBeforeInitialCheck = TimeSpan.FromSeconds(10);
		//private static readonly TimeSpan cCheckInterval = TimeSpan.FromSeconds(5);
		//private static readonly TimeSpan cDurationAfterWhichToAutokillIfHighMemoryOrCpu = TimeSpan.FromMinutes(5.0);

		//private const long cMemoryThresholdBytes = 250 * 1024 * 1024;

		private static long GetCurrentMemoryUsageForProcess(Process process)
		{
			return process.PrivateMemorySize64;
		}

		private static double GetCurrentCpuLoadForProcess(Process process)
		{
			double cpuLoad = 0.0;

			process.Refresh();
			if (_previousCheckedTime.HasValue)
			{
				double totalMillisecondsAddedAfterLastCheck = process.TotalProcessorTime.TotalMilliseconds - _previousTotalMilliseconds;
				TimeSpan durationAfterLastCheck = DateTime.Now - _previousCheckedTime.Value;
				double currentTotalCPUload = 100D * (totalMillisecondsAddedAfterLastCheck / durationAfterLastCheck.TotalMilliseconds);
				cpuLoad = currentTotalCPUload / (double)Environment.ProcessorCount;
			}

			_previousCheckedTime = DateTime.Now;
			_previousTotalMilliseconds = process.TotalProcessorTime.TotalMilliseconds;

			return cpuLoad;
		}

		private static HighResourceUsageWindow busyShowingHighMemoryUsageWindow = null;
		private static HighResourceUsageWindow busyShowingHighCpuLoadWindow = null;

		private static DateTime? startTimeOfShowingHighMemoryUsageMessage = null;
		private static bool donotShowHighMemoryUsageMessages = false;
		private static void CheckMemoryUsage(Process process)
		{
			process.Refresh();

			SettingsSimple.HighResourceUsageSettings settings = SettingsSimple.HighResourceUsageSettings.Instance;

			long memoryThresholdBytes = settings.MemoryThreshold_MegaBytes * 1024 * 1024;
			double currentMemoryUsage = GetCurrentMemoryUsageForProcess(process);
			//BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(currentProcess.PrivateMemorySize64)
			if (currentMemoryUsage > memoryThresholdBytes)
			{
				if (!donotShowHighMemoryUsageMessages)
				{
					if (!startTimeOfShowingHighMemoryUsageMessage.HasValue)
					{
						startTimeOfShowingHighMemoryUsageMessage = DateTime.Now;
						ThreadingInterop.DoAction(delegate
						{
							string appname = OwnAppsShared.GetApplicationName();

							var result = HighResourceUsageWindow.ShowHighResourceUsageWindowReturnResult(
								string.Format(
									"WARNING!!! Confirm to terminate application '{0}' (cancel to not show message again).? Current Memory usage is above {1} (current memory usage is {2})."
									+ Environment.NewLine + Environment.NewLine
									+ "Application will automatically exit after {3} minutes if no option is chosen.",
									appname,
									BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(memoryThresholdBytes),
									BytesToHumanfriendlyStringConverter.ConvertBytesToHumanreadableString(process.PrivateMemorySize64),
									((int)Math.Round(settings.DurationToKillIfNoUserResponse_Min)).ToString()),
								"Current time is " + DateTime.Now.ToString("HH:mm:ss"),
								ref busyShowingHighMemoryUsageWindow);

							if (result == HighResourceUsageWindow.ReturnResult.ForceCloseNow)
								OwnAppsShared.ExitAppWithExitCode();
							else if (result == HighResourceUsageWindow.ReturnResult.IgnoreUntilClose)
								donotShowHighMemoryUsageMessages = true;

							startTimeOfShowingHighMemoryUsageMessage = null;
							busyShowingHighMemoryUsageWindow = null;
						},
						false,
						apartmentState: System.Threading.ApartmentState.STA);
					}
					else if (startTimeOfShowingHighMemoryUsageMessage.HasValue)
					{
						if (DateTime.Now - startTimeOfShowingHighMemoryUsageMessage.Value > TimeSpan.FromMinutes(settings.DurationToKillIfNoUserResponse_Min))
							AutoKillNowDueToNoUserResponse(null, currentMemoryUsage);
					}
				}
			}
			else
			{
				startTimeOfShowingHighMemoryUsageMessage = null;
				if (busyShowingHighMemoryUsageWindow != null)
				{
					busyShowingHighMemoryUsageWindow.SetDialogResultToIgnoreOnceAndClose();
					busyShowingHighMemoryUsageWindow = null;
				}
			}
		}

		//private const double cCPUthresholdPercentage = 3.0;//40.0;
		//private const double cWarningSecondsIfAboveCPUThresholdForLongerThan = 10;

		private static Timer _resourceUsageLoggingTimer = null;
		private static Timer _memoryWatcherTimer = null;
		private static DateTime _timerStartedTime = DateTime.Now;
		private static TimeSpan? _delayBeforeStart = null;
		private static DateTime? _previousCheckedTime = null;
		private static double _previousTotalMilliseconds;
		private static int numberConsecutiveTimesCPUabove50 = 0;
		private static DateTime? startTimeOfShowingHighCpuUsageMessage = null;
		private static bool donotShowHighCpuUsageMessages = false;
		private static void CheckCpuLoad(Process process)
		{
			process.Refresh();

			//if (_previousCheckedTime.HasValue)
			//{
			double currentTotalCPUload = GetCurrentCpuLoadForProcess(process);

			SettingsSimple.HighResourceUsageSettings settings = SettingsSimple.HighResourceUsageSettings.Instance;

			//We are currently measuring the CURRENT cpu load, not the AVERAGE
			if (currentTotalCPUload > settings.CpuThreshold_Percentage)
			{
				numberConsecutiveTimesCPUabove50++;
				if (settings.CheckInterval_Sec * (double)numberConsecutiveTimesCPUabove50 > settings.DurationCpuThresholdMustBeOver_Sec)
				{
					if (!donotShowHighCpuUsageMessages)
					{
						if (!startTimeOfShowingHighCpuUsageMessage.HasValue)
						{
							startTimeOfShowingHighCpuUsageMessage = DateTime.Now;
							ThreadingInterop.DoAction(delegate
							{
								string appname = OwnAppsShared.GetApplicationName();
								double secondsTheCpuIsAboveThreshold = settings.CheckInterval_Sec * (double)numberConsecutiveTimesCPUabove50;
								var result = HighResourceUsageWindow.ShowHighResourceUsageWindowReturnResult(
									string.Format(
										"WARNING!!! Confirm to terminate application '{0}' (cancel to not show message again)? Current CPU load is above {1} (current load is {2}) for more than {3} seconds."
										+ Environment.NewLine + Environment.NewLine
										+ "Application will automatically exit after {4} minutes if no option is chosen.",
										appname,
										settings.CpuThreshold_Percentage,
										currentTotalCPUload.ToString("0.##"),
										secondsTheCpuIsAboveThreshold,
										((int)Math.Round(settings.DurationToKillIfNoUserResponse_Min)).ToString()),
									"Current time is " + DateTime.Now.ToString("HH:mm:ss"),
									ref busyShowingHighCpuLoadWindow);
								if (result == HighResourceUsageWindow.ReturnResult.ForceCloseNow)
									OwnAppsShared.ExitAppWithExitCode();
								else if (result == HighResourceUsageWindow.ReturnResult.IgnoreUntilClose)
									donotShowHighCpuUsageMessages = true;

								startTimeOfShowingHighCpuUsageMessage = null;
								busyShowingHighCpuLoadWindow = null;
							},
							false,
							apartmentState: System.Threading.ApartmentState.STA);
						}
						else if (startTimeOfShowingHighCpuUsageMessage.HasValue)
						{
							if (DateTime.Now - startTimeOfShowingHighCpuUsageMessage.Value > TimeSpan.FromMinutes(settings.DurationToKillIfNoUserResponse_Min))
								AutoKillNowDueToNoUserResponse(currentTotalCPUload, null);
						}
					}
				}
			}
			else
			{
				numberConsecutiveTimesCPUabove50 = 0;
				startTimeOfShowingHighCpuUsageMessage = null;
				if (busyShowingHighCpuLoadWindow != null)
				{
					busyShowingHighCpuLoadWindow.SetDialogResultToIgnoreOnceAndClose();
					busyShowingHighCpuLoadWindow = null;
				}
			}

			//Console.WriteLine("CPU load = {0}", currentTotalCPUload);
			//}
		}

		private static void AutoKillNowDueToNoUserResponse(double? cpuLoad, double? memoryUsage)
		{
			var appname = OwnAppsShared.GetApplicationName();
			try
			{
				currentProcess.Refresh();
				var currentCPUload = cpuLoad ?? GetCurrentCpuLoadForProcess(currentProcess);
				var currentMemoryUsage = memoryUsage ?? GetCurrentMemoryUsageForProcess(currentProcess);
				var filePath = Logging.LogWarningToFile(
					string.Format(
						"No user reponse received due to high resources, now killing app '{0}'. CPU load was {1} and Memory Usage was {2}",
						appname, currentCPUload, currentMemoryUsage),
					Logging.ReportingFrequencies.Secondly,
					appname,
					"AutoKills_HighResourceUsages");

				if (filePath != null)
					Process.Start("explorer", string.Format("/select,\"{0}\"", filePath));
			}
			catch (Exception exc)
			{
				Logging.LogErrorToFile(
					string.Format("Could not log AutoKill occurrance for {0}: {1}", appname, exc.Message),
					Logging.ReportingFrequencies.Daily,
					appname);
			}
			OwnAppsShared.ExitAppWithExitCode();
		}

		private const string cLogFilenameDateFormat = @"yyyy-MM-dd HH\hmm";//Not allowing to log more than every second (we do not include the milliseconds)
		private const string cApplicationNameForRecordedResourceUsages = "SharedClasses\\ResourceUsageTracker";
		private static string GetLogLineForCurrentResourceUsage(Process process)
		{
			try
			{
				long memory = GetCurrentMemoryUsageForProcess(process);
				double cpu = GetCurrentCpuLoadForProcess(process);

				bool hadStatus = _lastOperationLoggedForApplication != null;
				string lastStatus = _lastOperationLoggedForApplication;
				_lastOperationLoggedForApplication = null;
				string timeString = DateTime.Now.ToString("HH:mm:ss");
				string result = string.Format("{0},{1},{2},{3}", timeString, memory.ToString(), Math.Round(cpu, 2), hadStatus ? lastStatus : "");
				lastStatus = null;
				return result;
			}
			catch { return null; }
		}

		private const string cLogFileExtensionWithDot = ".log";
		private static string GetFilepathToLogFileNow()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata(
					DateTime.Now.ToString(cLogFilenameDateFormat) + cLogFileExtensionWithDot, cApplicationNameForRecordedResourceUsages, OwnAppsShared.GetApplicationName());
		}

		private static void LogResourceUsageLines(Process process, List<string> lines)
		{
			try
			{
				string filepath = GetFilepathToLogFileNow();

				List<string> fileLines = new List<string>();
				fileLines.Add("Time,Memory,Cpu,LastStatus");
				fileLines.AddRange(lines);

				if (!File.Exists(filepath))
					File.WriteAllLines(filepath, fileLines);

				fileLines.Clear(); fileLines = null;
			}
			catch { }
		}

		private static int recordedCount = 0;
		private static Process currentProcess = null;
		private static List<string> linesToLogForResourceUsages = new List<string>();
		public static void RegisterMemoryAndCpuWatcher()
		{
			currentProcess = Process.GetCurrentProcess();
			DeleteFilesOlderThanNinetyDays();

			SettingsSimple.HighResourceUsageSettings settings = SettingsSimple.HighResourceUsageSettings.Instance;

			_resourceUsageLoggingTimer = new Timer();
			_resourceUsageLoggingTimer.Interval = SettingsSimple.ResourceUsageTracker.Instance.TakeMeasurementIntervalInMilliseconds;
			_resourceUsageLoggingTimer.Tick +=
				delegate
				{
					recordedCount++;
					var min = TimeSpan.FromMilliseconds(recordedCount * SettingsSimple.ResourceUsageTracker.Instance.TakeMeasurementIntervalInMilliseconds)
						.TotalMinutes;
					if (min > 0) { }
					if (TimeSpan.FromMilliseconds(recordedCount * SettingsSimple.ResourceUsageTracker.Instance.TakeMeasurementIntervalInMilliseconds)
						.TotalMinutes < SettingsSimple.ResourceUsageTracker.Instance.FlushToFileIntervalInMinutes)
						linesToLogForResourceUsages.Add(GetLogLineForCurrentResourceUsage(currentProcess));
					else
					{
						if (FlushAllCurrentLogLines())
							recordedCount = 0;
					}
				};
			_resourceUsageLoggingTimer.Start();

			_memoryWatcherTimer = new Timer();
			_delayBeforeStart = TimeSpan.FromSeconds(settings.DelayBeforeInitialCheck_Sec);

			//_memoryWatcherTimer.Interval = (int)settings.DelayBeforeInitialCheck_Sec * 1000;
			_memoryWatcherTimer.Interval = (int)settings.CheckInterval_Sec * 1000;
			_memoryWatcherTimer.Tick +=
				delegate
				{
					if (DateTime.Now.Subtract(_timerStartedTime) >= _delayBeforeStart)
					{
						//_memoryWatcherTimer.Interval = (int)SettingsSimple.HighResourceUsageSettings.Instance.CheckInterval_Sec * 1000;
						CheckMemoryUsage(currentProcess);
						CheckCpuLoad(currentProcess);
					}
				};
			_timerStartedTime = DateTime.Now;
			_memoryWatcherTimer.Start();
		}

		public static bool FlushAllCurrentLogLines()
		{
			if (linesToLogForResourceUsages == null || linesToLogForResourceUsages.Count == 0)
				return true;
			try
			{
				currentProcess.Refresh();
				var tmplist = linesToLogForResourceUsages.Clone();
				linesToLogForResourceUsages.Clear();
				LogResourceUsageLines(currentProcess, tmplist);
				tmplist.Clear(); tmplist = null;
				return true;
			}
			catch { return false; }
		}

		private static void DeleteFilesOlderThanNinetyDays()
		{
			DateTime now = DateTime.Now;
			var dir = Path.GetDirectoryName(GetFilepathToLogFileNow());
			var logFiles = Directory.GetFiles(dir, "*" + cLogFileExtensionWithDot, SearchOption.TopDirectoryOnly);
			foreach (var f in logFiles)
			{
				try
				{
					if (now.Subtract(File.GetLastWriteTime(f)).TotalDays > 90)
						File.Delete(f);
				}
				catch (Exception exc)
				{
					Logging.LogErrorToFile(
						"Unable to delete resource usage log file '" + f + "', error: " + exc.Message,
						Logging.ReportingFrequencies.Daily,
						OwnAppsShared.GetApplicationName(),
						"DeleteFails");
				}
			}
		}

		private static string _lastOperationLoggedForApplication = null;
		public static void LogStartOfOperationInApplication(string descriptionOfStatus)
		{
			_lastOperationLoggedForApplication = descriptionOfStatus;
		}

		private static Dictionary<DateTime, SnapshotOfResourcesUsed> GetRecordedResourceUsages(Func<DateTime, DateTime> dateRounding)
		{
			var tmpDict = new Dictionary<DateTime, SnapshotOfResourcesUsed>();

			string tmpdir = Path.GetDirectoryName(SettingsInterop.GetFullFilePathInLocalAppdata(
				"tmp.txt", cApplicationNameForRecordedResourceUsages, OwnAppsShared.GetApplicationName()));

			var allLogFiles = Directory.GetFiles(tmpdir, "*.log");
			foreach (var logFile in allLogFiles)
			{
				string fileNameOnly = Path.GetFileNameWithoutExtension(logFile);
				DateTime tmpDateTime;
				if (!DateTime.TryParseExact(fileNameOnly, cLogFilenameDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpDateTime))
					continue;

				var fileLines = File.ReadAllLines(logFile);
				if (fileLines.Length < 2)
					continue;
				if (!fileLines[1].Contains(','))
					continue;

				string possibleMemoryBytes = fileLines[1].Split(',')[0];
				string possibleCpuLoadPercentage = fileLines[1].Split(',')[1];

				bool hasLoggedStatus = fileLines.Length >= 3 && !string.IsNullOrWhiteSpace(fileLines[2]);
				string loggedStatus = hasLoggedStatus ? fileLines[2] : null;

				double memoryBytes, cpuLoad;
				if (!double.TryParse(possibleMemoryBytes, out memoryBytes)
					|| !double.TryParse(possibleCpuLoadPercentage, out cpuLoad))
					continue;

				double memoryGB = memoryBytes / (1024 * 1024 * 1024);
				DateTime roundedDate = dateRounding(tmpDateTime);

				if (tmpDict.ContainsKey(roundedDate))
				{
					if (memoryGB > tmpDict[roundedDate].MemoryUsedInGB)
						tmpDict[roundedDate].SetMemoryUsedInGBValue(memoryGB);
					if (cpuLoad > tmpDict[roundedDate].CpuLoadPercentage)
						tmpDict[roundedDate].SetCpuLoadPercentageValue(cpuLoad);
					if (loggedStatus != null && tmpDict[roundedDate].LoggedStatus == null)
						tmpDict[roundedDate].SetLoggedStatus(loggedStatus);
				}
				else
					tmpDict.Add(roundedDate, new SnapshotOfResourcesUsed(roundedDate, memoryGB, cpuLoad, loggedStatus));
			}

			return tmpDict;
		}

		public static void ShowResourceUsageChart()
		{
			TempShowChartsAndExit();

			//var win = new ResourceUsageChart(
			//	() =>
			//	{
			//		var recordedResourceUsages = GetRecordedResourceUsages(
			//		(unroundedDate) =>
			//		{
			//			return unroundedDate;
			//			/*const int cInterval = 5;
			//			int nextMinute = unroundedDate.Minute;
			//			int nextHour = unroundedDate.Hour;
			//			int rem;
			//			Math.DivRem(nextMinute, cInterval, out rem);
			//			if (rem > 0)
			//				nextMinute += cInterval - rem;
			//			if (nextMinute >= 60)
			//			{
			//				nextMinute -= 60;
			//				nextHour += 1;
			//			}
			//			return new DateTime(unroundedDate.Year, unroundedDate.Month, unroundedDate.Day, nextHour, nextMinute, 0);*/
			//		});
			//		var listOfGroupedResources = new List<GroupedResourceUsages>();

			//		DateTime startToday = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

			//		var groupnamesWithRangesToCheck = new Dictionary<string, KeyValuePair<DateTime, DateTime>>();
			//		groupnamesWithRangesToCheck.Add("Last hour", new KeyValuePair<DateTime, DateTime>(DateTime.Now.Subtract(TimeSpan.FromHours(1)), DateTime.Now));
			//		groupnamesWithRangesToCheck.Add("Today", new KeyValuePair<DateTime, DateTime>(startToday, DateTime.Now));
			//		groupnamesWithRangesToCheck.Add("Last 7 days", new KeyValuePair<DateTime, DateTime>(DateTime.Now.Subtract(TimeSpan.FromDays(7)), DateTime.Now));
			//		groupnamesWithRangesToCheck.Add("Last 30 days", new KeyValuePair<DateTime, DateTime>(DateTime.Now.Subtract(TimeSpan.FromDays(30)), DateTime.Now));
			//		//groupnamesWithRangesToCheck.Add("All (might be slow)", new KeyValuePair<DateTime, DateTime>(DateTime.MinValue, DateTime.MaxValue));

			//		foreach (var grpname in groupnamesWithRangesToCheck.Keys)
			//		{
			//			bool outListWasEmpty;
			//			var tmpgroup = new GroupedResourceUsages(
			//				grpname,
			//				groupnamesWithRangesToCheck[grpname],
			//				recordedResourceUsages,
			//				out outListWasEmpty);
			//			if (!outListWasEmpty)
			//				listOfGroupedResources.Add(tmpgroup);
			//		}
			//		return listOfGroupedResources;
			//	});
			//win.ShowDialog();
		}

		//private static string GetLoggedStatusLabels(Dictionary<DateTime, SnapshotOfResourcesUsed> recordedUsages)
		//{
		//	string tmpstr = "";
		//	foreach (var datetime in recordedUsages.Keys)
		//	{
		//		var snapshot = recordedUsages[datetime];
		//		if (string.IsNullOrWhiteSpace(snapshot.LoggedStatus))
		//			continue;
		//		if (tmpstr != "")
		//			tmpstr += "," + Environment.NewLine;
		//		tmpstr += string.Format("'{0}'", snapshot.LoggedStatus);
		//	}
		//	return string.Format("[{0}]", tmpstr);
		//}

		private static string GetMemoryDataPointsAsJavascriptArrayFromRecordedResourceUsages(Dictionary<DateTime, SnapshotOfResourcesUsed> recordedUsages)
		{
			string tmpstr = "";
			foreach (var datetime in recordedUsages.Keys)
			{
				if (tmpstr != "")
					tmpstr += "," + Environment.NewLine;
				var snapshot = recordedUsages[datetime];
				tmpstr += string.Format("[Date.UTC({0}, {1}, {2}, {3}, {4}, {5}), {6}]",
					datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, snapshot.MemoryUsedInGB);
			}
			return tmpstr;
		}

		private static string GetCpuloadDataPointsAsJavascriptArrayFromRecordedResourceUsages(Dictionary<DateTime, SnapshotOfResourcesUsed> recordedUsages)
		{
			string tmpstr = "";
			foreach (var datetime in recordedUsages.Keys)
			{
				if (tmpstr != "")
					tmpstr += "," + Environment.NewLine;
				var snapshot = recordedUsages[datetime];
				if (snapshot.HasLoggedStatus())
				{
					string xval = string.Format("Date.UTC({0}, {1}, {2}, {3}, {4}, {5})", datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second);
					string labelFormatter = "function() { return 'Hai'; }";
					string yval = snapshot.CpuLoadPercentage.ToString();
					tmpstr +=
						"{ dataLabels: { enabled: true, crop: false, align: 'left', x: " + xval
						+ ", verticalAlign: 'middle', formatter: " + labelFormatter
						+ "}, y: " + yval
						+ " }";
				}
				else
					tmpstr += string.Format("[Date.UTC({0}, {1}, {2}, {3}, {4}, {5}), {6}]",
						datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, snapshot.CpuLoadPercentage);
			}
			return tmpstr;
		}

		//private static string GetLoggedstatusLabelsAsJavascriptArrayFromRecordedResourceUsages(Dictionary<DateTime, SnapshotOfResourcesUsed> recordedUsages)
		//{
		//	string tmpstr = "";
		//	foreach (var datetime in recordedUsages.Keys)
		//	{
		//		var snapshot = recordedUsages[datetime];
		//		//if (string.IsNullOrWhiteSpace(snapshot.LoggedStatus))
		//		//	continue;
		//		if (tmpstr != "")
		//			tmpstr += "," + Environment.NewLine;
		//		tmpstr += "'" + (snapshot.LoggedStatus ?? "") + "'";
		//		//tmpstr += string.Format("[Date.UTC({0}, {1}, {2}, {3}, {4}, {5}), {6}]",
		//		//	datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, snapshot.LoggedStatus);
		//	}
		//	return tmpstr;
		//}

		private static void TempShowChartsAndExit()
		{
			var recordedResourceUsages = GetRecordedResourceUsages(dt => dt);

			var dir = Path.Combine(Path.GetTempPath(), "ResourceUsageCharts\\" + OwnAppsShared.GetApplicationName());
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			//var file = Path.Combine(dir, DateTime.Now.ToString(@"yyyy-MM-ss HH_mm_s") + ".html");
			var file = @"C:\Users\Francois\Downloads\Highcharts-3.0.0\examples\spline-irregular-time\tmp.html";

			string applicationName = OwnAppsShared.GetApplicationName();
			string maxMemoryGB = (new ComputerInfo().TotalPhysicalMemory / (1024 * 1024 * 1024)).ToString();
			string memoryDataPoints = GetMemoryDataPointsAsJavascriptArrayFromRecordedResourceUsages(recordedResourceUsages);
			string cpuloadDataPoints = GetCpuloadDataPointsAsJavascriptArrayFromRecordedResourceUsages(recordedResourceUsages);
			//string loggedstatusDataPoints = GetLoggedstatusLabelsAsJavascriptArrayFromRecordedResourceUsages(recordedResourceUsages);

			File.WriteAllText(file,
				@"<!DOCTYPE HTML>
				<html>
					<head>
						<meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
						<title>Resource usage history: " + applicationName + @"</title>

						<script type='text/javascript' src='http://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js'></script>
						<script type='text/javascript'>
				$(function () {
						$('#container').highcharts({
							chart: {
								type: 'line',
								zoomType: 'x',
								spacingRight: 20
							},
							title: {
								text: 'Resource Usage History of " + applicationName + @"'
							},
							subtitle: {
								text: 'Resource usage recorded over time (memory/RAM and CPU load)'
							},
							xAxis: {
								type: 'datetime',
								dateTimeLabelFormats: { // don't display the dummy year
									second: '%H:%M:%S',
									minute: '%H:%M',
									hour: '%H:%M',
									day: '%e. %b',
									week: '%e. %b',
									month: '%b \'%y',
									year: '%Y'
								}
							},
							yAxis: [{ // Primary yAxis
								min: 0,
								max: " + maxMemoryGB + @",
								labels: {
									format: '{value} GB',
									style: {
										color: '#89A54E'
									}
								},
								title: {
									text: 'Memory Usage',
									style: {
										color: '#89A54E'
									}
								}
							}, { // Secondary yAxis
								min: 0,
								max: 105,
								title: {
									text: 'CPU Load',
									style: {
										color: '#4572A7'
									}
								},
								labels: {
									format: '{value} %',
									style: {
										color: '#4572A7'
									}
								},
								opposite: true
							}],
							tooltip: {
								shared: true
							},
            
							series: [{
								name: 'Memory Usage',
								yAxis: 0,
								data: [
									" + memoryDataPoints + @"
								],
								tooltip: {
									valueSuffix: ' GB'
								}
							}, {
								name: 'CPU Load',
								yAxis: 1,
								data: [
									" + cpuloadDataPoints + @"
								],
								tooltip: {
									valueSuffix: ' %'
								}
							}]
						});
					});
    

						</script>
					</head>
					<body>
				<script src='../../js/highcharts.js'></script>
				<script src='../../js/modules/exporting.js'></script>

				<div id='container' style='min-width: 400px; height: 400px; margin: 0 auto'></div>

					</body>
				</html>");

			/*File.WriteAllText(file,
				@"
				<html>
				  <head>
					<script type='text/javascript' src='https://www.google.com/jsapi'></script>
					<script type='text/javascript'>
					  google.load('visualization', '1', {packages:['corechart']});
					  google.setOnLoadCallback(drawChart);
      
					  function drawChart() {
						  var data = new google.visualization.DataTable();
							data.addColumn('datetime', 'Time');
							data.addColumn('number', 'Stock low');
							data.addColumn('number', 'Stock open');
							data.addColumn('number', 'Stock close');
							data.addColumn('number', 'Stock high');

						 data.addRows([
										  [new Date(2008, 1 ,1), 1000, 1000, 1500, 2000],
										  [new Date(2008, 1 ,2), 500, 1000, 1500, 2500],
										  [new Date(2008, 1 ,3), 1000, 1000, 1500, 2000]
							]);

						data.addRows([
							  [new Date(2008, 1 ,1, 00, 00, 00), 1000, 1000, 1500, 2000],
							  [new Date(2008, 1 ,1, 01, 00, 00), 500, 1000, 1500, 2500],
							  [new Date(2008, 1 ,1, 02, 00, 00), 1000, 1000, 1500, 2000]
						]);

						var options = {
						  title: 'Company Performance'
						};

						var chart = new google.visualization.LineChart(document.getElementById('chart_div'));
						chart.draw(data, options);

						//var data = google.visualization.arrayToDataTable([
						//  ['Year', 'Sales', 'Expenses'],
						//  ['2004',  1000,      400],
						//  ['2005',  1170,      460],
						//  ['2006',  660,       1120],
						//  ['2007',  1030,      540]
						//]);
		
						//var options = {
						//  title: 'Company Performance'
						//};

						//var chart = new google.visualization.LineChart(document.getElementById('chart_div'));
						//chart.draw(data, options);
					  }
					</script>
				  </head>
				  <body>
					<div id='chart_div' style='width: 1500px; height: 500px;'></div>
				  </body>
				</html>");*/
			Process.Start(file);
			OwnAppsShared.ExitAppWithExitCode();
		}

		public class GroupedResourceUsages
		{
			public string GroupName { get; private set; }
			public KeyValuePair<DateTime, DateTime> DateRange;
			public List<SnapshotOfResourcesUsed> ListOfResourceUsageSnapshots { get; private set; }

			public GroupedResourceUsages(
				string GroupName,
				KeyValuePair<DateTime, DateTime> DateRange,
				Dictionary<DateTime, SnapshotOfResourcesUsed> originalCompleteList,
				out bool listIsEmpty)
			{
				this.GroupName = GroupName;
				this.DateRange = DateRange;

				this.ListOfResourceUsageSnapshots = new List<SnapshotOfResourcesUsed>();
				foreach (var dateWithResourcesSnapshot in originalCompleteList)
					if (dateWithResourcesSnapshot.Key >= DateRange.Key
						&& dateWithResourcesSnapshot.Key <= DateRange.Value)
						ListOfResourceUsageSnapshots.Add(dateWithResourcesSnapshot.Value);

				listIsEmpty = this.ListOfResourceUsageSnapshots.Count == 0;
			}

			public override string ToString()
			{
				return this.GroupName;
			}
		}

		public class SnapshotOfResourcesUsed
		{
			public DateTime SnapshotTime { get; private set; }
			public double MemoryUsedInGB { get; private set; }
			public double CpuLoadPercentage { get; private set; }
			public string LoggedStatus { get; private set; }
			public SnapshotOfResourcesUsed(DateTime SnapshotTime, double MemoryUsedInGB, double CpuLoadPercentage, string LoggedStatus)
			{
				this.SnapshotTime = SnapshotTime;
				this.MemoryUsedInGB = MemoryUsedInGB;
				this.CpuLoadPercentage = CpuLoadPercentage;
				this.LoggedStatus = LoggedStatus ?? " ";//Need to do this otherwise labels do not work correct in Chart
			}
			public void SetMemoryUsedInGBValue(double newValue) { this.MemoryUsedInGB = newValue; }
			public void SetCpuLoadPercentageValue(double newValue) { this.CpuLoadPercentage = newValue; }
			public void SetLoggedStatus(string newValue) { this.LoggedStatus = newValue; }
			public bool HasLoggedStatus() { return !string.IsNullOrWhiteSpace(this.LoggedStatus); }
		}
	}
}