using System;
using System.Windows.Documents;

namespace SharedClasses
{
	public enum MessageTypes { Error, Success, Noteworthy, Subtle };

	public class MessagesParagraph : Paragraph
	{
		public bool Unread { get; set; }
		public MessageTypes MessageType;

		public MessagesParagraph(MessageTypes FeedbackType) : base() { Unread = true; this.MessageType = FeedbackType; }
		public MessagesParagraph(Inline inline, MessageTypes MessageType) : base(inline) { Unread = true; this.MessageType = MessageType; }
	}
}