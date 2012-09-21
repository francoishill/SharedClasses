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
	public static class HttpOnlineComputer
	{
		private const int cBufferSizes = 2048;
		private static bool IsServer;

		public const string connectBaseUrlFromClientToServer = "https://commandlistener.getmyip.com";
		//public const string connectBaseUrlFromClientToServer = "http://localhost:9002";

		public enum ConnectionTypes { In, Out };
		public enum GuidChanges { Added, Removed };

		//private static byte[] buffer = new byte[2048];
		public static Dictionary<Guid, Dictionary<GenericHttpRequest, KeyValuePair<GenericHttpResponse, KeyValuePair<ConnectionTypes, Queue<OnlineCommands>>>>> currentConnections = new Dictionary<Guid, Dictionary<GenericHttpRequest, KeyValuePair<GenericHttpResponse, KeyValuePair<ConnectionTypes, Queue<OnlineCommands>>>>>();
		private static Dictionary<GenericHttpRequest, byte[]> byteBufferForeachRequest = new Dictionary<GenericHttpRequest, byte[]>();
		//private Dictionary<Guid, Queue<OnlineServerCommands>> currentlyHookedGuidsWithCommandToSendQueue = new Dictionary<Guid, Queue<OnlineServerCommands>>();
		private static List<Thread> threadsForListening = new List<Thread>();
		private static bool mustStop = false;
		private static Action<string> actionOnStatus;
		private static Action actionToShowProgress;
		private static Action<int> actionOnProgressPercentage;
		private static Action actionToHideProgress;
		private static Action<Guid, GuidChanges> actionOnGuidAddedOrRemoved;

		private static Guid clientsideGuidOfServer;

		public static void MakeConnection(
			bool IsServerIn,
			Action<string> actionOnStatusIn,
			Action actionToShowProgressIn,
			Action<int> actionOnProgressPercentageIn,
			Action actionToHideProgressIn,
			Action<Guid, GuidChanges> actionOnGuidAddedOrRemovedIn)
		{
			IsServer = IsServerIn;

			if (actionOnStatusIn == null)
				actionOnStatusIn = delegate { };
			actionOnStatus = actionOnStatusIn;

			if (actionToShowProgressIn == null)
				actionToShowProgressIn = delegate { };
			actionToShowProgress = actionToShowProgressIn;

			if (actionOnProgressPercentageIn == null)
				actionOnProgressPercentageIn = delegate { };
			actionOnProgressPercentage = actionOnProgressPercentageIn;

			if (actionToHideProgressIn == null)
				actionToHideProgressIn = delegate { };
			actionToHideProgress = actionToHideProgressIn;

			if (actionOnGuidAddedOrRemovedIn == null)
				actionOnGuidAddedOrRemovedIn = delegate { };
			actionOnGuidAddedOrRemoved = actionOnGuidAddedOrRemovedIn;

			if (IsServerIn)
			{
				threadsForListening.Add(ThreadingInterop.DoAction(() =>
				{
					int portnum = 9002;


					HttpListener listener = new HttpListener();
					listener.Prefixes.Add(string.Format("http://+:{0}/", portnum));
					listener.Start();
					actionOnStatus("Running server on port " + portnum);
					while (true)
					{
						//Each connection will be handled with ListenerCallback, then will wait again for new connection
						IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
						result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));//Only wait a second then retry, so we dont hang forever
						if (mustStop)
							break;
					}
				},
				false));
			}
			else
			{
				ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
				clientsideGuidOfServer = Guid.NewGuid();
				//Create 4 connections (threads): 2 for reading, 2 for writing
				threadsForListening.Add(ThreadingInterop.DoAction(() =>
				{
					var contype = ConnectionTypes.In;
					var tmpreq = CreateHttpWebRequestFromClient(contype);
					var req = new GenericHttpRequest(tmpreq);
					AddHttpContextToList(clientsideGuidOfServer, req, new GenericHttpResponse((HttpWebResponse)tmpreq.GetResponse()), contype);
					ProcessCommandsQueue(clientsideGuidOfServer, req);
					RemoveHttpContextFromList(clientsideGuidOfServer, req);
				},
				false));
				threadsForListening.Add(ThreadingInterop.DoAction(() =>
				{
					var contype = ConnectionTypes.Out;
					var tmpreq = CreateHttpWebRequestFromClient(contype);
					var req = new GenericHttpRequest(tmpreq);
					AddHttpContextToList(clientsideGuidOfServer, req, null, contype);
					ProcessCommandsQueue(clientsideGuidOfServer, req);
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

		public static bool EnqueueCommandOnComputer(Guid pcguid, OnlineCommands command, Action<string> actionOnError)
		{
			if (actionOnError == null) actionOnError = delegate { };
			var contexesForSending = HttpOnlineComputer.currentConnections[pcguid]
				.Where(kv => kv.Value.Value.Key == HttpOnlineComputer.ConnectionTypes.Out).ToArray();
			if (contexesForSending.Length == 0)
			{
				actionOnError("Cannot send command, no HttpListenerContext yet.");
				return false;
			}
			//Just enqueue to FIRST command if same queue count (for now)
			var minqueucountContext = contexesForSending.Min(kv => kv.Value.Value.Value.Count);
			var firstMeetingMinimum = contexesForSending.First(kv => kv.Value.Value.Value.Count <= minqueucountContext);
			firstMeetingMinimum.Value.Value.Value.Enqueue(command);
			return true;
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

		private static void ListenerCallback(IAsyncResult result)
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
				ProcessCommandsQueue(tmpguid, genRequest);
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

		private static void ProcessCommandsQueue(Guid computerguid, GenericHttpRequest request)
		{
			if (currentConnections[computerguid][request].Value.Key == ConnectionTypes.In)
			{
				//The FromClient stream is only to read data on server, if is client then is only to write
				var readStream = IsServer ? request.InStream : currentConnections[computerguid][request].Key.OutStream;
				using (BinaryReader br = new BinaryReader(readStream, Encoding.UTF8))
				{
					while (!mustStop)
					{
						try
						{
							if (br.ReadBoolean())//Has data
							{
								//Read the data available from client
								string tmpCommandTypeStr = br.ReadString();
								OnlineCommands tmpcomm;
								if (!Enum.TryParse<OnlineCommands>(tmpCommandTypeStr, true, out tmpcomm))
									throw new Exception("Unable to parse enum [OnlineCommands] from string = " + tmpCommandTypeStr);//Cannot parse command

								switch (tmpcomm)
								{
									case OnlineCommands.ThisSide_TakeScreenshotAndSend:
										break;
									case OnlineCommands.ThisSide_ReceiveScreenshot:
									case OnlineCommands.OtherSide_ReceiveScreenshot:
										//Receiving screenshot
										string jpegSavepath = @"c:\francois\" + computerguid + ".jpg";
										using (var fs = new FileStream(jpegSavepath, FileMode.Create))
										{
											int readcount = 0;
											byte[] buffer = byteBufferForeachRequest[request];
											while ((readcount = (IsServer ? request.InStream : currentConnections[computerguid][request].Key.OutStream)
												.Read(buffer, 0, buffer.Length)) > 0)
											{
												fs.Write(buffer, 0, readcount);
											}
										}
										Process.Start(jpegSavepath);
										//context.Response.Close();
										break;
									case OnlineCommands.ThisSide_DownloadFileFromUrl:
									case OnlineCommands.OtherSide_DownloadFileFromUrl:
										string uri = br.ReadString();
										actionToShowProgress();
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

											int readcount;
											long totalread = 0;
											string localfullpath = Path.Combine(tmpdir, tmpfilenameFromheader);
											using (var fs = new FileStream(localfullpath, FileMode.Create))
											{
												byte[] buffer = byteBufferForeachRequest[request];
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
											actionToHideProgress();
										}

										//context.Response.Close(); DO NOT CLOSE must be kept open
										break;
									case OnlineCommands.ThisSide_SendFile:
										break;
									case OnlineCommands.OtherSide_TakeScreenshot:
										EnqueueCommandOnComputer(computerguid, OnlineCommands.ThisSide_TakeScreenshotAndSend, actionOnStatus);
										break;
									default:
										break;
								}
							}
							else
							{
								//Polling received, to keep stream alive
							}
						}
						catch { }
					}
				}
			}
			else// if (currentConnections[computerguid][context].Key == ConnectionTypes.ToClient)
			{
				var writeStream = IsServer ? currentConnections[computerguid][request].Key.OutStream : request.InStream;
				using (BinaryWriter bw = new BinaryWriter(writeStream, Encoding.UTF8))
				{
					//Writes this (server) computer's guid, only happes with the connections having an OutputStream, the ConnectionTypes.ToClient ones
					//bw.Write(SettingsInterop.GetComputerGuid().ToByteArray());Leave this, we only need one Guid if on clientside, so just generate any guid

					while (!mustStop)
					{
						try
						{
							if (currentConnections[computerguid][request].Value.Value.Count == 0)//No queued command to be sent to client
							{
								bw.Write(false);//Tells the client there is no data available
								bw.Flush();
							}
							else//Has command queued to be sent to client
							{
								bw.Write(true);//Tells the client there is data on its way
								var command = currentConnections[computerguid][request].Value.Value.Dequeue();
								//bw.Write(command.ToString());

								if (command == OnlineCommands.ThisSide_SendFile)
								{
									//Send command to receive file on other side

									//bw.Write(OnlineCommands.ThisSide_SendFile
									//filename, filesize, file content
									string filepath = @"C:\Francois\other\tmp.png";

									bw.Write("My filename on server");
									bw.Write((long)(new FileInfo(filepath).Length));
									using (var fs = new FileStream(filepath, FileMode.Open))
									{
										int readcount = 0;
										byte[] buffer = byteBufferForeachRequest[request];
										while ((readcount = fs.Read(buffer, 0, buffer.Length)) > 0)
										{
											(IsServer ? currentConnections[computerguid][request].Key.OutStream : request.InStream)
												.Write(buffer, 0, readcount);
										}
									}
								}
								else if (command == OnlineCommands.ThisSide_TakeScreenshotAndSend)
								{
									bw.Write(OnlineCommands.OtherSide_ReceiveScreenshot.ToString());
									string jpegPath = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyyMMdd HH_mm_ss") + ".jpg");
									ScreenAndDrawingInterop.CaptureScreen.CaptureScreenNow.GetDesktopImage().Save(jpegPath, System.Drawing.Imaging.ImageFormat.Jpeg);
									//Process.Start(jpegPath);
									byte[] buffer = byteBufferForeachRequest[request];
									int readcount = 0;
									using (var fs = new FileStream(jpegPath, FileMode.Open))
										while ((readcount = fs.Read(buffer, 0, buffer.Length)) > 0)
										{
											writeStream.Write(buffer, 0, readcount);
										}
									writeStream.Flush();
								}

								bw.Flush();
							}
						}
						catch { }
					}
				}
			}
		}

		private static ConnectionTypes GetConnectionTypeFromRequest(HttpListenerRequest request)
		{
			string connectiontypeStr = request.QueryString["connectiontype"];
			ConnectionTypes contype;
			if (string.IsNullOrWhiteSpace(connectiontypeStr) || !Enum.TryParse<ConnectionTypes>(connectiontypeStr, true, out contype))
				return ConnectionTypes.In;//Default if unable to find it
			return contype;
		}

		private static object lockobj = new object();
		private static void AddHttpContextToList(Guid computerGuid, GenericHttpRequest request, GenericHttpResponse response, ConnectionTypes connectionType)
		{
			lock (lockobj)
			{
				if (!currentConnections.ContainsKey(computerGuid))
				{
					currentConnections.Add(computerGuid, new Dictionary<GenericHttpRequest, KeyValuePair<GenericHttpResponse, KeyValuePair<ConnectionTypes, Queue<OnlineCommands>>>>());
					actionOnGuidAddedOrRemoved(computerGuid, GuidChanges.Added);
				}
				if (!currentConnections[computerGuid].ContainsKey(request))
					currentConnections[computerGuid].Add(
						request, new KeyValuePair<GenericHttpResponse, KeyValuePair<ConnectionTypes, Queue<OnlineCommands>>>(
						response, new KeyValuePair<ConnectionTypes, Queue<OnlineCommands>>(
							connectionType,
							new Queue<OnlineCommands>())));
				if (!byteBufferForeachRequest.ContainsKey(request))
					byteBufferForeachRequest.Add(request, new byte[cBufferSizes]);
			}
		}

		private static void RemoveHttpContextFromList(Guid computerGuid, GenericHttpRequest request)
		{
			if (currentConnections.ContainsKey(computerGuid))
			{
				if (currentConnections[computerGuid].ContainsKey(request))
					currentConnections[computerGuid].Remove(request);
				if (byteBufferForeachRequest.ContainsKey(request))
				{
					byteBufferForeachRequest[request] = null;//Remove the byte buffer
					byteBufferForeachRequest.Remove(request);
				}
				if (currentConnections[computerGuid].Count == 0)
				{
					currentConnections.Remove(computerGuid);
					actionOnGuidAddedOrRemoved(computerGuid, GuidChanges.Removed);
				}
			}
		}
	}

	public class GenericHttpRequest//Works with both HttpListenerRequest and HttpWebRequest
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


	public class GenericHttpResponse//Works with both HttpListenerResponse and HttpWebResponse
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
