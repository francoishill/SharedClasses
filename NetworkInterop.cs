﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

//TODO: Check out extension functions (a static class with methods which uses this as one of the parameters)
public static class StringExtensions
{
	public static bool IsFrancois(this string str, bool CaseSensitive)
	{
		return str.Equals("Francois", CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
	}

	public static bool IsNullOrEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}

	public static bool IsNullOrWhiteSpace(this string str)
	{
		return string.IsNullOrWhiteSpace(str);
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

	//		if (tcpclient.Client != null)
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

	public static IPAddress GetIPAddressFromString(string ipAddressString, int timeout = 10000)
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
			try
			{
				GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
				IAsyncResult result = callback.BeginInvoke(ipAddressString, null, null);
				if (result.AsyncWaitHandle.WaitOne(timeout, false))
				{
					IPHostEntry iphostEntry = callback.EndInvoke(result);
					if (iphostEntry == null || iphostEntry.AddressList.Length == 0)
					{
						UserMessages.ShowErrorMessage("Could not resolve DNS from " + ipAddressString);
						return null;
					}
					else returnIPAddress = iphostEntry.AddressList[0];
				}
				else
				{
					UserMessages.ShowErrorMessage("Timeout to resolve DNS from " + ipAddressString);
					return null;
				}
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Error occurred resolving DNS from " + ipAddressString + ": " + exc.Message);
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

	public static void SetupServerSocketSettings(ref Socket serverListeningSocketToUse, int listeningPort, int maxBufferPerTransfer, int maxNumberPendingConnections)
	{
		serverListeningSocketToUse.NoDelay = true;
		serverListeningSocketToUse.Ttl = 112;
		serverListeningSocketToUse.ReceiveBufferSize = maxBufferPerTransfer;
		serverListeningSocketToUse.SendBufferSize = maxBufferPerTransfer;
		serverListeningSocketToUse.Bind(NetworkInterop.GetLocalIPEndPoint(listeningPort));
		serverListeningSocketToUse.Listen(maxNumberPendingConnections);
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

	private static void EnsureValuesForGuidAndTotalSizes(ProgressChangedEventHandler ProgressChangedEvent, long totalBytesProcessed, byte[] firstConstantBytesForGuidInfoandFilesize, ref Guid receivedGuid, ref long totalFileSizeToRead, ref long totalInfoSizeToRead, int actualReceivedLength, bool UpdateProgress = true)
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
				UserMessages.ShowWarningMessage("Could not get info size from string = " + totalInfoSizeToReadString);
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
				UserMessages.ShowWarningMessage("Could not get file size from string = " + totalFileSizeToReadString);
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

	private static string ObtainOriginalFilenameFromInfoOfTransferToServer(InfoOfTransferToServer info, ref TextFeedbackEventHandler TextFeedbackEvent)
	{
		if (info == null && UserMessages.ShowWarningMessage("Cannot obtain filename from NULL InfoOfTransferToServer object"))
			return null;
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

	private static void CloseAndDisposeMemoryStream(ref MemoryStream memorystream)
	{
		memorystream.Close();
		memorystream.Dispose();
		memorystream = null;
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

		SetupServerSocketSettings(ref serverListeningSocketToUse, listeningPort, maxBufferPerTransfer, maxNumberPendingConnections);

		TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, TextFeedbackEvent, "Server started, waiting for clients...");

		while (true)
		{
			Socket handler = null;
			try { handler = serverListeningSocketToUse.Accept(); }
			catch (SocketException sexc)
			{
				if (IsSocketTryingToCloseUponApplicationExit(sexc)) break;
				else UserMessages.ShowErrorMessage("SocketException occurred: " + sexc.Message);
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

				EnsureValuesForGuidAndTotalSizes(ProgressChangedEvent, totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize, ref receivedGuid, ref totalFileSizeToRead, ref totalInfoSizeToRead, actualReceivedLength);

				WriteBytesToMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

				if (totalInfoSizeToRead != -1 && totalFileSizeToRead > 0 && totalBytesProcessed + actualReceivedLength >= lengthOfFirstConstantBuffer + totalInfoSizeToRead)
				{
					if (localFileName == null)
						localFileName = ObtainOriginalFilenameFromInfoOfTransferToServer((InfoOfTransferToServer)SerializationInterop.DeserializeCustomObjectFromStream(memoryStreamForInfo, new InfoOfTransferToServer(), false), ref TextFeedbackEvent);
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

			CloseAndDisposeMemoryStream(ref memoryStreamForInfo);

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
		IPAddress ipAddress = null,
		int listeningPort = defaultListeningPort,
		int maxBufferPerTransfer = defaultMaxBufferPerTransfer,
		TextFeedbackEventHandler TextFeedbackEvent = null,
		ProgressChangedEventHandler ProgressChangedEvent = null)
	{
		senderSocketToUse = null;
		if (!File.Exists(filePath))
			UserMessages.ShowWarningMessage("File does not exist and cannot be transferred: " + filePath);
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

					EnsureValuesForGuidAndTotalSizes(ProgressChangedEvent, totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize, ref receivedGuid, ref totalFileSizeToRead, ref totalInfoSizeToRead, actualReceivedLength, false);

					WriteBytesToMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

					if (totalInfoSizeToRead != -1 && totalBytesProcessed + actualReceivedLength >= lengthOfFirstConstantBuffer + totalInfoSizeToRead)
					{
						if (totalFileSizeToRead > 0)
							UserMessages.ShowWarningMessage("Function not incorporated yet to transfer file back to client.");
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
							CloseAndDisposeMemoryStream(ref memoryStreamForInfo);
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

	public static void MergeFiles(string firstFileName, bool DeleteOriginalFilesUponSuccess = true)
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
			if (UserMessages.Confirm(orgFile + " already exists, do you want to delete it"))
				File.Delete(orgFile);
			else
			{
				UserMessages.ShowInfoMessage("File not assembled. Operation cancelled by user.");
				return;
			}
		}

		//Assembling starts from here
		BinaryWriter bw = new BinaryWriter(File.Open(orgFile, FileMode.Append));
		string nextFileName = "";
		byte []buffer=new byte[bw.BaseStream.Length];


		int counter=int.Parse(endPart);
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

	public async static Task FtpUploadFiles(Object textfeedbackSenderObject, string ftpRootUri, string userName, string password, string[] localFilenames, string urlWhenSuccessullyUploaded = null, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null)
	{
		try
		{
			bool DirexistCanContinue = false;
			if (!await FtpDirectoryExists(ftpRootUri, userName, password))
			{
				if (await CreateFTPDirectory(ftpRootUri, userName, password))
					DirexistCanContinue = true;
			}
			else DirexistCanContinue = true;
			if (DirexistCanContinue)
			{
				using (System.Net.WebClient client = new System.Net.WebClient())
				{
					client.Credentials = new System.Net.NetworkCredential(userName, password);
					//client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");
					foreach (string localFilename in localFilenames)
					{
						string fileNameOnServer = new FileInfo(localFilename).Name;
						Console.WriteLine("fileNameOnServer" + fileNameOnServer);
						string dirOnFtpServer = ftpRootUri + "/" + fileNameOnServer;

						bool isComplete = false;
						client.UploadFileCompleted += (snder, evtargs) =>
						{
							isComplete = true;
							RaiseProgressChangedEvent_Ifnotnull(ref progressChanged,
								100,
								100);
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
						await client.UploadFileTaskAsync(dirOnFtpServer, "STOR", localFilename);
						TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Successfully uploaded " + fileNameOnServer);
					}
					if (urlWhenSuccessullyUploaded != null) Process.Start(urlWhenSuccessullyUploaded);
					client.Dispose();
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
			}
			else UserMessages.ShowErrorMessage("Could not upload files (could not find/create directory online: " + ftpRootUri);
		}
		catch (Exception exc)
		{
			if (exc.Message.ToLower().Contains("the operation has timed out"))
			{
				if (UserMessages.Confirm("Upload to ftp timed out, the System.Net.ServicePointManager.DefaultConnectionLimit has been reached, restart the application now?"))
					//Application.Restart();
					ApplicationRecoveryAndRestart.TestCrash(false);
			}
			MessageBox.Show("Exception in transfer: " + exc.Message);
		}
	}

	public async static Task<bool> FtpDirectoryExists(string directoryPath, string ftpUser, string ftpPassword)
	{
		bool IsExists = true;
		try
		{
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create(directoryPath);
			request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
			request.Method = WebRequestMethods.Ftp.ListDirectory;

			FtpWebResponse response = (FtpWebResponse)(await request.GetResponseAsync());
			IsExists = true;
		}
		catch (WebException ex)
		{
			Console.WriteLine("WebException on FtpDirectoryExists" + ex.Message);
			IsExists = false;
		}
		return IsExists;
	}

	public async static Task<bool> CreateFTPDirectory(string directory, string ftpUser, string ftpPassword)
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
			FtpWebResponse response = (FtpWebResponse)(await requestDir.GetResponseAsync());
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
			Console.WriteLine("Client disconnected: {0} active connections",
												 connections);
		}
	}*/
}


public class AsyncTcpClient
{
	private IPAddress[] addresses;
	private int port;
	private WaitHandle addressesSet;
	private TcpClient tcpClient;
	private int failedConnectionCount;

	/// <summary>
	/// Construct a new client from a known IP Address
	/// </summary>
	/// <param name="address">The IP Address of the server</param>
	/// <param name="port">The port of the server</param>
	public AsyncTcpClient(IPAddress address, int port)
		: this(new[] { address }, port)
	{
	}

	/// <summary>
	/// Construct a new client where multiple IP Addresses for
	/// the same client are known.
	/// </summary>
	/// <param name="addresses">The array of known IP Addresses</param>
	/// <param name="port">The port of the server</param>
	public AsyncTcpClient(IPAddress[] addresses, int port)
		: this(port)
	{
		this.addresses = addresses;
	}

	/// <summary>
	/// Construct a new client where the address or host name of
	/// the server is known.
	/// </summary>
	/// <param name="hostNameOrAddress">The host name or address of the server</param>
	/// <param name="port">The port of the server</param>
	public AsyncTcpClient(string hostNameOrAddress, int port)
		: this(port)
	{
		addressesSet = new AutoResetEvent(false);
		Dns.BeginGetHostAddresses(hostNameOrAddress, GetHostAddressesCallback, null);
	}

	/// <summary>
	/// Private constuctor called by other constuctors
	/// for common operations.
	/// </summary>
	/// <param name="port"></param>
	private AsyncTcpClient(int port)
	{
		if (port < 0)
			throw new ArgumentException();
		this.port = port;
		this.tcpClient = new TcpClient();
		this.Encoding = Encoding.Default;
	}

	/// <summary>
	/// The endoding used to encode/decode string when sending and receiving.
	/// </summary>
	public Encoding Encoding { get; set; }

	/// <summary>
	/// Attempts to connect to one of the specified IP Addresses
	/// </summary>
	public void Connect()
	{
		if (addressesSet != null)
			//Wait for the addresses value to be set
			addressesSet.WaitOne();
		//Set the failed connection count to 0
		Interlocked.Exchange(ref failedConnectionCount, 0);
		//Start the async connect operation
		tcpClient.BeginConnect(addresses, port, ConnectCallback, null);
	}

	/// <summary>
	/// Writes a string to the network using the defualt encoding.
	/// </summary>
	/// <param name="data">The string to write</param>
	/// <returns>A WaitHandle that can be used to detect
	/// when the write operation has completed.</returns>
	public void Write(string data)
	{
		byte[] bytes = Encoding.GetBytes(data);
		Write(bytes);
	}

	/// <summary>
	/// Writes an array of bytes to the network.
	/// </summary>
	/// <param name="bytes">The array to write</param>
	/// <returns>A WaitHandle that can be used to detect
	/// when the write operation has completed.</returns>
	public void Write(byte[] bytes)
	{
		NetworkStream networkStream = tcpClient.GetStream();
		//Start async write operation
		networkStream.BeginWrite(bytes, 0, bytes.Length, WriteCallback, null);
	}

	/// <summary>
	/// Callback for Write operation
	/// </summary>
	/// <param name="result">The AsyncResult object</param>
	private void WriteCallback(IAsyncResult result)
	{
		NetworkStream networkStream = tcpClient.GetStream();
		networkStream.EndWrite(result);
	}

	/// <summary>
	/// Callback for Connect operation
	/// </summary>
	/// <param name="result">The AsyncResult object</param>
	private void ConnectCallback(IAsyncResult result)
	{
		try
		{
			tcpClient.EndConnect(result);
		}
		catch
		{
			//Increment the failed connection count in a thread safe way
			Interlocked.Increment(ref failedConnectionCount);
			if (failedConnectionCount >= addresses.Length)
			{
				//We have failed to connect to all the IP Addresses
				//connection has failed overall.
				return;
			}
		}

		//We are connected successfully.
		NetworkStream networkStream = tcpClient.GetStream();
		byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
		//Now we are connected start asyn read operation.
		networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);
	}

	/// <summary>
	/// Callback for Read operation
	/// </summary>
	/// <param name="result">The AsyncResult object</param>
	private void ReadCallback(IAsyncResult result)
	{
		int read;
		NetworkStream networkStream;
		try
		{
			networkStream = tcpClient.GetStream();
			read = networkStream.EndRead(result);
		}
		catch
		{
			//An error has occured when reading
			return;
		}

		if (read == 0)
		{
			//The connection has been closed.
			return;
		}

		byte[] buffer = result.AsyncState as byte[];
		string data = this.Encoding.GetString(buffer, 0, read);
		//Do something with the data object here.
		//Then start reading from the network again.
		networkStream.BeginRead(buffer, 0, buffer.Length, ReadCallback, buffer);
	}

	/// <summary>
	/// Callback for Get Host Addresses operation
	/// </summary>
	/// <param name="result">The AsyncResult object</param>
	private void GetHostAddressesCallback(IAsyncResult result)
	{
		addresses = Dns.EndGetHostAddresses(result);
		//Signal the addresses are now set
		((AutoResetEvent)addressesSet).Set();
	}
}