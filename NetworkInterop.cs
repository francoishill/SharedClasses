using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedClasses;
using System.Text.RegularExpressions;

//TODO: Check out extension functions (a static class with methods which uses this as one of the parameters)
//TODO: Check out BeforeFieldInit and NotBeforeFieldInit for static initializers: http://geekswithblogs.net/BlackRabbitCoder/archive/2010/09/02/c.net-five-more-little-wonders-that-make-code-better-2.aspx
//TODO: Check out cross-calling constructors: http://geekswithblogs.net/BlackRabbitCoder/archive/2010/12/16/c.net-little-wonders-ndash-cross-calling-constructors.aspx
public static class StringExtensions
{
	//public static bool IsFrancois(this string str, bool CaseSensitive)
	//{
	//	return str.Equals("Francois", CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
	//}

	public static string GetExternalIp()
	{
		try
		{
			string externalIP;
			externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
			externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
						 .Matches(externalIP)[0].ToString();
			return externalIP;
		}
		catch { return null; }
	}

	public static bool IsNullOrEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}

	public static bool IsNullOrWhiteSpace(this string str)
	{
		return string.IsNullOrWhiteSpace(str);
	}

	public static void CloseAndDispose(this MemoryStream memorystream)
	{
		memorystream.Close();
		memorystream.Dispose();
		memorystream = null;
	}
}

public static class SocketExtensions
{
	private static bool IsConnectionSuccessful = false;
	private static Exception socketexception;
	private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);

	public static bool BeginConnect_Ext(this TcpClient client, IPEndPoint remoteEndPoint, int timeoutMSec, out string errorMessage)
	{
		TimeoutObject.Reset();
		socketexception = null;

		string serverip = Convert.ToString(remoteEndPoint.Address);
		int serverport = remoteEndPoint.Port;
		//TcpClient tcpclient = new TcpClient();
		//client = new TcpClient();
		//client = new TcpClient();

		client.BeginConnect(serverip, serverport,
			new AsyncCallback(BeginConnect_CallBackMethod), client);

		if (TimeoutObject.WaitOne(timeoutMSec, false))
		{
			if (IsConnectionSuccessful)
			{
				errorMessage = null;
				return true;
			}
			else
			{
				errorMessage = socketexception.Message;
				return false;
				//throw socketexception;
			}
		}
		else
		{
			client.Close();
			client = null;
			errorMessage = "Timeout exception on connecting socket";
			return false;
			//throw new TimeoutException("TimeOut Exception");
			//UserMessages.ShowErrorMessage("Timeout on connection");
			//return null;
		}
	}
	private static void BeginConnect_CallBackMethod(IAsyncResult asyncresult)
	{
		try
		{
			IsConnectionSuccessful = false;
			TcpClient tcpclient = asyncresult.AsyncState as TcpClient;

			if (tcpclient.Client != null)
			{
				tcpclient.EndConnect(asyncresult);
				IsConnectionSuccessful = true;
			}
		}
		catch (Exception ex)
		{
			IsConnectionSuccessful = false;
			socketexception = ex;
		}
		finally
		{
			TimeoutObject.Set();
		}
	}

	//public static bool BeginWrite_Ext(this TcpClient client, , out string errorMessage)
	//{
	//	TimeoutObject.Reset();
	//	socketexception = null;

	//	string serverip = Convert.ToString(remoteEndPoint.Address);
	//	int serverport = remoteEndPoint.Port;
	//	//TcpClient tcpclient = new TcpClient();
	//	//client = new TcpClient();
	//	//client = new TcpClient();

	//	client.BeginConnect(serverip, serverport,
	//		new AsyncCallback(CallBackMethod), client);

	//	if (TimeoutObject.WaitOne(timeoutMSec, false))
	//	{
	//		if (IsConnectionSuccessful)
	//		{
	//			errorMessage = null;
	//			return true;
	//		}
	//		else
	//		{
	//			errorMessage = socketexception.Message;
	//			return false;
	//			//throw socketexception;
	//		}
	//	}
	//	else
	//	{
	//		client.Close();
	//		client = null;
	//		errorMessage = "Timeout exception on connecting socket";
	//		return false;
	//		//throw new TimeoutException("TimeOut Exception");
	//		//UserMessages.ShowErrorMessage("Timeout on connection");
	//		//return null;
	//	}
	//}
	//private static void CallBackMethod(IAsyncResult asyncresult)
	//{
	//	try
	//	{
	//		IsConnectionSuccessful = false;
	//		TcpClient tcpclient = asyncresult.AsyncState as TcpClient;

	//		if (tcpclient.ClientOnServerSide != null)
	//		{
	//			tcpclient.EndConnect(asyncresult);
	//			IsConnectionSuccessful = true;
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		IsConnectionSuccessful = false;
	//		socketexception = ex;
	//	}
	//	finally
	//	{
	//		TimeoutObject.Set();
	//	}
	//}

	///// <summary>
	///// Connects the specified socket.
	///// </summary>
	///// <param name="socket">The socket.</param>
	///// <param name="endpoint">The IP endpoint.</param>
	///// <param name="timeout">The timeout.</param>
	//public static bool BeginConnect_Ext(this Socket socket, EndPoint endpoint, TimeSpan timeout)
	//{
	//	var result = socket.BeginConnect(endpoint, null, null);

	//	bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
	//	if (!success)
	//	{
	//		socket.Close();
	//		return false;
	//		//throw new SocketException(10060); // Connection timed out.
	//	}
	//	else return true;
	//}
}

public class NetworkInterop
{
	private delegate IPHostEntry GetHostEntryHandler(string ip);

	public static IPAddress GetIPAddressFromString(string ipAddressString, Action<string> actionOnError, int timeout = 10000)
	{
		bool resolveDnsMode = false;
		foreach (char chr in ipAddressString)
			if (!char.IsNumber(chr) && chr != '.')
				resolveDnsMode = true;
		IPAddress returnIPAddress = null;

		if (!resolveDnsMode && !IPAddress.TryParse(ipAddressString, out returnIPAddress))
		{
			actionOnError("Invalid IP address: " + (ipAddressString ?? ""));
			return null;
		}
		if (resolveDnsMode)
		{
			try
			{
				GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
				IAsyncResult result = callback.BeginInvoke(ipAddressString, null, null);
				if (result.AsyncWaitHandle.WaitOne(timeout, false))
				{
					IPHostEntry iphostEntry = callback.EndInvoke(result);
					if (iphostEntry == null || iphostEntry.AddressList.Length == 0)
					{
						actionOnError("Could not resolve DNS from " + ipAddressString);
						return null;
					}
					else returnIPAddress = iphostEntry.AddressList[0];
				}
				else
				{
					actionOnError("Timeout to resolve DNS from " + ipAddressString);
					return null;
				}
			}
			catch (Exception exc)
			{
				actionOnError("Error occurred resolving DNS from " + ipAddressString + ": " + exc.Message);
				return null;
			}
		}
		return returnIPAddress;
	}

	/*public static IPAddress GetIPAddressFromString(string ipAddressString)
	{
		bool resolveDnsMode = false;
		foreach (char chr in ipAddressString)
			if (!char.IsNumber(chr) && chr != '.')
				resolveDnsMode = true;
		IPAddress returnIPAddress = null;

		if (!resolveDnsMode && !IPAddress.TryParse(ipAddressString, out returnIPAddress))
		{
			UserMessages.ShowErrorMessage("Invalid IP address: " + (ipAddressString ?? ""));
			return null;
		}
		if (resolveDnsMode)
		{
			IPHostEntry iphostEntry = Dns.GetHostEntry(ipAddressString);
			if (iphostEntry == null || iphostEntry.AddressList.Length == 0)
			{
				UserMessages.ShowErrorMessage("Could not resolve DNS from " + ipAddressString);
				return null;
			}
			else returnIPAddress = iphostEntry.AddressList[0];
		}
		return returnIPAddress;
	}*/

	public static IPAddress GetLocalIPaddress()
	{
		IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
		IPAddress ipAddress = ipHostInfo.AddressList[0];
		foreach (IPAddress ip in ipHostInfo.AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) ipAddress = ip;
		return ipAddress;
	}

	public static IPEndPoint GetLocalIPEndPoint(int portNumber)
	{
		//IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
		//IPAddress ipAddress = ipHostInfo.AddressList[0];
		//foreach (IPAddress ip in ipHostInfo.AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) ipAddress = ip;
		return new IPEndPoint(GetLocalIPaddress(), portNumber);
	}

	//public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
	//public class ProgressChangedEventArgs : EventArgs
	//{
	//	public int CurrentValue;
	//	public int MaximumValue;
	//	public double BytesPerSecond;
	//	public ProgressChangedEventArgs(int CurrentValueIn, int MaximumValueIn, double BytesPerSecondIn = -1)
	//	{
	//		CurrentValue = CurrentValueIn;
	//		MaximumValue = MaximumValueIn;
	//		BytesPerSecond = BytesPerSecondIn;
	//	}
	//}

	//public delegate void TextFeedbackEventHandler(object sender, TextFeedbackEventArgs e);
	//public class TextFeedbackEventArgs : EventArgs
	//{
	//	public string FeedbackText;
	//	public TextFeedbackEventArgs(string FeedbackTextIn)
	//	{
	//		FeedbackText = FeedbackTextIn;
	//	}
	//}

	private const string defaultFolderToSaveIn = @"c:\tempReceived";//@"C:\Francois\other\Test\CS_TestListeningServerReceivedFiles";
	private const string defaultFilePathForSavingForServer = defaultFolderToSaveIn + "\\filereceivedserver.tmp";
	private const string defaultFilePathForSavingForClient = defaultFolderToSaveIn + "\\filereceivedclient.tmp";
	private const int defaultListeningPort = 11000;
	private const int defaultMaxNumberPendingConnections = 100;
	private const int defaultMaxBufferPerTransfer = 1024 * 1024 * 10;
	private const int defaultMaxTotalFileSize = 1024 * 1024 * 1000;//10;
	//public static int maxTransferBuffer = 1024 * 1024 * 10;
	private const int lengthOfGuid = 16;
	private const int lengthOfInfoSize = 10;
	private const int lengthOfFilesize = 16;
	private static int lengthOfFirstConstantBuffer { get { return lengthOfGuid + lengthOfInfoSize + lengthOfFilesize; } }
	private const SerializationInterop.SerializationFormat defaultSerializationFormat = SerializationInterop.SerializationFormat.Binary;

	//private static void RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEventHandler textFeedbackEvent, string textMessage)
	//{
	//	if (textFeedbackEvent != null) textFeedbackEvent(null, new TextFeedbackEventArgs(textMessage));
	//}

	private static void RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEventHandler progressChangedEvent, int currentValue, int maximumValue, double bytesPerSecond = -1)
	{
		if (progressChangedEvent != null) progressChangedEvent(null, new ProgressChangedEventArgs(currentValue, maximumValue, bytesPerSecond));
	}

	private static void RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEventHandler progressChangedEvent, long currentValue, long maximumValue, double bytesPerSecond = -1)
	{
		if (progressChangedEvent != null) progressChangedEvent(null, new ProgressChangedEventArgs((int)currentValue, (int)maximumValue, bytesPerSecond));
	}

	private static void HookIntoFormDisposedEventAndCloseSocket(Socket serverListeningSocketToUse, Form formToHookSocketClosingIntoFormDisposedEvent)
	{
		if (!dictionaryWithFormAndSocketsToCloseUponFormClosing.ContainsKey(formToHookSocketClosingIntoFormDisposedEvent))
			dictionaryWithFormAndSocketsToCloseUponFormClosing.Add(formToHookSocketClosingIntoFormDisposedEvent, serverListeningSocketToUse);

		formToHookSocketClosingIntoFormDisposedEvent.Disposed += (snder, evtargs) =>
		{
			ThreadingInterop.ForceExitAllTreads = true;
			if (dictionaryWithFormAndSocketsToCloseUponFormClosing.ContainsKey(snder as Form))
			{
				if (dictionaryWithFormAndSocketsToCloseUponFormClosing[snder as Form] != null)
				{
					try
					{
						dictionaryWithFormAndSocketsToCloseUponFormClosing[snder as Form].Blocking = false;
						dictionaryWithFormAndSocketsToCloseUponFormClosing[snder as Form].Close();
					}
					catch { }
				}
			}
		};
	}

	public static bool SetupServerSocketSettings(ref Socket serverListeningSocketToUse, int listeningPort, int maxBufferPerTransfer, int maxNumberPendingConnections, Action<string> actionOnError)
	{
		try
		{
			serverListeningSocketToUse.NoDelay = true;
			serverListeningSocketToUse.Ttl = 112;
			serverListeningSocketToUse.ReceiveBufferSize = maxBufferPerTransfer;
			serverListeningSocketToUse.SendBufferSize = maxBufferPerTransfer;
			serverListeningSocketToUse.Bind(NetworkInterop.GetLocalIPEndPoint(listeningPort));
			serverListeningSocketToUse.Listen(maxNumberPendingConnections);
			return true;
		}
		catch (Exception exc)
		{
			actionOnError(string.Format("Unable to start socket server on port {0}, an exceptions occurred: {1}", listeningPort, exc.Message));
			return false;
		}
	}

	public static bool IsSocketTryingToCloseUponApplicationExit(SocketException sexc)
	{
		/* This is normal behavior when interrupting a blocking socket (i.e. waiting for clients). WSACancelBlockingCall is called and a SocketException
		is thrown (see my post above). Just catch this exception and use it to exit the thread ('break' in the infinite while loop).
		http://www.switchonthecode.com/tutorials/csharp-tutorial-simple-threaded-tcp-server */
		return sexc.Message.ToLower().Contains("WSACancelBlockingCall".ToLower());
	}

	public static bool GetBytesAvailable(ref Socket socketToCheck, out int AvailableBytes)
	{
		if (socketToCheck == null)
		{
			AvailableBytes = 0;
			return false;
		}
		AvailableBytes = socketToCheck.Available;
		return AvailableBytes > 0 ? true : false;
	}

	private static void SendResponseToClient(ref Socket handler, Guid receivedGuid, InfoOfTransferToClient infoToTransferToClient)
	{
		//string filePath = @"C:\Francois\other\Test\OCR\Capture.PNG";
		//FileStream fileToRead = new FileStream(filePath, FileMode.Open);
		NetworkStream ns = new NetworkStream(handler);

		byte[] bytesOfInfo = GetSerializedBytesOfObject(infoToTransferToClient);
		int infoSize = bytesOfInfo.Length;
		Console.WriteLine("Number bytes sent to client: " + infoSize);

		WriteGuidSectionToStream(ref ns, receivedGuid);
		WriteInfoSectionSizeToStream(ref ns, infoSize);
		WriteFileSizeToStream(ref ns, 0);
		//WriteFileSizeToStream(ref ns, filePath);

		ns.Write(bytesOfInfo, 0, infoSize);

		//long totalFileSizeToTransfer = (new FileInfo(filePath)).Length;
		////RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, 0, (int)totalFileSizeToTransfer);

		//int maxBytesToReadfromFile = 10240;
		//int numberReadBytes;
		//do
		//{
		//	byte[] bytesRead = new byte[maxBytesToReadfromFile];
		//	numberReadBytes = fileToRead.Read(bytesRead, 0, maxBytesToReadfromFile);
		//	if (numberReadBytes > 0)
		//		ns.Write(bytesRead, 0, numberReadBytes);
		//}
		//while (numberReadBytes > 0);

		ns.Flush();
	}

	private static void EnsureFirstConstantBufferIsFullyPopulated(long totalBytesProcessed, ref byte[] firstConstantBytesForGuidInfoandFilesize, ref byte[] receivedBytes, int actualReceivedLength)
	{
		if (totalBytesProcessed < lengthOfFirstConstantBuffer)
		{
			if (!WillCurrentReceivedBytesCauseExceedingFirstConstantBufferLength(totalBytesProcessed, actualReceivedLength))
				receivedBytes.CopyTo(firstConstantBytesForGuidInfoandFilesize, totalBytesProcessed);
			else
			{
				for (long i = totalBytesProcessed; i < lengthOfFirstConstantBuffer; i++)
					firstConstantBytesForGuidInfoandFilesize[i] = receivedBytes[i - totalBytesProcessed];
			}
		}
	}

	private static void EnsureValuesForGuidAndTotalSizes(ProgressChangedEventHandler ProgressChangedEvent, long totalBytesProcessed, byte[] firstConstantBytesForGuidInfoandFilesize, ref Guid receivedGuid, ref long totalFileSizeToRead, ref long totalInfoSizeToRead, int actualReceivedLength, Action<string> actionOnError, bool UpdateProgress = true)
	{
		if (totalBytesProcessed + actualReceivedLength >= lengthOfFirstConstantBuffer && (totalFileSizeToRead == -1 || totalInfoSizeToRead == -1 || receivedGuid == Guid.Empty))
		{
			byte[] guidbytes = new byte[lengthOfGuid];
			Array.Copy(firstConstantBytesForGuidInfoandFilesize, guidbytes, lengthOfGuid);
			receivedGuid = new Guid(guidbytes);

			string totalInfoSizeToReadString = Encoding.ASCII.GetString(firstConstantBytesForGuidInfoandFilesize, lengthOfGuid, lengthOfInfoSize);
			if (!long.TryParse(totalInfoSizeToReadString, out totalInfoSizeToRead))
			{
				totalInfoSizeToRead = -1;
				actionOnError("Could not get info size from string = " + totalInfoSizeToReadString);
			}
			else
			{
				if (UpdateProgress && ProgressChangedEvent != null)
					ProgressChangedEvent(null, new ProgressChangedEventArgs(0, (int)((totalFileSizeToRead != -1 ? totalFileSizeToRead : 0) + (totalInfoSizeToRead != -1 ? totalInfoSizeToRead : 0))));
			}

			string totalFileSizeToReadString = Encoding.ASCII.GetString(firstConstantBytesForGuidInfoandFilesize, lengthOfGuid + lengthOfInfoSize, lengthOfFilesize);
			if (!long.TryParse(totalFileSizeToReadString, out totalFileSizeToRead))
			{
				totalFileSizeToRead = -1;
				actionOnError("Could not get file size from string = " + totalFileSizeToReadString);
			}
			else
			{
				if (UpdateProgress && ProgressChangedEvent != null)
					ProgressChangedEvent(null, new ProgressChangedEventArgs(0, (int)((totalFileSizeToRead != -1 ? totalFileSizeToRead : 0) + (totalInfoSizeToRead != -1 ? totalInfoSizeToRead : 0))));
			}
		}
	}

	private static bool WillCurrentReceivedBytesCauseExceedingFirstConstantBufferLength(long totalBytesProcessed, long actualReceivedLength)
	{
		return totalBytesProcessed + actualReceivedLength > lengthOfFirstConstantBuffer;
	}

	private static void WriteBytesToMemorystream(long totalBytesProcessed, long totalInfoSizeToRead, ref MemoryStream memoryStreamForInfo, ref byte[] receivedBytes, int actualReceivedLength)
	{
		if (totalBytesProcessed < lengthOfFirstConstantBuffer)
		{
			if (totalBytesProcessed + actualReceivedLength > lengthOfFirstConstantBuffer)
			{
				long memoryStreamStartBytes = lengthOfFirstConstantBuffer - totalBytesProcessed;
				long memoryStreamNumberBytesToRead = (int)(totalBytesProcessed + actualReceivedLength - lengthOfFirstConstantBuffer);
				if (memoryStreamNumberBytesToRead > totalInfoSizeToRead)
					memoryStreamNumberBytesToRead = totalInfoSizeToRead;

				//byte[] bytesForMemoryStream = new byte[memoryStreamNumberBytesToRead];
				//Array.Copy(receivedBytes, memoryStreamStartBytes, bytesForMemoryStream, 0, memoryStreamNumberBytesToRead);
				memoryStreamForInfo.Write(receivedBytes, (int)memoryStreamStartBytes, (int)memoryStreamNumberBytesToRead);
				//memoryStreamForInfo.Write(bytesForMemoryStream, 0, bytesForMemoryStream.Length);
			}
		}
		else
		{
			if (totalBytesProcessed <= lengthOfFirstConstantBuffer + totalInfoSizeToRead)
			{
				long memoryStreamStartBytes = 0;
				long memoryStreamNumberBytesToRead =
							(int)
					(totalBytesProcessed + actualReceivedLength > lengthOfFirstConstantBuffer + totalInfoSizeToRead
					? lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed
					: actualReceivedLength);
				//(int)(lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed);
				if (memoryStreamNumberBytesToRead > lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed)
					memoryStreamNumberBytesToRead = lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed;
				memoryStreamForInfo.Write(receivedBytes, (int)memoryStreamStartBytes, (int)memoryStreamNumberBytesToRead);
			}
		}
	}

	private static void WriteBytesToFilestream(long totalBytesProcessed, long totalInfoSizeToRead, ref FileStream fileStreamIn, ref byte[] receivedBytes, int actualReceivedLength, string filePathIfFileStreamIsNull)
	{
		if (fileStreamIn == null)
		{
			if (File.Exists(filePathIfFileStreamIsNull))
				File.Delete(filePathIfFileStreamIsNull);
			fileStreamIn = new FileStream(filePathIfFileStreamIsNull, FileMode.CreateNew);
		}

		if (totalBytesProcessed < lengthOfFirstConstantBuffer)
		{
			if (totalBytesProcessed + actualReceivedLength > lengthOfFirstConstantBuffer)
			{
				long fileStreamStartBytes = lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed;
				long fileStreamNumberBytesToRead = (int)(totalBytesProcessed + actualReceivedLength - lengthOfFirstConstantBuffer - totalInfoSizeToRead);

				if (fileStreamNumberBytesToRead > 0)
				{
					if (fileStreamIn != null)//Allow for null if trying to NOT write out the file
						fileStreamIn.Write(receivedBytes, (int)fileStreamStartBytes, (int)fileStreamNumberBytesToRead);
				}
			}
		}
		else
		{
			if (totalBytesProcessed > lengthOfFirstConstantBuffer + totalInfoSizeToRead)
			{
				if (fileStreamIn != null)//Allow for null if trying to NOT write out the file
					fileStreamIn.Write(receivedBytes, 0, actualReceivedLength);
			}
			else
			{
				long fileStreamStartBytes = lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed;
				long fileStreamNumberBytesToRead = (int)(totalBytesProcessed + actualReceivedLength - lengthOfFirstConstantBuffer - totalInfoSizeToRead);
				if (fileStreamNumberBytesToRead > 0)
				{
					if (fileStreamIn != null)//Allow for null if trying to NOT write out the file
						//if (fileStreamIn == null) fileStreamIn = new FileStream(defaultFolderToSaveIn + "\\filestream.tmp", FileMode.Create);
						fileStreamIn.Write(receivedBytes, (int)fileStreamStartBytes, (int)fileStreamNumberBytesToRead);
				}
			}
		}
	}

	private static void FireProgressChangedEventForTransfer(ref ProgressChangedEventHandler ProgressChangedEvent, long totalBytesProcessed, long totalFileSizeToRead, long totalInfoSizeToRead, DateTime timeTransferStarted)
	{
		double currentBytesPerSecond = totalBytesProcessed / (new TimeSpan(DateTime.Now.Ticks - timeTransferStarted.Ticks)).TotalSeconds;
		if (totalFileSizeToRead != -1 && totalInfoSizeToRead != -1 && totalBytesProcessed - lengthOfFirstConstantBuffer > 0)
			if (ProgressChangedEvent != null)
				ProgressChangedEvent(null, new ProgressChangedEventArgs(
					(int)(totalBytesProcessed - lengthOfFirstConstantBuffer),
					(int)(totalFileSizeToRead + totalInfoSizeToRead),
					currentBytesPerSecond));
	}

	private static bool IsAlldataCompletelyTransferred(long totalBytesProcessed, long totalFileSizeToRead, long totalInfoSizeToRead)
	{
		return totalFileSizeToRead != -1 && totalInfoSizeToRead != -1 && totalBytesProcessed >= (lengthOfFirstConstantBuffer + totalFileSizeToRead + totalInfoSizeToRead);
	}

	private static string ObtainOriginalFilenameFromInfoOfTransferToServer(InfoOfTransferToServer info, ref TextFeedbackEventHandler TextFeedbackEvent, Action<string> actionOnError)
	{
		if (info == null)// &&
		{
			actionOnError("Cannot obtain filename from NULL InfoOfTransferToServer object");
			return null;
		}
		string fileNameToReturn = defaultFolderToSaveIn + "\\" + Path.GetFileName(info.OriginalFilePath);
		//if (File.Exists(fileNameToReturn)) File.Delete(fileNameToReturn);
		return fileNameToReturn;
	}

	//private static void RenameFileBasedOnInfoOfTransfer(InfoOfTransferToServer info, ref TextFeedbackEventHandler TextFeedbackEvent)
	//{
	//	if (info == null) return;
	//	string fromPath = defaultFilePathForSavingForServer;
	//	string toPath = defaultFolderToSaveIn + "\\" + Path.GetFileName(info.OriginalFilePath);
	//	if (File.Exists(fromPath))
	//	{
	//		bool FileDoesExist = false;
	//		if (File.Exists(toPath))
	//		{
	//			try
	//			{
	//				FileDoesExist = true;
	//				File.Delete(toPath);
	//				FileDoesExist = false;
	//			}
	//			catch (Exception exc)
	//			{
	//				RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEvent, "Unable to delete file " + toPath + Environment.NewLine + "Old file name remains: " + fromPath + Environment.NewLine + exc.Message);
	//				//UserMessages.ShowWarningMessage("Unable to delete file " + toPath + Environment.NewLine + "Old file name remains: " + fromPath + Environment.NewLine + exc.Message);
	//			}
	//		}
	//		if (!FileDoesExist) File.Move(fromPath, toPath);
	//	}
	//	else
	//	{
	//		UserMessages.ShowErrorMessage("Could not find written file: " + fromPath);
	//	}
	//}

	private static void CloseAndDisposeFileStream(ref FileStream filestream)
	{
		filestream.Close();
		filestream.Dispose();
		filestream = null;
	}

	private static void CloseAndDisposeNetworkStream(ref NetworkStream network)
	{
		network.Close();
		network.Dispose();
		network = null;
	}

	public static void StartServer_FileStream(
		Object textfeedbackSenderObject,
		out Socket serverListeningSocketToUse,
		Action<string> actionOnError,
		Form formToHookSocketClosingIntoFormDisposedEvent = null,
		int listeningPort = defaultListeningPort,
		string FolderToSaveIn = defaultFolderToSaveIn,
		int maxBufferPerTransfer = defaultMaxBufferPerTransfer,
		int maxTotalFileSize = defaultMaxTotalFileSize,
		int maxNumberPendingConnections = defaultMaxNumberPendingConnections,
		TextFeedbackEventHandler TextFeedbackEvent = null,
		ProgressChangedEventHandler ProgressChangedEvent = null
		)
	{
		serverListeningSocketToUse = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		if (formToHookSocketClosingIntoFormDisposedEvent != null)
			HookIntoFormDisposedEventAndCloseSocket(serverListeningSocketToUse, formToHookSocketClosingIntoFormDisposedEvent);

		if (!SetupServerSocketSettings(ref serverListeningSocketToUse, listeningPort, maxBufferPerTransfer, maxNumberPendingConnections, err => UserMessages.ShowErrorMessage(err)))
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, TextFeedbackEvent, "Server was unable to start.");
		else
		{
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, TextFeedbackEvent, "Server started, waiting for clients...");

			while (true)
			{
				Socket handler = null;
				try { handler = serverListeningSocketToUse.Accept(); }
				catch (SocketException sexc)
				{
					if (IsSocketTryingToCloseUponApplicationExit(sexc)) break;
					else actionOnError("SocketException occurred: " + sexc.Message);
				}

				if (handler == null) continue;
				DateTime timeTransferStarted = DateTime.Now;

				//TODO: receiving speed decreases over time quite hectically
				long totalBytesProcessed = 0;
				byte[] firstConstantBytesForGuidInfoandFilesize = new byte[lengthOfFirstConstantBuffer];
				Guid receivedGuid = Guid.Empty;
				long totalFileSizeToRead = -1;
				long totalInfoSizeToRead = -1;
				int availableBytes;
				if (!Directory.Exists(defaultFolderToSaveIn)) Directory.CreateDirectory(defaultFolderToSaveIn);
				string localFileName = null;
				FileStream fileStreamIn = null;//new FileStream(defaultFilePathForSavingForServer, FileMode.Create);
				MemoryStream memoryStreamForInfo = new MemoryStream();
				while (true)
				{
					if (!GetBytesAvailable(ref handler, out availableBytes)) continue;
					//Console.WriteLine("availableBytes " + availableBytes.ToString());

					byte[] receivedBytes = new byte[availableBytes];
					int actualReceivedLength = handler.Receive(receivedBytes);

					EnsureFirstConstantBufferIsFullyPopulated(totalBytesProcessed, ref firstConstantBytesForGuidInfoandFilesize, ref receivedBytes, actualReceivedLength);
					//DONE TODO: Fix this
					//string tryingToSendDataToClientCrashesIt;//Assuming it has to do with client needs to be reset if bytes received as defined in infolength
					if (totalBytesProcessed >= lengthOfFirstConstantBuffer && totalFileSizeToRead != -1 && totalInfoSizeToRead != -1)
					{
						double totalSecondCurrently = new TimeSpan(DateTime.Now.Ticks - timeTransferStarted.Ticks).TotalSeconds;
						double averabeBytesPerSecond = totalBytesProcessed / totalSecondCurrently;
						InfoOfTransferToClient infoToTransferToClient_Inprogress = new InfoOfTransferToClient(
							false,
							totalSecondCurrently,
							averabeBytesPerSecond,
							totalBytesProcessed,
							lengthOfFirstConstantBuffer + totalInfoSizeToRead + totalFileSizeToRead);
						SendResponseToClient(ref handler, receivedGuid, infoToTransferToClient_Inprogress);
					}

					EnsureValuesForGuidAndTotalSizes(ProgressChangedEvent, totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize, ref receivedGuid, ref totalFileSizeToRead, ref totalInfoSizeToRead, actualReceivedLength, err => UserMessages.ShowErrorMessage(err));

					WriteBytesToMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

					if (totalInfoSizeToRead != -1 && totalFileSizeToRead > 0 && totalBytesProcessed + actualReceivedLength >= lengthOfFirstConstantBuffer + totalInfoSizeToRead)
					{
						if (localFileName == null)
							localFileName = ObtainOriginalFilenameFromInfoOfTransferToServer((InfoOfTransferToServer)SerializationInterop.DeserializeCustomObjectFromStream(memoryStreamForInfo, new InfoOfTransferToServer(), false), ref TextFeedbackEvent, err => UserMessages.ShowErrorMessage(err));
						if (localFileName != null)
							WriteBytesToFilestream(totalBytesProcessed, totalInfoSizeToRead, ref fileStreamIn, ref receivedBytes, actualReceivedLength, localFileName);
					}

					totalBytesProcessed += actualReceivedLength;

					//Console.WriteLine("FireProgressChangedEventForTransfer: TBP=" + totalBytesProcessed + ", TFS=" + totalFileSizeToRead + ", TIS=" + totalInfoSizeToRead);
					FireProgressChangedEventForTransfer(ref ProgressChangedEvent, totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead, timeTransferStarted);

					if (IsAlldataCompletelyTransferred(totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead))
					{
						double totalSeconds = new TimeSpan(DateTime.Now.Ticks - timeTransferStarted.Ticks).TotalSeconds;
						double averageBytesPerSecond = totalBytesProcessed / totalSeconds;
						InfoOfTransferToClient infoToTransferToClient_Completed = new InfoOfTransferToClient(
								true,
								totalSeconds,
								averageBytesPerSecond,
								totalBytesProcessed,
								lengthOfFirstConstantBuffer + totalFileSizeToRead + totalInfoSizeToRead);

						TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, TextFeedbackEvent, "Successfully received file = " + localFileName
							+ Environment.NewLine + "size of " + (infoToTransferToClient_Completed.TotalNumberofBytesToTransfer / 1024).ToString("0,0.00") + "kB"
							+ Environment.NewLine + "in " + infoToTransferToClient_Completed.DurationOfTransferInSeconds.ToString("0.0#") + " seconds"
							+ Environment.NewLine + "at " + (infoToTransferToClient_Completed.AverageBytesPerSecond / 1024).ToString("0,0.00") + "kB/s");
						SendResponseToClient(ref handler, receivedGuid, infoToTransferToClient_Completed);

						break;
					}
				}

				CloseAndDisposeFileStream(ref fileStreamIn);

				//RenameFileBasedOnInfoOfTransfer((InfoOfTransferToServer)SerializationInterop.DeserializeCustomObjectFromStream(memoryStreamForInfo, new InfoOfTransferToServer(), false), ref TextFeedbackEvent);

				memoryStreamForInfo.CloseAndDispose();

				RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, 0, 100);

				totalBytesProcessed = 0;
				firstConstantBytesForGuidInfoandFilesize = null;
				receivedGuid = Guid.Empty;
				totalFileSizeToRead = 0;
				totalInfoSizeToRead = 0;
				availableBytes = 0;
				localFileName = null;
				fileStreamIn = null;//new FileStream(defaultFilePathForSavingForServer, FileMode.Create);
				memoryStreamForInfo = null;
			}
		}
	}

	private static Dictionary<Form, Socket> dictionaryWithFormAndSocketsToCloseUponFormClosing = new Dictionary<Form, Socket>();

	private static void WriteGuidSectionToStream(ref NetworkStream nwStream, Guid guidIn)
	{
		nwStream.Write((byte[])guidIn.ToByteArray().Clone(), 0, 16);
		guidIn = Guid.Empty;
	}

	private static void WriteNewGuidSectionToStream(ref NetworkStream nwStream)
	{
		WriteGuidSectionToStream(ref nwStream, Guid.NewGuid());
	}

	private static void WriteInfoSectionSizeToStream(ref NetworkStream nwStream, int infoLength)
	{
		string infoLengthString = infoLength.ToString();
		while (infoLengthString.Length < lengthOfInfoSize) infoLengthString = "0" + infoLengthString;
		nwStream.Write(Encoding.ASCII.GetBytes(infoLengthString), 0, lengthOfInfoSize);
		infoLengthString = null;
	}

	private static byte[] GetSerializedBytesOfObject(Object obj)
	{
		MemoryStream memoryStream = new MemoryStream();
		SerializationInterop.SerializeCustomObjectToStream(obj, memoryStream, false);
		memoryStream.Position = 0;
		long lngth = memoryStream.Length;
		byte[] bytesOfInfo = new byte[lngth];
		memoryStream.Read(bytesOfInfo, 0, (int)lngth);
		return bytesOfInfo;
	}

	private static void WriteFileSizeToStream(ref NetworkStream nwStream, long fileSizeInBytes)
	{
		string fileSizeString = fileSizeInBytes.ToString();
		while (fileSizeString.Length < lengthOfFilesize) fileSizeString = "0" + fileSizeString;
		nwStream.Write(Encoding.ASCII.GetBytes(fileSizeString), 0, lengthOfFilesize);
		fileSizeString = null;
	}

	private static void WriteFileSizeToStream(ref NetworkStream nwStream, string filePath)
	{
		WriteFileSizeToStream(ref nwStream, (new FileInfo(filePath)).Length);
	}

	public static void TransferFile_FileStream(
		Object textfeedbackSenderObject,
		string filePath,
		out Socket senderSocketToUse,
		Action<string> actionOnError,
		IPAddress ipAddress = null,
		int listeningPort = defaultListeningPort,
		int maxBufferPerTransfer = defaultMaxBufferPerTransfer,
		TextFeedbackEventHandler TextFeedbackEvent = null,
		ProgressChangedEventHandler ProgressChangedEvent = null)
	{
		senderSocketToUse = null;
		if (!File.Exists(filePath))
			actionOnError("File does not exist and cannot be transferred: " + filePath);
		else
		{
			if (ConnectToServer(out senderSocketToUse, ipAddress, listeningPort))
			{
				NetworkStream networkStream = new NetworkStream(senderSocketToUse);
				FileStream fileToWrite = new FileStream(filePath, FileMode.Open);

				byte[] bytesOfInfo = GetSerializedBytesOfObject(new InfoOfTransferToServer(filePath));
				int infoSize = bytesOfInfo.Length;

				WriteNewGuidSectionToStream(ref networkStream);
				WriteInfoSectionSizeToStream(ref networkStream, infoSize);
				WriteFileSizeToStream(ref networkStream, filePath);

				networkStream.Write(bytesOfInfo, 0, infoSize);

				long totalFileSizeToTransfer = (new FileInfo(filePath)).Length;
				RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, 0, (int)totalFileSizeToTransfer);

				int maxBytesToReadfromFile = 10240;//16;
				int numberReadBytes;
				do
				{
					byte[] bytesRead = new byte[maxBytesToReadfromFile];
					numberReadBytes = fileToWrite.Read(bytesRead, 0, maxBytesToReadfromFile);
					if (numberReadBytes > 0)
						networkStream.Write(bytesRead, 0, numberReadBytes);
				}
				while (numberReadBytes > 0);

			RestartReceivingLoop:
				//TODO: For memory management all these objects (timeTransferStarted, receivedGuid, etc) must still be destroyd later
				DateTime timeTransferStarted = DateTime.Now;
				long totalBytesProcessed = 0;
				byte[] firstConstantBytesForGuidInfoandFilesize = new byte[lengthOfFirstConstantBuffer];
				Guid receivedGuid = Guid.Empty;
				long totalFileSizeToRead = -1;
				long totalInfoSizeToRead = -1;
				int availableBytes;
				if (!Directory.Exists(defaultFolderToSaveIn)) Directory.CreateDirectory(defaultFolderToSaveIn);
				while (true)
				{
					//FileStream fileStreamIn = null;// new FileStream(defaultFilePathForSavingForClient, FileMode.Create);
					MemoryStream memoryStreamForInfo = new MemoryStream();

					if (!GetBytesAvailable(ref senderSocketToUse, out availableBytes)) continue;

					if ((totalFileSizeToRead == -1 || totalInfoSizeToRead == -1) && totalBytesProcessed + availableBytes > lengthOfFirstConstantBuffer)
						availableBytes = lengthOfFirstConstantBuffer - (int)totalBytesProcessed;
					else if (totalFileSizeToRead != -1 && totalInfoSizeToRead != -1 && totalBytesProcessed + availableBytes > lengthOfFirstConstantBuffer + totalInfoSizeToRead + totalFileSizeToRead)
						availableBytes = lengthOfFirstConstantBuffer + (int)totalInfoSizeToRead + (int)totalFileSizeToRead - (int)totalBytesProcessed;
					byte[] receivedBytes = new byte[availableBytes];
					int actualReceivedLength = senderSocketToUse.Receive(receivedBytes);

					//if (IsreceivingFinished(totalFileSizeToRead, totalInfoSizeToRead, totalBytesProcessed, actualReceivedLength))
					//	FinishedReceivingRespondToClient(ref senderSocketToUse, receivedGuid);

					EnsureFirstConstantBufferIsFullyPopulated(totalBytesProcessed, ref firstConstantBytesForGuidInfoandFilesize, ref receivedBytes, actualReceivedLength);

					EnsureValuesForGuidAndTotalSizes(ProgressChangedEvent, totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize, ref receivedGuid, ref totalFileSizeToRead, ref totalInfoSizeToRead, actualReceivedLength, err => UserMessages.ShowErrorMessage(err), false);

					WriteBytesToMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

					if (totalInfoSizeToRead != -1 && totalBytesProcessed + actualReceivedLength >= lengthOfFirstConstantBuffer + totalInfoSizeToRead)
					{
						if (totalFileSizeToRead > 0)
							actionOnError("Function not incorporated yet to transfer file back to client.");
						//string localFileName = ObtainOriginalFilenameFromInfoOfTransferToServer((InfoOfTransferToServer)SerializationInterop.DeserializeCustomObjectFromStream(memoryStreamForInfo, new InfoOfTransferToServer(), false), ref TextFeedbackEvent);
						//if (localFileName != null)
						//	WriteBytesToFilestream(totalBytesProcessed, totalInfoSizeToRead, ref fileStreamIn, ref receivedBytes, actualReceivedLength, localFileName);
					}
					//WriteBytesToFilestream(totalBytesProcessed, totalInfoSizeToRead, ref fileStreamIn, ref receivedBytes, actualReceivedLength);

					totalBytesProcessed += actualReceivedLength;

					//FireProgressChangedEventForTransfer(ref ProgressChangedEvent, totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead, timeTransferStarted);

					if (IsAlldataCompletelyTransferred(totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead))
					{
						//CloseAndDisposeFileStream(ref fileStreamIn);
						InfoOfTransferToClient info = (InfoOfTransferToClient)SerializationInterop.DeserializeCustomObjectFromStream(memoryStreamForInfo, new InfoOfTransferToClient(), false);
						if (info != null)
						{
							//MessageBox.Show(info.AverageBytesPerSecond + ", " + info.DurationOfTransfer.TotalSeconds + ", " + info.SuccessfullyReceived);
							//RenameFileBasedOnInfoOfTransfer((InfoOfTransfer)SerializationInterop.DeserializeObject(memoryStreamForInfo, defaultSerializationFormat, typeof(InfoOfTransfer), false));
							//if (File.Exists(defaultFilePathForSavingForClient))
							//{
							//	try
							//	{
							//		File.Delete(defaultFilePathForSavingForClient);
							//	}
							//	catch (Exception exc)
							//	{
							//		RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEvent,
							//			"Could not delete client file: " + defaultFilePathForSavingForClient +
							//			Environment.NewLine + exc.Message);
							//	}
							//}
							memoryStreamForInfo.CloseAndDispose();
							if (info.SuccessfullyReceiveComplete)
							{
								TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(
								textfeedbackSenderObject,
								TextFeedbackEvent,
								"Successfully transferred file = " + filePath
								+ Environment.NewLine + "size of " + (info.TotalNumberofBytesToTransfer / 1024).ToString("0,0.00") + "kB"
								+ Environment.NewLine + "in " + info.DurationOfTransferInSeconds.ToString("0.0#") + " seconds"
								+ Environment.NewLine + "at " + (info.AverageBytesPerSecond / 1024).ToString("0,0.00") + "kB/s");
								break;
							}
							else
							{
								RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, info.CurrentNumberofBytesTransferred, info.TotalNumberofBytesToTransfer, info.AverageBytesPerSecond);
								goto RestartReceivingLoop;
							}
						}
					}
				}

				RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, 0, 100);

				CloseAndDisposeFileStream(ref fileToWrite);
				CloseAndDisposeNetworkStream(ref networkStream);

				//MemoryManagement.FreeObjects(totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize);
				totalBytesProcessed = 0;
				firstConstantBytesForGuidInfoandFilesize = null;
				receivedGuid = Guid.Empty;
				totalFileSizeToRead = 0;
				totalInfoSizeToRead = 0;
				availableBytes = 0;
			}
		}
	}

	/*public static void TransferText(string text, ref Socket senderSocketToUse)
	{
		byte[] byData;
		byData = System.Text.Encoding.ASCII.GetBytes(text + "<EOF>");

		senderSocketToUse.Close();
		senderSocketToUse.Dispose();
		senderSocketToUse = null;

		byData = null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
	}*/

	public static string Get16lengthStringOfNumber(long longIn)
	{
		string tmpstr = longIn.ToString();
		while (tmpstr.Length < 16) tmpstr = "0" + tmpstr;
		return tmpstr;
	}

	public static bool ConnectToServer(out Socket socketToInitialize, IPAddress ipAddress = null, int listeningPort = defaultListeningPort, int maxBufferPerTransfer = defaultMaxBufferPerTransfer)
	{
		try
		{
			socketToInitialize = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socketToInitialize.ReceiveTimeout = 5000;
			socketToInitialize.SendTimeout = 5000;
			socketToInitialize.NoDelay = true;
			socketToInitialize.Ttl = 112;
			socketToInitialize.SendBufferSize = maxBufferPerTransfer;
			socketToInitialize.ReceiveBufferSize = maxBufferPerTransfer;
			socketToInitialize.Connect(ipAddress == null ? NetworkInterop.GetLocalIPEndPoint(listeningPort) : new IPEndPoint(ipAddress, listeningPort));
			return true;
		}
		catch (SocketException se)
		{
			MessageBox.Show("ConnectToServer experienced SocketException:" + Environment.NewLine + se.Message + Environment.NewLine + se.TargetSite);
			socketToInitialize = null;
			return false;
		}
	}

	public static void MergeFiles(string firstFileName, Func<string, bool> actionToConfirm, Action<string> actionOnInfo, bool DeleteOriginalFilesUponSuccess = true)
	{
		if (firstFileName.Length < 1)
			return;

		string endPart = firstFileName;
		string orgFile = "";

		orgFile = endPart.Substring(0, endPart.LastIndexOf("."));
		endPart = endPart.Substring(endPart.LastIndexOf(".") + 1);

		if (endPart.ToUpper() == "final".ToUpper())//If only one slice is there
		{
			orgFile = orgFile.Substring(0, orgFile.LastIndexOf("."));
			endPart = "0";
		}

		if (File.Exists(orgFile))
		{
			if (actionToConfirm(orgFile + " already exists, do you want to delete it"))
				File.Delete(orgFile);
			else
			{
				actionOnInfo("File not assembled. Operation cancelled by user.");
				return;
			}
		}

		//Assembling starts from here
		BinaryWriter bw = new BinaryWriter(File.Open(orgFile, FileMode.Append));
		string nextFileName = "";
		byte[] buffer = new byte[bw.BaseStream.Length];


		int counter = int.Parse(endPart);
		while (true)
		{
			nextFileName = orgFile + "." + counter.ToString();
			if (File.Exists(nextFileName + ".final"))
			{
				//Last slice
				buffer = File.ReadAllBytes(nextFileName + ".final");
				bw.Write(buffer);
				break;
			}
			else
			{
				buffer = File.ReadAllBytes(nextFileName);
				bw.Write(buffer);
			}
			counter++;
		}
		bw.Close();

		counter = 0;
		while (true)
		{
			nextFileName = orgFile + "." + counter.ToString();
			if (File.Exists(nextFileName + ".final"))
			{
				File.Delete(nextFileName + ".final");
				break;
			}
			else File.Delete(nextFileName);
			counter++;
		}
		//MessageBox.Show(this, "File assebled successfully");
	}

	public static string InsertPortNumberIntoUrl(string originalUrl, int portNumber)
	{
		int posDoubleSlash = originalUrl.IndexOf("//");
		int posFirstSingleSlash = originalUrl.IndexOf("/", (posDoubleSlash >= 0 ? posDoubleSlash : 0) + 2);
		if (posFirstSingleSlash != -1)
			return originalUrl.Insert(posFirstSingleSlash, ":" + portNumber.ToString());
		else
			return originalUrl + ":" + portNumber.ToString();
	}

	public static bool FtpUploadFiles(Object textfeedbackSenderObject, string ftpRootUri, string userName, string password, string[] localFilenames, Action<string> actionOnError, string urlWhenSuccessullyUploaded = null, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null)
	{
		ftpRootUri = ftpRootUri.Replace('\\', '/');
		try
		{
			//bool DirexistCanContinue = false;
			//if (!FtpDirectoryExists(ftpRootUri, userName, password))
			//{
			//    if (CreateFTPDirectory(ftpRootUri, userName, password))
			//        DirexistCanContinue = true;
			//}
			//else DirexistCanContinue = true;
			//if (DirexistCanContinue)
			if (CreateFTPDirectory(textFeedbackEvent, ftpRootUri, userName, password))
			{
				using (System.Net.WebClient client = new System.Net.WebClient())
				{
					client.Credentials = new System.Net.NetworkCredential(userName, password);
					//client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");

					bool isComplete = false;
					client.UploadFileCompleted += (snder, evtargs) =>
					{
						RaiseProgressChangedEvent_Ifnotnull(ref progressChanged,
							100,
							100);
						isComplete = true;
					};
					client.UploadProgressChanged += (snder, evtargs) =>
					{
						if (!isComplete)
						{
							RaiseProgressChangedEvent_Ifnotnull(ref progressChanged,
								evtargs.ProgressPercentage,
								100);
						}
					};

					foreach (string localFilename in localFilenames)
					{
						string fileNameOnServer = new FileInfo(localFilename).Name;
						//Console.WriteLine("fileNameOnServer" + fileNameOnServer);
						string dirOnFtpServer = ftpRootUri + "/" + fileNameOnServer;

						isComplete = false;

						string startMsg = "Starting upload for ";
						TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent,
								startMsg + localFilename,
								HyperlinkRangeIn: new Range(startMsg.Length, localFilename.Length, Range.LinkTypes.ExplorerSelect));
						client.UploadFileAsync(new Uri(dirOnFtpServer), "STOR", localFilename);
						while (!isComplete)
							Application.DoEvents();
						TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Successfully uploaded " + fileNameOnServer);
					}
					if (urlWhenSuccessullyUploaded != null) Process.Start(urlWhenSuccessullyUploaded);
					client.Dispose();
					GC.Collect();
					GC.WaitForPendingFinalizers();
					return true;
				}
			}
			else
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Could not upload files (could not find/create directory online: " + ftpRootUri, TextFeedbackType.Error);
			//UserMessages.ShowErrorMessage("Could not upload files (could not find/create directory online: " + ftpRootUri);
		}
		catch (Exception exc)
		{
			if (exc.Message.ToLower().Contains("the operation has timed out"))
			{
				actionOnError("Upload to ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached");
				/*if (UserMessages.Confirm("Upload to ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached, restart the application now?"))
					//Application.Restart();
					ApplicationRecoveryAndRestart.TestCrash(false);*/
			}
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Exception in transfer: " + exc.Message, TextFeedbackType.Error);
			//MessageBox.Show("Exception in transfer: " + exc.Message);
		}
		return false;
	}

	public static long FtpGetFileSize(string fullFileUri, string userName, string password, TextFeedbackEventHandler textFeedbackHandler)
	{
		try
		{
			FtpWebRequest reqSize = (FtpWebRequest)FtpWebRequest.Create(new Uri(fullFileUri));
			reqSize.Credentials = new NetworkCredential(userName, password);
			reqSize.Method = WebRequestMethods.Ftp.GetFileSize;
			reqSize.UseBinary = true;

			FtpWebResponse loginresponse = (FtpWebResponse)reqSize.GetResponse();
			FtpWebResponse respSize = (FtpWebResponse)reqSize.GetResponse();
			respSize = (FtpWebResponse)reqSize.GetResponse();
			long size = respSize.ContentLength;

			respSize.Close();

			return size;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response.StatusCode ==
				FtpStatusCode.ActionNotTakenFileUnavailable)
			{
				response.Close();
				return -1;//Does not exist
			}
			response.Close();
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, textFeedbackHandler, "Cannot determine ftp file size for '" + fullFileUri + ex.Message, TextFeedbackType.Error);
			return -2;//Cannot obtain size (could be internet connectivity, timeout, etc)
		}
	}

	public static string FtpDownloadFile(Object textfeedbackSenderObject, string localRootFolder, string userName, string password, string onlineFileUrl, Action<string> actionOnError, TextFeedbackEventHandler textFeedbackHandler = null, ProgressChangedEventHandler progressChanged = null)
	{
		int maxRetries = 5;
		int retryCount = 0;
		try
		{
			if (!Directory.Exists(localRootFolder))
				Directory.CreateDirectory(localRootFolder);
			using (System.Net.WebClient client = new System.Net.WebClient())
			{
				client.Credentials = new System.Net.NetworkCredential(userName, password);
				//client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");
				//foreach (string localFilename in localFilenames)
				//{
				bool isComplete = false;
				long filesize = FtpGetFileSize(onlineFileUrl, userName, password, textFeedbackHandler);

				if (filesize == -1)//File does not exist
				{
					string errMsg = "Ftp file does not exist: " + onlineFileUrl;
					actionOnError(errMsg);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackHandler, errMsg);
					return null;
				}

				//MiniDownloadBarForm.ShowMiniDownloadBar();
				List<Thread> progressbarThreads = new List<Thread>();
				client.DownloadFileCompleted += (snder, evtargs) =>
				{
					RaiseProgressChangedEvent_Ifnotnull(ref progressChanged,
						100,
						100);
					isComplete = true;
				};
				client.DownloadProgressChanged += (snder, evtargs) =>
				{
					if (!isComplete)
					{
						//int percentage = evtargs.ProgressPercentage;
						int percentage = (int)Math.Truncate((double)100 * (double)evtargs.BytesReceived / (double)filesize);
						RaiseProgressChangedEvent_Ifnotnull(ref progressChanged,
							percentage,
							100);
						//Thread tmpthread =
						MiniDownloadBarForm.UpdateProgress(percentage);
						//if (!progressbarThreads.Contains(tmpthread))
						//    progressbarThreads.Add(tmpthread);
					}
				};
				string localFilepath = localRootFolder.TrimEnd('\\') + "\\" + Path.GetFileName(onlineFileUrl.Replace("ftp://", "").Replace("/", "\\"));

			retryhere:
				client.DownloadFileAsync(new Uri(onlineFileUrl), localFilepath);
				while (!isComplete)
					Application.DoEvents();

				//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackHandler, "Successfully downloaded " + onlineFileUrl);

				//int tmptodo;
				//TODO: Checking file length = 0? What if its a blank/empty file??
				if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
				{
					if (File.Exists(localFilepath) && new FileInfo(localFilepath).Length == 0)
						if (filesize > 0)
						{
							retryCount++;
							isComplete = false;
							Thread.Sleep(1000);
							if (retryCount <= maxRetries && (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length == 0))
								goto retryhere;
						}
				}

				MiniDownloadBarForm.CloseDownloadBar();
				//for (int i = 0; i < progressbarThreads.Count; i++)
				//{
				//    try
				//    {
				//        MiniDownloadBarForm.CloseDownloadBarUsingThread(progressbarThreads[i]);
				//    }
				//    catch { }
				//}
				//progressbarThreads.Clear();

				//}
				//client.Dispose();
				//client = null;
				//GC.Collect();
				//GC.WaitForPendingFinalizers();
				if (!File.Exists(localFilepath) || new FileInfo(localFilepath).Length != filesize)
				{
					if (File.Exists(localFilepath))
						File.Delete(localFilepath);
					return null;
				}
				return localFilepath;
			}
		}
		catch (Exception exc)
		{
			if (exc.Message.ToLower().Contains("the operation has timed out"))
			{
				actionOnError("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached");
				/*if (UserMessages.Confirm("Download from ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached, restart the application now?"))
					//Application.Restart();
					ApplicationRecoveryAndRestart.TestCrash(false);*/
			}
			MessageBox.Show("Exception in transfer: " + exc.Message);
		}
		return null;
	}

	//Changed to bool? so that null tells there was an error
	public static bool? FtpFileExists(string filePath, string ftpUser, string ftpPassword, Action<string> actionOnError)
	{
		var request = (FtpWebRequest)WebRequest.Create(filePath);
		request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
		request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
		request.UseBinary = true;
		try
		{
			FtpWebResponse response = (FtpWebResponse)request.GetResponse();
			response.Close();
			return true;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response.StatusCode ==
				FtpStatusCode.ActionNotTakenFileUnavailable)
			{
				response.Close();
				return false;
				//Does not exist
			}
			response.Close();
			actionOnError("Cannot determine whether file '" + filePath + "' exists: " + ex.Message);
			return null;
		}
	}

	//Rather use "CreateFTPDirectory" it will fail and also return true if directory already existed
	//public static bool FtpDirectoryExists(string directoryPath, string ftpUser, string ftpPassword)
	//{
	//    bool IsExists = true;
	//    try
	//    {
	//        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(directoryPath);
	//        request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
	//        request.Method = WebRequestMethods.Ftp.ListDirectory;
	//        request.KeepAlive = false;

	//        using (FtpWebResponse response = (FtpWebResponse)(request.GetResponse())) { response.Close(); }
	//        IsExists = true;
	//    }
	//    catch (WebException ex)
	//    {
	//        FtpWebResponse response = (FtpWebResponse)ex.Response;
	//        if (response.StatusCode ==
	//            FtpStatusCode.ActionNotTakenFileUnavailable)
	//        {
	//            response.Close();
	//            return false;
	//            //Does not exist
	//        }

	//        //Console.WriteLine("WebException on FtpDirectoryExists" + ex.Message);
	//        //if (ex.Message.IndexOf("File unavailable", StringComparison.InvariantCultureIgnoreCase) == -1)
	//        UserMessages.ShowErrorMessage("WebException on FtpDirectoryExists: " + ex.Message);
	//        IsExists = false;
	//    }
	//    catch (Exception ex)
	//    {
	//        UserMessages.ShowErrorMessage("Exception on FtpDirectoryExists" + ex.Message);
	//    }
	//    return IsExists;
	//}

	//Null means it already existed
	public static bool? CreateFTPDirectory_NullIfExisted(TextFeedbackEventHandler textFeedbackEvent, string directory, string ftpUser, string ftpPassword, int? timeout = null)
	{
		try
		{
			//create the directory
			FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(new Uri(directory));
			requestDir.Method = WebRequestMethods.Ftp.MakeDirectory;
			requestDir.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			requestDir.UsePassive = true;
			requestDir.UseBinary = true;
			requestDir.KeepAlive = false;
			if (timeout.HasValue)
				requestDir.Timeout = timeout.Value;
			FtpWebResponse response = (FtpWebResponse)(requestDir.GetResponse());
			Stream ftpStream = response.GetResponseStream();

			ftpStream.Close();
			response.Close();

			//Directory did not exist, successfully created
			return true;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response != null && response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable
				//DONE: Will it always work to check the StatusDescription?
				//&& response.StatusDescription.IndexOf("Directory already exists", StringComparison.InvariantCultureIgnoreCase) != -1
				)
			{
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "FTP directory already existed: " + directory, TextFeedbackType.Subtle);
				//Directory already existed
				response.Close();
				return null;
			}
			else
			{
				//Error occurred, directory not created/existed (could have timed out?)
				if (response != null)
					response.Close();
				TextFeedbackEventArgs.RaiseSimple(textFeedbackEvent, "Could not create directory (" + directory + "): " + ex.Message, TextFeedbackType.Error);
				return false;
			}
		}
	}

	public static bool CreateFTPDirectory(TextFeedbackEventHandler textFeedbackEvent, string directory, string ftpUser, string ftpPassword)
	{
		return CreateFTPDirectory_NullIfExisted(textFeedbackEvent, directory, ftpUser, ftpPassword) != false;
		/*try
		{
			//create the directory
			FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(new Uri(directory));
			requestDir.Method = WebRequestMethods.Ftp.MakeDirectory;
			requestDir.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			requestDir.UsePassive = true;
			requestDir.UseBinary = true;
			requestDir.KeepAlive = false;
			FtpWebResponse response = (FtpWebResponse)(requestDir.GetResponse());
			Stream ftpStream = response.GetResponseStream();

			ftpStream.Close();
			response.Close();

			//Directory did not exist, successfully created
			return true;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable
				//DONE: Will it always work to check the StatusDescription?
				//&& response.StatusDescription.IndexOf("Directory already exists", StringComparison.InvariantCultureIgnoreCase) != -1
				)
			{
				//Directory already existed
				response.Close();
				return true;
			}
			else
			{
				//Error occurred, directory not created/existed (could have timed out?)
				response.Close();
				return false;
			}
		}*/
	}

	public static bool RemoveFTPDirectory(string directory, string ftpUser, string ftpPassword)
	{
		try
		{
			FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(new Uri(directory));
			requestDir.Method = WebRequestMethods.Ftp.RemoveDirectory;
			requestDir.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			requestDir.UsePassive = true;
			requestDir.UseBinary = true;
			requestDir.KeepAlive = false;
			FtpWebResponse response = (FtpWebResponse)(requestDir.GetResponse());
			Stream ftpStream = response.GetResponseStream();

			ftpStream.Close();
			response.Close();

			return true;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
			{
				response.Close();
				return true;
			}
			else
			{
				response.Close();
				return false;
			}
		}
	}

	public static bool DeleteFTPfile(Object textfeedbackSenderObject, string ftpFilePath, string ftpUser, string ftpPassword, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null)
	{
		try
		{
			//create the directory
			FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpFilePath));
			requestDir.Method = WebRequestMethods.Ftp.DeleteFile;
			requestDir.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			requestDir.UsePassive = true;
			requestDir.UseBinary = true;
			requestDir.KeepAlive = false;
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Attempting to delete file from server: " + ftpFilePath);
			FtpWebResponse response = (FtpWebResponse)(requestDir.GetResponse());
			Stream ftpStream = response.GetResponseStream();

			ftpStream.Close();
			response.Close();

			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Successfully deleted file from server: " + ftpFilePath);
			return true;
		}
		catch (WebException ex)
		{
			FtpWebResponse response = (FtpWebResponse)ex.Response;
			if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
			{
				response.Close();
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "File was not deleted, did not exist on server: " + ftpFilePath);
				return true;
			}
			else
			{
				response.Close();
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "An error occurred trying to delete file (" + ftpFilePath + ") from server:" + Environment.NewLine + ex.Message);
				return false;
			}
		}
	}

	public static string[] GetFileList(string directory, string ftpUser, string ftpPassword, Action<string> actionOnError)
	{
		string[] downloadFiles;
		StringBuilder result = new StringBuilder();
		WebResponse response = null;
		StreamReader reader = null;
		try
		{
			FtpWebRequest reqFTP;
			reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(directory));
			//reqFTP.UseBinary = true;
			reqFTP.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
			//reqFTP.Proxy = null;
			//reqFTP.KeepAlive = false;
			//reqFTP.UsePassive = false;
			response = reqFTP.GetResponse();
			reader = new StreamReader(response.GetResponseStream());
			string line = reader.ReadLine();
			while (line != null)
			{
				result.Append(line);
				result.Append("\n");
				line = reader.ReadLine();
			}
			// to remove the trailing '\n'
			result.Remove(result.ToString().LastIndexOf('\n'), 1);

			reader.Close();
			response.Close();
			return result.ToString().Split('\n');
		}
		catch (Exception ex)
		{
			actionOnError("Error getting file list: " + ex.Message);
			if (reader != null)
			{
				reader.Close();
			}
			if (response != null)
			{
				response.Close();
			}
			downloadFiles = null;
			return downloadFiles;
		}
	}

	/// <summary>
	/// Post data to php, maximum length of data is 8Mb
	/// </summary>
	/// <param name="url">The url of the php, do not include the ?</param>
	/// <param name="data">The data, i.e. "name=koos&surname=koekemoer". Note to not include the ?</param>
	/// <returns>Returns the data received from the php (usually the "echo" statements in the php.</returns>
	public static string PostPHP(string url, string data, Action<string> actionOnError)
	{
		string vystup = "";
		try
		{
			data = data.Replace("+", "[|]");
			//Our postvars
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			//Initialisation, we use localhost, change if appliable
			HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create(url);
			//Our method is post, otherwise the buffer (postvars) would be useless
			WebReq.Method = "POST";
			//We use form contentType, for the postvars.
			WebReq.ContentType = "application/x-www-form-urlencoded";
			//The length of the buffer (postvars) is used as contentlength.
			WebReq.ContentLength = buffer.Length;
			//We open a stream for writing the postvars
			Stream PostData = WebReq.GetRequestStream();
			//Now we write, and afterwards, we close. Closing is always important!
			PostData.Write(buffer, 0, buffer.Length);
			PostData.Close();
			//Get the response handle, we have no true response yet!
			HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
			//Let's show some information about the response
			//System.Windows.Forms.MessageBox.Show(WebResp.StatusCode.ToString());
			//System.Windows.Forms.MessageBox.Show(WebResp.Server);

			//Now, we read the response (the string), and output it.
			Stream Answer = WebResp.GetResponseStream();
			StreamReader _Answer = new StreamReader(Answer);
			vystup = _Answer.ReadToEnd();

			_Answer.Close();
			Answer.Close();

			//Congratulations, you just requested your first POST page, you
			//can now start logging into most login forms, with your application
			//Or other examples.
			string tmpresult = vystup.Trim() + "\n";
		}
		catch (WebException webex)
		{
			if (webex.Response != null)
				webex.Response.Close();
			actionOnError("Unable to do post php query: " + webex.Message);
		}
		catch (Exception exc)
		{
			actionOnError("Unable to do post php query: " + exc.Message);
			//if (!exc.Message.ToUpper().StartsWith("The remote name could not be resolved:".ToUpper()))
			//	//LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHP, exc.Message);
			//	appendLogTextbox("Post php: " + exc.Message);
			//else //LoggingClass.AddToLogList(UserMessages.MessageTypes.PostPHPremotename, exc.Message);
			//	appendLogTextbox("Post php remote name: " + exc.Message);
			//SysWinForms.MessageBox.Show("Error (092892): " + Environment.NewLine + exc.Message, "Exception error", SysWinForms.MessageBoxButtons.OK, SysWinForms.MessageBoxIcon.Error);
		}
		return vystup;
	}

	/*public static Socket CreateLocalServer(int portNumber, int maximumPendingConnections = 100)
	{
		//IPEndPoint listeningIPEndPoint;
		//listeningIPEndPoint = new IPEndPoint(ipAddress, portNumber);

		//Socket listeningSocket;

		//listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		//listeningSocket.NoDelay = true;
		//listeningSocket.Ttl = 112;
		//listeningSocket.ReceiveBufferSize = receiveBufferSize;

		//listeningSocket.Bind(listeningIPEndPoint);
		//listeningSocket.Listen(maximumPendingConnections);

		//return listeningSocket;

		TcpListener server;
		server = new TcpListener(GetLocalIPaddress(), portNumber);
		server.Start(maximumPendingConnections);

		Console.WriteLine("Waiting for clients...");
		while (true)
		{
			while (!server.Pending())
			{
				ThreadingInterop.PerformVoidFunctionSeperateThread(() => { Thread.Sleep(1000); }, ThreadName: "SleepThread");
			}
			ConnectionThread newconnection = new ConnectionThread();
			newconnection.threadListener = server;
			ThreadPool.QueueUserWorkItem(new WaitCallback(newconnection.HandleConnection));
		}
	}*/

	//public static int maxBufferSize = 100;

	/*class ConnectionThread
	{
		public TcpListener threadListener;
		private static int connections = 0;

		int tmpCounter = 0;
		public void HandleConnection(object state)
		{
			int recv;
			byte[] data = new byte[maxBufferSize];

			TcpClient client = threadListener.AcceptTcpClient();
			NetworkStream ns = client.GetStream();
			connections++;
			Console.WriteLine("New client accepted: {0} active connections",
												 connections);

			string welcome = "Welcome to my test server";
			data = Encoding.ASCII.GetBytes(welcome);
			ns.Write(data, 0, data.Length);

			while (true)
			{
				data = new byte[maxBufferSize];
				if (ns.DataAvailable) recv = ns.Read(data, 0, data.Length);
				else recv = 0;
				if (recv == 0)
					break;

				ns.Write(data, 0, recv);
				Console.WriteLine(tmpCounter++ + "Server, data received: " + Encoding.ASCII.GetString(data));
			}
			ns.Close();
			client.Close();
			connections--;
			Console.WriteLine("ClientOnServerSide disconnected: {0} active connections",
												 connections);
		}
	}*/
}

public class StateObject
{
	// ClientOnServerSide  socket.
	public Socket workSocket = null;
	// Size of receive buffer.
	public const int BufferSize = 1024;
	// Receive buffer.
	public byte[] buffer = new byte[BufferSize];
	// Received data string.
	public StringBuilder sb = new StringBuilder();
}

public class MyEventArgs : EventArgs
{
	private TcpClient sock;
	public TcpClient clientSock
	{
		get { return sock; }
		set { sock = value; }
	}

	public MyEventArgs(TcpClient tcpClient)
	{
		sock = tcpClient;
	}


}

public class Server
{
	public TextFeedbackEventHandler TextFeedbackEvent;

	private TcpListener tcpServer;
	private TcpClient tcpClient;
	private Thread th;
	public ClientOnServerSide ctd;
	private ArrayList formArray = new ArrayList();
	private ArrayList threadArray = new ArrayList();
	List<string> tvClientList = new List<string>();
	public delegate void ChangedEventHandler(object sender, EventArgs e);
	//public event ChangedEventHandler Changed;

	public Server()
	{
		// Add Event to handle when a client is connected
		//Changed += new ChangedEventHandler(ClientAdded);

		// Add node in Tree View
		//TreeNode node;
		//node = tvClientList.Add("Connected Clients");
		//ImageList il = new ImageList();
		//   il.Images.Add(new Icon("audio.ico"));
		//il.Images.Add(new Icon("messenger.ico"));
		//tvClientList.ImageList = il;
		//node.ImageIndex = 1;
	}

	~Server()
	{
		StopServer();
	}

	public void NewClient(Object obj)
	{
		ClientAdded(this, new MyEventArgs((TcpClient)obj));
	}

	private int portNumber;
	public void StartServer(int portNumber)
	{
		this.portNumber = portNumber;
		//tbPortNumber.Enabled = false;
		th = new Thread(new ThreadStart(StartListen));
		th.Start();

	}


	bool IsBusyStoppingServer = false;
	private void StartListen()
	{
		IPAddress localAddr = IPAddress.Parse("127.0.0.1");

		tcpServer = new TcpListener(localAddr, portNumber);
		tcpServer.Start();

		//Console.WriteLine("Server started.");
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Server started.", TextFeedbackType.Success);

		// Keep on accepting ClientOnServerSide Connection
		while (true)
		{
			if (IsBusyStoppingServer)
				break;
			// New ClientOnServerSide connected, call Event to handle it.
			Thread t = new Thread(new ParameterizedThreadStart(NewClient));
			try
			{
				tcpClient = tcpServer.AcceptTcpClient();
			}
			catch (SocketException sexc)
			{
				if (NetworkInterop.IsSocketTryingToCloseUponApplicationExit(sexc))
					break;
				else
					//UserMessages.ShowErrorMessage("Socket error: " + sexc.Message);
					//MessageBox.Show("Socket exception: " + sexc.Message);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Socket exception: " + sexc.Message, TextFeedbackType.Error);
			}
			catch (Exception exc)
			{
				//MessageBox.Show("Error acception Tcp ClientOnServerSide: " + exc.Message);
				if (exc.Message.Equals("Thread was being aborted.", StringComparison.InvariantCultureIgnoreCase))
					break;
				else
					//MessageBox.Show("Error acception Tcp ClientOnServerSide: " + exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Error acception Tcp ClientOnServerSide: " + exc.Message, TextFeedbackType.Error);
				//UserMessages.ShowErrorMessage("Error Accepting Tcp ClientOnServerSide: " + exc.Message);
			}
			t.Start(tcpClient);
		}
	}

	public void StopServer()
	{
		if (tcpServer != null)
		{
			// Close all Socket connection
			foreach (ClientOnServerSide c in formArray)
				c.connectedClient.Client.Close();

			// Abort All Running Threads
			foreach (Thread t in threadArray)
				t.Abort();

			// Clear all ArrayList
			threadArray.Clear();
			formArray.Clear();
			tvClientList.Clear();

			IsBusyStoppingServer = true;
			// Abort Listening Thread and Stop listening
			tcpServer.Stop();
			th.Abort();

		}
		//tbPortNumber.Enabled = true;
	}

	public void ClientAdded(object sender, EventArgs e)
	{
		tcpClient = ((MyEventArgs)e).clientSock;
		String remoteIP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
		String remotePort = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();

		// Call Delegate Function to update Tree View
		UpdateClientList(remoteIP + " : " + remotePort, "Add");

		// Show Dialog Box for Chatting
		ctd = new ClientOnServerSide(this, tcpClient);
		ctd.nestedTextFeedbackEvent += (snder, evtargs) =>
		{
			if (TextFeedbackEvent != null)
				TextFeedbackEvent(snder, evtargs);
		};
		//ctd.Text = "Connected to " + remoteIP + "on port number " + remotePort;
		//Console.WriteLine("Server connected to client: " + remoteIP + "on port number " + remotePort);
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Server connected to client: " + remoteIP + "on port number " + remotePort, TextFeedbackType.Success);

		// Add Dialog Box Object to Array List
		formArray.Add(ctd);
		threadArray.Add(Thread.CurrentThread);
		//ctd.ShowDialog();
	}

	public void DisconnectClient(String remoteIP, String remotePort)
	{
		// Delete ClientOnServerSide from Tree View
		UpdateClientList(remoteIP + " : " + remotePort, "Delete");

		// Find ClientOnServerSide Chat Dialog box corresponding to this Socket
		int counter = 0;
		foreach (ClientOnServerSide c in formArray)
		{
			String remoteIP1 = ((IPEndPoint)c.connectedClient.Client.RemoteEndPoint).Address.ToString();
			String remotePort1 = ((IPEndPoint)c.connectedClient.Client.RemoteEndPoint).Port.ToString();

			if (remoteIP1.Equals(remoteIP) && remotePort1.Equals(remotePort))
			{
				break;
			}
			counter++;
		}

		// Terminate Chat Dialog Box
		//ChatDialog cd = (ChatDialog)formArray[counter];
		if (formArray.Count > counter)
			formArray.RemoveAt(counter);

		if (threadArray.Count > counter)
		{
			((Thread)(threadArray[counter])).Abort();
			threadArray.RemoveAt(counter);
		}
	}

	private void UpdateClientList(string str, string type)
	{
		// If type is Add, the add ClientOnServerSide info in Tree View
		if (type.Equals("Add"))
		{
			this.tvClientList.Add(str);
		}
		// Else delete ClientOnServerSide information from Tree View
		else
		{
			foreach (string s in this.tvClientList)
			{
				if (s.Equals(str))
					this.tvClientList.Remove(s);
			}
		}
	}

	public class ClientOnServerSide
	{
		public TextFeedbackEventHandler nestedTextFeedbackEvent;//Nested inside a Server class

		private TcpClient client;
		private NetworkStream clientStream;
		public delegate void SetTextCallback(string s);
		private Server owner;

		public TcpClient connectedClient
		{
			get { return client; }
			set { client = value; }
		}

		public ClientOnServerSide(Server parent, TcpClient tcpClient)
		{
			this.owner = parent;

			connectedClient = tcpClient;
			clientStream = tcpClient.GetStream();

			// Create the state object.
			StateObject state = new StateObject();
			state.workSocket = connectedClient.Client;

			//Call Asynchronous Receive Function
			connectedClient.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(OnReceive), state);

			//connectedClient.ClientOnServerSide.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), state);
			//rtbChat.AppendText("Chat Log Here------>");
			//Console.WriteLine("ClientOnServerSide has connected");
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, nestedTextFeedbackEvent, "Client has connected.", TextFeedbackType.Success);
		}

		public void Send(string txt)
		{
			byte[] bt;
			bt = Encoding.ASCII.GetBytes(txt);
			//connectedClient.Client.Send(bt);
			Send(bt);

			//rtbChat.SelectionColor = Color.IndianRed;
			//rtbChat.SelectedText = "\nMe:     " + txtMessage.Text;
			//txtMessage.Text = ""; 
		}

		public void Send(byte[] bytes)
		{
			connectedClient.Client.Send(bytes);
		}

		public void OnReceive(IAsyncResult ar)
		{
			String content = String.Empty;

			// Retrieve the state object and the handler socket
			// from the asynchronous state object.
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			int bytesRead;

			if (handler.Connected)
			{

				// Read data from the client socket. 
				try
				{
					bytesRead = handler.EndReceive(ar);
					if (bytesRead > 0)
					{
						// There  might be more data, so store the data received so far.
						state.sb.Remove(0, state.sb.Length);
						state.sb.Append(Encoding.ASCII.GetString(
										 state.buffer, 0, bytesRead));

						// Display Text in Rich Text Box
						content = state.sb.ToString();
						SetText(content);

						handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
							new AsyncCallback(OnReceive), state);

					}
				}

				catch (SocketException socketException)
				{
					//WSAECONNRESET, the other side closed impolitely
					if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
					{
						// Complete the disconnect request.
						String remoteIP = ((IPEndPoint)handler.RemoteEndPoint).Address.ToString();
						String remotePort = ((IPEndPoint)handler.RemoteEndPoint).Port.ToString();
						this.owner.DisconnectClient(remoteIP, remotePort);

						handler.Close();
						handler = null;

					}
				}

			// Eat up exception....Hmmmm I'm loving eat!!!
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message + "\n" + exception.StackTrace);

				}
			}
		}

		private void SetText(string text)
		{
			// InvokeRequired required compares the thread ID of the
			// calling thread to the thread ID of the creating thread.
			// If these threads are different, it returns true.
			//if (this.rtbChat.InvokeRequired)
			//{
			//	SetTextCallback d = new SetTextCallback(SetText);
			//	this.Invoke(d, new object[] { text });
			//}
			//else
			//{
			//this.rtbChat.SelectionColor = Color.Blue;
			//this.rtbChat.SelectedText = "\nFriend: " + text;
			//Console.WriteLine("\nFriend: " + text);
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, nestedTextFeedbackEvent, "Friend: " + text, TextFeedbackType.Noteworthy);
			//}
		}
	}
}

public class ClientOnClientSide
{
	public TextFeedbackEventHandler TextFeedbackEvent;

	public void tmp()
	{
		byte[] data = new byte[1024];
		string input;
		int port;
		TcpClient server;

		//System.Console.WriteLine("Please Enter the port number of Server:\n");
		port = Int32.Parse(
#if WPF
					InputBoxWPF.Prompt(
#elif WINFORMS
DialogBoxStuff.InputDialog(
#elif CONSOLE
GlobalSettings.ReadConsole(
#endif

"Please Enter the port number of Server")); //System.Console.ReadLine());
		try
		{
			server = new TcpClient("127.0.0.1", port);
		}
		catch (SocketException)
		{
			//Console.WriteLine("Unable to connect to server");
			//UserMessages.ShowWarningMessage("Unable to connect to server");
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Unable to connect to server", TextFeedbackType.Error);
			return;
		}
		//Console.WriteLine("Connected to the Server...");
		//UserMessages.ShowInfoMessage("Connected to the Server...");
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Connected to the Server...", TextFeedbackType.Success);
		//Console.WriteLine("Enter the message to send it to the Sever");
		NetworkStream ns = server.GetStream();

		StateObject state = new StateObject();
		state.workSocket = server.Client;
		server.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(OnReceive), state);

		while (true)
		{
			input =
#if WPF
					InputBoxWPF.Prompt(
#elif WINFORMS
 DialogBoxStuff.InputDialog(
#elif CONSOLE
GlobalSettings.ReadConsole(
#endif
"Enter the message to send it to the Sever (type exit to exit)");//Console.ReadLine();
			if (input == "exit" || input == null)
				break;
			ns.Write(Encoding.ASCII.GetBytes(input), 0, input.Length);
			ns.Flush();

			//data = new byte[1024];
			//recv = ns.Read(data, 0, data.Length);
			//stringData = Encoding.ASCII.GetString(data, 0, recv);
			//Console.WriteLine(stringData);
		}
		//Console.WriteLine("Disconnecting from server...");
		//UserMessages.ShowInfoMessage("Disconnecting from server...");
		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, "Disconnecting from Server...", TextFeedbackType.Success);
		ns.Close();
		server.Close();
	}

	public void OnReceive(IAsyncResult ar)
	{
		String content = String.Empty;

		// Retrieve the state object and the handler socket
		// from the asynchronous state object.
		StateObject state = (StateObject)ar.AsyncState;
		Socket handler = state.workSocket;
		int bytesRead;

		if (handler.Connected)
		{

			// Read data from the client socket. 
			try
			{
				bytesRead = handler.EndReceive(ar);
				if (bytesRead > 0)
				{
					// There  might be more data, so store the data received so far.
					state.sb.Remove(0, state.sb.Length);
					state.sb.Append(Encoding.ASCII.GetString(
									 state.buffer, 0, bytesRead));

					// Display Text in Rich Text Box
					content = state.sb.ToString();
					//Console.WriteLine(content);
					//UserMessages.ShowInfoMessage(content);
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(null, TextFeedbackEvent, content, TextFeedbackType.Noteworthy);

					handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
						new AsyncCallback(OnReceive), state);

				}
			}

			catch (SocketException socketException)
			{
				//WSAECONNRESET, the other side closed impolitely
				if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
				{
					handler.Close();
				}
			}

		// Eat up exception....Hmmmm I'm loving eat!!!
			catch (Exception)// exception)
			{
				//MessageBox.Show(exception.Message + "\n" + exception.StackTrace);

			}
		}
	}
}