using System;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

//*****************************************************************************************
//                           LICENSE INFORMATION
//*****************************************************************************************
//   PCFtp Version 1.0.0.0
//   Class library for FTP Client
//
//   Copyright (C) 2007  
//   Richard L. McCutchen 
//   Email: richard@psychocoder.net
//   Created: 12JAN07
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
//*****************************************************************************************

public class FTP
{
	#region Variables
	private static int BUFFER_SIZE = 512;
	private static Encoding ASCII = Encoding.ASCII;
	private bool _doVerbose = false;
	private string _ftpServer = string.Empty;
	private string _ftpPath = ".";
	private string _ftpUsername = string.Empty;
	private string _ftpPassword = string.Empty;
	private string statusMessage = string.Empty;
	private string result = string.Empty;
	private int _ftpPort = 21;
	private int bytes = 0;
	private int _statusCode = 0;
	private bool _isLoggedIn = false;
	private bool _isBinary = false;
	private Byte[] buffer = new Byte[BUFFER_SIZE];
	private Socket ftpSocket = null;
	private int _timeOut = 10;
	#endregion

	#region Class Properties
	#region Read/Write Properties
	/// <summary>
	/// Display all communications to the debug log
	/// </summary>
	public bool DoVerbose
	{
		get { return _doVerbose; }
		set { _doVerbose = value; }
	}

	/// <summary>
	/// FTP Server port to use, default is usually 21
	/// </summary>
	public int FtpPort
	{
		get { return _ftpPort; }
		set { _ftpPort = value; }
	}

	/// <summary>
	/// Timeout waiting for a response from server, in seconds.
	/// </summary>
	public int TimeoutValue
	{
		get { return _timeOut; }
		set { _timeOut = value; }
	}

	/// <summary>
	/// Name of the FTP server we wish to connect to
	/// </summary>
	/// <returns></returns>
	public string FtpServer
	{
		get { return _ftpServer; }
		set { _ftpServer = value; }
	}

	/// <summary>
	/// The remote port we wish to connect through
	/// </summary>
	/// <returns></returns>
	public int RemotePort
	{
		get { return _ftpPort; }
		set { _ftpPort = value; }
	}

	/// <summary>
	/// The working directory
	/// </summary>
	public string FtpPath
	{
		get { return _ftpPath; }
		set { _ftpPath = value; }

	}

	/// <summary>
	/// Server username
	/// </summary>
	public string FtpUsername
	{
		get { return _ftpUsername; }
		set { _ftpUsername = value; }
	}

	/// <summary>
	/// Server password
	/// </summary>
	public string FtpPassword
	{
		get { return _ftpPassword; }
		set { _ftpPassword = value; }
	}

	/// <summary>
	/// If the value of mode is true, set 
	/// binary mode for downloads, else, Ascii mode.
	/// </summary>
	public bool IsBinary
	{
		get { return _isBinary; }
		set
		{
			//if _isBinary already exit
			if (_isBinary == value) return;
			//check the value being passed
			//if it's true send the command
			//for binary download
			if (value)
				Execute("TYPE I");
			else
				//otherwise stay in Ascii mode
				Execute("TYPE A");
			//now check the status code, if
			//its not 200 throw an exception
			if (_statusCode != 200)
			{
				throw new FtpException(result.Substring(4));
			}
		}
	}
	#endregion

	#region ReadOnly Properties
	/// <summary>
	/// determine if the user is logged in
	/// </summary>
	public bool IsLoggedIn
	{
		get { return _isLoggedIn; }
	}

	/// <summary>
	/// returns the status code of the command
	/// </summary>
	public int StatusCode
	{
		get { return _statusCode; }
	}
	#endregion
	#endregion

	#region Class Methods
	#region FTPFtpLogin
	/// <summary>
	/// method to log in to the remote ftp server
	/// </summary>
	public void FtpLogin()
	{
		//check if the connection is currently open
		if (_isLoggedIn)
		{
			//its open so we need to close it
			CloseConnection();
		}
		//message that we're connection to the server
		Debug.WriteLine("Opening connection to " + _ftpServer, "FtpClient");
		//create our ip address object
		IPAddress remoteAddress = null;
		//create our end point object
		IPEndPoint addrEndPoint = null;
		try
		{
			//create our ftp socket
			ftpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//retrieve the server ip
			remoteAddress = Dns.GetHostEntry(_ftpServer).AddressList[0];
			//set the endpoint value
			addrEndPoint = new IPEndPoint(remoteAddress, _ftpPort);
			//connect to the ftp server
			ftpSocket.Connect(addrEndPoint);
		}
		catch (Exception ex)
		{
			// since an error happened, we need to
			//close the connection and throw an exception
			if (ftpSocket != null && ftpSocket.Connected)
			{
				ftpSocket.Close();
			}
			throw new FtpException("Couldn't connect to remote server", ex);
		}
		//read the host response
		readResponse();
		//check for a status code of 220
		if (_statusCode != 220)
		{
			//failed so close the connection
			CloseConnection();
			//throw an exception
			throw new FtpException(result.Substring(4));
		}
		//execute the USER ftp command (sends the username)
		Execute("USER " + _ftpUsername);
		//check the returned status code
		if (!(_statusCode == 331 || _statusCode == 230))
		{
			//not what we were looking for so
			//logout and throw an exception
			LogOut();
			throw new FtpException(result.Substring(4));
		}
		//if the status code isnt 230
		if (_statusCode != 230)
		{
			//execute the PASS ftp command (sends the password)
			Execute("PASS " + _ftpPassword);
			//check the returned status code
			if (!(_statusCode == 230 || _statusCode == 202))
			{
				//not what we were looking for so
				//logout and throw an exception
				LogOut();
				throw new FtpException(result.Substring(4));
			}
		}
		//we made it this far so we're logged in
		_isLoggedIn = true;
		//verbose the login message
		Debug.WriteLine("Connected to " + _ftpServer, "FtpClient");
		//set the initial working directory
		ChangeWorkingDirectory(_ftpPath);
	}
	#endregion

	#region CloseConnection
	/// <summary>
	/// method to close the connection
	/// </summary>
	public void CloseConnection()
	{
		//display the closing message
		Debug.WriteLine("Closing connection to " + _ftpServer, "FtpClient");
		//check to see if the connection is still active
		//if it is then execute the ftp quit command
		//which terminates the connection
		if (ftpSocket != null)
		{
			Execute("QUIT");
		}
		//log the user out
		LogOut();
	}
	#endregion

	#region ListFiles
	/// <summary>
	/// Return a string array containing the remote directory's file list.
	/// </summary>
	/// <param name="mask"></param>
	/// <returns></returns>
	public string[] ListFiles(string mask)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//create new socket
		Socket dataSocket = OpenSocketForTransfer();
		//execute the ftp nlst command, which
		//returns a list of files on the remote server
		Execute("NLST " + mask);
		//check the return code, we're looking for
		//either 150 or 125, otherwise the command failed
		if (!(_statusCode == 150 || _statusCode == 125))
		{
			//failed, throw an exception
			throw new FtpException(result.Substring(4));
		}
		//set the message to empty
		statusMessage = "";
		//create a timeout value based on our timeout property
		DateTime timeout = DateTime.Now.AddSeconds(_timeOut);
		//loop while out timeout value is
		//greater than the current time
		while (timeout > DateTime.Now)
		{
			//retrieve the data from the host
			int bytes = dataSocket.Receive(buffer, buffer.Length, 0);
			//convert it to Ascii format
			statusMessage += ASCII.GetString(buffer, 0, bytes);
			//exit the method is nothing is returned
			if (bytes < buffer.Length) break;
		}
		//throw the returned message into a string array
		string[] msg = statusMessage.Replace("\r", "").Split('\n');
		//close the socket connection
		dataSocket.Close();
		//check the return message
		if (statusMessage.IndexOf("No such file or directory") != -1)
			//return an empty message
			msg = new string[] { };
		//read the host's response
		readResponse();
		//if we didnt receive a status code of 226
		//then the process failed
		if (_statusCode != 226)
			//return an empty message
			msg = new string[] { };
		//	throw new FtpException(result.Substring(4));

		return msg;
	}
	#endregion

	#region GetFileSize
	/// <summary>
	/// Method to retrieve the size of the file based
	/// on the name provided
	/// </summary>
	/// <param name="file">Name of the file to get the size of</param>
	/// <returns>The files size</returns>
	public long GetFileSize(string file)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//execute the size command, which
		//returns the files size as a decimal number
		Execute("SIZE " + file);
		long fileSize = 0;
		//check our returning status code
		//if it's not 213 the command failed
		if (_statusCode == 213)
		{
			//set the file size
			fileSize = long.Parse(result.Substring(4));
		}
		else
		{
			//command failed so throw an exception
			throw new FtpException(result.Substring(4));
		}
		//return the file size
		return fileSize;
	}
	#endregion

	#region DownloadFile
	/// <summary>
	/// Download a remote file to a local file name which can include
	/// a path, and set the resume flag. The local file name will be
	/// created or overwritten, but the path must exist.
	/// </summary>
	/// <param name="ftpFile">File on the server to download</param>
	/// <param name="localFile">Name of the file on the local machine</param>
	/// <param name="resume"></param>
	public void DownloadFile(string ftpFile, string localFile, Boolean resume)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}

		IsBinary = true;
		//display a downloading file message
		Debug.WriteLine("Downloading file " + ftpFile + " from " + _ftpServer + "/" + _ftpPath, "FtpClient");
		//check if a local file name was provided
		//if not then set its value to the ftp file name
		if (localFile.Equals(""))
		{
			localFile = ftpFile;
		}
		//create our filestream object
		FileStream output = null;
		//check to see if the local file exists
		//if it doesnt then create the file
		//otherwise overwrite it
		if (!File.Exists(localFile))
		{
			//create the new file
			output = File.Create(localFile);
		}
		else
		{
			//overwrite the existsing file
			output = new FileStream(localFile, FileMode.Open);
		}

		//create our new socket for the transfer
		Socket dataSocket = OpenSocketForTransfer();
		//create our resume point
		long resumeOffset = 0;
		//if resume was set to true
		if (resume)
		{
			//set the value of our resume variable
			resumeOffset = output.Length;
			//check if its value is greater than 0 (zero)
			if (resumeOffset > 0)
			{
				//execute our rest command, which sets the
				//resume point for the download in case
				//the download is interrupted
				Execute("REST " + resumeOffset);
				//check the status code, if not a 350
				//code then the server doesnt support resuming
				if (_statusCode != 350)
				{
					//Server dosnt support resuming
					resumeOffset = 0;
					Debug.WriteLine("Resuming not supported:" + result.Substring(4), "FtpClient");
				}
				else
				{
					Debug.WriteLine("Resuming at offset " + resumeOffset, "FtpClient");
					//seek to the interrupted point
					output.Seek(resumeOffset, SeekOrigin.Begin);
				}
			}
		}
		//execute out retr command
		//which starts the file transfer
		Execute("RETR " + ftpFile);
		//check the status code, we need 150 or 125
		//otherwise the download failed
		if (_statusCode != 150 && _statusCode != 125)
		{
			//throw an FtpException
			throw new FtpException(result.Substring(4));
		}
		//set a timeout value
		DateTime timeout = DateTime.Now.AddSeconds(_timeOut);
		//check the timeout value against the current time
		//if its less then download the file
		while (timeout > DateTime.Now)
		{
			//receive the binary data from the socket
			bytes = dataSocket.Receive(buffer, buffer.Length, 0);
			//write the file
			output.Write(buffer, 0, bytes);
			//make sure the file is greater than
			//zero in size, if not exit the method
			if (bytes <= 0)
			{
				break;
			}
		}
		//close our stream
		output.Close();
		//check to see if the socket is still open,
		//if it is then close it
		if (dataSocket.Connected)
		{
			dataSocket.Close();
		}
		//read the host's response
		readResponse();
		//we're looking for a status code of 226 or 250,
		//if that isnt returned the download failed
		if (_statusCode != 226 && _statusCode != 250)
		{
			throw new FtpException(result.Substring(4));
		}
	}
	#endregion

	#region UploadFile
	/// <summary>
	/// Upload a file and set the resume flag.
	/// </summary>
	/// <param name="fileName"></param>
	/// <param name="resume"></param>
	public void UploadFile(string fileName, bool resume)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}

		Socket dataSocket = null;
		long resumeOffset = 0;
		//if resume is true
		if (resume)
		{
			try
			{
				//set _isBinary to true
				IsBinary = true;
				//get the size of the file
				resumeOffset = GetFileSize(Path.GetFileName(fileName));
			}
			catch (Exception)
			{
				// file not exist
				resumeOffset = 0;
			}
		}

		// open stream to read file
		FileStream input = new FileStream(fileName, FileMode.Open);
		//if resume is true
		//and the size of the file read is
		//less than the initial value
		if (resume && input.Length < resumeOffset)
		{
			// different file size
			Debug.WriteLine("Overwriting " + fileName, "FtpClient");
			resumeOffset = 0;
		}
		else if (resume && input.Length == resumeOffset)
		{
			// file done
			input.Close();
			Debug.WriteLine("Skipping completed " + fileName + " - turn resume off to not detect.", "FtpClient");
			return;
		}

		//now create our socket needed for
		//the file transfer
		dataSocket = OpenSocketForTransfer();
		//if the file size is greater than 0
		if (resumeOffset > 0)
		{
			//execute the rest command, which
			//sets the point the resume will occurr
			//if the upload is interrupted
			Execute("REST " + resumeOffset);
			//check the status code, if it's not
			//350 the resume isnt supported by the server
			if (_statusCode != 350)
			{
				Debug.WriteLine("Resuming not supported", "FtpClient");
				resumeOffset = 0;
			}
		}
		//execute the store ftp command (starts the transfer of the file)
		Execute("STOR " + Path.GetFileName(fileName));
		//check the status code, we need a
		//value of 150 or 125, otherwise throw an exception
		if (_statusCode != 125 && _statusCode != 150)
		{
			throw new FtpException(result.Substring(4));
		}
		//now check the resumeOffset value,
		//if its not zero then we need to resume
		//the upload process where it ended
		if (resumeOffset != 0)
		{
			//let the user know the upload is resuming
			Debug.WriteLine("Resuming at offset " + resumeOffset, "FtpClient");
			//use the Seek method to get to where the upload ended
			input.Seek(resumeOffset, SeekOrigin.Begin);
		}
		//let the user know the uploading has begun
		Debug.WriteLine("Uploading file " + fileName + " to " + _ftpPath, "FtpClient");
		//upload the file
		while ((bytes = input.Read(buffer, 0, buffer.Length)) > 0)
		{
			dataSocket.Send(buffer, bytes, 0);
		}
		//close our reader
		input.Close();
		//check to see if the socket is still connected
		//if it is then disconnect it
		if (dataSocket.Connected)
		{
			dataSocket.Close();
		}
		//read the host's response
		readResponse();
		//checking for a successful upload code (226 or 250)
		//if not either then throw an exception
		if (_statusCode != 226 && _statusCode != 250)
		{
			throw new FtpException(result.Substring(4));
		}
	}
	#endregion

	#region UploadDirectory
	/// <summary>
	/// Upload a directory and its file contents
	/// </summary>
	/// <param name="dirPath">Path of the directory to upload</param>
	/// <param name="recursive">Whether to recurse sub directories</param>
	/// <param name="fileMask">Only upload files of the given mask(i.e;'*.*','*.jpg', ect..)</param>
	public void UploadDirectory(string dirPath, bool recursive, string fileMask)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		string[] directories = dirPath.Replace("/", @"\").Split('\\');
		string rootDirectory = directories[directories.Length - 1];

		// make the root dir if it does not exist
		if (ListFiles(rootDirectory).Length < 1)
		{
			CreateDirectory(rootDirectory);
		}
		//make the new directory the working directory
		ChangeWorkingDirectory(rootDirectory);
		//loop through the files in the directory
		foreach (string file in Directory.GetFiles(dirPath, fileMask))
		{
			//upload each file
			UploadFile(file, true);
		}
		//check if recusrsive was specified
		if (recursive)
		{
			//since recursive is true, we loop through all the
			//directories in the directory provided
			foreach (string directory in Directory.GetDirectories(dirPath))
			{
				//upload each directory
				UploadDirectory(directory, recursive, fileMask);
			}
		}
		//change working directory back to root level
		ChangeWorkingDirectory("..");
	}
	#endregion

	#region DeleteFile
	/// <summary>
	/// method to delete a file from the FTP server.
	/// </summary>
	/// <param name="file">File to delete</param>
	public void DeleteFile(string file)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//execute the delete command
		Execute("DELE " + file);
		//check for a status code of 250, if
		//not then throw an exception
		if (_statusCode != 250)
		{
			throw new FtpException(result.Substring(4));
		}
		Debug.WriteLine("Deleted file " + file, "FtpClient");
	}
	#endregion

	#region RenameFile
	/// <summary>
	/// Rename a file on the remote FTP server.
	/// </summary>
	/// <param name="oldName">File to rename</param>
	/// <param name="newName">New name of the file</param>
	/// <param name="replace">setting to false will throw exception if it exists</param>
	public void RenameFile(string oldName, string newName, bool replace)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//execute the rename from command
		Execute("RNFR " + oldName);
		//check for a status code of 350
		if (_statusCode != 350)
		{
			throw new FtpException(result.Substring(4));
		}
		//if they didnt choose to replace the file, and a 
		//file with that name already exists then throw an exception
		if (!replace && ListFiles(newName).Length > 0)
		{
			throw new FtpException("File already exists");
		}
		//execute the rename to command
		Execute("RNTO " + newName);
		//check for a status code of 250, if
		//not then throw an exception
		if (_statusCode != 250)
		{
			throw new FtpException(result.Substring(4));
		}
		//write the successful message out
		Debug.WriteLine("Renamed file " + oldName + " to " + newName, "FtpClient");
	}
	#endregion

	#region CreateDirectory
	/// <summary>
	/// Create a directory on the remote FTP server.
	/// </summary>
	/// <param name="dirName">Name of the directory to create</param>
	public void CreateDirectory(string dirName)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//check to make sure a directory name was supplied
		if (dirName == null || dirName.Equals(".") || dirName.Length == 0)
		{
			//no directory was provided so throw an exception 
			//and break out of the method
			throw new FtpException("A directory name wasn't provided. Please provide one and try your request again.");
		}
		//execute the make directory command
		Execute("MKD " + dirName);
		//check for a status code of 250 or 257
		if (_statusCode != 250 && _statusCode != 257)
		{
			//operation failed, throw an exception
			throw new FtpException(result.Substring(4));
		}

		Debug.WriteLine("Created directory " + dirName, "FtpClient");
	}
	#endregion

	#region RemoveDirectory
	/// <summary>
	/// Delete a directory on the remote FTP server.
	/// </summary>
	/// <param name="dirName"></param>
	public void RemoveDirectory(string dirName)
	{
		//make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//check to make sure a directory name was supplied
		if (dirName == null || dirName.Equals(".") || dirName.Length == 0)
		{
			//no directory was provided so throw an exception 
			//and break out of the method
			throw new FtpException("A directory name wasn't provided. Please provide one and try your request again.");
		}
		//execute the remove directory command
		Execute("RMD " + dirName);
		//check for a status code of 250
		if (_statusCode != 250)
		{
			throw new FtpException(result.Substring(4));
		}
		//we made it this far so print the name
		//of the removed directory to the window
		Debug.WriteLine("Removed directory " + dirName, "FtpClient");
	}
	#endregion

	#region ChangeWorkingDirectory
	/// <summary>
	/// Change the current working directory on the remote FTP server.
	/// </summary>
	/// <param name="dirName"></param>
	public void ChangeWorkingDirectory(string dirName)
	{
		//check to make sure a directory name was supplied
		if (dirName == null || dirName.Equals(".") || dirName.Length == 0)
		{
			//no directory was provided so throw an exception 
			//and break out of the method
			throw new FtpException("A directory name wasn't provided. Please provide one and try your request again.");
		}
		//before we can change the directory we need
		//to make sure the user is logged in
		if (!_isLoggedIn)
		{
			//FtpLogin();
			throw new FtpException("You need to log in before you can perform this operation");
		}
		//execute the CWD command = Change Working Directory
		Execute("CWD " + dirName);
		//check for a return status code of 250
		if (_statusCode != 250)
		{
			//operation failed, throw an exception
			throw new FtpException(result.Substring(4));
		}
		//execute the PWD command
		//Print Working Directory
		Execute("PWD");
		//check for a status code of 250
		if (_statusCode != 257)
		{
			//operation failed, throw an exception
			throw new FtpException(result.Substring(4));
		}
		// we made it this far so retrieve the
		//directory from the host response
		_ftpPath = statusMessage.Split('"')[1];

		Debug.WriteLine("Current directory is " + _ftpPath, "FtpClient");
	}
	#endregion

	#region readResponse
	/// <summary>
	/// 
	/// </summary>
	private void readResponse()
	{
		statusMessage = "";
		result = ParseHostResponse();
		_statusCode = int.Parse(result.Substring(0, 3));
	}
	#endregion

	#region ParseHostResponse
	/// <summary>
	/// Method to parse the response from the remote host
	/// </summary>
	/// <returns></returns>
	private string ParseHostResponse()
	{
		while (true)
		{
			//retrieve the host response and convert it to
			//a byte array
			bytes = ftpSocket.Receive(buffer, buffer.Length, 0);
			//decode the byte array and set the
			//statusMessage to its value
			statusMessage += ASCII.GetString(buffer, 0, bytes);
			//check the size of the byte array
			if (bytes < buffer.Length)
			{
				break;
			}
		}
		//split the host response
		string[] msg = statusMessage.Split('\n');
		//check the length of the response
		if (statusMessage.Length > 2)
			statusMessage = msg[msg.Length - 2];
		else
			statusMessage = msg[0];

		//check for a space in the host response, if it exists return
		//the message to the client
		if (!statusMessage.Substring(3, 1).Equals(" ")) return ParseHostResponse();
		//check if the user selected verbose Debugging
		if (_doVerbose)
		{
			//loop through the message from the host
			for (int i = 0; i < msg.Length - 1; i++)
			{
				//write each line out to the window
				Debug.Write(msg[i], "FtpClient");
			}
		}
		//return the message
		return statusMessage;
	}
	#endregion

	#region Execute
	/// <summary>
	/// method to send the ftp commands to the remove server
	/// </summary>
	/// <param name="command">the command to execute</param>
	private void Execute(String msg)
	{
		//check to see if verbose debugging is enabled
		//if so write the command to the window
		if (_doVerbose) Debug.WriteLine(msg, "FtpClient");
		//convert the command to a byte array
		Byte[] cmdBytes = Encoding.ASCII.GetBytes((msg + "\r\n").ToCharArray());
		//send the command to the host
		ftpSocket.Send(cmdBytes, cmdBytes.Length, 0);
		//read the returned response
		readResponse();
	}
	#endregion

	#region OpenSocketForTransfer
	/// <summary>
	/// when doing data transfers, we need to open another socket for it.
	/// </summary>
	/// <returns>Connected socket</returns>
	private Socket OpenSocketForTransfer()
	{
		//send the PASV command (Passive command)
		Execute("PASV");
		//check the status code, if it
		//isnt 227 (successful) then throw an exception
		if (_statusCode != 227)
		{
			throw new FtpException(result.Substring(4));
		}
		//find the index of the opening "("
		//and the closing ")". The return
		//message from the server, if successful, has
		//the IP and port number for the client in
		//enclosed in "(" & ")"
		int idx1 = result.IndexOf('(');
		int idx2 = result.IndexOf(')');
		//now we need to get everything in the parenthesis
		string ipData = result.Substring((idx1 + 1), (idx2 - idx1) - 1);
		//create new integer array with size of 6
		//the returning message is in 6 segments
		int[] msgSegments = new int[6];
		//get the length of the message
		int msgLength = ipData.Length;
		int partCount = 0;
		string buffer = "";
		//now we need to loop through the host response
		for (int i = 0; i < msgLength && partCount <= 6; i++)
		{
			//convert each character to a char
			char chr = char.Parse(ipData.Substring(i, 1));
			//check to see if the current character is numeric
			if (char.IsDigit(chr))
			{
				//since its a number we add it to our buffer variable
				buffer += chr;
			}
			//now we need to check for the
			//comma seperating the digits
			else if (chr != ',')
			{
				//no comma so throw an exception
				throw new FtpException("Malformed PASV result: " + result);
			}
			else
			{
				//check to see if the current character is a comma
				//or if the counter + 1 equals the host response length
				if (chr == ',' || i + 1 == msgLength)
				{
					try
					{
						//since its one of the 2 we add it to the
						//current index of the message segments
						msgSegments[partCount++] = int.Parse(buffer);
						buffer = "";
					}
					//handle any exceptions thrown
					catch (Exception ex)
					{
						throw new FtpException("Malformed PASV result (not supported?): " + result, ex);
					}
				}
			}
		}
		//now we assemble the IP address returned from the host
		string ipAddress = msgSegments[0] + "." + msgSegments[1] + "." + msgSegments[2] + "." + msgSegments[3];
		//the last 2 segments are the port we need to use
		int port = (msgSegments[4] << 8) + msgSegments[5];

		Socket tranferSocket = null;
		IPEndPoint ipEndPoint = null;

		try
		{
			//create our new socket for transfering data
			tranferSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			ipEndPoint = new IPEndPoint(Dns.GetHostEntry(ipAddress).AddressList[0], port);
			tranferSocket.Connect(ipEndPoint);
		}
		catch (Exception ex)
		{
			// doubtfull....
			if (tranferSocket != null && tranferSocket.Connected) tranferSocket.Close();
			//throw an FtpException
			throw new FtpException("Can't connect to remote server", ex);
		}
		//return the socket
		return tranferSocket;
	}

	#endregion

	#region LogOut
	/// <summary>
	/// method to release and remove any sockets left open
	/// </summary>
	private void LogOut()
	{
		//check to see if the sock is non existant
		if (ftpSocket != null)
		{
			//since its not we need to
			//close it and dispose of it
			ftpSocket.Close();
			ftpSocket = null;
		}
		//log the user out
		_isLoggedIn = false;
	}
	#endregion

	#region Destructor
	/// <summary>
	/// Destuctor
	/// </summary>
	~FTP()
	{
		LogOut();
	}
	#endregion

	#endregion
}