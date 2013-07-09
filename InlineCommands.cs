﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

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
			PublishVs,
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

		public void PerformCommand(string fullCommandText, ComboBox textboxtoClearOnSuccess, TextBox messagesTextbox)
		{
			string TextboxTextIn = fullCommandText;//textboxtoClearOnSuccess.Text;
			string argStr = TextboxTextIn.Substring(TextboxTextIn.IndexOf(' ') + 1);

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
							catch (Exception exc) { Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, exc.Message); }
						}
					}
					break;

				case PerformCommandTypeEnum.AddTodoitemFirepuma:
					PhpInterop.AddTodoItemFirepuma(
						messagesTextbox,
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
					 Convert.ToInt32(argStr.Split(';')[1]));
					break;

				case PerformCommandTypeEnum.CreateNewOutlookMessage:
					MicrosoftOfficeInterop.CreateNewOutlookMessage(
						messagesTextbox,
								argStr.Split(';')[0],
								argStr.Split(';')[1],
								argStr.Split(';').Length >= 3 ? argStr.Split(';')[2] : "");
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
					if (processes.Length > 1) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "More than one process found, cannot kill");
					else if (processes.Length == 0) Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Cannot find process with name " + processName);
					else
					{
						processes[0].Kill();
						Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Process killed: " + processName);
					}
					break;

				case PerformCommandTypeEnum.StartupBat:
					string filePath = @"C:\Francois\Other\Startup\work Startup.bat";
					string comm = argStr;
					//getall/getline 'xxx'/comment #/uncomment #
					StartupbatInterop.PerformStartupbatCommand(messagesTextbox, filePath, comm);
					break;

				case PerformCommandTypeEnum.Call:
					if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(argStr))
						Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, commandArguments[0].TokenWithReplaceStringPair[argStr] ?? argStr);
					else Logging.appendLogTextbox_OfPassedTextbox(messagesTextbox, "Call command not recognized: " + argStr);
					break;

				case PerformCommandTypeEnum.Cmd:
				case PerformCommandTypeEnum.VsCmd:
					string cmdpath = argStr;
					if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(cmdpath))
						cmdpath = commandArguments[0].TokenWithReplaceStringPair[cmdpath] ?? cmdpath;

					WindowsInterop.StartCommandPromptOrVScommandPrompt(messagesTextbox, cmdpath, PerformCommandType == PerformCommandTypeEnum.VsCmd);
					break;

				case PerformCommandTypeEnum.Btw:
					if (PhpInterop.AddBtwTextFirepuma(messagesTextbox, argStr))
						textboxtoClearOnSuccess.Text = "";
					break;

				case PerformCommandTypeEnum.Svncommit:
				case PerformCommandTypeEnum.Svnupdate:
				case PerformCommandTypeEnum.Svnstatus:
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
						: SvnInterop.SvnCommand.Status;

					string svnprojname = svnargs.Contains(ArgumentSeparator) ? svnargs.Split(ArgumentSeparator)[0] : svnargs;
					string svnlogmessage = svnargs.Contains(ArgumentSeparator) ? svnargs.Split(ArgumentSeparator)[1] : "";
					if (commandArguments[0].TokenWithReplaceStringPair != null && commandArguments[0].TokenWithReplaceStringPair.ContainsKey(svnprojname))
						svnargs = (commandArguments[0].TokenWithReplaceStringPair[svnprojname] ?? svnprojname) + (svnargs.Contains(ArgumentSeparator) ? ArgumentSeparator + svnlogmessage : "");
					SvnInterop.PerformSvn(messagesTextbox, svnargs, svnCommand);
					//}
					//else appendLogTextbox_OfPassedTextbox(messagesTextbox, "Error: No semicolon. Command syntax is 'svncommit proj/othercommand projname;logmessage'");
					//}
					break;

				case PerformCommandTypeEnum.PublishVs:
					VisualStudioInterop.PerformPublish(messagesTextbox, argStr);
					break;

				case PerformCommandTypeEnum.Undefined:
					MessageBox.Show("PerformCommandType is not defined");
					break;
				default:
					MessageBox.Show("PerformCommandType is not incorporated yet: " + PerformCommandType.ToString());
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