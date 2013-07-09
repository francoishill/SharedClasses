﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

public class NetworkInterop
{
	public static IPAddress GetIPAddressFromString(string ipAddressString)
	{
		bool resolveDndMode = false;
		foreach (char chr in ipAddressString)
			if (!char.IsNumber(chr) && chr != '.')
				resolveDndMode = true;
		IPAddress returnIPAddress = null;

		if (!resolveDndMode && !IPAddress.TryParse(ipAddressString, out returnIPAddress))
		{
			UserMessages.ShowErrorMessage("Invalid IP address: " + (ipAddressString ?? ""));
			return null;
		}
		if (resolveDndMode)
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
	}

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

	public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
	public class ProgressChangedEventArgs : EventArgs
	{
		public int CurrentValue;
		public int MaximumValue;
		public double BytesPerSecond;
		public ProgressChangedEventArgs(int CurrentValueIn, int MaximumValueIn, double BytesPerSecondIn = -1)
		{
			CurrentValue = CurrentValueIn;
			MaximumValue = MaximumValueIn;
			BytesPerSecond = BytesPerSecondIn;
		}
	}

	public delegate void TextFeedbackEventHandler(object sender, TextFeedbackEventArgs e);
	public class TextFeedbackEventArgs : EventArgs
	{
		public string FeedbackText;
		public TextFeedbackEventArgs(string FeedbackTextIn)
		{
			FeedbackText = FeedbackTextIn;
		}
	}

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

	private static void RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEventHandler textFeedbackEvent, string textMessage)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(null, new TextFeedbackEventArgs(textMessage));
	}

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

	private static void SetupSocketSettings(Socket serverListeningSocketToUse, int listeningPort, int maxBufferPerTransfer, int maxNumberPendingConnections)
	{
		serverListeningSocketToUse.NoDelay = true;
		serverListeningSocketToUse.Ttl = 112;
		serverListeningSocketToUse.ReceiveBufferSize = maxBufferPerTransfer;
		serverListeningSocketToUse.SendBufferSize = maxBufferPerTransfer;
		serverListeningSocketToUse.Bind(NetworkInterop.GetLocalIPEndPoint(listeningPort));
		serverListeningSocketToUse.Listen(maxNumberPendingConnections);
	}

	private static bool IsSocketTryingToCloseUponApplicationExit(SocketException sexc)
	{
		/* This is normal behavior when interrupting a blocking socket (i.e. waiting for clients). WSACancelBlockingCall is called and a SocketException
		is thrown (see my post above). Just catch this exception and use it to exit the thread ('break' in the infinite while loop).
		http://www.switchonthecode.com/tutorials/csharp-tutorial-simple-threaded-tcp-server */
		return sexc.Message.ToLower().Contains("WSACancelBlockingCall".ToLower());
	}

	private static bool GetBytesAvailable(ref Socket socketToCheck, out int AvailableBytes)
	{
		if (socketToCheck == null)
		{
			AvailableBytes = 0;
			return false;
		}
		AvailableBytes = socketToCheck.Available;
		return AvailableBytes > 0 ? true : false;
	}

	private static void SendResponseToClient(bool SuccessfullyCompletedTransfer, ref Socket handler, Guid receivedGuid, DateTime timeTransferStarted, long totalBytesTransferred, long totalSizeToRead)
	{
		NetworkStream ns = new NetworkStream(handler);

		double averageBytesPerSecond = totalBytesTransferred / (new TimeSpan(DateTime.Now.Ticks - timeTransferStarted.Ticks).TotalSeconds);
		byte[] bytesOfInfo = GetSerializedBytesOfObject(
			new InfoOfTransferToClient(
				SuccessfullyCompletedTransfer,
				new TimeSpan(DateTime.Now.Ticks - timeTransferStarted.Ticks).TotalSeconds,
				averageBytesPerSecond,
				totalBytesTransferred,
				totalSizeToRead));
		int infoSize = bytesOfInfo.Length;
		Console.WriteLine("Number bytes sent to client: " + infoSize);

		WriteGuidSectionToStream(ref ns, receivedGuid);
		WriteInfoSectionSizeToStream(ref ns, infoSize);
		WriteFileSizeToStream(ref ns, 0);

		ns.Write(bytesOfInfo, 0, infoSize);
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

	private static void WriteBytesToFilestreamAndOrMemorystream(long totalBytesProcessed, long totalInfoSizeToRead, ref FileStream fileStreamIn, ref MemoryStream memoryStreamForInfo, ref byte[] receivedBytes, int actualReceivedLength)
	{
		if (totalBytesProcessed < lengthOfFirstConstantBuffer)
		{
			if (totalBytesProcessed + actualReceivedLength > lengthOfFirstConstantBuffer)
			{
				long memoryStreamStartBytes = lengthOfFirstConstantBuffer - totalBytesProcessed;
				long memoryStreamNumberBytesToRead = (int)(totalBytesProcessed + actualReceivedLength - lengthOfFirstConstantBuffer);
				if (memoryStreamNumberBytesToRead > lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed)
					memoryStreamNumberBytesToRead = lengthOfFirstConstantBuffer + totalInfoSizeToRead - totalBytesProcessed;

				memoryStreamForInfo.Write(receivedBytes, (int)memoryStreamStartBytes, (int)memoryStreamNumberBytesToRead);

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

	private static void RenameFileBasedOnInfoOfTransfer(InfoOfTransferToServer info, ref TextFeedbackEventHandler TextFeedbackEvent)
	{
		if (info == null) return;
		string fromPath = defaultFilePathForSavingForServer;
		string toPath = defaultFolderToSaveIn + "\\" + Path.GetFileName(info.OriginalFilePath);
		if (File.Exists(fromPath))
		{
			bool FileDoesExist = false;
			if (File.Exists(toPath))
			{
				try
				{
					FileDoesExist = true;
					File.Delete(toPath);
					FileDoesExist = false;
				}
				catch (Exception exc)
				{
					RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEvent, "Unable to delete file " + toPath + Environment.NewLine + "Old file name remains: " + fromPath + Environment.NewLine + exc.Message);
					//UserMessages.ShowWarningMessage("Unable to delete file " + toPath + Environment.NewLine + "Old file name remains: " + fromPath + Environment.NewLine + exc.Message);
				}
			}
			if (!FileDoesExist) File.Move(fromPath, toPath);
		}
		else
		{
			UserMessages.ShowErrorMessage("Could not find written file: " + fromPath);
		}
	}

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

		SetupSocketSettings(serverListeningSocketToUse, listeningPort, maxBufferPerTransfer, maxNumberPendingConnections);

		RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEvent, "Server started, waiting for clients...");

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
			FileStream fileStreamIn = new FileStream(defaultFilePathForSavingForServer, FileMode.Create);
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
					SendResponseToClient(false, ref handler, receivedGuid, timeTransferStarted, totalBytesProcessed + actualReceivedLength, lengthOfFirstConstantBuffer + totalFileSizeToRead + totalInfoSizeToRead);

				EnsureValuesForGuidAndTotalSizes(ProgressChangedEvent, totalBytesProcessed, firstConstantBytesForGuidInfoandFilesize, ref receivedGuid, ref totalFileSizeToRead, ref totalInfoSizeToRead, actualReceivedLength);

				WriteBytesToFilestreamAndOrMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref fileStreamIn, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

				totalBytesProcessed += actualReceivedLength;

				//Console.WriteLine("FireProgressChangedEventForTransfer: TBP=" + totalBytesProcessed + ", TFS=" + totalFileSizeToRead + ", TIS=" + totalInfoSizeToRead);
				FireProgressChangedEventForTransfer(ref ProgressChangedEvent, totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead, timeTransferStarted);

				if (IsAlldataCompletelyTransferred(totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead))
				{
					SendResponseToClient(true, ref handler, receivedGuid, timeTransferStarted, totalBytesProcessed, lengthOfFirstConstantBuffer + totalFileSizeToRead + totalInfoSizeToRead);
					break;
				}
			}

			CloseAndDisposeFileStream(ref fileStreamIn);

			RenameFileBasedOnInfoOfTransfer((InfoOfTransferToServer)SerializationInterop.DeserializeCustom(memoryStreamForInfo, new InfoOfTransferToServer(), false), ref TextFeedbackEvent);

			CloseAndDisposeMemoryStream(ref memoryStreamForInfo);

			RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, 0, 100);
		}
	}

	private static Dictionary<Form, Socket> dictionaryWithFormAndSocketsToCloseUponFormClosing = new Dictionary<Form, Socket>();
	/// <summary>
	/// Starts a Tcp server on specified settings. Can also attach event hooks to:
	/// NetworkInterop.textFeedback, and NetworkInterop.progressChanged. Just remember
	/// to use ThreadingInterop.UpdateGuiFromThread if server was created on separate thread.
	/// </summary>
	/// <param name="serverListeningSocketToUse">The socket only has to be assigned with a NULL.</param>
	/// <param name="formToHookSocketClosingIntoFormDisposedEvent">The form (usually main form) to hook unto its closing event to close the server socket.</param>
	/// <param name="listeningPort">The port for the server to listen on.</param>
	/// <param name="FolderToSaveIn">The folder to use when receiving files.</param>
	/// <param name="maxBufferPerTransfer">Maximum file size per transfer (files larger than this buffer will be split).</param>
	/// <param name="maxTotalFileSize">Maximum size of the total size (including all splits if larger than maximum buffer).</param>
	/// <param name="maxNumberPendingConnections">Maximum number of pending connections to keep waiting while one is busy.</param>
	public static void StartServer(
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
		serverListeningSocketToUse.NoDelay = true;
		serverListeningSocketToUse.Ttl = 112;
		serverListeningSocketToUse.ReceiveBufferSize = maxBufferPerTransfer;

		if (formToHookSocketClosingIntoFormDisposedEvent != null)
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
						dictionaryWithFormAndSocketsToCloseUponFormClosing[snder as Form].Blocking = false;
						dictionaryWithFormAndSocketsToCloseUponFormClosing[snder as Form].Close();
					}
				}
			};
		}

		string data = null;

		bool checkout;//function string.IsNullOrEmpty
		serverListeningSocketToUse.Bind(NetworkInterop.GetLocalIPEndPoint(listeningPort));
		serverListeningSocketToUse.Listen(maxNumberPendingConnections);

		if (TextFeedbackEvent != null) TextFeedbackEvent(null, new TextFeedbackEventArgs("Server started, waiting for clients..."));

		bool continuewith;//this conecpt (due to a large file being split)
		Dictionary<string, bool?> listofGuidAndIsComplete = new Dictionary<string, bool?>();

		// Start listening for connections.
		while (true)
		{
			//Application.DoEvents();
			//AppendRichTextBox("Waiting for a connection...");
			// Program is suspended while waiting for an incoming connection.
			Socket handler = null;
			try
			{
				handler = serverListeningSocketToUse.Accept();
			}
			catch (SocketException sexc)
			{
				if (sexc.Message.ToLower().Contains("WSACancelBlockingCall".ToLower()))
				{
					/*
					 This is normal behavior when interrupting a blocking socket
					 (i.e. waiting for clients). WSACancelBlockingCall is called and a SocketException
					 is thrown (see my post above).
					 Just catch this exception and use it to exit the thread
					 ('break' in the infinite while loop).
					 http://www.switchonthecode.com/tutorials/csharp-tutorial-simple-threaded-tcp-server
					 */
					break;
				}
				else
				{
					UserMessages.ShowErrorMessage("SocketException occurred: " + sexc.Message);
				}
			}

			if (handler == null) continue;

			string first26Characters = "";

			Application.DoEvents();
			data = null;
			string startdata = "";

			int maxProgress = 0;
			//Console.WriteLine("Data transfer started...");
			// An incoming connection needs to be processed.
			//SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, 0, 1000 * 1024 * 1024);
			int totalbytelength = 0;
			byte[] allbytes = new byte[maxTotalFileSize];
			while (true)
			{
				byte[] currentbytes;
				int currbytelength = handler.Available;
				totalbytelength += currbytelength;
				//progressBar1.Value = progressBar1.Maximum * totalbytelength / maxProgress;
				currentbytes = new byte[currbytelength];
				int bytesRec = handler.Receive(currentbytes);
				//Console.WriteLine("Total bytes transferred: " + totalbytelength);
				currentbytes.CopyTo(allbytes, totalbytelength - currbytelength);

				if (first26Characters.Length < 26)
				{
					for (int i = 0; i < currbytelength; i++)
					{
						if (first26Characters.Length < 26)
							first26Characters += Encoding.ASCII.GetString(currentbytes, i, 1);
					}
				}

				data += Encoding.ASCII.GetString(currentbytes);

				if (startdata.Length < 10000) startdata += data;
				if (startdata.StartsWith("totalsize://"))
				{
					string totalSizeString = startdata.Substring(0, "totalsize://0000000000000000//:endoftotalsize".Length);
					long Totalsize;
					if (long.TryParse(totalSizeString.Substring(12, 16), out Totalsize))
						if (maxProgress != Totalsize) maxProgress = (int)Totalsize;
					totalSizeString = null;
					Totalsize = 0;
				}

				if (maxProgress > 0 && totalbytelength >= 0 && totalbytelength <= maxProgress)
					if (ProgressChangedEvent != null)
						ProgressChangedEvent(null, new ProgressChangedEventArgs(totalbytelength, maxProgress));

				if (data.IndexOf("<EOF>") > -1)
				{
					break;
				}
				if (data.Length > 10) data = data.Substring(data.Length - 10);

				currbytelength = 0;
				currentbytes = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Console.WriteLine("Data transfer complete.");

			// Show the data on the console
			if (startdata.StartsWith("totalsize://"))
			{
				if (!Directory.Exists(FolderToSaveIn)) Directory.CreateDirectory(FolderToSaveIn);
				string totalSizeString = startdata.Substring(0, "totalsize://0000000000000000//:endoftotalsize".Length);
				string originalFileName = startdata.Substring(totalSizeString.Length + "file://".Length, startdata.IndexOf("//:endoffile") - totalSizeString.Length - "file://".Length);
				int filestringlength = "file://".Length + originalFileName.Length + "//:endoffile".Length;

				byte[] filebytes = new byte[totalbytelength - totalSizeString.Length - filestringlength - 5];
				for (int i = totalSizeString.Length + filestringlength; i < totalbytelength - 5; i++)
					filebytes[i - totalSizeString.Length - filestringlength] = allbytes[i];

				string newFileName = FolderToSaveIn + @"\" + Path.GetFileName(originalFileName);
				//Console.WriteLine("Original filename: " + originalFileName);
				//Console.WriteLine("New filename: " + newFileName);
				//Console.WriteLine("Writing file...");
				File.WriteAllBytes(newFileName, filebytes);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				filestringlength = 0;
				totalSizeString = null;
				newFileName = null;
				filebytes = null;
				newFileName = null;

				if (originalFileName.ToUpper().EndsWith(".final".ToUpper()))
				{
					string firstFilename = FolderToSaveIn + @"\" + Path.GetFileName(originalFileName).Substring(0, Path.GetFileName(originalFileName).Length - 6);
					string finalFilename = firstFilename.Substring(0, firstFilename.LastIndexOf("."));
					firstFilename = firstFilename.Substring(0, firstFilename.LastIndexOf(".")) + ".0";
					MergeFiles(firstFilename);
					System.Diagnostics.Process.Start("explorer", "/select, \"" + finalFilename + "\"");
					if (ProgressChangedEvent != null)
						ProgressChangedEvent(null, new ProgressChangedEventArgs(0, 100));
				}

				originalFileName = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			else
			{
				Console.WriteLine(data.Replace("<EOF>", ""));
			}

			totalbytelength = 0;
			maxProgress = 0;
			allbytes = null;
			data = null;
			startdata = null;
			first26Characters = null;

			handler.Shutdown(SocketShutdown.Both);
			handler.Disconnect(false);
			handler.Close();
			handler.Dispose();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			Application.DoEvents();
		}
	}

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
		SerializationInterop.SerializeCustom(obj, memoryStream, false);
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

				int maxBytesToReadfromFile = 10240;
				int numberReadBytes;
				long totalBytesWritten = 0;
				do
				{
					byte[] bytesRead = new byte[maxBytesToReadfromFile];
					numberReadBytes = fileToWrite.Read(bytesRead, 0, maxBytesToReadfromFile);
					if (numberReadBytes > 0)
						networkStream.Write(bytesRead, 0, numberReadBytes);
					totalBytesWritten += numberReadBytes;
					//RaiseProgressChangedEvent_Ifnotnull(ref ProgressChangedEvent, (int)totalBytesWritten, (int)totalFileSizeToTransfer);
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
					FileStream fileStreamIn = null;// new FileStream(defaultFilePathForSavingForClient, FileMode.Create);
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

					WriteBytesToFilestreamAndOrMemorystream(totalBytesProcessed, totalInfoSizeToRead, ref fileStreamIn, ref memoryStreamForInfo, ref receivedBytes, actualReceivedLength);

					totalBytesProcessed += actualReceivedLength;

					//FireProgressChangedEventForTransfer(ref ProgressChangedEvent, totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead, timeTransferStarted);

					if (IsAlldataCompletelyTransferred(totalBytesProcessed, totalFileSizeToRead, totalInfoSizeToRead))
					{
						//CloseAndDisposeFileStream(ref fileStreamIn);
						InfoOfTransferToClient info = (InfoOfTransferToClient)SerializationInterop.DeserializeCustom(memoryStreamForInfo, new InfoOfTransferToClient(), false);
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
								RaiseTextFeedbackEvent_Ifnotnull(ref TextFeedbackEvent,
								"Successfully transferred (" + info.SuccessfullyReceiveComplete + ") file = " + filePath
								+ Environment.NewLine + "In " + info.DurationOfTransferInSeconds + " seconds"
								+ Environment.NewLine + "At " + info.AverageBytesPerSecond + " B/s");
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

				fileToWrite.Close();
				fileToWrite.Dispose(); fileToWrite = null;
				networkStream.Close();
				networkStream.Dispose(); networkStream = null;
			}
		}
	}

	public static void TransferFile(
		string filePath,
		out Socket senderSocketToUse,
		IPAddress ipAddress = null,
		int listeningPort = defaultListeningPort,
		int maxBufferPerTransfer = defaultMaxBufferPerTransfer,
		TextFeedbackEventHandler TextFeedbackEvent = null)
	{
		senderSocketToUse = null;
		byte[] byData;
		if (!File.Exists(filePath))
			UserMessages.ShowWarningMessage("File does not exist and cannot be transferred: " + filePath);
		else
		{
			int counter = 0;
			byte[] AllFileDataBytes = File.ReadAllBytes(filePath);

			while (counter * maxBufferPerTransfer < AllFileDataBytes.Length)
			{
				if (ConnectToServer(out senderSocketToUse, ipAddress, listeningPort))
				{
					if (TextFeedbackEvent != null)
						TextFeedbackEvent(null, new TextFeedbackEventArgs("Connected to server, transferring file segment " + (counter + 1) + "..."));
					string fileAddonExtension = ((counter + 1) * maxBufferPerTransfer) >= AllFileDataBytes.Length
							? "." + counter + ".final"
							: "." + counter;
					byte[] FileStartBytes = System.Text.Encoding.ASCII.GetBytes("file://" + filePath + fileAddonExtension + "//:endoffile");

					long PieceSize = AllFileDataBytes.Length - (counter * maxBufferPerTransfer) < maxBufferPerTransfer
							? AllFileDataBytes.Length - (counter * maxBufferPerTransfer)
							: maxBufferPerTransfer;

					byte[] FileDataPieceBytes = new byte[PieceSize];
					for (int i = counter * maxBufferPerTransfer; i < (counter + 1) * maxBufferPerTransfer && i < AllFileDataBytes.Length; i++)
						FileDataPieceBytes[i - counter * maxBufferPerTransfer] = AllFileDataBytes[i];

					byte[] EOFbytes = System.Text.Encoding.ASCII.GetBytes("<EOF>");

					int TotalByteCount = Encoding.ASCII.GetBytes("totalsize://0000000000000000//:endoftotalsize").Length
						+ FileStartBytes.Length + FileDataPieceBytes.Length + EOFbytes.Length;

					byte[] TotalByteCountBytes = Encoding.ASCII.GetBytes("totalsize://" + Get16lengthStringOfNumber(TotalByteCount) + "//:endoftotalsize");

					byData = new byte[TotalByteCount];
					TotalByteCountBytes.CopyTo(byData, 0);
					FileStartBytes.CopyTo(byData, TotalByteCountBytes.Length);
					FileDataPieceBytes.CopyTo(byData, TotalByteCountBytes.Length + FileStartBytes.Length);
					EOFbytes.CopyTo(byData, TotalByteCountBytes.Length + FileStartBytes.Length + FileDataPieceBytes.Length);

					senderSocketToUse.Send(byData);
					PieceSize = 0;
					TotalByteCount = 0;
					fileAddonExtension = null;
					FileStartBytes = null;
					FileDataPieceBytes = null;
					EOFbytes = null;
					TotalByteCountBytes = null;

					senderSocketToUse.Close();
					senderSocketToUse.Dispose();
					senderSocketToUse = null;

					counter++;
				}
			}

			counter = 0;
			AllFileDataBytes = null;
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
			socketToInitialize.ReceiveTimeout = 1000;
			socketToInitialize.SendTimeout = 1000;
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
