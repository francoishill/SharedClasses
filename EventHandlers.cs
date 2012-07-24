using System;
using System.Collections.Generic;

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

public struct Range
{
	public enum LinkTypes { ExplorerSelect, OpenUrl };
	public int Start;
	public int Length;
	public LinkTypes LinkType;
	public Range(int Start, int Length, LinkTypes LinkType)
	{
		this.Start = Start;
		this.Length = Length;
		this.LinkType = LinkType;
	}

	public int End { get { return Start + Length - 1; } }
}

public enum TextFeedbackType { Error, Success, Noteworthy, Subtle };
public delegate void TextFeedbackEventHandler(object sender, TextFeedbackEventArgs e);
public class TextFeedbackEventArgs : EventArgs
{
	public string FeedbackText;
	public TextFeedbackType FeedbackType;
	public Range? HyperlinkRange;
	private TextFeedbackEventArgs() { }
	public TextFeedbackEventArgs(string FeedbackTextIn, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle, Range? HyperlinkRange = null)
	{
		FeedbackText = FeedbackTextIn;
		FeedbackType = FeedbackTypeIn;
		this.HyperlinkRange = HyperlinkRange;
	}
	public static void RaiseTextFeedbackEvent_Ifnotnull(object SenderObject, TextFeedbackEventHandler textFeedbackEvent, string textMessage, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle, Range? HyperlinkRangeIn = null)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(SenderObject, new TextFeedbackEventArgs(textMessage, FeedbackTypeIn, HyperlinkRangeIn));
	}
	public static void RaiseSimple(TextFeedbackEventHandler textFeedbackEvent, string textMessage, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(null, new TextFeedbackEventArgs(textMessage, FeedbackTypeIn, null));
	}
}

public class TextFeedbackSection
{
	public enum DisplayTypeEnum { Normal, MakeButton };//, Bold, LargerSize, };

	public string Text;
	public DisplayTypeEnum DisplayType;
	public Action<object> ActionOnDoubleClick;
	//TODO: Currently the user must set an ActionTag to transfer information into the Action, is there not a better way?
	public object ActionTag;
	public TextFeedbackSection(string Text, DisplayTypeEnum DisplayType = DisplayTypeEnum.Normal)
	{
		this.Text = Text;
		this.DisplayType = DisplayType;
	}
	public TextFeedbackSection(string Text, Action<object> ActionOnDoubleClick, object ActionTag, DisplayTypeEnum DisplayType = DisplayTypeEnum.Normal)
	{
		this.Text = Text;
		this.DisplayType = DisplayType;
		this.ActionOnDoubleClick = ActionOnDoubleClick;
		this.ActionTag = ActionTag;
	}
}

public class TextFeedbackEventArgs_MultiObjects : TextFeedbackEventArgs
{
	[Obsolete("This member is hidden in the inherited class, to use it use the base class TextFeedbackEventArgs.", true)]
	new public string FeedbackText;
	public List<TextFeedbackSection> FeedbackStringList;
	public bool AutoSeparateWithSpaces;
	public TextFeedbackEventArgs_MultiObjects(List<TextFeedbackSection> FeedbackStringList, TextFeedbackType FeedbackType, bool AutoSeparateWithSpaces = true)
		: base(null, TextFeedbackType.Subtle)
	{
		this.FeedbackStringList = FeedbackStringList;
		this.FeedbackType = FeedbackType;
		this.AutoSeparateWithSpaces = AutoSeparateWithSpaces;
	}
	[Obsolete("This member is hidden in the inherited class, rather use the other overload which takes a List<string> instead of one string. To use the single string method it use the base class TextFeedbackEventArgs.", true)]
	public static void RaiseTextFeedbackEvent_Ifnotnull(object SenderObject, TextFeedbackEventHandler textFeedbackEvent, List<TextFeedbackSection> MessagesList, TextFeedbackType FeedbackTypeIn = TextFeedbackType.Subtle, bool AutoSeparateWithSpaces = true)
	{
		if (textFeedbackEvent != null) textFeedbackEvent(SenderObject, new TextFeedbackEventArgs_MultiObjects(MessagesList, FeedbackTypeIn, AutoSeparateWithSpaces));
	}
}