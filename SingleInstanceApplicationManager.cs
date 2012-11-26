using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;//System.Runtime.Remoting must be referenced (part of 4.0 client)
using System.Threading;
using System.Security.Permissions;
using System.Reflection;

namespace SharedClasses
{
	/// <summary>
	/// Application Instance Manager
	/// </summary>
	public static class SingleInstanceApplicationManager<MainFormOrWindowType> where MainFormOrWindowType : new()
	{
		private static MainFormOrWindowType mainformOrwindow;
		private static EventHandler<InstanceCallbackEventArgs> callbackInFirstInstanceWhenAnotherStarts;

		/// <summary>
		/// This call should be placed inside the App.xaml.cs OnStartup for WPF app, or in the void Main of a forms/console app.
		/// NB (remember to Environment.Exit(0)) on the window closing event of WPF) The main function which defines the actions for the application
		/// </summary>
		/// <param name="actionBeforeRunning">This could typically be the following for a winforms application: Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);</param>
		/// <param name="actionToRunApplication">Typically for a winforms application: Application.Run(frm);</param>
		/// <param name="actionToPerformOnFirstInstanceWhenSecondInstanceStarts">What should happen to the first started application when a second is called (parameters that are passed via this action are the second one's arguments and the first created form)</param>
		public static MainFormOrWindowType CheckIfAlreadyRunningElseCreateNew(Action<InstanceCallbackEventArgs, MainFormOrWindowType> actionToPerformOnFirstInstanceWhenSecondInstanceStarts, Action<string[], MainFormOrWindowType> actionToStartAppWithFormOrWindowWithCommandlineArguments)
		{
			if (!CreateSingleInstance(
					Assembly.GetExecutingAssembly().GetName().Name,
					actionToPerformOnFirstInstanceWhenSecondInstanceStarts))
				return mainformOrwindow;//default(MainFormOrWindowType);
			mainformOrwindow = new MainFormOrWindowType();
			actionToStartAppWithFormOrWindowWithCommandlineArguments(Environment.GetCommandLineArgs(), mainformOrwindow);
			CheckApplicationTypes.CheckIfWpfAppHasStartupUri(typeof(MainFormOrWindowType));
			return mainformOrwindow;
		}

		//public static void MyHandler(object sender, EventArgs args)
		//{
		//    Console.WriteLine("hi");
		//}

		/// <summary>
		/// Creates the single instance.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="callback">The callback.</param>
		/// <returns></returns>
		private static bool CreateSingleInstance(string name, Action<InstanceCallbackEventArgs, MainFormOrWindowType> actionToPerformOnFirstInstanceWhenSecondInstanceStarts)
		{
			EventWaitHandle eventWaitHandle = null;
			string eventName = string.Format("{0}-{1}", Environment.MachineName, name);

			InstanceProxy.IsFirstInstance = false;
			InstanceProxy.CommandLineArgs = Environment.GetCommandLineArgs();

			try
			{
				// try opening existing wait handle
				eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
			}
			catch
			{
				// got exception = handle wasn't created yet
				InstanceProxy.IsFirstInstance = true;
			}

			if (InstanceProxy.IsFirstInstance)
			{
				// init handle
				eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

				callbackInFirstInstanceWhenAnotherStarts += (sn, ev) =>
				{
					while (mainformOrwindow == null) { }//Wait until main form created
					actionToPerformOnFirstInstanceWhenSecondInstanceStarts(ev, mainformOrwindow);
				};
				// register wait handle for this instance (process)
				ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, WaitOrTimerCallback,
					//callback,
					callbackInFirstInstanceWhenAnotherStarts,
					Timeout.Infinite, false);
				eventWaitHandle.Close();

				// register shared type (used to pass data between processes)
				RegisterRemoteType(name);
			}
			else
			{
				// pass console arguments to shared object
				UpdateRemoteObject(name);

				// invoke (signal) wait handle on other process
				if (eventWaitHandle != null) eventWaitHandle.Set();


				// kill current process
				Environment.Exit(0);
			}

			return InstanceProxy.IsFirstInstance;
		}

		/// <summary>
		/// Updates the remote object.
		/// </summary>
		/// <param name="uri">The remote URI.</param>
		private static void UpdateRemoteObject(string uri)
		{
			// register net-pipe channel
			var clientChannel = new IpcClientChannel();
			ChannelServices.RegisterChannel(clientChannel, true);

			// get shared object from other process
			var proxy =
				Activator.GetObject(typeof(InstanceProxy),
				string.Format("ipc://{0}{1}/{1}", Environment.MachineName, uri)) as InstanceProxy;

			// pass current command line args to proxy
			if (proxy != null)
				proxy.SetCommandLineArgs(InstanceProxy.IsFirstInstance, InstanceProxy.CommandLineArgs);

			// close current client channel
			ChannelServices.UnregisterChannel(clientChannel);
		}

		/// <summary>
		/// Registers the remote type.
		/// </summary>
		/// <param name="uri">The URI.</param>
		private static void RegisterRemoteType(string uri)
		{
			// register remote channel (net-pipes)
			var serverChannel = new IpcServerChannel(Environment.MachineName + uri);
			ChannelServices.RegisterChannel(serverChannel, true);

			// register shared type
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(InstanceProxy), uri, WellKnownObjectMode.Singleton);

			// close channel, on process exit
			Process process = Process.GetCurrentProcess();
			process.Exited += delegate { ChannelServices.UnregisterChannel(serverChannel); };
		}

		/// <summary>
		/// Wait Or Timer Callback Handler
		/// </summary>
		/// <param name="state">The state.</param>
		/// <param name="timedOut">if set to <c>true</c> [timed out].</param>
		private static void WaitOrTimerCallback(object state, bool timedOut)
		{
			// cast to event handler
			var callback = state as EventHandler<InstanceCallbackEventArgs>;
			if (callback == null) return;

			// invoke event handler on other process
			callback(state,
					 new InstanceCallbackEventArgs(InstanceProxy.IsFirstInstance,
												   InstanceProxy.CommandLineArgs));
		}
	}

	/// <summary>
	/// shared object for processes
	/// </summary>
	[Serializable]
	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	internal class InstanceProxy : MarshalByRefObject
	{
		/// <summary>
		/// Gets a value indicating whether this instance is first instance.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is first instance; otherwise, <c>false</c>.
		/// </value>
		public static bool IsFirstInstance { get; internal set; }

		/// <summary>
		/// Gets the command line args.
		/// </summary>
		/// <value>The command line args.</value>
		public static string[] CommandLineArgs { get; internal set; }

		/// <summary>
		/// Sets the command line args.
		/// </summary>
		/// <param name="isFirstInstance">if set to <c>true</c> [is first instance].</param>
		/// <param name="commandLineArgs">The command line args.</param>
		public void SetCommandLineArgs(bool isFirstInstance, string[] commandLineArgs)
		{
			IsFirstInstance = isFirstInstance;
			CommandLineArgs = commandLineArgs;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class InstanceCallbackEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceCallbackEventArgs"/> class.
		/// </summary>
		/// <param name="isFirstInstance">if set to <c>true</c> [is first instance].</param>
		/// <param name="commandLineArgs">The command line args.</param>
		internal InstanceCallbackEventArgs(bool isFirstInstance, string[] commandLineArgs)
		{
			IsFirstInstance = isFirstInstance;
			CommandLineArgs = commandLineArgs;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is first instance.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is first instance; otherwise, <c>false</c>.
		/// </value>
		public bool IsFirstInstance { get; private set; }

		/// <summary>
		/// Gets or sets the command line args.
		/// </summary>
		/// <value>The command line args.</value>
		public string[] CommandLineArgs { get; private set; }
	}
}