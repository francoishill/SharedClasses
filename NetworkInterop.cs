using System;
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

		if (!resolveDndMode && !IPAddress.TryParse(ipAddressString, out returnIPAddress)){
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
		public ProgressChangedEventArgs(int CurrentValueIn, int MaximumValueIn)
		{
			CurrentValue = CurrentValueIn;
			MaximumValue = MaximumValueIn;
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

	public static event ProgressChangedEventHandler progressChanged;
	public static event TextFeedbackEventHandler textFeedback;

	private const string defaultFolderToSaveIn = @"c:\tempReceived";//@"C:\Francois\other\Test\CS_TestListeningServerReceivedFiles";
	private const int defaultListeningPort = 11000;
	private const int defaultMaxNumberPendingConnections = 100;
	private const int defaultMaxBufferPerTransfer = 1024 * 1024 * 10;
	private const int defaultMaxTotalFileSize = 1024 * 1024 * 1000;//10;
	//public static int maxTransferBuffer = 1024 * 1024 * 10;

	private static Dictionary<Form, Socket> dictionaryWithFormAndSocketsToCloseUponFormClosing = new Dictionary<Form, Socket>();
	/*
	 
	 */
	/// <summary>
	/// Starts a Tcp server on specified settings. Can also attach event hooks to:
	/// NetworkInterop.textFeedback, and NetworkInterop.progressChanged. Just remember
	/// to use ThreadingInterop.UpdateGuiFromThread if server was created on separate thread.
	/// </summary>
	/// <param name="serverListeningSocketToUse">The socket only has to be assigned with a NULL.</param>
	/// <param name="formToHookSocketClosingIntoFormClosingEvent">The form (usually main form) to hook unto its closing event to close the server socket.</param>
	/// <param name="listeningPort">The port for the server to listen on.</param>
	/// <param name="FolderToSaveIn">The folder to use when receiving files.</param>
	/// <param name="maxBufferPerTransfer">Maximum file size per transfer (files larger than this buffer will be split).</param>
	/// <param name="maxTotalFileSize">Maximum size of the total size (including all splits if larger than maximum buffer).</param>
	/// <param name="maxNumberPendingConnections">Maximum number of pending connections to keep waiting while one is busy.</param>
	public static void StartServer(ref Socket serverListeningSocketToUse, Form formToHookSocketClosingIntoFormClosingEvent = null, int listeningPort = defaultListeningPort, string FolderToSaveIn = defaultFolderToSaveIn, int maxBufferPerTransfer = defaultMaxBufferPerTransfer, int maxTotalFileSize = defaultMaxTotalFileSize, int maxNumberPendingConnections = defaultMaxNumberPendingConnections)
	{
		serverListeningSocketToUse = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		serverListeningSocketToUse.NoDelay = true;
		serverListeningSocketToUse.Ttl = 112;
		serverListeningSocketToUse.ReceiveBufferSize = maxBufferPerTransfer;

		if (formToHookSocketClosingIntoFormClosingEvent != null)
		{
			if (!dictionaryWithFormAndSocketsToCloseUponFormClosing.ContainsKey(formToHookSocketClosingIntoFormClosingEvent))
				dictionaryWithFormAndSocketsToCloseUponFormClosing.Add(formToHookSocketClosingIntoFormClosingEvent, serverListeningSocketToUse);

			formToHookSocketClosingIntoFormClosingEvent.FormClosing += (snder, evtargs) =>
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

		serverListeningSocketToUse.Bind(NetworkInterop.GetLocalIPEndPoint(listeningPort));
		serverListeningSocketToUse.Listen(maxNumberPendingConnections);

		if (textFeedback != null)
			textFeedback(null, new TextFeedbackEventArgs("Server started, waiting for clients..."));

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

			Application.DoEvents();
			data = null;
			string startdata = "";

			int maxProgress = 0;
			Console.WriteLine("Data transfer started...");
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
				Console.WriteLine("Total bytes transferred: " + totalbytelength);
				currentbytes.CopyTo(allbytes, totalbytelength - currbytelength);
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
					if (progressChanged != null)
						progressChanged(null, new ProgressChangedEventArgs(totalbytelength, maxProgress));

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

			// Show the data on the console.
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
				Console.WriteLine("Original filename: " + originalFileName);
				Console.WriteLine("New filename: " + newFileName);
				Console.WriteLine("Writing file...");
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
					NetworkInterop.MergeFiles(firstFilename);
					System.Diagnostics.Process.Start("explorer", "/select," + finalFilename);
					if (progressChanged != null)
						progressChanged(null, new ProgressChangedEventArgs(0, 100));
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

			handler.Shutdown(SocketShutdown.Both);
			handler.Disconnect(false);
			handler.Close();
			handler.Dispose();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			Application.DoEvents();
		}
	}

	public static void TransferFile(string filePath, ref Socket senderSocketToUse, IPAddress ipAddress = null, int listeningPort = defaultListeningPort, int maxBufferPerTransfer = defaultMaxBufferPerTransfer)
	{
		byte[] byData;
		if (!File.Exists(filePath))
			UserMessages.ShowWarningMessage("File does not exist and cannot be transferred: " + filePath);
		else
		{
			int counter = 0;
			byte[] AllFileDataBytes = File.ReadAllBytes(filePath);

			while (counter * maxBufferPerTransfer < AllFileDataBytes.Length)
			{
				if (ConnectToServer(ref senderSocketToUse, ipAddress, listeningPort))
				{
					if (textFeedback != null)
						textFeedback(null, new TextFeedbackEventArgs("Connected to server, transferring file segment " + (counter + 1) + "..."));
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

	public static bool ConnectToServer(ref Socket socketToInitialize, IPAddress ipAddress = null, int listeningPort = defaultListeningPort, int maxBufferPerTransfer = defaultMaxBufferPerTransfer)
	{
		try
		{
			socketToInitialize = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socketToInitialize.ReceiveTimeout = 1000;
			socketToInitialize.SendTimeout = 1000;
			socketToInitialize.NoDelay = true;
			socketToInitialize.Ttl = 112;
			socketToInitialize.SendBufferSize = maxBufferPerTransfer;
			socketToInitialize.Connect(ipAddress == null ? NetworkInterop.GetLocalIPEndPoint(listeningPort) : new IPEndPoint(ipAddress, listeningPort));
			return true;
		}
		catch (SocketException se)
		{
			MessageBox.Show("SocketException Connect()" + se.Message + Environment.NewLine + se.TargetSite);
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
