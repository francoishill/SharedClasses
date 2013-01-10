using System;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using System.Collections.Generic;
using System.IO;//Required reference to Microsoft.WindowsAPICodePack.Shell.dll
using System.Linq;

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
			_jumpList.Refresh();

			return _jumpList;
		}
	}
}