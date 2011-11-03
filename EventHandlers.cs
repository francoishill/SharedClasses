using System;

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
	public static void RaiseTextFeedbackEvent_Ifnotnull(TextFeedbackEventHandler textFeedbackEvent, string textMessage)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(null, new TextFeedbackEventArgs(textMessage));
	}
}