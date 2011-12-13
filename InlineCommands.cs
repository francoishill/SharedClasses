using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Documents;

namespace InlineCommands
{
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

	public class CommandsManagerClass
	{
		private static List<ICommandWithHandler> listOfInitializedCommandInterfaces = null;
		public static List<ICommandWithHandler> ListOfInitializedCommandInterfaces
		{
			get
			{
				if (listOfInitializedCommandInterfaces == null)
				{
					listOfInitializedCommandInterfaces = new List<ICommandWithHandler>();
					Type[] types = typeof(CommandsManagerClass).GetNestedTypes(BindingFlags.Public);
					foreach (Type type in types)
						if (!type.IsInterface && !type.IsAbstract)
							if (type.GetInterfaces().Contains(typeof(ICommandWithHandler)))
								listOfInitializedCommandInterfaces.Add((ICommandWithHandler)type.GetConstructor(new Type[0]).Invoke(new object[0]));
				}
				return listOfInitializedCommandInterfaces;
			}
		}

		private static bool CanParseToInt(string str)
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

		//TODO: Check out "Code Definition Window" in the view menu of Visual Studio
		//TODO: May look at google chrome after typing commandname, hit TAB to invoke that commands interface: http://thecodingbug.com/blog/2010/7/26/how-to-use-the-omnibox-api-in-google-chrome/
		public interface ICommandWithHandler
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
			ObservableCollection<Paragraph> ParagraphListForMessages { get; set; }
			//ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair { get; }
		}

		public abstract class OverrideToStringClass : ICommandWithHandler, INotifyPropertyChanged
		{
			//public abstract override string ToString();
			public override string ToString() { return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CommandName); }
			public abstract string CommandName { get; }
			public abstract string DisplayName { get; }
			public abstract string Description { get; }
			public abstract string ArgumentsExample { get; }
			public abstract bool PreValidateArgument(out string errorMessage, int Index, string argumentValue);
			public abstract bool ValidateArguments(out string errorMessage, params string[] arguments);
			public abstract bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments);

			public abstract CommandArgument[] AvailableArguments { get; set; }
			internal ObservableCollectionWithValidationOnAdd<CommandArgument> currentArguments;
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
			//				keyandvalue.PropertyChanged += (snder, propchangedevent) =>
			//				{
			//					NotifyPropertyChanged("CurrentArgumentsPair");
			//					//(snder as KeyAndValuePair).
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
			private void NotifyPropertyChanged(String info)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(info));
				}
			}

			private ObservableCollection<Paragraph> paragraphListForMessages;
			public virtual ObservableCollection<Paragraph> ParagraphListForMessages
			{
				get
				{
					if (paragraphListForMessages == null) paragraphListForMessages = new ObservableCollection<Paragraph>();
					return paragraphListForMessages;
				}
				set { paragraphListForMessages = value; }
			}
		}

		public class RunCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "run"; } }
			public override string DisplayName { get { return "Run"; } }
			public override string Description { get { return "Run any file/folder"; } }
			public override string ArgumentsExample { get { return "outlook"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "cmd", "outlook" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
				new Dictionary<string, string>() { { "cmd", "cmd1" }, { "outlook", "outlook1" } }
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument allowed for Run command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of run command may not be null/empty/whitespaces only";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Run command";
				//else if (!(arguments[0] is string)) errorMessage = "First argument of run command must be of type string";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				//if (!ValidateArguments(out errorMessage, arguments)) return false;
				try
				{
					System.Diagnostics.Process.Start(arguments[0]);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot run: " + arguments [0] + Environment.NewLine + exc.Message);
					errorMessage = "Cannot run: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "parameter", new ObservableCollection<string>() { "cmd", "outlook" })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "parameter")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class GoogleSearchCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "google"; } }
			public override string DisplayName { get { return "Google Search"; } }
			public override string Description { get { return "Google search a word/phrase"; } }
			public override string ArgumentsExample { get { return "first man on the moon"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument allowed for Google search command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Google search command may not be null/empty/whitespaces only";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Google search command";
				//else if (!(arguments[0] is string)) errorMessage = "First argument of Google search command must be of type string";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				//if (!ValidateArguments(out errorMessage, arguments)) return false;
				try
				{
					System.Diagnostics.Process.Start("http://www.google.co.za/search?q=" + arguments[0]);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot google search: " + arguments[0] + Environment.NewLine + exc.Message);
					errorMessage = "Cannot google search: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "search phrase", new ObservableCollection<string>() { })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "search phrase")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class ExploreCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "explore"; } }
			public override string DisplayName { get { return "Explore"; } }
			public override string Description { get { return "Explore a folder"; } }
			public override string ArgumentsExample { get { return @"c:\windows"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { @"c:\", @"c:\Program Files" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument allowed for Explore command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Explore command may not be null/empty/whitespaces only";
				else if (Index == 0 && !Directory.Exists(argumentValue))
					errorMessage = "First argument of Explore command must be existing directory";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Explore command";
				//else if (!(arguments[0] is string)) errorMessage = "First argument of Explore command must be of type string";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				//if (!ValidateArguments(out errorMessage, arguments)) return false;
				try
				{
					if (!Directory.Exists(arguments[0])) throw new DirectoryNotFoundException("Directory not found: " + arguments[0]);
					System.Diagnostics.Process.Start(arguments[0]);
					//Process.Start("explorer", "/select, \"" + argumentString + "\"");
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot explore: " + arguments[0] + Environment.NewLine + exc.Message);
					errorMessage = "Cannot explore: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "folder", new ObservableCollection<string>() { @"c:\", @"c:\Program Files" })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "folder")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class AddTodoitemFirepumaCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "addtodo"; } }
			public override string DisplayName { get { return "Add todo"; } }
			public override string Description { get { return "Add todo item to firepuma"; } }
			public override string ArgumentsExample { get { return "13;30;Reminder;Buy milk => (MinutesFromNow, Autosnooze, Name, Description)"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "5", "30", "60" },
				new ObservableCollection<string>() { "15", "30", "60" },
				new ObservableCollection<string>() { "Shop" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index >= 4)
					errorMessage = "More than 4 arguments not allowed for Add todo command (minutesfromnow, autosnooze, name, desc)";
				else if (Index == 0 && !CanParseToInt(argumentValue))
					errorMessage = "First argument (minutesfromnow) of Add todo command must be of type int";
				else if (Index == 1 && !CanParseToInt(argumentValue))
					errorMessage = "Second argument (autosnooze) of Add todo command must be of type int";
				else if (Index == 2 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Third argument (name) of Add todo command may not be null/empty/whitespaces only";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				//minutes, autosnooze, name, desc
				errorMessage = "";
				if (arguments.Length < 3) errorMessage = "At least 3 arguments required for Add todo command (minutesfromnow, autosnooze, name, desc)";
				else if (arguments.Length > 4) errorMessage = "More than 4 arguments not allowed for Add todo command (minutesfromnow, autosnooze, name, desc)";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else if (!PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else if (!PreValidateArgument(out errorMessage, 2, arguments[2]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				//if (!ValidateArguments(out errorMessage, arguments)) return false;
				try
				{
					PhpInterop.AddTodoItemFirepuma(
						this,
						PhpInterop.ServerAddress,
						PhpInterop.doWorkAddress,
						PhpInterop.Username,
						PhpInterop.Password,
						"QuickAccess",
						"Quick todo",
						arguments[2],
						arguments.Length > 3 ? arguments[3] : "",
						false,
						DateTime.Now.AddMinutes(Convert.ToInt32(arguments[0])),
						DateTime.Now,
						0,
						false,
						Convert.ToInt32(arguments[1]),
						textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot add todo item: " + Environment.NewLine + exc.Message);
					errorMessage = "Cannot add todo item: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "minutes", new ObservableCollection<string>() { "5", "30", "60" }),
				new CommandArgument("", "autosnooze", new ObservableCollection<string>() { "15", "30", "60" }),
				new CommandArgument("", "name", new ObservableCollection<string>() { "Shop" }),
				new CommandArgument("", "description", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "minutes"),
			//	new KeyAndValuePair("", "autosnooze"),
			//	new KeyAndValuePair("", "name"),
			//	new KeyAndValuePair("", "description")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 4; } }
		}

		public class MailCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "mail"; } }
			public override string DisplayName { get { return "Mail"; } }
			public override string Description { get { return "Send an email"; } }
			public override string ArgumentsExample { get { return "billgates@microsoft.com;My subject;Hi Bill.\nHow have you been?"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "fhill@gls.co.za", "francoishill11@gmail.com" },
				//new ObservableCollection<string>() { "Hi there", "This is a subject" },
				//new ObservableCollection<string>() { "How have you been?", "This is the body" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index >= 3)
					errorMessage = "More than 3 arguments not allowed for Mail command (mail, subject, body)";
				else if (Index == 0 && !IsEmail(argumentValue))
					errorMessage = "First argument (to) of Mail command must be a valid email address";
				else if (Index == 1 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Second argument (subject) of Mail command may not be null/empty/whitespaces only";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length < 2) errorMessage = "At least 2 arguments required for Mail command (mail, subject, body)";
				else if (arguments.Length > 3) errorMessage = "More than 3 arguments not allowed for Mail command (mail, subject, body)";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else if (!PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					MicrosoftOfficeInterop.CreateNewOutlookMessage(
						this,
						arguments[0],
						arguments[1],
						arguments.Length >= 3 ? arguments[2] : "",
						textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot send mail: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "to address", new ObservableCollection<string>() { "fhill@gls.co.za", "francoishill11@gmail.com" }),
				new CommandArgument("", "subject", new ObservableCollection<string>()),
				new CommandArgument("", "body", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "to address"),
			//	new KeyAndValuePair("", "subject"),
			//	new KeyAndValuePair("", "body"),
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 3; } }
		}

		public class WebCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "web"; } }
			public override string DisplayName { get { return "Web"; } }
			public override string Description { get { return "Open a web URL"; } }
			public override string ArgumentsExample { get { return "google.com"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "google.com", "firepuma.com", "fjh.dyndns.org" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument required for Web command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Web command may not be null/empty/whitespaces only";
				else if (Index == 0 && !argumentValue.Contains('.') && !argumentValue.ToLower().Contains("localhost"))
					errorMessage = "First argument of Web command must contain a '.' or be localhost.";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Web command";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				//if (!ValidateArguments(out errorMessage, arguments)) return false;
				try
				{
					if (!arguments[0].StartsWith("http://") && !arguments[0].StartsWith("https://") && !arguments[0].StartsWith("www."))
						arguments[0] = "http://" + arguments[0];
					System.Diagnostics.Process.Start(arguments[0]);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot google search: " + arguments[0] + Environment.NewLine + exc.Message);
					errorMessage = "Cannot open web url: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "url", new ObservableCollection<string>() { "google.com", "firepuma.com", "fjh.dyndns.org" })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "url")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class CallCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "call"; } }
			public override string DisplayName { get { return "Call"; } }
			public override string Description { get { return "Shows the phone number of a contact"; } }
			public override string ArgumentsExample { get { return "yolwork"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>(NameAndNumberDictionary.Keys)
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument required for Call command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Call command may not be null/empty/whitespaces";
				else if (Index == 0 && !NameAndNumberDictionary.ContainsKey(argumentValue))
					errorMessage = "Name not found in contact list: " + argumentValue;
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Call command";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(this, textFeedbackEvent, NameAndNumberDictionary[arguments[0]], TextFeedbackType.Noteworthy);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot find name in dictionary: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private static Dictionary<string, string> NameAndNumberDictionary = new Dictionary<string, string>()
			{
				{ "yolwork", "Yolande work: (021) 853 3564" },
				{ "imqs", "IMQS office: 021-880 2712 / 880 1632" },
				{ "kerry", "Kerry extension: 107" },
				{ "adrian", "Adrian extension: 106" },
				{ "deon",   "Deon extension: 121" },
				{ "johann", "Johann extension: 119" },
				{ "wesley", "Wesley extension: 111" },
				{ "honda",  "Honda Tygervalley: 021 910 8300" }
			};

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "name", new ObservableCollection<string>(NameAndNumberDictionary.Keys))
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "name")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class KillCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "kill"; } }
			public override string DisplayName { get { return "Kill"; } }
			public override string Description { get { return "Kills a process"; } }
			public override string ArgumentsExample { get { return "notepad"; } }

			private ObservableCollection<string> LastProcessList;
			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { }//Empty collection because populated on demand
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList
			{
				get
				{
					if (LastProcessList == null) LastProcessList = new ObservableCollection<string>();
					List<string> tmpList = new List<string>();
					Process[] processes = System.Diagnostics.Process.GetProcesses();
					foreach (Process proc in processes)
						tmpList.Add(proc.ProcessName);
					tmpList.Sort();
					for (int i = LastProcessList.Count - 1; i >= 0; i--)
						if (!tmpList.Contains(LastProcessList[i]))
							LastProcessList.RemoveAt(i);
					foreach (string item in tmpList)
						if (!LastProcessList.Contains(item))
							LastProcessList.Add(item);
					predefinedArgumentsList[0] = LastProcessList;
					return predefinedArgumentsList;
				}
			}

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument required for Kill command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Kill command may not be null/empty/whitespaces";
				else if (Index == 0 && !GetPredefinedArgumentsList(Index).Contains(argumentValue))
					errorMessage = "Process not found in running list: " + argumentValue;
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Kill command";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					errorMessage = "";
					string processName = arguments[0];
					Process[] processes = Process.GetProcessesByName(processName);
					if (processes.Length > 1) errorMessage = "More than one process found, cannot kill";
					else if (processes.Length == 0) errorMessage = "Cannot find process with name ";
					else
					{
						if (UserMessages.Confirm("Confirm to kill process '" + processes[0].ProcessName + "'"))
						{
							processes[0].Kill();
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(this, textFeedbackEvent, "Process killed: " + processName, TextFeedbackType.Noteworthy);
							errorMessage = "";

							//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Process killed: " + processName);
						}
						else errorMessage = "User cancelled to kill process";
						return true;
					}
					return false;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot kill process: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "process name", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "process name")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class StartubBatCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "startupbat"; } }
			public override string DisplayName { get { return "Startup bat"; } }
			public override string Description { get { return "Startup batch file"; } }
			public override string ArgumentsExample { get { return "getline outlook.exe"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "open", "getall", "getline", "comment", "uncomment" },
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			private bool IsStartubBatCommand(string command)
			{
				string commandLowercase = command.ToLower().Trim();
				return
					commandLowercase == "open" ||
					commandLowercase == "getall" ||
					commandLowercase == "getline" ||
					commandLowercase == "comment" ||
					commandLowercase == "uncomment";
			}

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index >= 2)
					errorMessage = "More than 2 arguments not allowed for Startub bat command";
				else if (Index == 0 && !IsStartubBatCommand(argumentValue))
					errorMessage = "First argument of Startup bat command is invalid, must be one of the predefined commands: " + argumentValue;
				else if (Index == 1 && (new string[] { "open", "getall" }).Contains(CurrentArguments[0].CurrentValue))
					errorMessage = "No additional arguments allowed for '" + CurrentArguments[0].CurrentValue + "'";
				else if (Index == 1 && (new string[] { "comment", "uncomment" }).Contains(CurrentArguments[0].CurrentValue) && !CanParseToInt(argumentValue))
					errorMessage = "Second argument of Startup bat command (" + CurrentArguments[0].CurrentValue + ") must be a valid integer";
				else if (Index == 1 && "getline" == CurrentArguments[0].CurrentValue && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Second argument of Startup bat command (" + CurrentArguments[0].CurrentValue + ") may not be null/empty/whitespaces";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				//minutes, autosnooze, name, desc
				errorMessage = "";
				if (arguments.Length < 1) errorMessage = "At least 1 argument is required for Startub bat command";
				else if (arguments.Length > 2) errorMessage = "More than 2 arguments not allowed for Startup bat command";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else if (arguments.Length > 1 && !PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					string filePath = @"C:\Francois\Other\Startup\work Startup.bat";
					StartupbatInterop.PerformStartupbatCommand(this, filePath, arguments[0] + (arguments.Length > 1 ? " " + arguments[1] : ""), textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot add todo item: " + Environment.NewLine + exc.Message);
					errorMessage = "Cannot perform startup bat command: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "sub-command", new ObservableCollection<string>() { "open", "getall", "getline", "comment", "uncomment" }),
				new CommandArgument("", "parameter", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "sub-command"),
			//	new KeyAndValuePair("", "parameter")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments
			{
				get
				{
					if (currentArguments.Count > 0 && (new string[] { "getline", "comment", "uncomment" }).Contains(currentArguments[0].CurrentValue))
						return 2;
					else return 1;
				}
			}
		}

		public class CmdCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "cmd"; } }
			public override string DisplayName { get { return "Cmd"; } }
			public override string Description { get { return "Open a folder in Command Prompt"; } }
			public override string ArgumentsExample { get { return @"c:\windows\system32"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { @"c:\windows", @"c:\windows\system32" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument allowed for Cmd command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of Cmd command may not be null/empty/whitespaces only";
				else if (Index == 0 && !Directory.Exists(argumentValue))
					errorMessage = "First argument of Cmd command must be existing directory";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for Cmd command";
				//else if (!(arguments[0] is string)) errorMessage = "First argument of Explore command must be of type string";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					WindowsInterop.StartCommandPromptOrVScommandPrompt(this, arguments[0], false, textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot open Cmd: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "folder", new ObservableCollection<string>() { @"c:\Windows", @"c:\Windows\System32" })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "folder")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class VsCmdCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "vscmd"; } }
			public override string DisplayName { get { return "VsCmd"; } }
			public override string Description { get { return "Open a folder in Visual Command Prompt"; } }
			public override string ArgumentsExample { get { return @"c:\windows\system32"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { @"c:\windows", @"c:\windows\system32" }
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Only one argument allowed for VsCmd command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument of VsCmd command may not be null/empty/whitespaces only";
				else if (Index == 0 && !Directory.Exists(argumentValue))
					errorMessage = "First argument of VsCmd command must be existing directory";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exactly one argument required for VsCmd command";
				//else if (!(arguments[0] is string)) errorMessage = "First argument of Explore command must be of type string";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					WindowsInterop.StartCommandPromptOrVScommandPrompt(this, arguments[0], true, textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot open VsCmd: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "folder", new ObservableCollection<string>() { @"c:\Windows", @"c:\Windows\System32" })
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "folder")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class BtwCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "btw"; } }
			public override string DisplayName { get { return "Btwtodo"; } }
			public override string Description { get { return "Add btw (by the way) item on firepuma"; } }
			public override string ArgumentsExample { get { return "Steve Jobs was friends with Bill Gates"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index != 0)
					errorMessage = "Exaclty one argument required for Btw command";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument Btw command may not be null/empty/whitespaces only";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				//minutes, autosnooze, name, desc
				errorMessage = "";
				if (arguments.Length != 1) errorMessage = "Exaclty one argument required for Btw command";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					PhpInterop.AddBtwTextFirepuma(this, arguments[0], textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					//UserMessages.ShowWarningMessage("Cannot add todo item: " + Environment.NewLine + exc.Message);
					errorMessage = "Cannot add btw item: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "btw text", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "btw text")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 1; } }
		}

		public class SvnCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "svn"; } }
			public override string DisplayName { get { return "Svn"; } }
			public override string Description { get { return "Perform svn command(s) on a folder"; } }
			public override string ArgumentsExample { get { return @"commit c:\dev86\myproject1;Bug fixed where it automatically..."; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "commit", "update", "status", "statuslocal" },
				new ObservableCollection<string>() { "all", "QuickAccess", "SharedClasses", "TestingSharedClasses" },
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index >= 3)
					errorMessage = "More than 3 arguments not allowed for Svn command (sub-command, folder, description)";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument (sub-command) of Svn command may not be null/empty/whitespaces";
				else if (Index == 0 && !(predefinedArgumentsList[0].ToArray()).Contains(argumentValue))
					errorMessage = "First argument of Svn command is an invalid sub-command";
				else if (Index == 1 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Second argument (folder) of Svn command may not be null/empty/whitespaces";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				//minutes, autosnooze, name, desc
				errorMessage = "";
				if (arguments.Length < 2) errorMessage = "At least 2 arguments required for Svn command (sub-command, folder, description)";
				else if (arguments.Length > 3) errorMessage = "More than 3 arguments not allowed for Svn command (sub-command, folder, description)";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else if (!PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					SvnInterop.SvnCommand svnCommand;
					if (Enum.TryParse<SvnInterop.SvnCommand>(arguments[0], true, out svnCommand))
					{
						SvnInterop.PerformSvn(
							this,
							arguments[1] + (arguments[0] == "commit" ? ";" + arguments[2] : ""),
						 svnCommand,
						 textFeedbackEvent);
						errorMessage = "";
						return true;
					}
					else
					{
						errorMessage = "Invalid svn command = " + arguments[0];
						return false;
					}
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot perform Svn command: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "sub-command", new ObservableCollection<string>() { "commit", "update", "status", "statuslocal" }),
				new CommandArgument("", "folder/path", new ObservableCollection<string>() { "all", "QuickAccess", "SharedClasses", "TestingSharedClasses"}),
				new CommandArgument("", "description", new ObservableCollection<string>())
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "sub-command"),
			//	new KeyAndValuePair("", "Folder/path"),
			//	new KeyAndValuePair("", "Description")
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments
			{
				get
				{
					if (currentArguments.Count > 0 && (new string[] { "commit" }).Contains(currentArguments[0].CurrentValue))
						return 3;
					else return 2;
				}
			}
		}

		public class PublishCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "publish"; } }
			public override string DisplayName { get { return "Publish"; } }
			public override string Description { get { return "Perform publish command(s) on a folder"; } }
			public override string ArgumentsExample { get { return @"localvs c:\dev86\myproject1"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				new ObservableCollection<string>() { "localvs", "onlinevs" },
				new ObservableCollection<string>() { "QuickAccess", "MonitorSystem" },
			};
			public override ObservableCollection<string>[] PredefinedArgumentsList { get { return predefinedArgumentsList; } }

			private readonly Dictionary<string, string>[] argumentsReplaceKeyValuePair =
			{
			};
			public override Dictionary<string, string>[] ArgumentsReplaceKeyValuePair { get { return argumentsReplaceKeyValuePair; } }

			public override bool PreValidateArgument(out string errorMessage, int Index, string argumentValue)
			{
				errorMessage = "";
				if (Index < argumentsReplaceKeyValuePair.Length && argumentsReplaceKeyValuePair[Index].ContainsKey(argumentValue))
					argumentValue = argumentsReplaceKeyValuePair[Index][argumentValue];
				if (Index >= 2)
					errorMessage = "More than 2 arguments not allowed for Publish command (sub-command, folder/project)";
				else if (Index == 0 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "First argument (sub-command) of Publish command may not be null/empty/whitespaces";
				else if (Index == 0 && !(predefinedArgumentsList[0].ToArray()).Contains(argumentValue))
					errorMessage = "First argument of Publish command is an invalid sub-command";
				else if (Index == 1 && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Second argument (folder) of Publish command may not be null/empty/whitespaces";
				else return true;
				return false;
			}

			public override bool ValidateArguments(out string errorMessage, params string[] arguments)
			{
				//minutes, autosnooze, name, desc
				errorMessage = "";
				if (arguments.Length != 2) errorMessage = "Exactly 2 arguments required for Publish command (sub-command, folder/project)";
				else if (!PreValidateArgument(out errorMessage, 0, arguments[0]))
					errorMessage = errorMessage + "";
				else if (!PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					if (arguments[0] == "localvs")
					{
						string tmpNoUseVersionStr;
						VisualStudioInterop.PerformPublish(
							this,
							arguments[1],
							out tmpNoUseVersionStr,
							UserMessages.Confirm("Update the revision also?"),
							textFeedbackEvent: textFeedbackEvent);
					}
					else if (arguments[0] == "onlinevs")
					{
						VisualStudioInterop.PerformPublishOnline(
								 this,
								 arguments[1],
								 UserMessages.Confirm("Update the revision also?"),
								 textFeedbackEvent,
								 progressChangedEvent);
					}
					else
					{
						errorMessage = "Invalid sub-command for Publish: " + arguments[0];
						return false;
					}
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot perform Publish command: " + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private CommandArgument[] availableArguments = new CommandArgument[]
			{
				new CommandArgument("", "sub-command", new ObservableCollection<string>() { "localvs", "onlinevs" }),
				new CommandArgument("", "folder/path", new ObservableCollection<string>() { "QuickAccess", "MonitorSystem"})
			};
			public override CommandArgument[] AvailableArguments { get { return availableArguments; } set { availableArguments = value; } }

			//private KeyAndValuePair[] availableArgumentAndDescriptionsPair = new KeyAndValuePair[]
			//{
			//	new KeyAndValuePair("", "sub-command"),
			//	new KeyAndValuePair("", "Folder/Path"),
			//};
			//public override KeyAndValuePair[] AvailableArgumentAndDescriptionsPair { get { return availableArgumentAndDescriptionsPair; } set { availableArgumentAndDescriptionsPair = value; } }

			public override int ArgumentCountForCurrentPopulatedArguments { get { return 2; } }
		}

		//TODO: This platform (using interface) is already working fine, should build on on it and add all commands
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
}