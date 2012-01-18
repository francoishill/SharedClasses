using System;

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
	public double DurationOfTransferInSeconds;
	public double AverageBytesPerSecond;
	public long CurrentNumberofBytesTransferred;
	public long TotalNumberofBytesToTransfer;
	public InfoOfTransferToClient() { }
	public InfoOfTransferToClient(bool SuccessfullyReceiveCompleteIn, double DurationOfTransferInSecondsIn, double AverageBytesPerSecondIn, long CurrentNumberofBytesTransferredIn, long TotalNumberofBytesToTransferIn)
	{
		SuccessfullyReceiveComplete = SuccessfullyReceiveCompleteIn;
		DurationOfTransferInSeconds = DurationOfTransferInSecondsIn;
		AverageBytesPerSecond = AverageBytesPerSecondIn;
		CurrentNumberofBytesTransferred = CurrentNumberofBytesTransferredIn;
		TotalNumberofBytesToTransfer = TotalNumberofBytesToTransferIn;
	}
}