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
using InlineCommandToolkit;
using ICommandWithHandler = InlineCommandToolkit.InlineCommands.ICommandWithHandler;
//using OverrideToStringClass = InlineCommandToolkit.InlineCommands.OverrideToStringClass;

namespace InlineCommands
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