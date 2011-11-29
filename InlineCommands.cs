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

namespace InlineCommands
{
	public class CommandsManagerClass
	{
		public delegate bool ActionDelegate(params string[] p);

		private static Dictionary<string, CommandClass> commandList = new Dictionary<string, CommandClass>();

		public static void AddToCommandList(CommandClass command)
		{
			commandList.Add(command.Name, command);
		}

		public static bool InvokeCommandAction(string fullCommandString)
		{
			string commandName = fullCommandString.Substring(0, fullCommandString.IndexOf(' '));
			if (!commandList.ContainsKey(commandName.ToLower()) && UserMessages.ShowErrorMessage("Could not invoke command, command not found: " + commandName.ToLower()))
				return false;

			CommandClass commandToCall = commandList[commandName.ToLower()];
			string[] commandParams = fullCommandString.Substring(fullCommandString.IndexOf(' ') + 1).Split(commandToCall.CommandArgumentSeparatorChar);
			if (commandParams.Length == 0 && UserMessages.ShowErrorMessage("Could not invoke command, command has no parameters passed: " + fullCommandString))
				return false;

			//string[] arguments = fullCommandString.Substring(.Split(commandToCall.CommandArgumentSeparatorChar);
			//MessageBox.Show("Invoking: " + fullCommandString);
			commandToCall.Action(commandParams);
			return false;
		}

		public enum ArgumentTypeEnum { Text, Int }
		[Flags]
		public enum ValidationTypeEnum
		{
			None = 0,
			File = 1,
			Directory = 2,
			Email = 4
		}

		interface IGeneralObjectWithName
		{
			string Name { get; set; }
			string DisplayName { get; set; }
			string Description { get; set; }
		}

		public class CommandClass : IGeneralObjectWithName
		{
			public string Name { get; set; }
			public string DisplayName { get; set; }
			public string Description { get; set; }
			public List<CommandArgument> CommandArguments { get; set; }
			public char CommandArgumentSeparatorChar { get; set; }
			public ActionDelegate Action { get; set; }

			public CommandClass(string NameIn, string DisplayNameIn, string DescriptionIn, List<CommandArgument> CommandArgumentsIn, ActionDelegate ActionIn, char CommandArgumentSeparatorCharIn = ';')
			{
				Name = NameIn;
				DisplayName = DisplayNameIn;
				Description = DescriptionIn;
				CommandArguments = CommandArgumentsIn;
				CommandArgumentSeparatorChar = CommandArgumentSeparatorCharIn;
				Action = ActionIn;
			}
		}

		public class CommandArgument : IGeneralObjectWithName
		{
			public string Name { get; set; }
			public string DisplayName { get; set; }
			public string Description { get; set; }
			public bool Required { get; set; }
			public ArgumentTypeEnum ArgumentType { get; set; }
			public ValidationTypeEnum ValidationType { get; set; }

			public CommandArgument(string NameIn, string DisplayNameIn, string DescriptionIn, bool RequiredIn, ArgumentTypeEnum ArgumentTypeIn, ValidationTypeEnum ValidationTypeIn)
			{
				Name = NameIn;
				DisplayName = DisplayNameIn;
				Description = DescriptionIn;
				Required = RequiredIn;
				ArgumentType = ArgumentTypeIn;
				ValidationType = ValidationTypeIn;
			}

			public bool ValidateArgument(ValidationTypeEnum validationType, string ArgumentText)
			{
				if (validationType == ValidationTypeEnum.File) return File.Exists(ArgumentText);
				if (validationType == ValidationTypeEnum.Directory) return Directory.Exists(ArgumentText);
				if (validationType == ValidationTypeEnum.Email) return IsValidEmail(ArgumentText);
				return false;
			}

			public static bool IsValidEmail(string strIn)
			{
				// Return true if strIn is in valid e-mail format.
				return Regex.IsMatch(strIn,
							 @"^(?("")(""[^""]+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
							 @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
			}
		}
	}

	public class InlineCommands
	{
		public static readonly string AvailableActionList = "Type tasks: todo, run, mail, explore, web, google, kill, startupbat, call, cmd, btw, svncommit, etc";

		public static Dictionary<string, CommandDetails> CommandList = new Dictionary<string, CommandDetails>();
		public static AutoCompleteStringCollection AutoCompleteAllactionList;

		private static void AddToCommandList(string commandNameIn, string UserLabelIn, List<CommandDetails.CommandArgumentClass> commandArgumentsIn, CommandDetails.PerformCommandTypeEnum PerformCommandTypeIn)
		{
			bool requiredFoundAfterOptional = false;
			if (commandArgumentsIn != null)
			{
				bool optionalFound = false;
				foreach (CommandDetails.CommandArgumentClass ca in commandArgumentsIn)
				{
					if (!ca.Required) optionalFound = true;
					if (optionalFound && ca.Required)
						requiredFoundAfterOptional = true;
				}
			}
			if (!requiredFoundAfterOptional)
			{
				CommandList.Add(commandNameIn.ToLower(), new CommandDetails(commandNameIn, UserLabelIn, commandArgumentsIn, PerformCommandTypeIn));
				RepopulateAutoCompleteAllactionList();
			}
			else MessageBox.Show("Cannot have required parameter after optional: " + commandNameIn, "Error in argument list", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public static void PopulateCommandList()
		{
			AddToCommandList("todo",
				"todo MinutesFromNow;Autosnooze;Item name;Description",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("MinutesFromNow", true, CommandDetails.TypeArg.Int, null),
						new CommandDetails.CommandArgumentClass("AutosnoozeInterval", true, CommandDetails.TypeArg.Int, null),
						new CommandDetails.CommandArgumentClass("ItemName", true, CommandDetails.TypeArg.Text, null),
						new CommandDetails.CommandArgumentClass("ItemDescription", false, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.AddTodoitemFirepuma);

			AddToCommandList("run",
				"run chrome/canary/delphi2010/delphi2007/phpstorm",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrPath", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "Chrome", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\Application\chrome.exe" },
								{ "Canary", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome SxS\Application\chrome.exe" },
								{ "Delphi2007", @"C:\Program Files (x86)\CodeGear\RAD Studio\5.0\bin\bds.exe" },
								{ "Delphi2010", @"C:\Program Files (x86)\Embarcadero\RAD Studio\7.0\bin\bds.exe" },
								{ "PhpStorm", @"C:\Program Files (x86)\JetBrains\PhpStorm 2.1.4\bin\PhpStorm.exe" },
								{ "SqliteSpy", @"C:\Francois\Programs\SQLiteSpy_1.9.1\SQLiteSpy.exe" }
							},
							CommandDetails.PathAutocompleteEnum.Both)
					},
				CommandDetails.PerformCommandTypeEnum.CheckFileExistRun_ElseTryRun);

			AddToCommandList("mail",
				"mail to;subject;body",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("Toaddress", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "fhill@gls.co.za", null },
								{ "francoishill11@gmail.com", null },
								{ "fhillhome@gmail.com", null }
							}),
						new CommandDetails.CommandArgumentClass("Subject", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "Please remember", null },
							}),
						new CommandDetails.CommandArgumentClass("Body", false, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.CreateNewOutlookMessage);

			AddToCommandList("explore",
				"explore franother/prog/docs/folderpath",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrPath", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "franother", @"c:\francois\other" },
								{ "prog", @"c:\programming" },
								{ "docs", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }
							},
							CommandDetails.PathAutocompleteEnum.Directories)
					},
				CommandDetails.PerformCommandTypeEnum.CheckDirectoryExistRun_ElseTryRun);

			AddToCommandList("web", "web google.com",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrUrl", true, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.WebOpenUrl);

			AddToCommandList("google", "google search on google",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("StringToSearchInGoogle", true, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.WebSearchGoogle);

			AddToCommandList("kill", "kill processNameToKill",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrProcessname", true, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.KillProcess);

			AddToCommandList("startupbat",
				"startupbat open/getall/getline xxx/comment #/uncomment #",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("Command", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "open", null },
								{ "getall", null },
								{ "getline xxx", null },
								{ "comment #", null },
								{ "uncomment #", null }
							}),
						new CommandDetails.CommandArgumentClass("ArgumentForCommand", false, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.StartupBat);

			AddToCommandList("call",
				"call yolwork/imqs/kerry/deon/johann/wesley/honda",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("Token", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "yolwork", "Yolande work: (021) 853 3564" },
								{ "imqs", "IMQS office: 021-880 2712 / 880 1632" },
								{ "kerry", "Kerry extension: 107" },
								{ "adrian", "Adrian extension: 106" },
								{ "deon",   "Deon extension: 121" },
								{ "johann", "Johann extension: 119" },
								{ "wesley", "Wesley extension: 111" },
								{ "honda",  "Honda Tygervalley: 021 910 8300" }
							})
					},
				CommandDetails.PerformCommandTypeEnum.Call);

			AddToCommandList("cmd",
				"cmd Firepuma/folderpath",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrPath", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "firepuma", @"c:\francois\websites\firepuma" }
							},
							CommandDetails.PathAutocompleteEnum.Directories)
					},
				CommandDetails.PerformCommandTypeEnum.Cmd);

			AddToCommandList("vscmd",
				"vscmd AllbionX86/folderpath",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TokenOrPath", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "Albionx86", @"c:\devx86\Albion" }
							},
							CommandDetails.PathAutocompleteEnum.Directories)
					},
				CommandDetails.PerformCommandTypeEnum.VsCmd);

			AddToCommandList("btw", "btw text",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("TextToUploadToBtw", true, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.Btw);

			AddToCommandList("svncommit",
				"svncommit User32stuff;Log message",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("VsProjectName", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "Wadiso5", @"C:\Programming\Wadiso5\W5Source" },
								{ "GLScore_programming", @"C:\Programming\GLSCore" },
								{ "GLScore_srmsdata", @"C:\Data\Delphi\GLSCore" },
								{ "DelphiChromiumEmbedded", WindowsInterop.MydocsPath + @"\RAD Studio\Projects\TestChrome_working_svn" },
								{ "GLSreports_cssjs", @"C:\ProgramData\GLS\Wadiso\Reports" },
								{ "GLSreports_xmlsql", @"C:\ProgramData\GLS\ReportSQLqueries" },
								{ "QuickAccess", null },
								{ "MonitorSystem", null },
								//TODO: Need to add SharedClasses into all autocomplete lists (svncommint, svnupdate, etc...)
								{ "NSISinstaller", null }
							},
							CommandDetails.PathAutocompleteEnum.Both),
						new CommandDetails.CommandArgumentClass("LogMessage", true, CommandDetails.TypeArg.Text, null)
					},
				CommandDetails.PerformCommandTypeEnum.Svncommit);

			AddToCommandList("svnupdate",
				"svnupdate User32stuff",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("VsProjectName", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "wadiso5", @"C:\Programming\Wadiso5\W5Source" },
								{ "GLScore_programming", @"C:\Programming\GLSCore" },
								{ "GLScore_srmsdata", @"C:\Data\Delphi\GLSCore" },
								{ "DelphiChromiumEmbedded", WindowsInterop.MydocsPath + @"\RAD Studio\Projects\TestChrome_working_svn" },
								{ "GLSreports_cssjs", @"C:\ProgramData\GLS\Wadiso\Reports" },
								{ "GLSreports_xmlsql", @"C:\ProgramData\GLS\ReportSQLqueries" },
								{ "QuickAccess", null },
								{ "MonitorSystem", null },
								{ "NSISinstaller", null }
							},
							CommandDetails.PathAutocompleteEnum.Both)
					},
				CommandDetails.PerformCommandTypeEnum.Svnupdate);

			AddToCommandList("svnstatusboth",
				"svnstatusboth User32stuff",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("VsProjectName", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "Wadiso5", @"C:\Programming\Wadiso5\W5Source" },
								{ "GLScore_programming", @"C:\Programming\GLSCore" },
								{ "GLScore_srmsdata", @"C:\Data\Delphi\GLSCore" },
								{ "DelphiChromiumEmbedded", WindowsInterop.MydocsPath + @"\RAD Studio\Projects\TestChrome_working_svn" },
								{ "GLSreports_cssjs", @"C:\ProgramData\GLS\Wadiso\Reports" },
								{ "GLSreports_xmlsql", @"C:\ProgramData\GLS\ReportSQLqueries" },
								{ "QuickAccess", null },
								{ "MonitorSystem", null },
								{ "NSISinstaller", null }
							},
							CommandDetails.PathAutocompleteEnum.Both)
					},
				CommandDetails.PerformCommandTypeEnum.Svnstatus);

			AddToCommandList("svnstatuslocal",
				"svnstatuslocal all",
				new List<CommandDetails.CommandArgumentClass>()
				{
					new CommandDetails.CommandArgumentClass("FolderName", false, CommandDetails.TypeArg.Text,
						new Dictionary<string, string>()
						{
							{ "all", null }
						},
						CommandDetails.PathAutocompleteEnum.Both)
				},
				CommandDetails.PerformCommandTypeEnum.SvnstatusLocal);

			AddToCommandList("publishvs",
				"publishvs QuickAccess",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("VsProjectName", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "QuickAccess", null },
								{ "MonitorSystem", null }
							},
							CommandDetails.PathAutocompleteEnum.Both)
					},
				CommandDetails.PerformCommandTypeEnum.PublishVs);

			AddToCommandList("publishvsonline",
				"publishvsonline QuickAccess",
				new List<CommandDetails.CommandArgumentClass>()
					{
						new CommandDetails.CommandArgumentClass("VsProjectName", true, CommandDetails.TypeArg.Text,
							new Dictionary<string,string>()
							{
								{ "QuickAccess", null },
								{ "MonitorSystem", null }
							},
							CommandDetails.PathAutocompleteEnum.Both)
					},
				CommandDetails.PerformCommandTypeEnum.PublishVsOnline);
		}

		public static CommandDetails GetCommandDetailsFromTextboxText(string TextboxTextIn)
		{
			if (CommandList != null && TextboxTextIn.Contains(' '))
			{
				string tmpkey = TextboxTextIn.Substring(0, TextboxTextIn.IndexOf(' ')).ToLower();
				if (CommandList.ContainsKey(tmpkey))
					return CommandList[tmpkey];
			}
			else if (CommandList != null)
			{
				string tmpkey = TextboxTextIn.ToLower();
				if (CommandList.ContainsKey(tmpkey))
					return CommandList[tmpkey];
			}
			return null;
		}

		private static void RepopulateAutoCompleteAllactionList()
		{
			if (AutoCompleteAllactionList != null) AutoCompleteAllactionList.Clear();
			AutoCompleteAllactionList = new AutoCompleteStringCollection();
			foreach (string key in CommandList.Keys)
				AutoCompleteAllactionList.Add(CommandList[key].commandName);
		}

		public class CommandDetails
		{
			public enum TypeArg { Int, Text };
			public enum PathAutocompleteEnum { Directories, Files, Both, None };
			public enum PerformCommandTypeEnum
			{
				CheckFileExistRun_ElseTryRun,
				CheckDirectoryExistRun_ElseTryRun,
				AddTodoitemFirepuma,
				CreateNewOutlookMessage,
				WebOpenUrl,
				WebSearchGoogle,
				KillProcess,
				StartupBat,
				Call,
				Cmd,
				VsCmd,
				Btw,
				Svncommit,
				Svnupdate,
				Svnstatus,
				SvnstatusLocal,
				PublishVs,
				PublishVsOnline,
				Undefined
			};
			public const char ArgumentSeparator = ';';

			public string commandName;
			public AutoCompleteStringCollection commandPredefinedArguments;//, originalPredefinedArguments;
			public string UserLabel;
			public List<CommandArgumentClass> commandArguments;
			public PerformCommandTypeEnum PerformCommandType;
			//public CommandForm commandForm;
			//public CommandWindow commandUsercontrol;
			public CommandUserControl commandUsercontrol;
			public CommandDetails(string commandNameIn, string UserLabelIn, List<CommandArgumentClass> commandArgumentsIn, PerformCommandTypeEnum PerformCommandTypeIn, CommandUserControl commandUsercontrolIn = null)
			{
				commandName = commandNameIn;
				commandPredefinedArguments = new AutoCompleteStringCollection();
				foreach (CommandArgumentClass commarg in commandArgumentsIn)
					if (commarg.TokenWithReplaceStringPair != null)
						foreach (string key in commarg.TokenWithReplaceStringPair.Keys)
							commandPredefinedArguments.Add(commandNameIn + " " + key);

				//commandPredefinedArguments = new AutoCompleteStringCollection();
				//originalPredefinedArguments = new AutoCompleteStringCollection();
				//if (commandPredefinedArgumentsIn != null)
				//  foreach (string arg in commandPredefinedArgumentsIn)
				//  //for (int i = 0; i < commandPredefinedArgumentsIn.Count; i++)
				//  {
				//    if (arg.Length > 0)
				//    {
				//      //string arg = commandPredefinedArgumentsIn[i];
				//      commandPredefinedArguments.Add(commandNameIn + " " + arg);
				//      originalPredefinedArguments.Add(commandNameIn + " " + arg);
				//      /*string[] argsSplitted = arg.Split(ArgumentSeparator);
				//      string argwithpaths = "";
				//      bool atleastOneMatchFound = false;
				//      for (int i = 0; i < argsSplitted.Length; i++)
				//      {
				//        if (commandArgumentsIn != null && commandArgumentsIn.Count > i
				//          && (commandArgumentsIn[i].PathAutocomplete == PathAutocompleteEnum.Directories
				//            || commandArgumentsIn[i].PathAutocomplete == PathAutocompleteEnum.Files
				//            || commandArgumentsIn[i].PathAutocomplete == PathAutocompleteEnum.Both))
				//        {
				//          argwithpaths += (argwithpaths.Length > 0 ? ";" : "") + @"c:\";
				//          atleastOneMatchFound = true;
				//        }
				//        else argwithpaths += (argwithpaths.Length > 0 ? ";" : "") + argsSplitted[i];
				//      }
				//      if (atleastOneMatchFound)
				//      {
				//        commandPredefinedArguments.Add(commandName + " " + argwithpaths);
				//        originalPredefinedArguments.Add(commandNameIn + " " + argwithpaths);
				//      }*/
				//    }
				//  }
				UserLabel = UserLabelIn;
				commandArguments = commandArgumentsIn;
				PerformCommandType = PerformCommandTypeIn;

				commandUsercontrol = commandUsercontrolIn;
			}

			public void PerformCommand(string fullCommandText, ComboBox textboxtoClearOnSuccess, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChanged = null)
			{
				PerformCommandStatic(
					PerformCommandType,
					commandArguments,
					fullCommandText,
					textboxtoClearOnSuccess,
					textFeedbackEvent,
					progressChanged);
			}

			public static void PerformCommandStatic(PerformCommandTypeEnum PerformCommandType, List<CommandArgumentClass> commandArguments, string fullCommandText, ComboBox textboxtoClearOnSuccess, TextFeedbackEventHandler textFeedbackEvent, ProgressChangedEventHandler progressChanged)
			{
				string TextboxTextIn = fullCommandText;//textboxtoClearOnSuccess.Text;
				string argStr = TextboxTextIn.Contains(' ') ? TextboxTextIn.Substring(TextboxTextIn.IndexOf(' ') + 1) : "";

				switch (PerformCommandType)
				{
					case PerformCommandTypeEnum.CheckFileExistRun_ElseTryRun:
					case PerformCommandTypeEnum.CheckDirectoryExistRun_ElseTryRun:
						if (commandArguments.Count > 1) MessageBox.Show("More than one command argument not yet incorporated");
						else
						{
							string exepath = argStr;
							if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(exepath))
								exepath = commandArguments[0].TokenWithReplaceStringPair[exepath] ?? exepath;
							if (
								(File.Exists(exepath) && PerformCommandType == PerformCommandTypeEnum.CheckFileExistRun_ElseTryRun) ||
								(Directory.Exists(exepath) && PerformCommandType == PerformCommandTypeEnum.CheckDirectoryExistRun_ElseTryRun))
								System.Diagnostics.Process.Start(exepath);
							else
							{
								try
								{
									System.Diagnostics.Process.Start(exepath);
								}
								catch (Exception exc)
								{
									TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, exc.Message);
									//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, exc.Message);
								}
							}
						}
						break;

					case PerformCommandTypeEnum.AddTodoitemFirepuma:
						PhpInterop.AddTodoItemFirepuma(
							PhpInterop.ServerAddress,
							PhpInterop.doWorkAddress,
							PhpInterop.Username,
							PhpInterop.Password,
						 "QuickAccess",
						 "Quick todo",
						 argStr.Split(';')[2],
						 argStr.Split(';')[3],
						 false,
						 DateTime.Now.AddMinutes(Convert.ToInt32(argStr.Split(';')[0])),
						 DateTime.Now,
						 0,
						 false,
						 Convert.ToInt32(argStr.Split(';')[1]),
						 textFeedbackEvent);
						break;

					case PerformCommandTypeEnum.CreateNewOutlookMessage:
						MicrosoftOfficeInterop.CreateNewOutlookMessage(
									argStr.Split(';')[0],
									argStr.Split(';')[1],
									argStr.Split(';').Length >= 3 ? argStr.Split(';')[2] : "",
									textFeedbackEvent);
						break;

					case PerformCommandTypeEnum.WebOpenUrl:
						string url = argStr;
						if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("www."))
							url = "http://" + url;
						System.Diagnostics.Process.Start(url);
						break;

					case PerformCommandTypeEnum.WebSearchGoogle:
						System.Diagnostics.Process.Start("http://www.google.co.za/search?q=" + argStr);
						break;

					case PerformCommandTypeEnum.KillProcess:
						string processName = argStr;
						Process[] processes = Process.GetProcessesByName(processName);
						if (processes.Length > 1) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "More than one process found, cannot kill");//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "More than one process found, cannot kill");
						else if (processes.Length == 0) TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Cannot find process with name " + processName);//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Cannot find process with name " + processName);
						else
						{
							if (UserMessages.Confirm("Confirm to kill process '" + processes[0].ProcessName + "'"))
							{
								processes[0].Kill();
								TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Process killed: " + processName);
								//Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Process killed: " + processName);
							}
						}
						break;

					case PerformCommandTypeEnum.StartupBat:
						string filePath = @"C:\Francois\Other\Startup\work Startup.bat";
						string comm = argStr;
						//getall/getline 'xxx'/comment #/uncomment #
						StartupbatInterop.PerformStartupbatCommand(filePath, comm, textFeedbackEvent);
						break;

					case PerformCommandTypeEnum.Call:
						if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(argStr))
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, commandArguments[0].TokenWithReplaceStringPair[argStr] ?? argStr);
						else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Call command not recognized: " + argStr);
						break;

					case PerformCommandTypeEnum.Cmd:
					case PerformCommandTypeEnum.VsCmd:
						string cmdpath = argStr;
						foreach (string commaSplitted in cmdpath.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						{
							string cmdpathSplitted = commaSplitted;
							if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(cmdpathSplitted))
								cmdpathSplitted = commandArguments[0].TokenWithReplaceStringPair[cmdpathSplitted] ?? cmdpathSplitted;

							WindowsInterop.StartCommandPromptOrVScommandPrompt(cmdpathSplitted, PerformCommandType == PerformCommandTypeEnum.VsCmd, textFeedbackEvent);
						}
						break;

					case PerformCommandTypeEnum.Btw:
						if (PhpInterop.AddBtwTextFirepuma(argStr, textFeedbackEvent))
							textboxtoClearOnSuccess.Text = "";
						break;

					case PerformCommandTypeEnum.Svncommit:
					case PerformCommandTypeEnum.Svnupdate:
					case PerformCommandTypeEnum.Svnstatus:
					case PerformCommandTypeEnum.SvnstatusLocal:
						string svnargs = argStr;// textBox1.Text.ToLower().Substring(10);
						//if (svncommitargs.Contains(' '))
						//{
						//string svncommand = svncommandwithargs.Substring(0, svncommandwithargs.IndexOf(' ')).ToLower();
						//string projnameAndlogmessage = svncommandwithargs.Substring(svncommandwithargs.IndexOf(' ') + 1);
						//if (svncommitargs.Contains(';'))//projnameAndlogmessage.Contains(';'))
						//{
						SvnInterop.SvnCommand svnCommand =
								PerformCommandType == PerformCommandTypeEnum.Svncommit ? SvnInterop.SvnCommand.Commit
							: PerformCommandType == PerformCommandTypeEnum.Svnupdate ? SvnInterop.SvnCommand.Update
							: PerformCommandType == PerformCommandTypeEnum.Svnstatus ? SvnInterop.SvnCommand.Status
							: PerformCommandType == PerformCommandTypeEnum.SvnstatusLocal ? SvnInterop.SvnCommand.StatusLocal
							: SvnInterop.SvnCommand.StatusLocal;

						string[] splittedArgsOnlyForUpdateAndStatus = svnargs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						if (svnCommand == SvnInterop.SvnCommand.Commit) splittedArgsOnlyForUpdateAndStatus = new string[] { svnargs };
						foreach (string commaSplitted in splittedArgsOnlyForUpdateAndStatus)
						{
							string tmpsplit = commaSplitted;
							string svnprojname = tmpsplit.Contains(ArgumentSeparator) ? tmpsplit.Split(ArgumentSeparator)[0] : tmpsplit;
							string svnlogmessage = tmpsplit.Contains(ArgumentSeparator) ? tmpsplit.Split(ArgumentSeparator)[1] : "";
							if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(svnprojname))
								tmpsplit = (commandArguments[0].TokenWithReplaceStringPair[svnprojname] ?? svnprojname) + (tmpsplit.Contains(ArgumentSeparator) ? ArgumentSeparator + svnlogmessage : "");
							SvnInterop.PerformSvn(tmpsplit, svnCommand, textFeedbackEvent);
						}
						//}
						//else appendLogTextbox_OfPassedTextbox(messagesTextbox, "Error: No semicolon. Command syntax is 'svncommit proj/othercommand projname;logmessage'");
						//}
						break;

					case PerformCommandTypeEnum.PublishVs:
						string tmpNoUseVersionStr;
						foreach (string commaSplit in argStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
							VisualStudioInterop.PerformPublish(commaSplit, out tmpNoUseVersionStr, textFeedbackEvent: textFeedbackEvent);//argStr);
						break;

					case PerformCommandTypeEnum.PublishVsOnline:
						foreach (string commaSplit in argStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
							VisualStudioInterop.PerformPublishOnline(
								commaSplit,
								UserMessages.Confirm("Update the revision also?"),
								textFeedbackEvent,
								progressChanged);//argStr);
						break;

					case PerformCommandTypeEnum.Undefined:
						UserMessages.ShowWarningMessage("PerformCommandType is not defined: " + fullCommandText);
						break;
					default:
						UserMessages.ShowWarningMessage("PerformCommandType is not incorporated yet: " + PerformCommandType.ToString());
						break;
				}
			}

			public bool CommandHasRequiredArguments()
			{
				if (commandArguments == null) return false;
				if (commandArguments.Count == 0) return false;
				return commandArguments[0].Required;
			}

			public bool CommandHasArguments()
			{
				return commandArguments != null && commandArguments.Count > 0;
			}

			public bool CommandHasPredefinedArguments()
			{
				return commandPredefinedArguments != null && commandPredefinedArguments.Count > 0;
			}

			public bool TextHasAllRequiredArguments(string TextboxTextIn)
			{
				if (!CommandHasRequiredArguments()) return true;
				else
				{
					int RequiredArgumentCount = 0;
					foreach (CommandArgumentClass ca in commandArguments)
						if (ca.Required)
							RequiredArgumentCount++;
					if (TextboxTextIn.Length == 0) return false;
					if (!TextboxTextIn.Contains(' ')) return false;
					string argStr = TextboxTextIn.Substring(TextboxTextIn.IndexOf(' ') + 1);
					if (argStr.Length == 0) return false;
					if (RequiredArgumentCount > 1 && !argStr.Contains(ArgumentSeparator)) return false;
					if (argStr.Split(ArgumentSeparator).Length < RequiredArgumentCount) return false;
				}
				return true;
			}

			private int GetArgumentCountFromString(string str)
			{
				return str.Split(ArgumentSeparator).Length;
			}

			public bool TextValidateArguments(string TextboxTextIn, out string Errormsg)
			{
				Errormsg = "";
				if (commandArguments == null) return true;
				string argStr = TextboxTextIn.Substring(TextboxTextIn.IndexOf(' ') + 1);
				int ArgCount = commandArguments.Count;
				if (GetArgumentCountFromString(argStr) > ArgCount) return false;
				string[] InputArguments = argStr.Split(ArgumentSeparator);
				int cnt = 0;
				foreach (string s in InputArguments)
				{
					int tmpint;
					CommandArgumentClass comm = commandArguments[cnt];
					switch (comm.TypeOfArgument)
					{
						case TypeArg.Int:
							if (comm.Required && !int.TryParse(s, out tmpint))
							{
								Errormsg = "Cannot convert argument to Integer: " + comm.ArgumentName;
								return false;
							}
							break;
						case TypeArg.Text:
							if (comm.Required && s.Length == 0)
							{
								Errormsg = "Argument may not be empty: " + comm.ArgumentName;
								return false;
							}
							break;
						default:
							break;
					}
					cnt++;
				}
				return true;
			}

			public class CommandArgumentsAndFunctionArguments
			{
				public CommandArgumentClass commandDetails;
				public Object FunctionArgumentObject;
				public CommandArgumentsAndFunctionArguments(CommandArgumentClass commandDetailsIn, Object FunctionArgumentObjectIn)
				{
					commandDetails = commandDetailsIn;
					FunctionArgumentObject = FunctionArgumentObjectIn;
				}
			}

			public class CommandArgumentClass
			{
				public delegate void functionDelegate(CommandArgumentsAndFunctionArguments args);

				public string ArgumentName;
				public bool Required;
				public TypeArg TypeOfArgument;
				public Dictionary<string, string> TokenWithReplaceStringPair;
				public PathAutocompleteEnum PathAutocomplete;
				public System.Windows.Controls.TextBox textBox;
				//public functionDelegate function;
				public CommandArgumentClass(string ArgumentNameIn, bool RequiredIn, TypeArg TypeOfArgumentIn, Dictionary<string, string> TokenWithReplaceStringPairIn, PathAutocompleteEnum PathAutocompleteIn = PathAutocompleteEnum.None)//, functionDelegate functionIn)
				{
					ArgumentName = ArgumentNameIn;
					Required = RequiredIn;
					TypeOfArgument = TypeOfArgumentIn;
					TokenWithReplaceStringPair = TokenWithReplaceStringPairIn;
					PathAutocomplete = PathAutocompleteIn;
					//function = functionIn;
				}
			}
		}
	}

	public class KeyAndValuePair
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public KeyAndValuePair(string KeyIn, string ValueIn)
		{
			Key = KeyIn;
			Value = ValueIn;
		}
	}

	public class TempNewCommandsManagerClass
	{
		private static List<ICommandWithHandler> listOfInitializedCommandInterfaces = null;
		public static List<ICommandWithHandler> ListOfInitializedCommandInterfaces
		{
			get
			{
				if (listOfInitializedCommandInterfaces == null)
				{
					listOfInitializedCommandInterfaces = new List<ICommandWithHandler>();
					Type[] types = typeof(TempNewCommandsManagerClass).GetNestedTypes(BindingFlags.Public);
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
			void ClearAndAddAllBlankArguments();
			BoolResultWithErrorMessage AddCurrentArgument(string argument);
			int CurrentArgumentCount { get; }
			void RemoveCurrentArgument(int Index);
			ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair { get; }
		}

		public abstract class OverrideToStringClass : ICommandWithHandler
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
			public abstract ObservableCollection<string>[] PredefinedArgumentsList { get; }
			public virtual ObservableCollection<string> GetPredefinedArgumentsList(int Index, bool SuppressErrors = false)
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
			public virtual Dictionary<string, string> GetArgumentReplaceKeyValuePair(int Index, bool SuppressErrors = false)
			{
				if (Index < ArgumentsReplaceKeyValuePair.Length)
					return ArgumentsReplaceKeyValuePair[Index];
				else
				{
					if (!SuppressErrors) UserMessages.ShowWarningMessage("Index out of bounds for predefinedArgumentsList, " + this.CommandName + " command, index = " + Index);
					return new Dictionary<string, string>();
				}
			}
			public abstract string[] ArgumentDescriptions { get; }
			public virtual void ClearAndAddAllBlankArguments()
			{
				CurrentArgumentsPair.Clear();
				foreach (string argdesc in ArgumentDescriptions)
					CurrentArgumentsPair.AddWithoutValidation(new KeyAndValuePair("", argdesc));
			}
			//public abstract BoolResultWithErrorMessage AddCurrentArgument(string argument);
			public virtual BoolResultWithErrorMessage AddCurrentArgument(string argument)
			{
				string argumentDescription = "";
				if (ArgumentDescriptions.Length >= currentArgumentsPair.Count + 1)
					argumentDescription = ArgumentDescriptions[currentArgumentsPair.Count];
				return currentArgumentsPair.Add(new KeyAndValuePair(argument, argumentDescription));
			}

			public virtual int CurrentArgumentCount
			{
				get { return CurrentArgumentsPair.Count; }
			}

			public virtual void RemoveCurrentArgument(int Index)
			{
				currentArgumentsPair.RemoveAt(Index);
			}

			//public abstract BoolResultWithErrorMessage ValidationFunction(KeyAndValuePair argumentToAdd);
			public virtual BoolResultWithErrorMessage ValidationFunction(KeyAndValuePair argumentToAdd)
			{
				string errorMessage;
				bool success = PreValidateArgument(out errorMessage, CurrentArgumentsPair.Count, argumentToAdd.Key);
				return new BoolResultWithErrorMessage(success, errorMessage);
			}
			private ObservableCollectionWithValidationOnAdd<KeyAndValuePair> currentArgumentsPair;
			public virtual ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair
			{
				get
				{
					if (currentArgumentsPair == null) currentArgumentsPair = new ObservableCollectionWithValidationOnAdd<KeyAndValuePair>(ValidationFunction);
					return currentArgumentsPair;
				}
				set { currentArgumentsPair = value; }
			}
			//public abstract int CurrentArgumentCount { get; }
			//public abstract void RemoveCurrentArgument(int Index);
			//public abstract ObservableCollectionWithValidationOnAdd<KeyAndValuePair> CurrentArgumentsPair { get; set; }
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

			private string[] argumentDescriptions = new string[]
			{
				"parameter"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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

			private string[] argumentDescriptions = new string[]
			{
				"search phrase"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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

			private string[] argumentDescriptions = new string[]
			{
				"folder"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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

			private string[] argumentDescriptions = new string[]
			{
				"minutes",
				"autosnooze",
				"name",
				"description"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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

			private string[] argumentDescriptions = new string[]
			{
				"to address",
				"subject",
				"body"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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

			private string[] argumentDescriptions = new string[]
			{
				"url"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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
					TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, NameAndNumberDictionary[arguments[0]]);
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

			private string[] argumentDescriptions = new string[]
			{
				"name"
			};
			public override string[] ArgumentDescriptions
			{
				get { return argumentDescriptions; }
			}
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
							TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Process killed: " + processName);
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

			private string[] argumentDescriptions = new string[]
			{
				"process name"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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
				else if (Index == 1 && (new string[] { "open", "getall" }).Contains(CurrentArgumentsPair[0].Key))
					errorMessage = "No additional arguments allowed for '" + CurrentArgumentsPair[0].Key + "'";
				else if (Index == 1 && (new string[] { "comment", "uncomment" }).Contains(CurrentArgumentsPair[0].Key) && !CanParseToInt(argumentValue))
					errorMessage = "Second argument of Startup bat command (" + CurrentArgumentsPair[0].Key + ") must be a valid integer";
				else if (Index == 1 && "getline" == CurrentArgumentsPair[0].Key && string.IsNullOrWhiteSpace(argumentValue))
					errorMessage = "Second argument of Startup bat command (" + CurrentArgumentsPair[0].Key + ") may not be null/empty/whitespaces";
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
				else if (!PreValidateArgument(out errorMessage, 1, arguments[1]))
					errorMessage = errorMessage + "";
				else return true;
				return false;
			}

			public override bool PerformCommand(out string errorMessage, TextFeedbackEventHandler textFeedbackEvent = null, ProgressChangedEventHandler progressChangedEvent = null, params string[] arguments)
			{
				try
				{
					string filePath = @"C:\Francois\Other\Startup\work Startup.bat";
					StartupbatInterop.PerformStartupbatCommand(filePath, arguments[0] + " " + arguments[1], textFeedbackEvent);
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

			private string[] argumentDescriptions = new string[]
			{
				"sub-command",
				"parameter",
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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
					WindowsInterop.StartCommandPromptOrVScommandPrompt(arguments[0], false, textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot open Cmd: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private string[] argumentDescriptions = new string[]
			{
				"folder"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
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
					WindowsInterop.StartCommandPromptOrVScommandPrompt(arguments[0], true, textFeedbackEvent);
					errorMessage = "";
					return true;
				}
				catch (Exception exc)
				{
					errorMessage = "Cannot open VsCmd: " + arguments[0] + Environment.NewLine + exc.Message;
					return false;
				}
			}

			private string[] argumentDescriptions = new string[]
			{
				"folder"
			};
			public override string[] ArgumentDescriptions { get { return argumentDescriptions; } }
		}

		public class BtwCommand : OverrideToStringClass
		{
			public override string CommandName { get { return "btw"; } }
			public override string DisplayName { get { return "Btwtodo"; } }
			public override string Description { get { return "Add btw (by the way) item on firepuma"; } }
			public override string ArgumentsExample { get { return "Steve Jobs was friends with Bill Gates"; } }

			private readonly ObservableCollection<string>[] predefinedArgumentsList =
			{
				//new ObservableCollection<string>() { "5", "30", "60" },
				//new ObservableCollection<string>() { "15", "30", "60" },
				//new ObservableCollection<string>() { "Shop" }
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
					PhpInterop.AddBtwTextFirepuma(arguments[0], textFeedbackEvent);
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

			private string[] argumentDescriptions = new string[]
			{
				"btw text"
			};
			public override string[] ArgumentDescriptions
			{
				get { return argumentDescriptions; }
			}
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
						 arguments[1],//+ ";" + arguments[2]; still need to add here for description
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

			private string[] argumentDescriptions = new string[]
			{
				"sub-command",
				"Folder/Path",
				//"Description",
			};
			public override string[] ArgumentDescriptions
			{
				get { return argumentDescriptions; }
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
						VisualStudioInterop.PerformPublish(arguments[1], out tmpNoUseVersionStr, textFeedbackEvent: textFeedbackEvent);
					}
					else if (arguments[0] == "onlinevs")
					{
						VisualStudioInterop.PerformPublishOnline(
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

			private string[] argumentDescriptions = new string[]
			{
				"sub-command",
				"Folder/Path",
				//"Description",
			};
			public override string[] ArgumentDescriptions
			{
				get { return argumentDescriptions; }
			}
		}

		//TODO: This platform (using interface) is already working fine, should build on on it and add all commands
		public static bool PerformCommand(ICommandWithHandler command, TextFeedbackEventHandler textfeedbackEvent, ProgressChangedEventHandler progresschangedEvent, params string[] arguments)
		{
			string errorMsg;
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackEvent, "Attempting to perform command: " + command.DisplayName + " (" + command.Description + ")");
			if (!command.ValidateArguments(out errorMsg, arguments)
				&& UserMessages.ShowWarningMessage("Invalid command arguments: " + errorMsg))
				return false;
			if (!command.PerformCommand(out errorMsg, textfeedbackEvent, progresschangedEvent, arguments)
				&& UserMessages.ShowWarningMessage("Cannot perform command: " + errorMsg))
				return false;
			TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackEvent, "Successfully performed command: " + command.DisplayName + " (" + command.Description + ")");
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
			foreach (KeyAndValuePair keyvaluePair in command.CurrentArgumentsPair)
				tmpList.Add(keyvaluePair.Key);
			return PerformCommand(command, textfeedbackEvent, progressChangedEvent, tmpList.ToArray());
		}
	}
}