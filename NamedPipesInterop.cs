using System;
using System.Text;
using System.IO.Pipes;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SharedClasses
{
	public enum PipeMessageTypes { unknown, ClientRegistrationRequest, AcknowledgeClientRegistration, ServerDisconnected, ClientDisconnected/*, ServerPolling, ReceivedPollFromServer */};
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
			//private static Dictionary<NamedPipeServerStream, string> PipeStreamAndNameList = new Dictionary<NamedPipeServerStream, string>();
			private static Dictionary<string, NamedPipeServerStream> PipeStreamAndNameList = new Dictionary<string, NamedPipeServerStream>();
			public ErrorEventHandler OnError = new ErrorEventHandler(delegate { });
			public PipeMessageRecievedEventHandler OnMessageReceived = new PipeMessageRecievedEventHandler(delegate { });

			bool running;
			Thread runningThread;
			EventWaitHandle terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
			public string PipeName { get; set; }

			public NamedPipeServer(string PipeName)
			{
				this.PipeName = PipeName;
				if (serverCheckConnectionsAlive == null)
					serverCheckConnectionsAlive = new Timer(
						delegate
						{
							var keys = new List<string>(PipeStreamAndNameList.Keys);
							//foreach (string name in keys)
							for (int i = 0; i < keys.Count; i++)
								if (!PipeStreamAndNameList[keys[i]].IsConnected)
								{
									PipeStreamAndNameList.Remove(keys[i]);
									OnError(this, new ErrorEventArgs(new Exception("Client disconnected without notice, removed from registered list.")));
								}
						},
						null,
						TimeSpan.FromSeconds(0),
						TimeSpan.FromMilliseconds(500));
			}

			public void SendMessageToClient(PipeMessageTypes messageType, string clientName, string additionalText = null)
			{
				if (PipeStreamAndNameList.ContainsKey(clientName))
					PipeStreamAndNameList[clientName].WriteMessage(messageType.ToString() + (string.IsNullOrWhiteSpace(additionalText) ? "" : ":" + additionalText));
			}

			public void ProcessClientThread(object o)
			{
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
							PipeStreamAndNameList.Add(clientName, pipeStream);
							break;
						}
					}					
					
					message = null;
				}
				while (pipeStream.IsConnected)
				{
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

			public void Run()
			{
				running = true;
				runningThread = new Thread(ServerLoop);
				runningThread.Start();
			}

			public void Stop()
			{
				running = false;
				terminateHandle.WaitOne();
			}

			public void ProcessNextClient()
			{
				try
				{
					NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 254, PipeTransmissionMode.Message);
					pipeStream.WaitForConnection();

					//Spawn a new thread for each request and continue waiting
					Thread t = new Thread(ProcessClientThread);
					t.Start(pipeStream);
				}
				catch (Exception)// e)
				{
					//If there are no more avail connections (254 is in use already) then just keep looping until one is avail
				}
			}
		}

		public class NamedPipeClient
		{
			private bool RegistrationSuccess = false;
			public ErrorEventHandler OnError = new ErrorEventHandler(delegate { });
			public PipeMessageRecievedEventHandler OnMessageReceived = new PipeMessageRecievedEventHandler(delegate { });

			public NamedPipeClient() { }

			public void Start()
			{
				using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", NamedPipesInterop.APPMANAGER_PIPE_NAME, PipeDirection.InOut))
				{
					try
					{
						pipeClient.Connect(1000);
						pipeClient.ReadMode = PipeTransmissionMode.Message;
						pipeClient.WriteMessage(PipeMessageTypes.ClientRegistrationRequest + ":" + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
					}
					catch (Exception exc)
					{
						OnError(this, new ErrorEventArgs(exc));
					}

					while (pipeClient.IsConnected && pipeClient.CanWrite)
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