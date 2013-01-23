using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SharedClasses
{
	public class WindowsServicesInterop<MainFormOrWindowType> : ServiceBase where MainFormOrWindowType : new()
	{
		public static bool RegisterAsService(string DisplayNameForService, StartupTypes StartupType, Action actionToRunApp_IfNotInvokedAsService, Action<string> actionIfError)
		{
			//Throws an exception if has StartupUri
			CheckApplicationTypes.CheckIfWpfAppHasStartupUri(typeof(MainFormOrWindowType));

			if (WindowsServicesInterop <MainFormOrWindowType>.IsInvokedAsService())
				ServiceBase.Run(new WindowsServicesInterop<MainFormOrWindowType>());
			else
			{
				if (!IsThisAppServiceAlreadyInstalled())
				{
					string errIfFail1;
					if (!InstallThisAppAsService(DisplayNameForService, StartupType, out errIfFail1))
					{
						if (actionIfError != null)
							actionIfError(errIfFail1);
						return false;
					}
				}
				if (actionToRunApp_IfNotInvokedAsService != null)
					actionToRunApp_IfNotInvokedAsService();
			}
			return true;
		}

		public static bool DeregisterAsService(Action<string> actionIfError)
		{
			string errIfFail;
			if (!DeleteThisAppAsService(out errIfFail))
				if (actionIfError != null)
				{
					actionIfError(errIfFail);
					return false;
				}
			return true;
		}

		protected override void OnStart(string[] args)
		{
			var argsEnv = Environment.GetCommandLineArgs().ToList();
			if (argsEnv != null)
				argsEnv.RemoveAll(a => a.Equals(ArgumentIfInvokedAsService, StringComparison.InvariantCultureIgnoreCase));

			//File.AppendAllLines(@"C:\Francois\_tmp\tmp.txt", new string[] { "OnStart:" + string.Join("|", argsEnv) });

			ProcessStarter ps = new ProcessStarter(GetThisAppServiceName(), Environment.GetCommandLineArgs()[0], string.Join(" ", argsEnv));
			int procID = ps.Run();
			if (procID != -1)
				procInvokedFromService = Process.GetProcessById(procID);
			else
				procInvokedFromService = Process.Start(Environment.GetCommandLineArgs()[0],
					string.Join(" ", argsEnv.Select(a => "\"" + a.Trim('"') + "\"")));

			threadWaitingForExit = new Thread((ThreadStart)delegate
			{
				if (procInvokedFromService != null)
					procInvokedFromService.WaitForExit();
				Environment.Exit(0);
			});
			threadWaitingForExit.Start();

			//It will probably never be that additional arguments will be passed if invoked via service??
			base.OnStart(args);
		}
		Thread threadWaitingForExit;
		protected override void OnStop()
		{
			//File.AppendAllLines(@"C:\Francois\_tmp\tmp_closed.txt", new string[] { "" });
			if (!procInvokedFromService.CloseMainWindow())//First tries to close main window
				procInvokedFromService.Kill();
			base.OnStop();
		}

		#region Private Methods and Variables

		private static string ArgumentIfInvokedAsService = "invokedasservice";
		private static Process procInvokedFromService;

		private static string GetThisAppServiceName() { return Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]); }

		public enum StartupTypes { Boot, System, Auto, Demand, Disabled, Delayed_Auto };
		private static string GetStartupTypeString(StartupTypes StartupType) { return StartupType.ToString().ToLower().Replace('_', '-'); }
		private static bool InstallThisAppAsService(string DisplayName, StartupTypes StartupType, out string errorIfFailed)
		{
			string ServiceName = GetThisAppServiceName();
			string thisExe = Environment.GetCommandLineArgs()[0];
			//Must have space after equal sign
			string args = string.Format(
				"create {0} binPath= {1} start= {2} DisplayName= \"{3}\"",
					ServiceName,
					"\"" + thisExe + " " + ArgumentIfInvokedAsService + "\"",
					GetStartupTypeString(StartupType),
					DisplayName ?? ServiceName);//"My display name");
			//string args = string.Format("delete {0}",// type= own start= system",
			//    "FrancoisService2");
			string output = RunScExeAndReturnOutput(args);
			if (output.Trim().Equals("[SC] CreateService SUCCESS"))
			{
				errorIfFailed = null;
				return true;
			}
			else
			{
				errorIfFailed = output;
				return false;
			}
		}

		private static bool DeleteThisAppAsService(out string errorIfFailed)
		{
			string ServiceName = GetThisAppServiceName();
			string thisExe = Environment.GetCommandLineArgs()[0];
			string args = string.Format("delete {0}", ServiceName);
			string output = RunScExeAndReturnOutput(args);
			if (output.Trim().Equals("[SC] DeleteService SUCCESS"))
			{
				errorIfFailed = null;
				return true;
			}
			else
			{
				errorIfFailed = output;
				return false;
			}
		}

		private static bool IsThisAppServiceAlreadyInstalled()
		{
			string serviceName = GetThisAppServiceName();
			string feedback = RunScExeAndReturnOutput("query \"" + serviceName + "\"");
			return feedback == null || feedback.IndexOf("service does not exist", StringComparison.InvariantCultureIgnoreCase) == -1;
		}

		private static string RunScExeAndReturnOutput(string commandlineArgs)
		{
			string thisExe = Environment.GetCommandLineArgs()[0];
			var startInfo = new ProcessStartInfo(
				"sc",
				commandlineArgs);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true; ;

			var proc = Process.Start(startInfo);
			string output = proc.StandardOutput.ReadToEnd();
			Logging.LogInfoToFile(
				string.Format("Ran sc.exe with following commanlineArguments: {0}. Ouput received from sc.exe: {1}",
					commandlineArgs,
					output.Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>").Replace("<br/><br/>", "<br/")),
					Logging.ReportingFrequencies.Daily,
					"WindowsServicesInterop");
			//Use the above logs to determine how to interpret all outputs
			return output;
		}

		private static bool IsInvokedAsService()
		{
			var args = Environment.GetCommandLineArgs();
			return args.Length >= 2 && args[1] == ArgumentIfInvokedAsService;
		}

		#endregion Private Methods and Variables
	}

	#region ProcessStarter
	public class ProcessStarter : IDisposable
	{
		#region Import Section

		private static uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		private static uint STANDARD_RIGHTS_READ = 0x00020000;
		private static uint TOKEN_ASSIGN_PRIMARY = 0x0001;
		private static uint TOKEN_DUPLICATE = 0x0002;
		private static uint TOKEN_IMPERSONATE = 0x0004;
		private static uint TOKEN_QUERY = 0x0008;
		private static uint TOKEN_QUERY_SOURCE = 0x0010;
		private static uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
		private static uint TOKEN_ADJUST_GROUPS = 0x0040;
		private static uint TOKEN_ADJUST_DEFAULT = 0x0080;
		private static uint TOKEN_ADJUST_SESSIONID = 0x0100;
		private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
		private static uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);

		private const uint NORMAL_PRIORITY_CLASS = 0x0020;

		private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;


		private const uint MAX_PATH = 260;

		private const uint CREATE_NO_WINDOW = 0x08000000;

		private const uint INFINITE = 0xFFFFFFFF;

		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES
		{
			public int nLength;
			public IntPtr lpSecurityDescriptor;
			public int bInheritHandle;
		}

		public enum SECURITY_IMPERSONATION_LEVEL
		{
			SecurityAnonymous,
			SecurityIdentification,
			SecurityImpersonation,
			SecurityDelegation
		}

		public enum TOKEN_TYPE
		{
			TokenPrimary = 1,
			TokenImpersonation
		}

		public enum WTS_CONNECTSTATE_CLASS
		{
			WTSActive,
			WTSConnected,
			WTSConnectQuery,
			WTSShadow,
			WTSDisconnected,
			WTSIdle,
			WTSListen,
			WTSReset,
			WTSDown,
			WTSInit
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct STARTUPINFO
		{
			public Int32 cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public Int32 dwX;
			public Int32 dwY;
			public Int32 dwXSize;
			public Int32 dwYSize;
			public Int32 dwXCountChars;
			public Int32 dwYCountChars;
			public Int32 dwFillAttribute;
			public Int32 dwFlags;
			public Int16 wShowWindow;
			public Int16 cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct WTS_SESSION_INFO
		{
			public Int32 SessionID;

			[MarshalAs(UnmanagedType.LPStr)]
			public String pWinStationName;

			public WTS_CONNECTSTATE_CLASS State;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern uint WTSGetActiveConsoleSessionId();

		[DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool WTSQueryUserToken(int sessionId, out IntPtr tokenHandle);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr newToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool CreateProcessAsUser(IntPtr token, string applicationName, string commandLine, ref SECURITY_ATTRIBUTES processAttributes, ref SECURITY_ATTRIBUTES threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetLastError();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int WaitForSingleObject(IntPtr token, uint timeInterval);

		[DllImport("wtsapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int WTSEnumerateSessions(System.IntPtr hServer, int Reserved, int Version, ref System.IntPtr ppSessionInfo, ref int pCount);

		[DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

		[DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
		public static extern void WTSFreeMemory(IntPtr memory);

		[DllImport("userenv.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

		#endregion

		public ProcessStarter()
		{

		}

		public ProcessStarter(string processName, string fullExeName)
		{
			processName_ = processName;
			processPath_ = fullExeName;
		}
		public ProcessStarter(string processName, string fullExeName, string arguments)
		{
			processName_ = processName;
			processPath_ = fullExeName;
			arguments_ = arguments;
		}

		public static IntPtr GetCurrentUserToken()
		{
			IntPtr currentToken = IntPtr.Zero;
			IntPtr primaryToken = IntPtr.Zero;
			IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

			int dwSessionId = 0;
			IntPtr hUserToken = IntPtr.Zero;
			IntPtr hTokenDup = IntPtr.Zero;

			IntPtr pSessionInfo = IntPtr.Zero;
			int dwCount = 0;

			WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref dwCount);

			Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

			Int32 current = (int)pSessionInfo;
			for (int i = 0; i < dwCount; i++)
			{
				WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
				if (WTS_CONNECTSTATE_CLASS.WTSActive == si.State)
				{
					dwSessionId = si.SessionID;
					break;
				}

				current += dataSize;
			}

			WTSFreeMemory(pSessionInfo);

			bool bRet = WTSQueryUserToken(dwSessionId, out currentToken);
			if (bRet == false)
			{
				return IntPtr.Zero;
			}

			bRet = DuplicateTokenEx(currentToken, TOKEN_ASSIGN_PRIMARY | TOKEN_ALL_ACCESS, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out primaryToken);
			if (bRet == false)
			{
				return IntPtr.Zero;
			}

			return primaryToken;
		}

		/// <summary>
		/// Returns ProcessID
		/// </summary>
		/// <returns>Returns ProcessID</returns>
		public int Run()
		{

			IntPtr primaryToken = GetCurrentUserToken();
			if (primaryToken == IntPtr.Zero)
			{
				return -1;
			}
			STARTUPINFO StartupInfo = new STARTUPINFO();
			processInfo_ = new PROCESS_INFORMATION();
			StartupInfo.cb = Marshal.SizeOf(StartupInfo);

			SECURITY_ATTRIBUTES Security1 = new SECURITY_ATTRIBUTES();
			SECURITY_ATTRIBUTES Security2 = new SECURITY_ATTRIBUTES();

			string command = "\"" + processPath_ + "\"";
			if ((arguments_ != null) && (arguments_.Length != 0))
			{
				command += " " + arguments_;
			}

			IntPtr lpEnvironment = IntPtr.Zero;
			bool resultEnv = CreateEnvironmentBlock(out lpEnvironment, primaryToken, false);
			if (resultEnv != true)
			{
				int nError = GetLastError();
			}

			CreateProcessAsUser(primaryToken, null, command, ref Security1, ref Security2, false, CREATE_NO_WINDOW | NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT, lpEnvironment, null, ref StartupInfo, out processInfo_);

			DestroyEnvironmentBlock(lpEnvironment);
			CloseHandle(primaryToken);
			return processInfo_.dwProcessId;
		}

		public void Stop()
		{
			Process[] processes = Process.GetProcesses();
			foreach (Process current in processes)
			{
				if (current.ProcessName == processName_)
				{
					current.Kill();
				}
			}
		}

		public int WaitForExit()
		{
			WaitForSingleObject(processInfo_.hProcess, INFINITE);
			int errorcode = GetLastError();
			return errorcode;
		}

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion

		private string processPath_ = string.Empty;
		private string processName_ = string.Empty;
		private string arguments_ = string.Empty;
		private PROCESS_INFORMATION processInfo_;

		public string ProcessPath
		{
			get
			{
				return processPath_;
			}
			set
			{
				processPath_ = value;
			}
		}

		public string ProcessName
		{
			get
			{
				return processName_;
			}
			set
			{
				processName_ = value;
			}
		}

		public string Arguments
		{
			get
			{
				return arguments_;
			}
			set
			{
				arguments_ = value;
			}
		}
	}
	#endregion ProcessStarter
}