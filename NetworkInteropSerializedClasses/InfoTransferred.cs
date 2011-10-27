using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public struct InfoOfTransferToServer
{
	private bool IsNULL;
	public string OriginalFilePath;
	//public InfoOfTransferToServer() { OriginalFilePath = null; }
	public InfoOfTransferToServer(string OriginalFilePathIn)
	{
		OriginalFilePath = OriginalFilePathIn;
		IsNULL = false;
	}
	public bool IsNull() { return IsNULL; }
}

[Serializable]
public struct InfoOfTransferToClient
{
	private bool IsNULL;
	public bool SuccessfullyReceiveComplete;
	public double DurationOfTransferInSeconds;
	public double AverageBytesPerSecond;
	public long CurrentNumberofBytesTransferred;
	public long TotalNumberofBytesToTransfer;
	//public InfoOfTransferToClient() { }
	public InfoOfTransferToClient(bool SuccessfullyReceiveCompleteIn, double DurationOfTransferInSecondsIn, double AverageBytesPerSecondIn, long CurrentNumberofBytesTransferredIn, long TotalNumberofBytesToTransferIn)
	{
		SuccessfullyReceiveComplete = SuccessfullyReceiveCompleteIn;
		DurationOfTransferInSeconds = DurationOfTransferInSecondsIn;
		AverageBytesPerSecond = AverageBytesPerSecondIn;
		CurrentNumberofBytesTransferred = CurrentNumberofBytesTransferredIn;
		TotalNumberofBytesToTransfer = TotalNumberofBytesToTransferIn;
		IsNULL = false;
	}
	public bool IsNull() { return IsNULL; }
}