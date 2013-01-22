using System;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAPICodePack.Shell;
using System.Diagnostics;//Required reference to Microsoft.WindowsAPICodePack.Shell.dll

namespace SharedClasses
{
	public static class Windows7JumpListsInterop
	{
		public struct JumplistItem
		{
			public string ExePath;
			public string DisplayName;
			public string Arguments;
			public string IconPath;//The exe path may point to current app, but Icon path points to Icon for app

			public JumplistItem(string ExePath, string DisplayName, string Arguments = null, string IconPath = null)
			{
				this.ExePath = ExePath;
				this.DisplayName = DisplayName;
				this.Arguments = Arguments;
				this.IconPath = IconPath ?? ExePath;
			}
		}

		private static List<JumpListLink> GetStandardUserTasksItems()
		{
			List<JumpListLink> tmplist = new List<JumpListLink>();

			tmplist.Add(new JumpListLink(Environment.GetCommandLineArgs()[0], "Request new feature")
			{
				Arguments = StandardUserTasks.StandardUserTasks_RequestNewFeature.ToString(),
				IconReference = new IconReference("SHELL32.dll", 205)
			});

			tmplist.Add(new JumpListLink(Environment.GetCommandLineArgs()[0], "Report a bug")
			{
				Arguments = StandardUserTasks.StandardUserTasks_ReportABug.ToString(),
				IconReference = new IconReference("SHELL32.dll", 131)
			});

			return tmplist;
		}

		public static bool HandleCommandlineJumplistCommand()
		{
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1)//Element 0 is this app EXE
			{
				foreach (StandardUserTasks sut in Enum.GetValues(typeof(StandardUserTasks)))
					if (args[1].Equals(sut.ToString(), StringComparison.InvariantCultureIgnoreCase))
					{
						string thisAppname = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
						string appVersion = FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion;

						switch (sut)
						{
							case StandardUserTasks.StandardUserTasks_RequestNewFeature:
								string newFeatureDescription = InputBoxWPF.Prompt("Please enter a description for the new feature of " + thisAppname, "New feature request");
								if (newFeatureDescription != null)
									DeveloperCommunication.RunMailto("Requesting a new feature for " + thisAppname + " v" + appVersion, newFeatureDescription);
								Environment.Exit(0);
								return true;
							case StandardUserTasks.StandardUserTasks_ReportABug:
								string newBugDescription = InputBoxWPF.Prompt("Please enter a description of the bug in " + thisAppname, "Bug to be reported");
								if (newBugDescription != null)
									DeveloperCommunication.RunMailto("A bug has been found in " + thisAppname + " v" + appVersion, newBugDescription);
								Environment.Exit(0);
								break;
							default:
								UserMessages.ShowInfoMessage("Unknown JumpList StandardUserTask = '" + sut.ToString() + "', just continuing to run application normal");
								break;
						}
					}
			}
			return false;
		}

		private static bool wasStandardUserTasksAlreadyAdded = false;//To see if we actually set it from AutoUpdating, if we use this .cs file in another independant app, we don't necessarily want to add these items
		private enum StandardUserTasks { StandardUserTasks_RequestNewFeature, StandardUserTasks_ReportABug };
		public static void AddStandardUserTasksItems(JumpList jumplist)
		{
			wasStandardUserTasksAlreadyAdded = true;
			jumplist.AddUserTasks(GetStandardUserTasksItems().ToArray());
			jumplist.Refresh();
		}

		public static JumpList RepopulateAndRefreshJumpList(List<KeyValuePair<string, IEnumerable<JumplistItem>>> jumpListGroupsWithItems)
		{
			var _jumpList = JumpList.CreateJumpList();
			//_jumpList.ClearAllUserTasks();

			foreach (var groupWithItems in jumpListGroupsWithItems)
			{
				JumpListCustomCategory userActionsCategory = new JumpListCustomCategory(groupWithItems.Key);

				//string thisAssemblyFullPath = Assembly.GetEntryAssembly().Location;
				foreach (var item in groupWithItems.Value)
				{
					string exePath = Environment.GetCommandLineArgs()[0];// item.ExePath;
					if (Path.GetFileName(exePath).IndexOf(".vshost.") != -1)
						exePath = item.ExePath;//We run the command via our app unless we are running in Debug mode, then we run it through the relevant app (like if it is chrome.exe)
					JumpListLink tmpAction = new JumpListLink(exePath, item.DisplayName);

					tmpAction.WorkingDirectory = Path.GetDirectoryName(item.ExePath);
					//Our exe path is this application, then the arguments are the FULL commandline of the app/file/folder we have a shortcut for
					tmpAction.Arguments = "\"" + item.ExePath.Trim('"', '\'') + "\"";
					if (item.Arguments != null)
						tmpAction.Arguments += " " + item.Arguments;

					if (File.Exists(item.ExePath))
						tmpAction.IconReference = new Microsoft.WindowsAPICodePack.Shell.IconReference(item.IconPath, 0);
					else if (Directory.Exists(item.ExePath))
						tmpAction.IconReference = new Microsoft.WindowsAPICodePack.Shell.IconReference("SHELL32.dll", 3);//, 4);
					userActionsCategory.AddJumpListItems(tmpAction);
				}

				if (groupWithItems.Value.Count() > _jumpList.MaxSlotsInList)
					UserMessages.ShowWarningMessage(
						string.Format("The taskbar jumplist has {0} maximum slots but the list is {1}, the extra items will be truncated", _jumpList.MaxSlotsInList, groupWithItems.Value.Count()));

				_jumpList.AddCustomCategories(userActionsCategory);

			}

			//Add the standard items again if they were already added, for example they are added via AutoUpdating.cs
			if (wasStandardUserTasksAlreadyAdded)
				AddStandardUserTasksItems(_jumpList);

			_jumpList.Refresh();

			return _jumpList;
		}
	}
}