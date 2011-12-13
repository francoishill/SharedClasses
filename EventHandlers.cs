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

public enum TextFeedbackType { Error, Success, Noteworthy, Subtle };
public delegate void TextFeedbackEventHandler(object sender, TextFeedbackEventArgs e);
public class TextFeedbackEventArgs : EventArgs
{
	public string FeedbackText;
	public TextFeedbackType FeedbackType;
	public TextFeedbackEventArgs(string FeedbackTextIn, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle)
	{
		FeedbackText = FeedbackTextIn;
		FeedbackType = FeedbackTypeIn;
	}
	public static void RaiseTextFeedbackEvent_Ifnotnull(object SenderObject, TextFeedbackEventHandler textFeedbackEvent, string textMessage, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(SenderObject, new TextFeedbackEventArgs(textMessage, FeedbackTypeIn));
	}
}