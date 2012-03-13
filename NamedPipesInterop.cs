using System;
using System.Text;
using System.Linq;
using System.IO.Pipes;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections.Specialized;
//using System.Windows.Threading;

namespace SharedClasses
{
	public enum PipeMessageTypes { unknown, ClientRegistrationRequest, AcknowledgeClientRegistration, ServerDisconnected, ClientDisconnected/*, ServerPolling, ReceivedPollFromServer */, Close, Show, Hide };
	public delegate void PipeMessageRecievedEventHandler(object sender, PipeMessageRecievedEventArgs e);
	public class PipeMessageRecievedEventArgs : EventArgs
	{
		public PipeMessageTypes MessageType;
		public string AdditionalText;
		public PipeMessageRecievedEventArgs(PipeMessageTypes MessageType, string AdditionalText)
		{
			this.MessageType = MessageType;
			this.AdditionalText = AdditionalText;
		}
	}

	public class MTObservableCollection<T> : ObservableCollection<T>
	{
		public override event NotifyCollectionChangedEventHandler CollectionChanged;
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			var eh = CollectionChanged;
			if (eh != null)
			{
				Dispatcher dispatcher = (from NotifyCollectionChangedEventHandler nh in eh.GetInvocationList()
										 let dpo = nh.Target as DispatcherObject
										 where dpo != null
										 select dpo.Dispatcher).FirstOrDefault();

				if (dispatcher != null && dispatcher.CheckAccess() == false)
				{
					dispatcher.Invoke(DispatcherPriority.DataBind, (Action)(() => OnCollectionChanged(e)));
				}
				else
				{
					foreach (NotifyCollectionChangedEventHandler nh in eh.GetInvocationList())
						nh.Invoke(this, e);
				}
			}
		}
	}

	public class NamedPipesInterop
	{
		public const string APPMANAGER_PIPE_NAME = "Application Manager Pipe Name";

		public static bool GetPipeMessageTypeFromString(string messageTypeString, out PipeMessageTypes pipeMessageType, out string additionalTextOrError)
		{
			if (string.IsNullOrWhiteSpace(messageTypeString))
			{
				pipeMessageType = PipeMessageTypes.unknown;
				additionalTextOrError = "Cannot cast blank string to PipeMessageTypes.";
				return false;
			}
			if (Enum.TryParse<PipeMessageTypes>(messageTypeString, true, out pipeMessageType))
			{
				additionalTextOrError = null;
				return true;
			}
			pipeMessageType = PipeMessageTypes.unknown;
			foreach (PipeMessageTypes pmt in Enum.GetValues(typeof(PipeMessageTypes)))
				if (messageTypeString.StartsWith(pmt.ToString(), StringComparison.InvariantCultureIgnoreCase))
				{
					pipeMessageType = pmt;
					additionalTextOrError = messageTypeString.Substring(pmt.ToString().Length);
					return true;
				}
			pipeMessageType = PipeMessageTypes.unknown;
			additionalTextOrError = string.Format("Unable to cast string '{0}' to PipeMessageTypes.", messageTypeString);
			return false;
		}

		private static Timer serverCheckConnectionsAlive;

		public class NamedPipeServer
		{
			//public static ObservableCollection<ClientApplication> ConnectedClientApplications = new ObservableCollection<ClientApplication>();
			//public static ClientApplicationCollection ConnectedClientApplications = new ClientApplicationCollection();
			public static MTObservableCollection<ClientApplication> ConnectedClientApplications = new MTObservableCollection<ClientApplication>();

			public ErrorEventHandler OnError = new ErrorEventHandler(delegate { });
			public PipeMessageRecievedEventHandler OnMessageReceived = new PipeMessageRecievedEventHandler(delegate { });

			bool running;
			Thread runningThread;
			EventWaitHandle terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
			public string PipeName { get; set; }

			public NamedPipeServer(
				string PipeName,
				Action<ErrorEventArgs> ActionOnError,
				Action<PipeMessageRecievedEventArgs, NamedPipeServer> ActionOnMessageReceived)
			{
				this.PipeName = PipeName;

				if (serverCheckConnectionsAlive == null)
					serverCheckConnectionsAlive = new Timer(
						delegate
						{
							lock (this)
							{
								if (ConnectedClientApplications != null)
									for (int i = ConnectedClientApplications.Count - 1; i >= 0; i--)
									{
										var tmpclientApp = ConnectedClientApplications[i];
										if (tmpclientApp.PipeStream != null)
											tmpclientApp.PipeStream.WriteMessage("Polling");
										if (tmpclientApp.PipeStream != null && !tmpclientApp.PipeStream.IsConnected)
										{
											var tmpclientapp = ConnectedClientApplications[i];
											ConnectedClientApplications.RemoveAt(i);
											tmpclientapp.OnPropertyChanged("IsAlive");
											OnError(this, new ErrorEventArgs(new Exception("Client disconnected without notice, removed from registered list.")));
										}
									}
							}
						},
						null,
						TimeSpan.FromSeconds(0),
						TimeSpan.FromMilliseconds(500));
				OnError += (s, e) => { ActionOnError(e); };
				OnMessageReceived += (s, m) => { ActionOnMessageReceived(m, this); };
			}

			public void SendMessageToClient(PipeMessageTypes messageType, string clientName, string additionalText = null)
			{
				foreach (ClientApplication ca in ConnectedClientApplications.Where(ca => ca.PipeName.Equals(clientName, StringComparison.InvariantCultureIgnoreCase)))
					ca.PipeStream.WriteMessage(messageType.ToString() + (string.IsNullOrWhiteSpace(additionalText) ? "" : ":" + additionalText));
			}

			//public void ProcessClientThread(IAsyncResult ar)
			public void ProcessClientThread(object o)
			{
				//NamedPipeServerStream pipeStream = (NamedPipeServerStream)ar.AsyncState;
				NamedPipeServerStream pipeStream = (NamedPipeServerStream)o;

				while (pipeStream.IsConnected)
				{
					string message;
					bool readSuccess = pipeStream.ReadMessage(out message);

					PipeMessageTypes messageType;
					string additionalText;
					if (readSuccess && GetPipeMessageTypeFromString(message, out messageType, out additionalText))
					{
						//TODO: Only currently allows registration request messages
						if (messageType == PipeMessageTypes.ClientRegistrationRequest)
						{
							string clientName = additionalText.TrimStart(':');
							OnMessageReceived(this, new PipeMessageRecievedEventArgs(messageType, additionalText));
							pipeStream.WriteMessage(PipeMessageTypes.AcknowledgeClientRegistration.ToString());
							var newclientApp = new ClientApplication(clientName, pipeStream);
							ConnectedClientApplications.Add(newclientApp);
							break;
						}
					}

					message = null;
				}
				while (pipeStream.IsConnected)
				{
					if (!running)
						break;
				}
				OnMessageReceived(this, new PipeMessageRecievedEventArgs(PipeMessageTypes.ClientDisconnected, "Client disconnected"));
			}

			void ServerLoop()
			{
				while (running)
				{
					ProcessNextClient();
				}

				terminateHandle.Set();
			}

			public NamedPipeServer Start()
			{
				running = true;
				runningThread = new Thread(ServerLoop);
				runningThread.Start();
				return this;
			}

			public void Stop()
			{
				running = false;
				//terminateHandle.WaitOne();
			}

			private NamedPipeServerStream lastPipeStream;
			public void ProcessNextClient()
			{
				try
				{
					NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 254, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
					lastPipeStream = pipeStream;
					var asyncResult = pipeStream.BeginWaitForConnection((ar) =>
					{
						Thread t = new Thread(ProcessClientThread);
						t.Start(ar.AsyncState);
					},
					pipeStream);

					if (asyncResult.AsyncWaitHandle.WaitOne(1000))
					{
						pipeStream.EndWaitForConnection(asyncResult);
						return;
					}
					pipeStream.Close();
					pipeStream.Dispose();
					pipeStream = null;
				}
				catch (Exception)// e)
				{
					//If there are no more avail connections (254 is in use already) then just keep looping until one is avail
				}
			}

			//public class ClientApplicationCollection : ObservableCollection<ClientApplication>
			//{
			//	public new void Add(ClientApplication item)
			//	{
			//		if (item == null)
			//			return;

			//		if (this.Count > 0)
			//		{
			//			var tmpitem = this.First(
			//				ca =>
			//					ca.PipeName.Equals(item.PipeName, StringComparison.InvariantCultureIgnoreCase)
			//					&& ca.PipeStream == null);
			//			if (tmpitem != null)
			//				tmpitem.PipeStream = item.PipeStream;
			//			else
			//				base.Add(item);
			//		}
			//		else
			//			base.Add(item);
			//	}
			//}

			public class ClientApplication : INotifyPropertyChanged
			{
				private string _pipename;
				public string PipeName { get { return _pipename; } private set { _pipename = value; OnPropertyChanged("PipeName"); } }
				private NamedPipeServerStream _pipestream;
				public NamedPipeServerStream PipeStream { get { return _pipestream; } set { _pipestream = value; OnPropertyChanged("PipeStream"); } }
				private bool _appnametextboxvisible;
				public bool AppNameTextboxVisible { get { return _appnametextboxvisible; } set { _appnametextboxvisible = value; OnPropertyChanged("AppNameTextboxVisible"); } }
				public bool IsAlive { get { return PipeStream != null && PipeStream.IsConnected; } }

				public ClientApplication(string PipeName, NamedPipeServerStream PipeStream)
				{
					this.PipeName = PipeName;
					this.PipeStream = PipeStream;
				}

				public void SendMessage(PipeMessageTypes messageType)
				{
					if (PipeStream == null)
						return;
					PipeStream.WriteMessage(messageType.ToString());
				}

				public bool StartProcessWithName(out string errorMessageIfFail)
				{
					if (string.IsNullOrWhiteSpace(this.PipeName))
					{
						errorMessageIfFail = "Cannot start application with blank name.";
						return false;
					}
					try
					{
						Process.Start(this.PipeName);
						errorMessageIfFail = null;
						return true;
					}
					catch (Exception exc)
					{
						errorMessageIfFail = "Exception trying to start application: " + exc.Message;
						return false;
					}

				}

				public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
				public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
			}
		}

		public class NamedPipeClient
		{
			private bool RegistrationSuccess = false;
			public ErrorEventHandler OnError = new ErrorEventHandler(delegate { });
			public PipeMessageRecievedEventHandler OnMessageReceived = new PipeMessageRecievedEventHandler(delegate { });
			public bool ForceCancelRetryLoop = false;

			public NamedPipeClient(
				Action<ErrorEventArgs> ActionOnError,
				Action<PipeMessageRecievedEventArgs> ActionOnMessageReceived)
			{
				OnError += (s, e) => { ActionOnError(e); };
				OnMessageReceived += (s, m) => { ActionOnMessageReceived(m); };
			}

			public static NamedPipeClient StartNewPipeClient(Action<ErrorEventArgs> ActionOnError,
				Action<PipeMessageRecievedEventArgs> ActionOnMessageReceived)
			{
				return new NamedPipeClient(ActionOnError, ActionOnMessageReceived).Start();
			}

			public NamedPipeClient Start()
			//public void Start()
			{
				//Timer clientCheckConnectionsAlive = null;

				Thread th = new Thread(() =>
				{
					using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", NamedPipesInterop.APPMANAGER_PIPE_NAME, PipeDirection.InOut))
					{
					retryconnect:
						try
						{
							pipeClient.Connect(1000);
							pipeClient.ReadMode = PipeTransmissionMode.Message;
							pipeClient.WriteMessage(PipeMessageTypes.ClientRegistrationRequest + ":" + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
						}
						catch (Exception exc)
						{
							if (exc.Message == "The operation has timed out.")
								if (!ForceCancelRetryLoop)
									goto retryconnect;
							OnError(this, new ErrorEventArgs(exc));
						}


						//clientCheckConnectionsAlive = new Timer(
						//    delegate
						//    {
						//        if (!pipeClient.IsConnected)
						//        {
						//            OnError(this, new ErrorEventArgs(new Exception("Server disconnected without notice")));
						//        }
						//    },
						//    null,
						//    TimeSpan.FromSeconds(0),
						//    TimeSpan.FromMilliseconds(500));

						while (pipeClient.IsConnected && pipeClient.CanWrite && !ForceCancelRetryLoop)
						{
							string tmpMessage;
							if (pipeClient.ReadMessage(out tmpMessage))
							{
								PipeMessageTypes messageType;
								string additionalText;
								if (GetPipeMessageTypeFromString(tmpMessage, out messageType, out additionalText))
								{
									if (messageType == PipeMessageTypes.AcknowledgeClientRegistration)
										RegistrationSuccess = true;
									//if (messageType == PipeMessageTypes.ServerPolling)
									//    pipeClient.WriteMessage(PipeMessageTypes.ReceivedPollFromServer.ToString());
									OnMessageReceived(this, new PipeMessageRecievedEventArgs(messageType, additionalText));
								}
								else if (!string.IsNullOrWhiteSpace(tmpMessage))
									OnMessageReceived(this, new PipeMessageRecievedEventArgs(PipeMessageTypes.unknown, tmpMessage));
							}
						}
						OnMessageReceived(this, new PipeMessageRecievedEventArgs(PipeMessageTypes.ServerDisconnected, "Server disconnected"));
					}
					//clientCheckConnectionsAlive.Dispose();
				});
				th.Start();
				return this;
			}
		}
	}

	public static class NamedPipesExtensions
	{
		public static Encoding encoding = Encoding.UTF8;
		private static Decoder decoder = encoding.GetDecoder();

		public static bool ReadMessage(this PipeStream pipeStream, out string messageOrErrorOut)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				try
				{
					byte[] bytes = new byte[1024];
					if (!pipeStream.IsConnected)
					{
						messageOrErrorOut = "Server disconnected without notice";
						return false;
					}
					int numread = pipeStream.Read(bytes, 0, 1024);
					sb.Append(encoding.GetString(bytes, 0, numread));
				}
				catch (Exception exc)
				{
					Console.WriteLine("Exception reading message: " + exc.Message);
					messageOrErrorOut = exc.Message;
					return false;
				}
			}
			while (!pipeStream.IsMessageComplete);
			messageOrErrorOut = sb.ToString();
			return true;
		}

		public static bool WriteMessage(this PipeStream pipeStream, string message, out string errorMessage)
		{
			try
			{
				byte[] bytes = encoding.GetBytes(message);
				pipeStream.Write(bytes, 0, bytes.Length);
				pipeStream.Flush();
				pipeStream.WaitForPipeDrain();
				errorMessage = null;
				return true;
			}
			catch (Exception exc)
			{
				errorMessage = exc.Message;
				return false;
			}
		}

		public static bool WriteMessage(this PipeStream pipeStream, string message)
		{
			string tmpstr;
			return pipeStream.WriteMessage(message, out tmpstr);
		}

		//public static IEnumerable<string> GetMessages(
		//    this PipeStream pipeStream)//NamedPipeClientStream pipeStream)
		//{
		//    const int BufferSize = 256;
		//    byte[] bytes = new byte[BufferSize];
		//    char[] chars = new char[BufferSize];
		//    int numBytes = 0;
		//    StringBuilder msg = new StringBuilder();
		//    do
		//    {
		//        msg.Length = 0;
		//        do
		//        {
		//            numBytes = pipeStream.Read(bytes, 0, BufferSize);
		//            if (numBytes > 0)
		//            {
		//                int numChars = decoder.GetCharCount(bytes, 0, numBytes);
		//                decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
		//                msg.Append(chars, 0, numChars);
		//            }
		//        } while (numBytes > 0 && !pipeStream.IsMessageComplete);
		//        decoder.Reset();
		//        if (numBytes > 0)
		//        {
		//            // we've got a message - yield it!
		//            yield return msg.ToString();
		//        }
		//    } while (numBytes != 0);
		//}
	}
}