using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Text;

namespace SharedClasses
{
	public static class WindowMessagesInterop
	{
		#region Example of code (winforms and wpf)
		#region client examples
		//CLIENT
		/*//To get working in winforms need to call WindowMessagesInterop.InitializeClientMessages (in form initialization)
		protected override void WndProc(ref Message m)
		{
			WindowMessagesInterop.MessageTypes mt;
			WindowMessagesInterop.ClientHandleMessage(m.Msg, m.WParam, m.LParam, out mt);
			if (mt == WindowMessagesInterop.MessageTypes.Show)
				this.Show();
			else if (mt == WindowMessagesInterop.MessageTypes.Hide)
				this.Hide();
			else if (mt == WindowMessagesInterop.MessageTypes.Close)
				this.Close();
			else
				base.WndProc(ref m);
		}*/

		/*//To get working in WPF need to call WindowMessagesInterop.InitializeClientMessages (in the window initialization)
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			WindowMessagesInterop.MessageTypes mt;
			WindowMessagesInterop.ClientHandleMessage(msg, wParam, lParam, out mt);
			if (mt == WindowMessagesInterop.MessageTypes.Show)
				this.Show();
			return IntPtr.Zero;
		}*/
		#endregion client examples

		#region application manager examples
		//MANAGER (not client)
		/*//To get application manager working (wpf)
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			string errMsg;
			if (!WindowMessagesInterop.ApplicationManagerHandleMessage(msg, wParam, lParam, out errMsg))
				MessageBox.Show(errMsg);
			return IntPtr.Zero;
		}*/

		/*//To get application manager working (winforms)
		protected override void WndProc(ref Message m)
		{
			string errMsg;
			if (!WindowMessagesInterop.ApplicationManagerHandleMessage(m.Msg, m.WParam, m.LParam, out errMsg))
				MessageBox.Show(errMsg);
		}*/
		#endregion application manager examples
		#endregion Example of code (winforms and wpf)

		#region Constants
		private static readonly TimeSpan CLIENT_NO_POLL_FROM_APPMANAGER_REREGISTER_TIMEOUT = TimeSpan.FromSeconds(3);
		private static readonly TimeSpan CLIENT_DELAY_BEFORE_POLLING_TO_APP_MANAGER = TimeSpan.FromSeconds(0);
		private static readonly TimeSpan CLIENT_INTERVAL_FOR_POLLING_TO_APP_MANAGER = TimeSpan.FromSeconds(0.4);
		private static readonly TimeSpan APPMANAGER_DELAY_BEFORE_POLLING_TO_CLIENTS = TimeSpan.FromSeconds(0);
		private static readonly TimeSpan APPMANAGER_INTERVAL_FOR_POLLING_TO_CLIENTS = TimeSpan.FromSeconds(0.4);
		private static readonly TimeSpan APPMANAGER_IS_ALIVE_TIMEOUT = TimeSpan.FromSeconds(1);
		private static readonly int APPMANAGER_APPID_UNAVAILABLE = -1;
		#endregion Constants

		public enum MessageTypes { None, Poll, Show, Hide, Close };

		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
		[DllImport("user32")]
		private static extern int RegisterWindowMessage(string message);

		private static Timer timerAppManagerPolling;
		private static void timerAppManagerPolling_Tick(object state)
		{
			for (int i = 0; i < RegisteredApplications.Count; i++)
				RegisteredApplications[i].Poll();
		}

		//private static string ClientAppName;
		private static int ClientAppId;
		private static bool SuccessfullyRegistered_Client = false;
		private static Timer timerToRegisterWithAppManager_Client;
		private static DateTime TimeLastPollReceived_Client = DateTime.MinValue;
		private static Timer timerClientPolling;
		private static void timerClientPolling_Tick(object state)
		{
			PostMessage((IntPtr)HWND_BROADCAST, WM_CLIENTPOLLING, (IntPtr)WM_CLIENTPOLLING, (IntPtr)ClientAppId);
		}

		public const int HWND_BROADCAST = 0xFFFF;
		public const int WM_REGISTER = 0xFFEE;
		public const int WM_REGISTRATIONACKNOWLEDGEMENT = 0xEFEF;
		public const int WM_FROMMANAGER = 0xEEFF;
		public const int WM_FROMMANAGER_STRINGSTART = 0xFEEF;
		public const int WM_FROMMANAGER_STRINGINTERMEDIATE = 0xEFFE;
		public const int WM_FROMMANAGER_STRINGEND = 0xEEEE;
		public const int WM_CLIENTPOLLING = 0xFEFE;
		public const int WM_CLIENTRECEIVEDSTRINGMESSAGE = 0xEEEF;//Received a character

		public static void InitializeClientMessages()//string Appname)
		{
			//Process currentProc = Process.GetCurrentProcess();
			//currentProc.Id;
			//ClientAppName = Appname;
			ClientAppId = Process.GetCurrentProcess().Id;//Environment.GetCommandLineArgs()[0];//Appname;
			timerToRegisterWithAppManager_Client = new Timer(
				timerToRegisterWithAppManager_Client_Tick,
				null,
				0,
				1000);
		}

		public static bool ClientHandleMessage(int message, IntPtr wparam, IntPtr lparam, out MessageTypes messageType)
		{
			messageType = MessageTypes.None;
			if (!SuccessfullyRegistered_Client && WindowMessagesInterop.IsRegistrationAcknowlegementFromManager(
				message, wparam, lparam, ClientAppId))
			{
				SuccessfullyRegistered_Client = true;
			}
			else if (WindowMessagesInterop.IsMessageFromManager(message, wparam, lparam, ClientAppId, out messageType))
			{
				if (messageType == WindowMessagesInterop.MessageTypes.Poll)
				{
					TimeLastPollReceived_Client = DateTime.Now;
				}
				else if (messageType != WindowMessagesInterop.MessageTypes.None)
				{
					return true;
				}
			}
			return false;
		}

		private static StringBuilder stringBuilder = new StringBuilder();
		public static bool ClientHandleStringMessage(int message, IntPtr wparam, IntPtr lparam, out string messageString)
		{
			char tmpchar;
			WindowMessagesInterop.CharacterPosition charPos;
			if (WindowMessagesInterop.IsMessageStringFromManager(message, wparam, lparam, ClientAppId, out tmpchar, out charPos))
			{
				messageString = "";
				if (charPos == CharacterPosition.First)
					stringBuilder.Clear();
				stringBuilder.Append(tmpchar);
				if (charPos == CharacterPosition.Last)
				{
					messageString = stringBuilder.ToString();
					stringBuilder.Clear();
				}
				PostMessage((IntPtr)HWND_BROADCAST, WM_CLIENTRECEIVEDSTRINGMESSAGE, (IntPtr)WM_CLIENTRECEIVEDSTRINGMESSAGE, (IntPtr)ClientAppId);

				if (charPos == CharacterPosition.Last)
					return true;
			}
			messageString = null;
			return false;
		}

		private static void timerToRegisterWithAppManager_Client_Tick(object state)//, EventArgs e)
		{
			if (!SuccessfullyRegistered_Client || DateTime.Now.Subtract(TimeLastPollReceived_Client) > CLIENT_NO_POLL_FROM_APPMANAGER_REREGISTER_TIMEOUT)
			{
				WindowMessagesInterop.RegisterWithAppManager();//ClientAppName);//, out ThisAppId);
			}
			//else
			//{
			//}
		}

		public static bool ApplicationManagerHandleMessage(int msg, IntPtr wParam, IntPtr lParam, out string errorMessage)
		{
			string failureReason;
			if (IsPollFromClient_UpdatePollTime(msg, wParam, lParam))
			{
				//Poll received from client
			}
			else if (IsMessageRegistrationRequest_AddToList(msg, wParam, lParam, out failureReason))
			{
				//Client application registered
			}
			else if (IsMessageFromClientSuccessfullyReceivedString(msg, wParam, lParam))
			{
				foreach (RegisteredApp ra in RegisteredApplications)
					if (ra.AppId == lParam.ToInt32())
						ra.successfullyReceivedByClient = true;
			}
			else if (!failureReason.Equals(NotRegistrationMessageText))
			{
				errorMessage = "Unable to register application: " + failureReason;
				return false;
			}
			errorMessage = "";
			return true;
		}

		public static void RegisterWithAppManager()//string AppName)//, out int AppId)
		{
			//int AppId = AppName.GetHashCode();
			//ClientAppId = AppName.GetHashCode();//AppId;
			PostMessage((IntPtr)HWND_BROADCAST, WM_REGISTER, (IntPtr)WM_REGISTER, (IntPtr)ClientAppId);
			if (timerClientPolling == null)
				timerClientPolling = new System.Threading.Timer(
					timerClientPolling_Tick,
					null,
					CLIENT_DELAY_BEFORE_POLLING_TO_APP_MANAGER,
					CLIENT_INTERVAL_FOR_POLLING_TO_APP_MANAGER);
		}

		public static bool BroadcastMessageFromManager(MessageTypes messageType, int appId)
		{
			return WindowMessagesInterop.PostMessage(
					(IntPtr)WindowMessagesInterop.HWND_BROADCAST,
					WindowMessagesInterop.WM_FROMMANAGER,
					(IntPtr)messageType,
					(IntPtr)appId);
		}

		public static bool BroadcastBeginCharacterFromManager(char character, int appId)
		{
			return WindowMessagesInterop.PostMessage(
					  (IntPtr)WindowMessagesInterop.HWND_BROADCAST,
					  WindowMessagesInterop.WM_FROMMANAGER_STRINGSTART,
					  (IntPtr)character,
					  (IntPtr)appId);
		}

		public static bool BroadcastIntermediateCharacterFromManager(char character, int appId)
		{
			return WindowMessagesInterop.PostMessage(
					  (IntPtr)WindowMessagesInterop.HWND_BROADCAST,
					  WindowMessagesInterop.WM_FROMMANAGER_STRINGINTERMEDIATE,
					  (IntPtr)character,
					  (IntPtr)appId);
		}

		public static bool BroadcastEndCharacterFromManager(char character, int appId)
		{
			return WindowMessagesInterop.PostMessage(
					  (IntPtr)WindowMessagesInterop.HWND_BROADCAST,
					  WindowMessagesInterop.WM_FROMMANAGER_STRINGEND,
					  (IntPtr)character,
					  (IntPtr)appId);
		}

		public static RegisteredApp GetRegisteredAppFromList(int AppId)
		{
			foreach (RegisteredApp ra in RegisteredApplications)
				if (ra.AppId == AppId)
					return ra;
			return null;
		}

		public static RegisteredApp GetRegisteredAppFromList(string appName)
		{
			foreach (RegisteredApp ra in RegisteredApplications)
				if (ra.AppName == appName)
					return ra;
			return null;
		}

		public class RegisteredApplicationCollection : ObservableCollection<RegisteredApp>
		{
			[Obsolete("Please use the other overload, which requires the AppId_lparam.", true)]
			public new void Add(RegisteredApp item)
			{
			}

			public void Add(IntPtr AppId_lparam)
			{
				RegisteredApp ra = GetRegisteredAppFromList(AppId_lparam.ToInt32());
				if (ra == null)
				{
					Process tmpproc = Process.GetProcessById(AppId_lparam.ToInt32());
					if (tmpproc != null)
						ra = GetRegisteredAppFromList(tmpproc.ProcessName);
				}

				if (ra == null)
				{
					base.Add(new RegisteredApp(AppId_lparam.ToInt32()));
					if (timerAppManagerPolling == null)
						timerAppManagerPolling = new System.Threading.Timer(
							timerAppManagerPolling_Tick,
							null,
							APPMANAGER_DELAY_BEFORE_POLLING_TO_CLIENTS,
							APPMANAGER_INTERVAL_FOR_POLLING_TO_CLIENTS);
				}
				else
				{
					ra.UpdateAppId(AppId_lparam.ToInt32());
					ra.UpdatePollReceivedTime();
				}
				PostMessage((IntPtr)HWND_BROADCAST, WM_REGISTRATIONACKNOWLEDGEMENT, (IntPtr)WM_REGISTRATIONACKNOWLEDGEMENT, AppId_lparam);
			}

			public void Add(string AppName)
			{
				RegisteredApp ra = GetRegisteredAppFromList(AppName);

				if (ra == null)
				{
					base.Add(new RegisteredApp(AppName));
					if (timerAppManagerPolling == null)
						timerAppManagerPolling = new System.Threading.Timer(
							timerAppManagerPolling_Tick,
							null,
							APPMANAGER_DELAY_BEFORE_POLLING_TO_CLIENTS,
							APPMANAGER_INTERVAL_FOR_POLLING_TO_CLIENTS);
				}
				else
				{
					ra.UpdatePollReceivedTime();
				}
				//PostMessage((IntPtr)HWND_BROADCAST, WM_REGISTRATIONACKNOWLEDGEMENT, (IntPtr)WM_REGISTRATIONACKNOWLEDGEMENT, AppId_lparam);
			}
		}

		public static List<string> PredefinedApplicationList = new List<string>();

		public static void AddPredefinedApplication(string appname)
		{
			if (!PredefinedApplicationList.Contains(appname))
				PredefinedApplicationList.Add(appname);
			RegisteredApplications.Add(appname);
		}

		public const string NotRegistrationMessageText = "Not a registration message.";
		public static RegisteredApplicationCollection RegisteredApplications = new RegisteredApplicationCollection();//ID, Name
		public static bool IsMessageRegistrationRequest_AddToList(int message, IntPtr wparam, IntPtr lparam, out string FailReason)
		{
			bool result = message == WM_REGISTER && wparam.ToInt32() == WM_REGISTER;
			if (result)
			{
				FailReason = "";
				RegisteredApplications.Add(lparam);
				//RegisteredApp ra = GetRegisteredAppFromList(AppId_lparam.ToInt32());
				//if (ra == null)
				//{
				//	Process tmpproc = Process.GetProcessById(AppId_lparam.ToInt32());
				//	if (tmpproc != null)
				//		ra = GetRegisteredAppFromList(tmpproc.ProcessName);
				//}

				//if (ra == null)
				//{
				//	FailReason = null;
				//	RegisteredApplications.Add(new RegisteredApp(AppId_lparam.ToInt32()), AppId_lparam);
				//}
				//else
				//{
				//	ra.UpdateAppId(AppId_lparam.ToInt32());
				//	ra.UpdatePollReceivedTime();
				//	FailReason = "";// "AppId already registered.";
				//}
			}
			else
				FailReason = NotRegistrationMessageText;
			return result;
		}

		public static bool IsMessageFromClientSuccessfullyReceivedString(int message, IntPtr wparam, IntPtr lparam)
		{
			return message == WM_CLIENTRECEIVEDSTRINGMESSAGE
				&& wparam.ToInt32() == WM_CLIENTRECEIVEDSTRINGMESSAGE;
				//&& lparam.ToInt32() == ClientAppId;
		}

		public static bool IsMessageFromManager(int message, IntPtr wparam, IntPtr lparam, int AppId, out MessageTypes messageType)
		{
			messageType = MessageTypes.None;
			bool result = message == WM_FROMMANAGER && lparam.ToInt32() == AppId;
			if (result)
				messageType = (MessageTypes)wparam.ToInt32();
			return result;
		}

		public enum CharacterPosition {First, Intermediate, Last};
		public static bool IsMessageStringFromManager(int message, IntPtr wparam, IntPtr lparam, int AppId, out char character, out CharacterPosition characterPosition)
		{
			bool result = (message == WM_FROMMANAGER_STRINGSTART || message == WM_FROMMANAGER_STRINGINTERMEDIATE || message == WM_FROMMANAGER_STRINGEND) && lparam.ToInt32() == AppId;
			if (result)
			{
				character = (char)wparam;
				characterPosition =
					message == WM_FROMMANAGER_STRINGSTART ? CharacterPosition.First :
					message == WM_FROMMANAGER_STRINGINTERMEDIATE ? CharacterPosition.Intermediate :
					message == WM_FROMMANAGER_STRINGEND ? CharacterPosition.Last :
					CharacterPosition.Last;
				return true;
			}
			character = '\0';
			characterPosition = CharacterPosition.Last;
			return result;
		}

		public static bool IsRegistrationAcknowlegementFromManager(int message, IntPtr wparam, IntPtr lparam, int AppId)
		{
			return message == WM_REGISTRATIONACKNOWLEDGEMENT && wparam.ToInt32() == WM_REGISTRATIONACKNOWLEDGEMENT && lparam.ToInt32() == AppId;
		}

		public static bool IsPollFromClient_UpdatePollTime(int msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == WM_CLIENTPOLLING && wParam.ToInt32() == WM_CLIENTPOLLING)
			{
				foreach (RegisteredApp ra in RegisteredApplications)
					if (lParam.ToInt32() == ra.AppId)
					{
						ra.UpdatePollReceivedTime();
						return true;
					}
			}
			return false;
		}

		public static bool UpdateApplicationName(int AppId, string newName)
		{
			RegisteredApp ra = GetRegisteredAppFromList(AppId);
			if (ra != null)
			{
				ra.UpdateAppName(newName);
				return true;
			}
			else
				return false;
		}

		public class RegisteredApp : INotifyPropertyChanged
		{
			private int _appid;
			public int AppId { get { return _appid; } set { _appid = value; OnPropertyChanged("AppId"); } }

			private string _appname;
			public string AppName { get { return _appname; } set { _appname = value; OnPropertyChanged("AppName"); } }

			private bool _appnametextboxvisible;
			public bool AppNameTextboxVisible { get { return _appnametextboxvisible; } set { _appnametextboxvisible = value; OnPropertyChanged("AppNameTextboxVisible"); } }

			public bool IsAlive
			{
				get
				{
					bool result = DateTime.Now.Subtract(LastPollFromClient) <= APPMANAGER_IS_ALIVE_TIMEOUT;
					if (!result)
					{
						AppId = APPMANAGER_APPID_UNAVAILABLE;
						if (!PredefinedApplicationList.Contains(this.AppName))
							RegisteredApplications.Remove(this);
					}
					return result;
				}
			}

			private DateTime _lastpollfromclient;

			public DateTime LastPollFromClient
			{
				get { return _lastpollfromclient; }
				private set { _lastpollfromclient = value; OnPropertyChanged("LastPollFromClient", "IsAlive"); }
			}

			//public string AppName { get; private set; }
			public RegisteredApp(int AppId)
			{
				this.AppId = AppId;
				Process tmpproc = Process.GetProcessById(AppId);
				this.AppName = tmpproc == null ? "" : tmpproc.ProcessName;
				this.LastPollFromClient = DateTime.Now;
			}

			public RegisteredApp(string AppName)
			{
				this.AppId = -1;
				this.AppName = AppName;
				this.LastPollFromClient = DateTime.MinValue;
			}

			public void UpdateAppName(string newAppName)
			{
				this.AppName = newAppName;
			}

			public void UpdateAppId(int newAppId)
			{
				this.AppId = newAppId;
			}

			public bool BroadCastMessage(MessageTypes messageType)
			{
				bool result = BroadcastMessageFromManager(messageType, AppId);
				if (messageType == MessageTypes.Close && result)
					this.LastPollFromClient = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
				return result;
			}

			public bool successfullyReceivedByClient = false;
			public bool BroadcastStringMessage(string message)
			{
				if (string.IsNullOrWhiteSpace(message) || message.Length < 2)
				{
					//UserMessages.ShowWarningMessage("May not send message to user smaller than 2 characters");
					throw new Exception("May not send message to user smaller than 2 characters");
					//return false;
				}

				successfullyReceivedByClient = false;
				bool result = BroadcastBeginCharacterFromManager(message[0], AppId);
				while (!successfullyReceivedByClient)
				{ }

				for (int i = 1; i < message.Length - 1; i++)
				{
					result = result && BroadcastIntermediateCharacterFromManager(message[i], AppId);
					while (!successfullyReceivedByClient)
					{ }
				}

				result = result && BroadcastEndCharacterFromManager(message[message.Length - 1], AppId);
				while (!successfullyReceivedByClient)
				{ }

				return result;
			}

			public bool StartProcessWithName(out string errorMessageIfFail)
			{
				if (string.IsNullOrWhiteSpace(this.AppName))
				{
					errorMessageIfFail = "Cannot start application with blank name.";
					return false;
				}
				try
				{
					Process.Start(this.AppName);
					errorMessageIfFail = null;
					return true;
				}
				catch (Exception exc)
				{
					errorMessageIfFail = "Exception trying to start application: " + exc.Message;
					return false;
				}

			}

			public void Poll()
			{
				BroadcastMessageFromManager(MessageTypes.Poll, AppId);
				OnPropertyChanged("IsAlive");
			}

			public void UpdatePollReceivedTime()
			{
				LastPollFromClient = DateTime.Now;
			}

			public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
			public void OnPropertyChanged(params string[] propertyNames)
			{
				foreach (string propname in propertyNames)
					PropertyChanged(this, new PropertyChangedEventArgs(propname));
			}
		}

		//public static readonly int HWND_BROADCAST;// = 0xFFFF;
		//private static readonly int WM_MY_MSG;// = RegisterWindowMessage("WM_MY_MSG");
		//public static RegisteredItem Register(string AppName)
		//{
		//    return new RegisteredItem(AppName);
		//}

		//public static void Unregister(RegisteredItem registeredItem)
		//{
		//    RegisteredMessages.Remove(registeredItem);
		//}

		//public static void SendMessageExternal(int RegisteredApp, MessageTypes MessageType)
		//{
		//    RegisteredItem.PostMessage(
		//        (IntPtr)RegisteredApp,
		//        (int)MessageType,
		//        (IntPtr)RegisteredItem.BaseUniquewparam,
		//        (IntPtr)RegisteredItem.Uniquelparam);
		//}

		//private static List<RegisteredItem> RegisteredMessages = new List<RegisteredItem>();
		//public class RegisteredItem
		//{
		//    public static long BaseUniquewparam = 1122334455;
		//    public static long Uniquelparam = 1144332211;

		//    public string AppName;
		//    private int RegisteredApp;
		//    public RegisteredItem(string AppName)
		//    {
		//        this.AppName = AppName;
		//        this.RegisteredApp = RegisterWindowMessage(AppName);
		//        //MessageBox.Show("Registered id = " + this.RegisteredApp);
		//        RegisteredMessages.Add(this);
		//    }

		//    public static MessageTypes GetMessageType(int message, Int64 wparam, Int64 AppId_lparam)
		//    {
		//        MessageTypes result = MessageTypes.None;
		//        if (wparam == BaseUniquewparam && AppId_lparam == Uniquelparam)
		//            result = (MessageTypes)message;
		//        return result;
		//    }

		//    //public void SendMessage(MessageTypes MessageType)
		//    //{
		//    //    PostMessage(
		//    //        (IntPtr)RegisteredMessage,
		//    //        90011 + (int)MessageType,
		//    //        Uniquewparam,
		//    //        Uniquelparam);
		//    //}
		//}
	}
}