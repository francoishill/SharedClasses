using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class InfoOfTransferToServer
{
	public string OriginalFilePath;
	public InfoOfTransferToServer() { OriginalFilePath = null; }
	public InfoOfTransferToServer(string OriginalFilePathIn)
	{
		OriginalFilePath = OriginalFilePathIn;
	}
}

[Serializable]
public class InfoOfTransferToClient
{
	public bool SuccessfullyReceiveComplete;
	public TimeSpan DurationOfTransfer;
	public double AverageBytesPerSecond;
	public long CurrentNumberofBytesTransferred;
	public long TotalNumberofBytesToTransfer;
	public InfoOfTransferToClient() { }
	public InfoOfTransferToClient(bool SuccessfullyReceiveCompleteIn, TimeSpan DurationOfTransferIn, double AverageBytesPerSecondIn, long CurrentNumberofBytesTransferredIn, long TotalNumberofBytesToTransferIn)
	{
		SuccessfullyReceiveComplete = SuccessfullyReceiveCompleteIn;
		DurationOfTransfer = DurationOfTransferIn;
		AverageBytesPerSecond = AverageBytesPerSecondIn;
		CurrentNumberofBytesTransferred = CurrentNumberofBytesTransferredIn;
		TotalNumberofBytesToTransfer = TotalNumberofBytesToTransferIn;
	}
}