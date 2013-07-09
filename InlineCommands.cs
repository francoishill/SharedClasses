﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
//using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ICommandWithHandler = InlineCommandToolkit.InlineCommands.ICommandWithHandler;
//using OverrideToStringClass = InlineCommandToolkit.InlineCommands.OverrideToStringClass;

namespace InlineCommandToolkit
{
	public class CommandsManagerClass
	{
		private static List<ICommandWithHandler> listOfInitializedCommandInterfaces = new List<ICommandWithHandler>();
		public static List<ICommandWithHandler> ListOfInitializedCommandInterfaces
		{
			get { return listOfInitializedCommandInterfaces; }
			set { listOfInitializedCommandInterfaces = value; }
		}

		public static bool PerformCommand(ICommandWithHandler command, TextFeedbackEventHandler textfeedbackEvent, ProgressChangedEventHandler progresschangedEvent, params string[] arguments)
		{
			string errorMsg;
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(command, textfeedbackEvent, "Attempting to perform command: " + command.DisplayName + " (" + command.Description + ")", TextFeedbackType.Subtle);
			if (!command.ValidateArguments(out errorMsg, arguments)
				&& UserMessages.ShowWarningMessage("Invalid command arguments: " + errorMsg))
				return false;
			if (!command.PerformCommand(out errorMsg, textfeedbackEvent, progresschangedEvent, arguments)
				&& UserMessages.ShowWarningMessage("Cannot perform command: " + errorMsg))
				return false;
			//TODO: For some reason (maybe not using async/await) this "Successfully performed command" message gets through before the PerformPublishOnline step is finished
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(command, textfeedbackEvent, "Successfully performed command: " + command.DisplayName + " (" + command.Description + ")", TextFeedbackType.Success);
			return true;
		}

		[Obsolete]
		public static bool PerformCommandFromString(ICommandWithHandler command, TextFeedbackEventHandler textfeedbackEvent, ProgressChangedEventHandler progressChangedEvent, string argumentsCombined)
		{
			return PerformCommand(command, textfeedbackEvent, progressChangedEvent, argumentsCombined.Split(';'));
		}

		public static bool PerformCommandFromCurrentArguments(ICommandWithHandler command, TextFeedbackEventHandler textfeedbackEvent, ProgressChangedEventHandler progressChangedEvent)
		{
			List<string> tmpList = new List<string>();
			foreach (CommandArgument commandArgument in command.CurrentArguments)
				tmpList.Add(commandArgument.CurrentValue);
			return PerformCommand(command, textfeedbackEvent, progressChangedEvent, tmpList.ToArray());
		}
	}

	public class CommandArgument : INotifyPropertyChanged
	{
		private string currentValue;
		public string CurrentValue { get { return currentValue; } set { currentValue = value; NotifyPropertyChanged("CurrentValue"); } }
		public ObservableCollection<string> PredefinedAutocompleteList { get; set; }
		public string DisplayName { get; set; }
		public CommandArgument(string CurrentValueIn, string DisplayNameIn, ObservableCollection<string> PredefinedAutocompleteListIn)
		{
			CurrentValue = CurrentValueIn;
			DisplayName = DisplayNameIn;
			PredefinedAutocompleteList = PredefinedAutocompleteListIn;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}

	public class MessagesParagraph : Paragraph
	{
		public bool Unread { get; set; }
		public TextFeedbackType FeedbackType;

		public MessagesParagraph(TextFeedbackType FeedbackType) : base() { Unread = true; this.FeedbackType = FeedbackType; }
		public MessagesParagraph(Inline inline, TextFeedbackType FeedbackType) : base(inline) { Unread = true; this.FeedbackType = FeedbackType; }
	}

	public class InlineCommands
	{
		public static bool CanParseToInt(string str)
		{
			int tmpInt;
			return int.TryParse(str, out tmpInt);
		}

		/// <summary>
		/// Checks whether the given Email-Parameter is a valid E-Mail address.
		/// </summary>
		/// <param name="email">Parameter-string that contains an E-Mail address.</param>
		/// <returns>True, when Parameter-string is not null and 
		/// contains a valid E-Mail address;
		/// otherwise false.</returns>
		public static bool IsEmail(string email)
		{
			if (email != null) return Regex.IsMatch(email,
				@"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
			 + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
			 + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
			 + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$");
			else return false;
		}

		public class BoolResultWithErrorMessage
		{
			public bool Success;
			public string ErrorMessage;
			public BoolResultWithErrorMessage(bool SuccessIn, string ErrorMessageIn)
			{
				Success = SuccessIn;
				ErrorMessage = ErrorMessageIn;
			}
		}
		public class ObservableCollectionWithValidationOnAdd<T> : ObservableCollection<T>
		{
			Func<T, BoolResultWithErrorMessage> ValidationFunction;
			public ObservableCollectionWithValidationOnAdd(Func<T, BoolResultWithErrorMessage> ValidationFunctionIn)
				: base()
			{
				ValidationFunction = ValidationFunctionIn;
			}
			//public new void Add(T value)
			//{
			//	if (ValidationFunction(value))
			//		base.Add(value);
			//}

			public new BoolResultWithErrorMessage Add(T value)
			{
				BoolResultWithErrorMessage boolResultWithErrorMessage = ValidationFunction(value);
				if (boolResultWithErrorMessage.Success)
					base.Add(value);
				return boolResultWithErrorMessage;
			}

			public void AddWithoutValidation(T value)
			{
				base.Add(value);
			}
		}

		//DONE: Check out "Code Definition Window" in the view menu of Visual Studio
		//DONE: May look at google chrome after typing commandname, hit TAB to invoke that commands interface: http://thecodingbug.com/blog/2010/7/26/how-to-use-the-omnibox-api-in-google-chrome/
		public interface ICommandWithHandler: InlineCommandToolkit.IQuickAccessPluginInterface
		{
			string CommandName { get; }
			string DisplayName { get; }
			string Description { get; }
			string ArgumentsExample { get; }
			bool PreValidateArgument(out string errorMessage, int Index, string argumentValue);
			bool ValidateArguments(out string errorMessage, params string[] arguments);
			bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments);
			ObservableCollection<string> GetPredefinedArgumentsList(int Index, bool SuppressErrors = false);
			Dictionary<string, string> GetArgumentReplaceKeyValuePair(int Index, bool SuppressErrors = false);
			//void Add_AfterClearing_AllBlankArguments();
			int CurrentArgumentCount { get; }
			void RemoveCurrentArgument(int Index);
			ObservableCollectionWithValidationOnAdd<CommandArgument> CurrentArguments { get; }
			void NotifyPropertyChanged(string propertyName);
			//List<MessagesParagraph> ArchivedMessages { get; set; }
			void ArchiveReadMessages();
			ObservableCollection<MessagesParagraph> MessagesList { get; set; }
		 	Dictionary<TextFeedbackType, int> NumberUnreadMessages { get; }
			//ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair { get; }
			System.Windows.Controls.ContextMenu CommandContextMenu { get; }
		}

		public abstract class OverrideToStringClass : ICommandWithHandler, INotifyPropertyChanged
		{
			//public abstract override string ToString();
			public override string ToString() { return DisplayName; }// CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CommandName); }
			public abstract string CommandName { get; }
			public abstract string DisplayName { get; }
			public abstract string Description { get; }
			public abstract string ArgumentsExample { get; }
			public abstract bool PreValidateArgument(out string errorMessage, int Index, string argumentValue);
			public abstract bool ValidateArguments(out string errorMessage, params string[] arguments);
			public abstract bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments);

			public abstract CommandArgument[] AvailableArguments { get; set; }
			protected ObservableCollectionWithValidationOnAdd<CommandArgument> currentArguments;
			public ObservableCollectionWithValidationOnAdd<CommandArgument> CurrentArguments
			{
				get
				{
					if (!KeyAndValuePair_ChangedEventsAdded)
					{
						foreach (CommandArgument commandArgument in AvailableArguments)
							commandArgument.PropertyChanged += (snder, propchangedevent) =>
							{
								NotifyPropertyChanged("CurrentArguments");
							};
						KeyAndValuePair_ChangedEventsAdded = true;
					}

					if (currentArguments == null) currentArguments = new ObservableCollectionWithValidationOnAdd<CommandArgument>(ValidationFunction);

					if (currentArguments.Count != ArgumentCountForCurrentPopulatedArguments)
					{
						if (currentArguments.Count < ArgumentCountForCurrentPopulatedArguments)
						{
							while (currentArguments.Count < ArgumentCountForCurrentPopulatedArguments)
							{
								currentArguments.AddWithoutValidation(
									AvailableArguments[currentArguments.Count]);
							}
						}
						else if (currentArguments.Count > ArgumentCountForCurrentPopulatedArguments)
						{
							while (currentArguments.Count > ArgumentCountForCurrentPopulatedArguments)
								currentArguments.RemoveAt(currentArguments.Count - 1);
						}
					}
					return currentArguments;
				}
				set { currentArguments = value; }
			}

			public abstract ObservableCollection<string>[] PredefinedArgumentsList { get; }
			public ObservableCollection<string> GetPredefinedArgumentsList(int Index, bool SuppressErrors = false)
			{
				if (Index < PredefinedArgumentsList.Length)
					return PredefinedArgumentsList[Index];
				else
				{
					if (!SuppressErrors) UserMessages.ShowWarningMessage("Index out of bounds for predefinedArgumentsList, " + this.CommandName + " command, index = " + Index);
					return new ObservableCollection<string>();
				}
			}
			public abstract Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get; }
			public Dictionary<string, string> GetArgumentReplaceKeyValuePair(int Index, bool SuppressErrors = false)
			{
				if (Index < ArgumentsReplaceKeyValuePair.Length)
					return ArgumentsReplaceKeyValuePair[Index];
				else
				{
					if (!SuppressErrors) UserMessages.ShowWarningMessage("Index out of bounds for predefinedArgumentsList, " + this.CommandName + " command, index = " + Index);
					return new Dictionary<string, string>();
				}
			}

			private bool KeyAndValuePair_ChangedEventsAdded = false;
			//public abstract KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get; set; }
			public virtual int CurrentArgumentCount
			{
				get { return CurrentArguments.Count; }
			}

			public virtual void RemoveCurrentArgument(int Index)
			{
				currentArguments.RemoveAt(Index);
			}

			public BoolResultWithErrorMessage ValidationFunction(CommandArgument argumentToAdd)
			{
				string errorMessage;
				bool success = PreValidateArgument(out errorMessage, CurrentArguments.Count, argumentToAdd.CurrentValue);
				return new BoolResultWithErrorMessage(success, errorMessage);
			}
			//public BoolResultWithErrorMessage ValidationFunction(KeyAndValuePair argumentToAdd)
			//{
			//	string errorMessage;
			//	bool success = PreValidateArgument(out errorMessage, CurrentArgumentsPair.Count, argumentToAdd.Key);
			//	return new BoolResultWithErrorMessage(success, errorMessage);
			//}


			//internal ObservableCollectionWithValidationOnAdd<KeyAndValuePair> currentArgumentsPair;
			//public ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair
			//{
			//	get
			//	{
			//		if (!KeyAndValuePair_ChangedEventsAdded)
			//		{
			//			foreach (KeyAndValuePair keyandvalue in AvailableArgumentAndDescriptionsPair)
			//				keyandvalue.PropertyChanged += (tag, propchangedevent) =>
			//				{
			//					NotifyPropertyChanged("CurrentArgumentsPair");
			//					//(tag as KeyAndValuePair).
			//				};
			//			KeyAndValuePair_ChangedEventsAdded = true;
			//		}

			//		if (currentArgumentsPair == null) currentArgumentsPair = new ObservableCollectionWithValidationOnAdd<KeyAndValuePair>(ValidationFunction);

			//		if (currentArgumentsPair.Count != ArgumentCountForCurrentPopulatedArguments)
			//		{
			//			if (currentArgumentsPair.Count < ArgumentCountForCurrentPopulatedArguments)
			//			{
			//				while (currentArgumentsPair.Count < ArgumentCountForCurrentPopulatedArguments)
			//				{
			//					currentArgumentsPair.AddWithoutValidation(
			//						AvailableArgumentAndDescriptionsPair[currentArgumentsPair.Count]);
			//					//currentArgumentsPair.AddWithoutValidation(new KeyAndValuePair("", currentArgumentsPair.Count <= ArgumentDescriptions.Length ? ArgumentDescriptions[currentArgumentsPair.Count] : "no argument description"));
			//				}
			//			}
			//			else if (currentArgumentsPair.Count > ArgumentCountForCurrentPopulatedArguments)
			//			{
			//				while (currentArgumentsPair.Count > ArgumentCountForCurrentPopulatedArguments)
			//					currentArgumentsPair.RemoveAt(currentArgumentsPair.Count - 1);
			//			}
			//		}
			//		return currentArgumentsPair;
			//	}
			//	set { currentArgumentsPair = value; }
			//}

			public abstract int ArgumentCountForCurrentPopulatedArguments { get; }
			//public abstract int CurrentArgumentCount { get; }
			//public abstract void RemoveCurrentArgument(int Index);
			//public abstract ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair { get; set; }

			public event PropertyChangedEventHandler PropertyChanged;
			public void NotifyPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}

			private List<MessagesParagraph> ArchivedMessages { get; set; }
			public virtual void ArchiveReadMessages()
			{
				int msgcount = MessagesList.Count;//Just incase another thread adds messages while we loop through them
				if (msgcount == 0) return;
				if (UserMessages.Confirm("Archived messages is currently not retrievable (for this version of the application), so they will be totally lost, continue to archive messages?"))
				{
					if (ArchivedMessages == null) ArchivedMessages = new List<MessagesParagraph>();
					for (int i = 0; i < msgcount; i++)
						if (!MessagesList[i].Unread)
							ArchivedMessages.Add(MessagesList[i]);
					for (int i = msgcount - 1; i >= 0; i--)
						if (!MessagesList[i].Unread)
							MessagesList.RemoveAt(i);
					NotifyPropertyChanged("MessagesList");
					NotifyPropertyChanged("NumberUnreadMessages");
				}
			}

			private ObservableCollection<MessagesParagraph> messagesList;
			public virtual ObservableCollection<MessagesParagraph> MessagesList
			{
				get
				{
					if (messagesList == null) messagesList = new ObservableCollection<MessagesParagraph>();
					return messagesList;
				}
				set { messagesList = value; }
			}

			private Dictionary<TextFeedbackType, int> numberUnreadMessages;
			public Dictionary<TextFeedbackType, int> NumberUnreadMessages
			{
				get
				{
					if (numberUnreadMessages == null)
						numberUnreadMessages = new Dictionary<TextFeedbackType, int>();
					else
						numberUnreadMessages.Clear();
					foreach (TextFeedbackType feedbackType in Enum.GetValues(typeof(TextFeedbackType)))
					{
						//var tmpunread = 
						//	from n in ParagraphListForMessages
						//	where n.FeedbackType == feedbackType
						//	select n;
						numberUnreadMessages.Add(feedbackType, MessagesList.Count(m => m.Unread && m.FeedbackType == feedbackType));
					}
					//return ParagraphListForMessages.Count(m => m.Unread);
					return numberUnreadMessages;
				}
			}


			private MenuItem GenerateMenuItem(string HeaderText, RoutedEventHandler ClickEventHandler)
			{
				MenuItem mi = new MenuItem() { Header = HeaderText };
				mi.Click += ClickEventHandler;
				return mi;
			}
			public virtual ContextMenu CommandContextMenu
			{
				get
				{
					ContextMenu cm = new ContextMenu();
					cm.Items.Add(GenerateMenuItem("Archive read messages", delegate { ArchiveReadMessages(); }));
					return cm;
				}
			}
		}
	}
}