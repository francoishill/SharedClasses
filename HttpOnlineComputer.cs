using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace SharedClasses
{
	public enum ConnectionTypes { In, Out };

	public sealed class RequestDetails
	{
		public GenericHttpResponse Response;
		public ConnectionTypes ConnectionType;
		public Queue<OnlineCommands> CommandsQueue;
		public RequestDetails(GenericHttpResponse Response, ConnectionTypes ConnectionType, Queue<OnlineCommands> CommandsQueue)
		{
			this.Response = Response;
			this.ConnectionType = ConnectionType;
			this.CommandsQueue = CommandsQueue;
		}
	}

	public abstract class HttpOnlineComputer
	{
		public enum GuidChanges { Added, Removed };
		public enum ProgressState { Show, Hide };

		protected static Action<string> actionOnStatus;
		protected static Action<ProgressState> actionOnProgressState;
		protected static Action<int> actionOnProgressPercentage;
		protected static Action<Guid, GuidChanges> actionOnGuidAddedOrRemoved;

		private const int cBufferSizes = 2048;

		//private static byte[] buffer = new byte[2048];
		public static Dictionary<Guid, Dictionary<GenericHttpRequest, RequestDetails>> currentConnections = new Dictionary<Guid, Dictionary<GenericHttpRequest, RequestDetails>>();
		public static Dictionary<Guid, Queue<OnlineCommands>> receivedCommandsQueue = new Dictionary<Guid, Queue<OnlineCommands>>();
		private static Dictionary<GenericHttpRequest, byte[]> byteBufferForeachRequest = new Dictionary<GenericHttpRequest, byte[]>();
		//private Dictionary<Guid, Queue<OnlineServerCommands>> currentlyHookedGuidsWithCommandToSendQueue = new Dictionary<Guid, Queue<OnlineServerCommands>>();
		protected static List<Thread> threadsForListening = new List<Thread>();
		protected static bool mustStop = false;

		protected abstract Stream GetAReadStream(Guid computerGuid);
		protected abstract Stream GetAWriteStream(Guid computerGuid);

		public HttpOnlineComputer(
			Action<string> actionOnStatusIn,
			Action<ProgressState> actionOnProgressStateIn,
			Action<int> actionOnProgressPercentageIn,
			Action<Guid, GuidChanges> actionOnGuidAddedOrRemovedIn)
		{
			if (actionOnStatusIn == null) actionOnStatusIn = delegate { };
			actionOnStatus = actionOnStatusIn;

			if (actionOnProgressStateIn == null) actionOnProgressStateIn = delegate { };
			actionOnProgressState = actionOnProgressStateIn;

			if (actionOnProgressPercentageIn == null) actionOnProgressPercentageIn = delegate { };
			actionOnProgressPercentage = actionOnProgressPercentageIn;

			if (actionOnGuidAddedOrRemovedIn == null) actionOnGuidAddedOrRemovedIn = delegate { };
			actionOnGuidAddedOrRemoved = actionOnGuidAddedOrRemovedIn;
		}

		private static object lockobj = new object();
		protected static void AddHttpContextToList(Guid computerGuid, GenericHttpRequest request, GenericHttpResponse response, ConnectionTypes connectionType)
		{
			if (actionOnGuidAddedOrRemoved == null) actionOnGuidAddedOrRemoved = delegate { };//Will be null on client pc

			lock (lockobj)
			{
				if (!receivedCommandsQueue.ContainsKey(computerGuid))
					receivedCommandsQueue.Add(computerGuid, new Queue<OnlineCommands>());

				if (!currentConnections.ContainsKey(computerGuid))
				{
					currentConnections.Add(computerGuid, new Dictionary<GenericHttpRequest,RequestDetails>());
					actionOnGuidAddedOrRemoved(computerGuid, GuidChanges.Added);
				}
				if (!currentConnections[computerGuid].ContainsKey(request))
					currentConnections[computerGuid].Add(
						request, new RequestDetails(response, connectionType, new Queue<OnlineCommands>()));
				if (!byteBufferForeachRequest.ContainsKey(request))
					byteBufferForeachRequest.Add(request, new byte[cBufferSizes]);
			}
		}

		protected static void RemoveHttpContextFromList(Guid computerGuid, GenericHttpRequest request)
		{
			if (actionOnGuidAddedOrRemoved == null) actionOnGuidAddedOrRemoved = delegate { };//Will be null on client pc

			if (currentConnections.ContainsKey(computerGuid))
			{
			removeagain:
				if (currentConnections[computerGuid].ContainsKey(request))
					currentConnections[computerGuid].Remove(request);
				if (byteBufferForeachRequest.ContainsKey(request))
				{
					byteBufferForeachRequest[request] = null;//Remove the byte buffer
					byteBufferForeachRequest.Remove(request);
				}
				if (currentConnections[computerGuid].Count(kv => kv.Value.ConnectionType == ConnectionTypes.In) == 0
					|| currentConnections[computerGuid].Count(kv => kv.Value.ConnectionType == ConnectionTypes.Out) == 0)
				{
					var inConnections = currentConnections[computerGuid].Where(kv => kv.Value.ConnectionType == ConnectionTypes.In).ToArray();
					if (inConnections.Length > 0)
					{
						request = inConnections[0].Key;
						goto removeagain;
					}
					var outConnections = currentConnections[computerGuid].Where(kv => kv.Value.ConnectionType == ConnectionTypes.Out).ToArray();
					if (outConnections.Length > 0)
					{
						request = outConnections[0].Key;
						goto removeagain;
					}
				}
				if (currentConnections[computerGuid].Count == 0)
				{
					currentConnections.Remove(computerGuid);
					actionOnGuidAddedOrRemoved(computerGuid, GuidChanges.Removed);
				}
			}
		}

		public static void CloseConnections(bool forceCloseImmediately)
		{
			mustStop = true;
			if (forceCloseImmediately)
			{
				for (int i = 0; i < threadsForListening.Count; i++)
				{
					try
					{
						var thr = threadsForListening[i];
						if (thr != null && thr.IsAlive)
							thr.Abort();
					}
					catch { }
				}
			}
		}

		public bool EnqueueCommandOnComputer(Guid pcguid, OnlineCommands command, Action<string> actionOnError)
		{
			if (actionOnError == null) actionOnError = delegate { };
			var contexesForSending = currentConnections[pcguid]
				.Where(kv => kv.Value.ConnectionType == ConnectionTypes.Out).ToArray();
			if (contexesForSending.Length == 0)
			{
				actionOnError("Cannot send command, no HttpListenerContext yet.");
				return false;
			}
			//Just enqueue to FIRST command if same queue count (for now)
			var minqueucountContext = contexesForSending.Min(kv => kv.Value.CommandsQueue.Count);
			var firstMeetingMinimum = contexesForSending.First(kv => kv.Value.CommandsQueue.Count <= minqueucountContext);
			firstMeetingMinimum.Value.CommandsQueue.Enqueue(command);
			return true;
		}

		private void ProcessReceivedCommand_UsingReadStream(Guid computerguid, GenericHttpRequest request, OnlineCommands command, Action<string> actionOnError)
		{
			int readcount = 0;
			byte[] buffer = byteBufferForeachRequest[request];
			var readStream = GetAReadStream(computerguid);
			var br = new BinaryReader(readStream);

			switch (command)
			{
				case OnlineCommands.TakeScreenshotAndSend:
				case OnlineCommands.SendFile:
					receivedCommandsQueue[computerguid].Enqueue(command);
					break;
				case OnlineCommands.DownloadFileFromUrl:
					string uri = br.ReadString();
					actionOnProgressState(ProgressState.Show);
					try
					{
						HttpWebRequest downloadRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
						downloadRequest.Method = WebRequestMethods.Http.Get;
						var resp = (HttpWebResponse)downloadRequest.GetResponse();
						var downloadstream = resp.GetResponseStream();
						long filelength = resp.ContentLength;

						string tmpdir = @"C:\Francois\Downloaded automatically";// Path.GetTempPath();
						if (!Directory.Exists(tmpdir))
							Directory.CreateDirectory(tmpdir);
						var tmpfilenameFromheader = resp.Headers[HttpResponseHeader.Location];
						if (tmpfilenameFromheader == null)
							tmpfilenameFromheader = resp.Headers[HttpResponseHeader.ContentLocation];
						if (tmpfilenameFromheader == null)
							if (resp.ResponseUri.Segments.Length > 0)
								tmpfilenameFromheader = resp.ResponseUri.Segments.Last();
						if (tmpfilenameFromheader == null)//Last resort
							tmpfilenameFromheader = DateTime.Now.ToString("yyyy MM dd \\a\\t HH mm ss");

						if (tmpfilenameFromheader.Contains('/') || tmpfilenameFromheader.Contains('\\'))
						{
							tmpfilenameFromheader = tmpfilenameFromheader.Replace("/", "\\");
							int lastindexslash = tmpfilenameFromheader.LastIndexOf('\\');
							tmpfilenameFromheader = tmpfilenameFromheader.Substring(lastindexslash + 1);
						}

						long totalread = 0;
						string localfullpath = Path.Combine(tmpdir, tmpfilenameFromheader);
						using (var fs = new FileStream(localfullpath, FileMode.Create))
						{
							while ((readcount = downloadstream.Read(buffer, 0, buffer.Length)) > 0)
							{
								totalread += readcount;
								actionOnProgressPercentage((int)Math.Truncate(100D * (double)totalread / (double)filelength));
								fs.Write(buffer, 0, readcount);
							}
						}
						Process.Start("explorer", "/select,\"" + localfullpath + "\"");
					}
					finally
					{
						actionOnProgressState(ProgressState.Hide);
					}

					//context.Response.Close(); DO NOT CLOSE must be kept open
					break;
				case OnlineCommands.ReceiveScreenshot:
					long filesizeToReceive = br.ReadInt64();
					Console.WriteLine("Filesize = " + filesizeToReceive);
					//Receiving screenshot
					string jpegSavepath = @"c:\francois\" + computerguid + ".jpg";
					using (var fs = new FileStream(jpegSavepath, FileMode.Create))
					{
						long totalread = 0;
						long numtoread = buffer.Length;
						if (totalread + numtoread > filesizeToReceive)
							numtoread = filesizeToReceive;
						while ((readcount = readStream.Read(buffer, 0, (int)numtoread)) > 0)
						{
							totalread += readcount;
							Console.WriteLine("Readcount = " + readcount);
							Console.WriteLine("Totalread = " + totalread);
							fs.Write(buffer, 0, readcount);
							numtoread = buffer.Length;
							if (totalread + numtoread > filesizeToReceive)
								numtoread = filesizeToReceive - totalread;
							if (numtoread == 0) break;
							Console.WriteLine("Numtoread = " + numtoread);
						}
					}
					Process.Start(jpegSavepath);
					//context.Response.Close();
					break;
				default:
					break;
			}
		}

		protected void ProcessEnqueuedCommands(Guid computerguid, GenericHttpRequest request)
		{
			if (currentConnections[computerguid][request].ConnectionType == ConnectionTypes.In)
			{
				//The FromClient stream is only to read data on server, if is client then is only to write
				var readStream = GetAReadStream(computerguid);
				try
				{
					using (BinaryReader br = new BinaryReader(readStream, Encoding.UTF8))
					{
						while (!mustStop)
						{
							try
							{
								bool? hasData = null;
								bool succeededReadingDidNotTimeout = ThreadingInterop.ActionWithTimeout(
									delegate { hasData = br.ReadBoolean(); },
									20000,
									actionOnStatus);

								if (!succeededReadingDidNotTimeout)
									break;//The read timed out, assuming the connection is lost

								if (hasData.Value)//Has data
								{
									//Read the data available from client
									string tmpCommandTypeStr = br.ReadString();
									OnlineCommands tmpcomm;
									if (!Enum.TryParse<OnlineCommands>(tmpCommandTypeStr, true, out tmpcomm))
										throw new Exception("Unable to parse enum [OnlineCommands] from string = " + tmpCommandTypeStr);//Cannot parse command

									ProcessReceivedCommand_UsingReadStream(computerguid, request, tmpcomm, actionOnStatus);
								}
								else
								{
									//Polling received, to keep stream alive
								}
							}
							catch
							{
								//WTF
								//The stream does not support concurrent IO read or write operations.
							}
						}
					}
				}
				catch { }
			}
			else// if (currentConnections[computerguid][context].Key == ConnectionTypes.ToClient)
			{
				var writeStream = GetAWriteStream(computerguid);
				try
				{
					//using (BinaryWriter bw = new BinaryWriter(writeStream, Encoding.UTF8))
					BinaryWriter bw = new BinaryWriter(writeStream, Encoding.UTF8);
					{
						//Writes this (server) computer's guid, only happes with the connections having an OutputStream, the ConnectionTypes.ToClient ones
						//bw.Write(SettingsInterop.GetComputerGuid().ToByteArray());Leave this, we only need one Guid if on clientside, so just generate any guid

						while (!mustStop)
						{
							try
							{
								if (currentConnections[computerguid][request].CommandsQueue.Count == 0)//No queued command to be sent to client
								{
									Thread.Sleep(TimeSpan.FromSeconds(2));//Just pauses for 2 seconds so we dont overuse the CPU
									bw.Write(false);//Tells the client there is no data available
									bw.Flush();
								}
								else//Has command queued to be sent to client
								{
									bw.Write(true);//Tells the client there is data on its way
									var commandToBeSent = currentConnections[computerguid][request].CommandsQueue.Dequeue();
									bw.Write(commandToBeSent.ToString());
									bw.Flush();
								}

								//Now process commands that use the write stream
								if (receivedCommandsQueue[computerguid].Count > 0)
								{
									var commandToUseWriteStream = receivedCommandsQueue[computerguid].Dequeue();

									int readcount = 0;
									byte[] buffer = byteBufferForeachRequest[request];
									switch (commandToUseWriteStream)
									{
										case OnlineCommands.TakeScreenshotAndSend:
											bw.Write(true);//Tell other side data there is data to be received
											bw.Write(OnlineCommands.ReceiveScreenshot.ToString());

											string jpegPath = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyyMMdd HH_mm_ss") + ".jpg");
											ScreenAndDrawingInterop.CaptureScreen.CaptureScreenNow.GetDesktopImage().Save(jpegPath, System.Drawing.Imaging.ImageFormat.Jpeg);
											//Process.Start(jpegPath);

											using (var fs = new FileStream(jpegPath, FileMode.Open))
											{
												bw.Write((long)fs.Length);
												bw.Flush();
												while ((readcount = fs.Read(buffer, 0, buffer.Length)) > 0)
												{
													writeStream.Write(buffer, 0, readcount);
												}
											}
											writeStream.Flush();
											break;
										case OnlineCommands.DownloadFileFromUrl:
											break;
										case OnlineCommands.SendFile:
											//Send command to receive file on other side

											//bw.Write(OnlineCommands.ThisSide_SendFile
											//filename, filesize, file content
											string filepath = @"C:\Francois\other\tmp.png";

											bw.Write("My filename on server");
											bw.Write((long)(new FileInfo(filepath).Length));
											using (var fs = new FileStream(filepath, FileMode.Open))
											{
												while ((readcount = fs.Read(buffer, 0, buffer.Length)) > 0)
												{
													writeStream.Write(buffer, 0, readcount);
												}
											}
											break;
										case OnlineCommands.ReceiveScreenshot:
											break;
										default:
											break;
									}
								}
							}
							catch { }
						}
					}
				}
				catch { }
			}
		}

		protected static ConnectionTypes GetConnectionTypeFromRequest(HttpListenerRequest request)
		{
			string connectiontypeStr = request.QueryString["connectiontype"];
			ConnectionTypes contype;
			if (string.IsNullOrWhiteSpace(connectiontypeStr) || !Enum.TryParse<ConnectionTypes>(connectiontypeStr, true, out contype))
				return ConnectionTypes.In;//Default if unable to find it
			return contype;
		}
	}

	public class HttpOnlineComputer_Server : HttpOnlineComputer
	{
		public HttpOnlineComputer_Server(
			Action<string> actionOnStatusIn,
			Action<ProgressState> actionOnProgressStateIn,
			Action<int> actionOnProgressPercentageIn,
			Action<Guid, GuidChanges> actionOnGuidAddedOrRemovedIn)
			: base(actionOnStatusIn, actionOnProgressStateIn, actionOnProgressPercentageIn, actionOnGuidAddedOrRemovedIn)
		{
			threadsForListening.Add(ThreadingInterop.DoAction(() =>
			{
				int portnum = 443;//9002;

				HttpListener listener = new HttpListener();
				listener.Prefixes.Add(string.Format("https://+/", portnum));
				listener.Start();
				actionOnStatus("Running server on port " + portnum);
				while (true)
				{
					//Each connection will be handled with ListenerCallback, then will wait again for new connection
					IAsyncResult result = listener.BeginGetContext(new AsyncCallback(OnClientConnectionCallback), listener);
					result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));//Only wait a second then retry, so we dont hang forever
					if (mustStop)
						break;
				}
			},
			false));
		}

		private void OnClientConnectionCallback(IAsyncResult result)
		{
			HttpListener listener = (HttpListener)result.AsyncState;
			// Call EndGetContext to complete the asynchronous operation.
			HttpListenerContext context = listener.EndGetContext(result);
			HttpListenerRequest request = context.Request;
			actionOnStatus("Accepted client request, Method=" + request.HttpMethod + "url=" + request.Url.ToString());

			string clientguid = request.QueryString["guid"];
			if (clientguid == null)
				return;//Cannot use without guid
			Guid tmpguid;
			if (!Guid.TryParse(clientguid, out tmpguid))
				return;//Cannot use without guid

			GenericHttpRequest genRequest = new GenericHttpRequest(request);

			AddHttpContextToList(tmpguid, genRequest, new GenericHttpResponse(context.Response), GetConnectionTypeFromRequest(request));

			//while (!mustStop)ALREADY a while loop inside ProcessCommandsQueue
			//{
			try
			{
				ProcessEnqueuedCommands(tmpguid, genRequest);
			}
			catch (Exception exc)
			{
				actionOnStatus("ERROR: " + exc.Message);
				//The other exceptions (inside the while loops) are handled inside the method 'ProcessCommandsQueue'
			}
			//}

			RemoveHttpContextFromList(tmpguid, genRequest);

			//string taskstr = request.QueryString["task"];
			//if (taskstr == null)
			//    return;//Cannot use without task specified
			//OnlineCommands? clientTask = OnlineCommandsInterop.GetOnlineServerCommandFromString(taskstr, false);
			//if (!clientTask.HasValue)
			//    return;//Cannot use without task specified
		}

		protected override Stream GetAReadStream(Guid computerGuid)
		{
			var inconnections = currentConnections[computerGuid].Where(kv2 => kv2.Value.ConnectionType == ConnectionTypes.In).ToArray();
			if (inconnections.Count() == 0)
				throw new Exception("Cannot GetWriteStream(), no Out connections");
			var stream = inconnections[0].Key.InStream;
			//stream.ReadTimeout = 5000;
			return stream;
			//return request.InStream;
		}

		protected override Stream GetAWriteStream(Guid computerGuid)
		{
			var outconnections = currentConnections[computerGuid].Where(kv2 => kv2.Value.ConnectionType == ConnectionTypes.Out).ToArray();
			if (outconnections.Count() == 0)
				throw new Exception("Cannot GetWriteStream(), no Out connections");
			var stream = outconnections[0].Value.Response.OutStream;
			//stream.WriteTimeout = 5000;
			return stream;
			//return currentConnections[computerGuid][request].Key.OutStream;
		}
	}

	public class HttpOnlineComputer_Client : HttpOnlineComputer
	{
		public const string connectBaseUrlFromClientToServer = "https://commandlistener.getmyip.com";
		//public const string connectBaseUrlFromClientToServer = "http://localhost:9002";

		private static Guid clientsideGuidOfServer;

		public HttpOnlineComputer_Client(
			Action<string> actionOnStatusIn,
			Action<ProgressState> actionOnProgressStateIn,
			Action<int> actionOnProgressPercentageIn,
			Action<Guid, GuidChanges> actionOnGuidAddedOrRemovedIn)
			: base(actionOnStatusIn, actionOnProgressStateIn, actionOnProgressPercentageIn, actionOnGuidAddedOrRemovedIn)
		{
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			clientsideGuidOfServer = Guid.NewGuid();
			//Create 4 connections (threads): 2 for reading, 2 for writing
			threadsForListening.Add(ThreadingInterop.DoAction(() =>
			{
				var contype = ConnectionTypes.In;
				var tmpreq = CreateHttpWebRequestFromClient(contype);
				var req = new GenericHttpRequest(tmpreq);
				var resp = new GenericHttpResponse((HttpWebResponse)tmpreq.GetResponse());
				AddHttpContextToList(clientsideGuidOfServer, req, resp, contype);
				ProcessEnqueuedCommands(clientsideGuidOfServer, req);
				RemoveHttpContextFromList(clientsideGuidOfServer, req);
			},
			false));
			threadsForListening.Add(ThreadingInterop.DoAction(() =>
			{
				var contype = ConnectionTypes.Out;
				var tmpreq = CreateHttpWebRequestFromClient(contype);
				var req = new GenericHttpRequest(tmpreq);
				AddHttpContextToList(clientsideGuidOfServer, req, null, contype);
				ProcessEnqueuedCommands(clientsideGuidOfServer, req);
				RemoveHttpContextFromList(clientsideGuidOfServer, req);
			},
			false));
			threadsForListening.Add(ThreadingInterop.DoAction(() =>
			{
			},
			false));
			threadsForListening.Add(ThreadingInterop.DoAction(() =>
			{
			},
			false));

			//ProcessCommandsQueue(SettingsInterop.GetComputerGuid(), 
		}

		private static HttpWebRequest CreateHttpWebRequestFromClient(ConnectionTypes connectionType)
		{
			var req = (HttpWebRequest)HttpWebRequest
				.Create(string.Format("{0}?connectiontype={1}&guid={2}",
					connectBaseUrlFromClientToServer,
					(connectionType == ConnectionTypes.In ? ConnectionTypes.Out : ConnectionTypes.In).ToString(),//Opposite so the server receives them correct
					HttpUtility.UrlEncodeUnicode(SettingsInterop.GetComputerGuidAsString())));

			switch (connectionType)
			{
				case ConnectionTypes.In:
					req.Method = WebRequestMethods.Http.Get;
					break;
				case ConnectionTypes.Out:
					req.Method = WebRequestMethods.Http.Post;
					req.ContentLength = 10000000;
					break;
			}

			return req;
		}

		protected override Stream GetAReadStream(Guid computerGuid)
		{
			var inconnections = currentConnections[computerGuid].Where(kv2 => kv2.Value.ConnectionType == ConnectionTypes.In).ToArray();
			if (inconnections.Count() == 0)
				throw new Exception("Cannot GetWriteStream(), no Out connections");
			var stream = inconnections[0].Value.Response.OutStream;
			//stream.ReadTimeout = 5000;
			return stream;
			//return currentConnections[computerGuid][request].Key.OutStream;
		}

		protected override Stream GetAWriteStream(Guid computerGuid)
		{
			var outconnections = currentConnections[computerGuid].Where(kv2 => kv2.Value.ConnectionType == ConnectionTypes.Out).ToArray();
			if (outconnections.Count() == 0)
				throw new Exception("Cannot GetWriteStream(), no Out connections");
			var stream = outconnections[0].Key.InStream;
			//stream.WriteTimeout = 5000;
			return stream;
			//return request.InStream;
		}
	}

	public class GenericHttpRequest//Supports both HttpListenerRequest and HttpWebRequest
	{
		bool IsHttpWebRequest_ElseHttpListenerRequest;
		HttpWebRequest possibleHttpWebRequest;
		HttpListenerRequest possibleHttpListenerRequest;

		public GenericHttpRequest(HttpWebRequest request)
		{
			this.possibleHttpWebRequest = request;
			this.IsHttpWebRequest_ElseHttpListenerRequest = true;
		}
		public GenericHttpRequest(HttpListenerRequest request)
		{
			this.possibleHttpListenerRequest = request;
			this.IsHttpWebRequest_ElseHttpListenerRequest = false;
		}

		public Stream InStream { get { return IsHttpWebRequest_ElseHttpListenerRequest ? possibleHttpWebRequest.GetRequestStream() : possibleHttpListenerRequest.InputStream; } }
	}

	public class GenericHttpResponse//Supports both HttpListenerResponse and HttpWebResponse
	{
		bool IsHttpWebResponse_ElseHttpListenerResponse;
		HttpWebResponse possibleHttpWebResponse;
		HttpListenerResponse possibleHttpListenerResponse;
		public GenericHttpResponse(HttpWebResponse response)
		{
			this.IsHttpWebResponse_ElseHttpListenerResponse = true;
			this.possibleHttpWebResponse = response;
		}
		public GenericHttpResponse(HttpListenerResponse response)
		{
			this.IsHttpWebResponse_ElseHttpListenerResponse = false;
			this.possibleHttpListenerResponse = response;
		}
		public Stream OutStream { get { return IsHttpWebResponse_ElseHttpListenerResponse ? possibleHttpWebResponse.GetResponseStream() : possibleHttpListenerResponse.OutputStream; } }
	}
}
