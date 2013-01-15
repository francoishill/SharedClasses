using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Timers;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SharedClasses
{
	public class WrapperForReportList
	{
		public List<WindowsMonitor.WindowTimes> ListOfReports;
		public WrapperForReportList() { }
		public WrapperForReportList(List<WindowsMonitor.WindowTimes> ListOfReports)
		{
			this.ListOfReports = ListOfReports;
		}
	}

	public class WindowsMonitor
	{
		public static bool IsFrancoisPC = Directory.Exists(@"c:\users\FrancoisLaptopDell") || Directory.Exists(@"c:\users\Francois");

		[DebuggerDisplay("IdleSeconds = {IdleSeconds}, TotalSeconds = {TotalSeconds}")]
		public class WindowTimes
		{
			public string WindowTitle { get; set; }
			public string ProcessPath { get; set; }
			public Dictionary<DateTime, DateTime> IdleTimes { get; set; }
			public Dictionary<DateTime, DateTime> TotalTimes { get; set; }
			public long IdleSeconds { get { return (long)this.IdleTimes.Sum(dateDur => dateDur.Value != DateTime.MinValue ? (dateDur.Value.Subtract(dateDur.Key).TotalSeconds) : 0); } }
			public long TotalSeconds { get { return (long)this.TotalTimes.Sum(dateDur => dateDur.Value != DateTime.MinValue ? (dateDur.Value.Subtract(dateDur.Key).TotalSeconds) : 0); } }
			public int IdleTimesCount { get { return IdleTimes != null ? IdleTimes.Count : 0; } }
			public int TotalTimesCount { get { return TotalTimes != null ? TotalTimes.Count : 0; } }
			//public long IdleSeconds { get; set; }
			//public long TotalSeconds { get; set; }

			public List<string> IdleTimeStrings
			{
				get
				{
					return
						this.IdleTimes
						.Where(kv => kv.Value != DateTime.MinValue)
						.Select(kv => kv.Key.ToString("HH:mm:ss") + " - " + kv.Value.ToString("HH:mm:ss") + " (total seconds = " + kv.Value.Subtract(kv.Key).TotalSeconds + ")")
						.ToList();
				}
			}
			public List<string> TotalTimeStrings
			{
				get
				{
					return
						this.TotalTimes
						.Where(kv => kv.Value != DateTime.MinValue)
						.Select(kv => kv.Key.ToString("HH:mm:ss") + " - " + kv.Value.ToString("HH:mm:ss") + " (total seconds = " + kv.Value.Subtract(kv.Key).TotalSeconds + ")")
						.ToList();
				}
			}

			public WindowTimes() { }//Must be here for json parser to create it
			public WindowTimes(string WindowTitle, string ProcessPath)
			{
				this.WindowTitle = WindowTitle;
				this.ProcessPath = ProcessPath;
				this.IdleTimes = new Dictionary<DateTime, DateTime>();
				this.TotalTimes = new Dictionary<DateTime, DateTime>();
				//this.IdleSeconds = 0;
				//this.TotalSeconds = 0;
			}
		}

		public const string cNullWindowTitle = "[NULLWINDOWTITLE]";
		public const string cNullFilePath = "[NULLFILEPATH]";
		public readonly static TimeSpan cMinimumIdleDuration = TimeSpan.FromSeconds(5);

		private DateTime StartTime;
		private Dictionary<string, WindowTimes> ActivatedWindowsAndTimes;
		private Timer ticker;
		private bool tickerRunning = false;

		string _activeWindowTitle;
		string _activeWindowModuleFilepath;
		string _activeWindowString;

		private string lastWindowString = null;
		private string lastIdleWindowString = null;
		bool isbusy = false;

		//private long idleoffset = 0;
		public WindowsMonitor(Action<string> actionOnActiveWindowTitleChanged, Action<DateTime, TimeSpan> actionOnLastinfoObtained_systemstartup_idleduration = null)
		{
			this.StartTime = DateTime.Now;
			ActivatedWindowsAndTimes = new Dictionary<string, WindowTimes>(StringComparer.InvariantCultureIgnoreCase);

			ticker = new Timer();
			ticker.Interval = 500;//1000;
			ticker.AutoReset = true;
			ticker.Elapsed += delegate
			{
				DateTime systemStartupTime;
				TimeSpan idleDuration;
				if (Win32Api.GetLastInputInfo(out systemStartupTime, out idleDuration))
				{
					actionOnLastinfoObtained_systemstartup_idleduration(systemStartupTime, idleDuration);

					if (isbusy)
						return;

					isbusy = true;
					var now = DateTime.Now;
					//IntPtr handle = GetForegroundWindow();
					//if (handle != IntPtr.Zero)
					//{
					_activeWindowTitle = GetActiveWindowTitle() ?? cNullWindowTitle;
					_activeWindowModuleFilepath = GetActiveWindowModuleFilePath() ?? cNullFilePath;
					_activeWindowString = _activeWindowTitle + "|" + _activeWindowModuleFilepath;
					//if (activeWindowTitle != null && activeWindowModuleFilepath != null)
					//{

					//TransparentWindowActiveTitle.ShowWindow();
					actionOnActiveWindowTitleChanged("Active: " + _activeWindowTitle);

					if (!ActivatedWindowsAndTimes.ContainsKey(_activeWindowString))
						ActivatedWindowsAndTimes.Add(_activeWindowString, new WindowTimes(_activeWindowTitle, _activeWindowModuleFilepath));
					var winTimes = ActivatedWindowsAndTimes[_activeWindowString];

					if (!_activeWindowString.Equals(lastWindowString, StringComparison.InvariantCultureIgnoreCase))
					{
						//var lastTime = ActivatedWindowsAndTimes[lastWindowTitle].TotalTimes.Last().Key;
						//ActivatedWindowsAndTimes[lastWindowTitle].TotalTimes[lastTime] = now;
						//ActivatedWindowsAndTimes[lastWindowTitle].IdleTimes[lastTime] = now;

						lastWindowString = _activeWindowString;

						winTimes.TotalTimes.Add(now, DateTime.MinValue);
						//if (ActivatedWindowsAndTimes.ContainsKey(_activeWindowString))
						//    idleoffset = ActivatedWindowsAndTimes[_activeWindowString].IdleSeconds;

					}

					//if (!ActivatedWindowsAndTimes.ContainsKey(_activeWindowString))
					//    ActivatedWindowsAndTimes.Add(_activeWindowString, new WindowTimes(_activeWindowTitle, _activeWindowModuleFilepath));


					var lastStartTime = winTimes.TotalTimes.Last().Key;
					winTimes.TotalTimes[lastStartTime] = now;//(winTimes.TotalTimes[lastStartTime] ?? TimeSpan.FromMilliseconds(0)) + TimeSpan.FromMilliseconds(ticker.Interval);

					if (idleDuration.TotalSeconds > WindowsMonitor.cMinimumIdleDuration.TotalSeconds)
					{
						if (!_activeWindowString.Equals(lastIdleWindowString, StringComparison.InvariantCultureIgnoreCase))
						{
							lastIdleWindowString = _activeWindowString;
							winTimes.IdleTimes.Add(now.Subtract(idleDuration), DateTime.MinValue);
						}

						var lastIdleStart = winTimes.IdleTimes.Last().Key;
						winTimes.IdleTimes[lastIdleStart] = now;//(winTimes.IdleTimes[lastStartTime] ?? TimeSpan.FromMilliseconds(0)) + idleTime;
					}
					else
					{
						//if (lastIdleWindowString != null)
						//{
						//    var idlestart = ActivatedWindowsAndTimes[lastIdleWindowString].IdleTimes.First().Key;
						//    ActivatedWindowsAndTimes[lastIdleWindowString].IdleTimes[idlestart] = now;
						//}
						lastIdleWindowString = null;
					}

					//if (idleTime.TotalSeconds > 1)
					//    ActivatedWindowsAndTimes[_activeWindowString].IdleSeconds = idleoffset + (long)idleTime.TotalSeconds;
					//ActivatedWindowsAndTimes[_activeWindowString].TotalSeconds += (long)(ticker.Interval / (double)1000);

					//}
					//MessageBox.Show("Active window: " + GetActiveWindowTitle());
					//}

					isbusy = false;
				}
			};
			StartTicker();
		}

		//private static int? recordingDeviceNumber = null;
		public static string TempRecordingDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TestHoursWorkedCalculator_recordings");
		//public static List<string> UnmoveableFiles = new List<string>();
		public static string tmpRecordingPath = null;
		//public static string newSaveToDirectory = null;
		//private static AudioRecorder recorder;
		//private static bool wasRecorderStoppedByCode_otherwiseautomaticafter1minunte = false;
		//private static bool StoppedEventAlreadySetOnRecorder = false;
		static bool actionAlreadySet = false;
		private static void StartRecordingAudio()
		{
			int deviceCount = WinMMinterop.GetRecordingDeviceCount();

			if (deviceCount == 0)
			{
				UserMessages.ShowWarningMessage("Cannot start recording, no audio devices found.");
				return;
			}
			/*if (deviceCount == 1)
				recordingDeviceNumber = 0;

			if (deviceCount > 1 && !recordingDeviceNumber.HasValue)
			{
				List<string> devices1 = new List<string>();
				foreach (var device in WinMMinterop.GetRecordingDevicesNames())
					devices1.Add(device);

				bool foundInFile = false;
				if (File.Exists(ReportWindow.SelectedRecordingDeviceFilepath))
				{
					for (int i = 0; i < devices1.Count; i++)
						if (devices1[i].Trim().Equals(File.ReadAllText(ReportWindow.SelectedRecordingDeviceFilepath).Trim(), StringComparison.InvariantCultureIgnoreCase))
						{
							recordingDeviceNumber = i;
							foundInFile = true;
							break;
						}
				}
				if (!foundInFile)
				{
					//ThreadingInterop.PerformOneArgFunctionSeperateThread((devices_in) =>
					//{
					//List<string> devices = devices_in as List<string>;
					//string pickedDevice = PickItemWPF.PickItem<string>(devices, "Please pick a recording device", devices[0]);
					//int pickedIndex = devices.IndexOf(pickedDevice);
					string pickedDevice = PickItemWPF.PickItem<string>(devices1, "Please pick a recording device", devices1[0]);
					int pickedIndex = devices1.IndexOf(pickedDevice);
					if (pickedIndex != -1)
					{
						recordingDeviceNumber = pickedIndex;
						File.WriteAllText(ReportWindow.SelectedRecordingDeviceFilepath, pickedDevice);
						//StopRecordingAudio(false);
						//StartRecordingAudio();
					}
					//},
					//devices1,
					//false,
					//apartmentState: System.Threading.ApartmentState.STA);
					else
					{
						UserMessages.ShowWarningMessage("No devices picked, using first device = " + devices1[0]);
						recordingDeviceNumber = 0;
					}
				}
			}*/

			if (!actionAlreadySet)
			{
				WinMMinterop.Recorder.Instance.SetAction((err) => { UserMessages.ShowErrorMessage(err); });
				actionAlreadySet = true;
			}
			WinMMinterop.Recorder.Instance.SetQuality(WinMMinterop.Recorder.Qualities._09_Bytespersec44100_Alignment2_Bitspersample16_Samplespersec22050_Channels1);
			//WinMMinterop.Recorder.Instance.SetDeviceNumberToUse(recordingDeviceNumber ?? 0);

			if (tmpRecordingPath == null)
			{
				tmpRecordingPath = System.IO.Path.Combine(TempRecordingDir, Guid.NewGuid().ToString() + ".wav");
				string tmpdir = System.IO.Path.GetDirectoryName(tmpRecordingPath);
				if (!Directory.Exists(tmpdir))
					Directory.CreateDirectory(tmpdir);
			}

			try
			{
				if (IsFrancoisPC)
					WinMMinterop.Recorder.Instance.StartRecording();
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("Unable to start recording: " + exc.Message);
			}

			//if (recorder == null)
			//{
			//    recorder = new VoiceRecorder.Audio.AudioRecorder()
			//    {
			//        RecordingFormat = new NAudio.Wave.WaveFormat(44100, 1)//WaveIn.GetCapabilities(recordingDeviceNumber ?? 0).Channels)
			//    };
			//    recorder.Stopped += new EventHandler(recorder_Stopped);
			//}
			//if (recorder.RecordingState != RecordingState.Monitoring && recorder.RecordingState != RecordingState.Recording)
			//{
			//    recorder.BeginMonitoring(recordingDeviceNumber ?? 0);
			//    recorder.BeginRecording(tmpRecordingPath);

			//    //if (!StoppedEventAlreadySetOnRecorder)
			//    //{
			//    //    StoppedEventAlreadySetOnRecorder = true;
			//    //    recorder.Stopped += new EventHandler(recorder_Stopped);
			//    //}
			//}
		}

		//private static void recorder_Stopped(object sender, EventArgs e)
		//{
		//    //bool autoStoppedAfter1minute = false;
		//    //if (!wasRecorderStoppedByCode_otherwiseautomaticafter1minunte)
		//    //    autoStoppedAfter1minute = true;
		//    //wasRecorderStoppedByCode_otherwiseautomaticafter1minunte = false;
		//    if (tmpRecordingPath != null)
		//    {
		//        if (recorder.RecordedTime.TotalSeconds < 2)
		//        {
		//            try
		//            {
		//                if (File.Exists(tmpRecordingPath))
		//                    File.Delete(tmpRecordingPath);
		//            }
		//            catch (Exception exc)
		//            {
		//                UserMessages.ShowWarningMessage("Unable to delete temp wave recording (audio shorter than 2 seconds): " + exc.Message);
		//            }
		//        }
		//        else 
		//        if (newSaveToDirectory != null)
		//        {
		//            try
		//            {
		//                //File.Move(tmpRecordingPath, System.IO.Path.Combine(newSaveToDirectory, System.IO.Path.GetFileName(tmpRecordingPath)));
		//                MoveAndConvertToMP3(tmpRecordingPath, newSaveToDirectory);
		//                newSaveToDirectory = null;
		//            }
		//            catch (Exception exc)
		//            {
		//                UserMessages.ShowWarningMessage("Unable to move temp wave recording (1): " + exc.Message);
		//            }
		//            while (UnmoveableFiles.Count > 0)
		//            {
		//                try
		//                {
		//                    //File.Move(UnmoveableFiles[0], System.IO.Path.Combine(newSaveToDirectory, System.IO.Path.GetFileName(UnmoveableFiles[0])));
		//                    MoveAndConvertToMP3(UnmoveableFiles[0], newSaveToDirectory);
		//                }
		//                catch (Exception exc)
		//                {
		//                    UserMessages.ShowWarningMessage("Unable to move temp wave recording (2): " + exc.Message);
		//                }
		//                //UnmoveableFiles.RemoveAt(0);
		//            }
		//        }
		//        tmpRecordingPath = null;//Always mark as null so new recording can start
		//        //File.AppendAllLines(@"C:\Users\francois\AppData\Local\Temp\TestHoursWorkedCalculator_recordings\tmp.txt", new string[] { DateTime.Now.ToString("HH:mm:ss.fff") + ": " + recorder.RecordingState.ToString() });
		//        if (autoStoppedAfter1minute)
		//            StartRecordingAudio();
		//        /*else
		//        {
		//            try
		//            {
		//                if (UserMessages.Confirm("No path was specified to move the temp recorded file to, delete file?"))
		//                    File.Delete(tmpRecordingPath);
		//                else
		//                    Process.Start("explorer", "/select,\"" + tmpRecordingPath + "\"");
		//                tmpRecordingPath = null;
		//                newSaveToPath = null;
		//            }
		//            catch (Exception exc)
		//            {
		//                UserMessages.ShowWarningMessage("Could not delete recorded file: " + exc.Message);
		//            }
		//        }*/
		//    }
		//}

		private static void ConvertToMp3(string originalWavPath, string newMP3path)
		{
			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;      // No Command Prompt window.
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.FileName =
				System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "lame.exe");
			psi.Arguments = "-b 128 --resample 22.05 -m j " +
							"\"" + originalWavPath + "\"" + " " +
							"\"" + newMP3path + "\"";
			Process p = Process.Start(psi);
			p.WaitForExit();
			p.Close();
			p.Dispose();
			File.SetLastWriteTime(newMP3path, File.GetLastWriteTime(originalWavPath));
			File.SetCreationTime(newMP3path, File.GetCreationTime(originalWavPath));
		}

		public static void MoveAndConvertToMP3(string originalPath, string newSavetoDir)
		{
			try
			{
				string destinationPath = System.IO.Path.Combine(newSavetoDir, System.IO.Path.GetFileName(originalPath));
				string mp3path = System.IO.Path.ChangeExtension(destinationPath, ".mp3");
				File.Move(originalPath, destinationPath);

				//////At this stage not converting to MP3 as it becomes larger
				ConvertToMp3(destinationPath, mp3path);
				File.Delete(destinationPath);

				//if (UnmoveableFiles.Contains(originalPath))
				//    UnmoveableFiles.Remove(originalPath);
			}
			catch (Exception exc)
			{
				throw exc;
			}
		}
		//public static void MoveAllTempRecordedWavFiles(string newSavetoDir)
		//{
		//    foreach (var file in Directory.GetFiles(TempRecordingDir, "*.wav"))
		//    {
		//        try { MoveAndConvertToMP3(file, newSavetoDir); }// File.Move(file, System.IO.Path.Combine(newSavetoDir, System.IO.Path.GetFileName(file))); }
		//        catch { WindowsMonitor.UnmoveableFiles.Add(file); }
		//    }
		//}

		private void StartTicker()
		{
			ticker.Start();
			if (IsFrancoisPC)
				StartRecordingAudio();
			tickerRunning = true;
		}
		public static void StopRecordingAudio()//bool preventAutoRestart = true)
		{
			//if (recorder != null)
			//{
			//    if (preventAutoRestart)
			//        wasRecorderStoppedByCode_otherwiseautomaticafter1minunte = true;
			//    recorder.Stop();
			//}

			if (IsFrancoisPC)
				WinMMinterop.Recorder.Instance.StopAndSave(tmpRecordingPath);
			tmpRecordingPath = null;

			//if (SndRec.Instance.Status != SndStatus.Uninitialized)
			//{
			//    SndRec.Instance.Stop();
			//    SndRec.Instance.Save(tmpRecordingPath);
			//    SndRec.Instance.Dispose();
			//    tmpRecordingPath = null;
			//}
		}
		public void Stop()
		{
			if (!tickerRunning)
				return;

			ticker.Stop();
			StopRecordingAudio();
			tickerRunning = false;
			var now = DateTime.Now;

			if (lastWindowString != null && ActivatedWindowsAndTimes.ContainsKey(lastWindowString))
			{
				var wintimes = ActivatedWindowsAndTimes[lastWindowString];
				if (wintimes.TotalTimes.Count > 0)
				{
					var lastTime = wintimes.TotalTimes.Last().Key;
					wintimes.TotalTimes[lastTime] = now;
				}
			}
			lastWindowString = null;

			if (lastIdleWindowString != null && ActivatedWindowsAndTimes.ContainsKey(lastIdleWindowString))
			{
				var wintimesidle = ActivatedWindowsAndTimes[lastIdleWindowString];
				if (wintimesidle.IdleTimes.Count > 0)
				{
					var lastIdleTime = wintimesidle.IdleTimes.Last().Key;
					wintimesidle.IdleTimes[lastIdleTime] = now;
				}
			}
			lastIdleWindowString = null;
		}
		public bool StopAndGetReport(out Dictionary<string, WindowTimes> activatedWindowsList)
		{
			Stop();
			return GetReport(out activatedWindowsList);
		}
		public void Restart()
		{
			StartTicker();
		}

		public bool GetReport(out Dictionary<string, WindowTimes> activeWindowsList)
		{
			activeWindowsList = ActivatedWindowsAndTimes
				.OrderBy(kv => -kv.Value.TotalTimes.Sum(dateDur => dateDur.Value != DateTime.MinValue ? (dateDur.Value.Subtract(dateDur.Key).TotalSeconds) : 0))
				.ToDictionary(kv => kv.Key, kv => kv.Value);
			return true;
		}

		public static string GetActiveWindowTitle()
		{
			const int nChars = 256;
			IntPtr handle = IntPtr.Zero;
			StringBuilder Buff = new StringBuilder(nChars);
			handle = GetForegroundWindow();

			if (GetWindowText(handle, Buff, nChars) > 0)
			{
				return Buff.ToString();
			}
			return null;
		}

		public static string GetActiveWindowModuleFilePath()
		{
			const int nChars = 256;
			IntPtr handle = IntPtr.Zero;
			StringBuilder Buff = new StringBuilder(nChars);
			handle = GetForegroundWindow();

			if (GetWindowModuleFileName(handle, Buff, nChars) > 0)
			{
				return Buff.ToString();
			}
			return null;
		}

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint GetWindowModuleFileName(IntPtr hwnd, StringBuilder lpszFileName, uint cchFileNameMax);

		public static ObservableCollection<WindowsMonitor.WindowTimes> LoadReportsFromJson(string filepath, int filterByMinimumSeconds, List<string> GroupingWindowTitlesBySubstring)
		{
			try
			{
				JSON.SetDefaultJsonInstanceSettings();
				if (!File.Exists(filepath))
				{
					UserMessages.ShowErrorMessage("File does not exist: " + filepath);
					return null;
				}
				string jsontext = FixAllDates(File.ReadAllText(filepath));
				var tmpWrappedList = new WrapperForReportList();
				JSON.Instance.FillObject(tmpWrappedList, jsontext);

				var tmplist = new ObservableCollection<WindowsMonitor.WindowTimes>(tmpWrappedList.ListOfReports);
				PopulateList(ref tmplist, filterByMinimumSeconds, GroupingWindowTitlesBySubstring);

				return tmplist;/*new ObservableCollection<WindowsMonitor.WindowTimes>(
				tmplist
				.OrderBy(it => -it.TotalTimes.Sum(dateDur => dateDur.Value != DateTime.MinValue ? (dateDur.Value.Subtract(dateDur.Key).TotalSeconds) : 0)));*/
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage(
					"Error loading report file: " + Environment.NewLine + exc.Message
					+ Environment.NewLine + Environment.NewLine + "Stacktrace:" + Environment.NewLine + exc.StackTrace);
				return null;
			}
		}

		public static void PopulateList(ref ObservableCollection<WindowsMonitor.WindowTimes> wintimes, int minimumSeconds, List<string> GroupingWindowTitlesBySubstring)
		{
			if (wintimes == null) return;

			//var tmplist = tmpWrappedList.ListOfReports;
			//var groupingLines = textboxGroupingOfWindowTitles.Text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			if (GroupingWindowTitlesBySubstring != null)
				foreach (var grp in GroupingWindowTitlesBySubstring)
				{
					WindowsMonitor.WindowTimes wintime = null;
					for (int i = wintimes.Count - 1; i >= 0; i--)
					{
						if (wintimes[i].WindowTitle.IndexOf(grp, StringComparison.InvariantCultureIgnoreCase) != -1)
						{
							if (wintime == null)
								wintime = new WindowsMonitor.WindowTimes("... " + grp + " ...", WindowsMonitor.cNullFilePath);
							foreach (var tottime in wintimes[i].TotalTimes)
								if (!wintime.TotalTimes.ContainsKey(tottime.Key))
									wintime.TotalTimes.Add(tottime.Key, tottime.Value);
							foreach (var idletime in wintimes[i].IdleTimes)
								if (!wintime.IdleTimes.ContainsKey(idletime.Key))
									wintime.IdleTimes.Add(idletime.Key, idletime.Value);
							wintimes.RemoveAt(i);
						}
					}
					if (wintime != null)
						wintimes.Add(wintime);
				}

			wintimes = new ObservableCollection<WindowsMonitor.WindowTimes>(
				wintimes
				.OrderBy(it => -it.TotalTimes.Sum(dateDur => dateDur.Value != DateTime.MinValue ? (dateDur.Value.Subtract(dateDur.Key).TotalSeconds) : 0))
				.Where(wt => wt.TotalSeconds >= minimumSeconds));
		}

		public static void SaveReportsToJsonAndHtmlAndRecordedWave(ObservableCollection<WindowsMonitor.WindowTimes> reportList, string jsonFilepath, string htmlFilepath, string recordingSaveToDirectory)
		{
			if (reportList == null || reportList.Count == 0)
			{
				UserMessages.ShowWarningMessage("There are no reports to save");
				return;
			}

			//Save report here via JSON
			JSON.SetDefaultJsonInstanceSettings();
			if (File.Exists(jsonFilepath))
				File.Delete(jsonFilepath);
			File.WriteAllText(jsonFilepath,
				//JSON.Instance.Beautify(Leave the beautify for now, it messes up the date strings and cannot be read back
				JSON.Instance.ToJSON(new WrapperForReportList(reportList.ToList()), false)
				//)
				);

			string htmltext = "";
			htmltext += "<html>";
			htmltext += "<head>";

			htmltext += "<style>";
			htmltext += "td { vertical-align: top; }";
			htmltext += ".title{ color: blue; }";
			htmltext += ".totaltime{ color: green; }";
			htmltext += ".idletime{ color: orange; }";
			htmltext += ".fullpath{ color: gray; font-size: 10px; }";
			htmltext += "</style>";

			htmltext += "</head>";
			htmltext += "<body>";

			htmltext += "<table cellspacing='0' border='1'>";
			htmltext += "<thead><th>Window Title</th><th>Total seconds</th><th>Idle seconds</th><th>Fullpath</th></thead>";

			foreach (var rep in reportList)
				htmltext +=
					"<tr>" +
					string.Format(
						"<td class='title'>{0}</td><td class='totaltime'>{1}</td><td class='idletime'>{2}</td><td class='fullpath'>{3}</td>",
						rep.WindowTitle,
						string.Join("<br/>", rep.IdleTimes.Select(idl => idl.Key.ToString("yyyy-MM-dd HH:mm:ss") + " for " + (idl.Value != DateTime.MinValue ? (idl.Value.Subtract(idl.Key).TotalSeconds) : 0) + " seconds")),
						string.Join("<br/>", rep.TotalTimes.Select(idl => idl.Key.ToString("yyyy-MM-dd HH:mm:ss") + " for " + (idl.Value != DateTime.MinValue ? (idl.Value.Subtract(idl.Key).TotalSeconds) : 0) + " seconds")),
						rep.ProcessPath)
					+ "</tr>";

			htmltext += "</table>";
			htmltext += "</body></html>";
			File.WriteAllText(htmlFilepath, htmltext);

			if (WindowsMonitor.IsFrancoisPC)
			{
				//foreach (var file in Directory.GetFiles(WindowsMonitor.TempRecordingDir, "*.wav"))
				//{
				//    try { File.Move(file, System.IO.Path.Combine(newSavetoDir, System.IO.Path.GetFileName(file))); }
				//    catch { WindowsMonitor.UnmoveableFiles.Add(file); }
				//}
				//WindowsMonitor.MoveAllTempRecordedWavFiles(newSavetoDir);
				//WindowsMonitor.newSaveToDirectory = newSavetoDir;
				//WindowsMonitor.StopRecordingAudio();
				//if (WindowsMonitor.tmpRecordingPath != null)
				foreach (var file in Directory.GetFiles(WindowsMonitor.TempRecordingDir, "*.wav"))
					WindowsMonitor.MoveAndConvertToMP3(file, recordingSaveToDirectory);
			}

			Process.Start("explorer", "/select,\"" + htmlFilepath + "\"");
		}

		/// <summary>
		/// Fixes a single (already trimmed) incorrect date string (caused by the JSON beautify method)
		/// </summary>
		/// <param name="incorrectdatestring">The date string to fix</param>
		/// <returns>Returns the fixed date string</returns>
		private static string FixDateString(string incorrectdatestring)
		{
			string tmpnospaces =
				incorrectdatestring							//2012-08-0208 : 19 : 52		(length = 22)
				.Replace(" ", "");					//2012-08-0208:19:52
			return tmpnospaces.Insert(10, " ");		//2012-08-02 08:19:52			(length = 19)
		}
		/// <summary>
		/// This function is to fix the incorrect datestrings (caused by the JSON beautify method)
		/// Wrong date format:	2012-08-0208 : 19 : 52	(regular expression: [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9][0-9] : [0-9][0-9] : [0-9][0-9])
		/// Correct date format:	2012-08-02 08:19:52		(regular expression: [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9])
		/// </summary>
		/// <param name="incorrectText">This text can contain more than one incorrect date</param>
		/// <returns>Returns the text with all the date strings fixed</returns>
		private static string FixAllDates(string incorrectText)
		{
			//Search for format 2012-08-0208 : 19 : 52
			MatchCollection matches = Regex.Matches(incorrectText, "[0-9]{4}-[0-9]{2}-[0-9]{4} : [0-9]{2} : [0-9]{2}", RegexOptions.Multiline);
			if (matches.Count == 0)
				return incorrectText;
			else
			{
				List<string> listToReplace = new List<string>();
				foreach (Match m in matches)
					if (!listToReplace.Contains(m.Value))
						listToReplace.Add(m.Value);
				string finalstr = incorrectText;
				foreach (string repl in listToReplace)
					finalstr = finalstr.Replace(repl, FixDateString(repl));
				return finalstr;
			}
		}
	}
}