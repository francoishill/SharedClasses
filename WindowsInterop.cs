using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
//using System.Windows.Interop;//Found in PresentationCore, also add PresentationFramework (for the Window class)
using Shell32;//Requires COM assembly: Microsoft Shell Control And Automation

namespace SharedClasses
{
	public class WindowsInterop
	{
		public static readonly string LocalAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		public static readonly string MydocsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		public static bool GetShortcutTargetFile(string shortcutFilename, out string outFilePathOrError, out string outArguments, out string iconPath)
		{
			try
			{
				string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
				string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

				//Requires the following DLL from the COM references
				//Microsoft Shell Control And Automation
				Shell shell = new Shell();
				Folder folder = shell.NameSpace(pathOnly);
				FolderItem folderItem = folder.ParseName(filenameOnly);
				if (folderItem != null)
				{
					Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
					if (link.GetIconLocation(out iconPath) != 0)//0 means it succeeded
						iconPath = null;
					outFilePathOrError = link.Path;
					outArguments = link.Arguments;
					return true;
				}
				outFilePathOrError = "Unable to parse to shortcut file";
				outArguments = null;
				iconPath = null;
				return false;
			}
			catch (Exception exc)
			{
				outFilePathOrError = exc.Message;
				outArguments = null;
				iconPath = null;
				return false;
			}
		}

		public static void StartCommandPromptOrVScommandPrompt(Object textfeedbackSenderObject, string cmdpath, bool VisualStudioMode, TextFeedbackEventHandler textFeedbackEvent = null)
		{
			const string vsbatfile = @"c:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat";

			string processArgs = 
								VisualStudioMode ? @"/k """ + vsbatfile + @""" x86"
				: "";

			if (Directory.Exists(cmdpath))
			{
				if (!VisualStudioMode || (VisualStudioMode && File.Exists(vsbatfile)))
				{
					Process proc = new Process();
					proc.StartInfo = new ProcessStartInfo("cmd", processArgs);
					proc.StartInfo.WorkingDirectory = cmdpath;
					proc.Start();
				}
				else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, @"Unable to start Visual Studio Command Prompt, cannot find file: """ + vsbatfile + @"""" + cmdpath);
			}
			else TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textfeedbackSenderObject, textFeedbackEvent, "Folder does not exist, cannot start cmd: " + cmdpath);
		}

		public static void ShowAndActivateForm(Form form)
		{
			form.Visible = true;
			form.Activate();
			form.WindowState = FormWindowState.Normal;
		}

		[Obsolete("This method was moved to WPFHelper", true)]
		public static void ShowAndActivateWindow(dynamic window)
		{
			/*System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(window);
			//window.Visibility = Visibility.Visible;
			window.Show();
			window.UpdateLayout();
			if (window.WindowState != WindowState.Normal) window.WindowState = WindowState.Normal;
			window.Activate();*/
		}

		[Obsolete("This method was moved to WPFHelper", true)]
		public static IntPtr GetWindowHandle(dynamic window) { return IntPtr.Zero; } //WindowInteropHelper(window).Handle; }

		[Obsolete("This method was moved to WPFHelper", true)]
		public static void SetHookForSystemMenu(dynamic window, dynamic wndProc, List<dynamic> MenuItemList)
		{
			/*if (
				window == null || GetWindowHandle(window) == IntPtr.Zero
				|| wndProc == null
				|| MenuItemList == null || MenuItemList.Count == 0)
				return;

			/// Get the Handle for the Forms System Menu
			IntPtr systemMenuHandle = GetSystemMenu(GetWindowHandle(window), false);

			int counter = 5;
			foreach (SystemMenuItem menuitem in MenuItemList)
			{
				switch (menuitem.SystemMenuItemType)
				{
					case SystemMenuItemTypeEnum.Separator:
						InsertMenu(systemMenuHandle, counter++, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty);
						break;
					case SystemMenuItemTypeEnum.String:
						InsertMenu(systemMenuHandle, counter++, MF_BYPOSITION, menuitem.wParamForItem, menuitem.DisplayText);
						break;
					//default:
					//	break;
				}
			}

			// Attach our WndProc handler to this Window
			HwndSource source = HwndSource.FromHwnd(GetWindowHandle(window));
			source.AddHook(wndProc);*/
		}
	}

	[Obsolete("This enum was moved to WPFHelper", true)]
	public enum SystemMenuItemTypeEnum { Separator, String }
	[Obsolete("This class was moved to WPFHelper", true)]
	public class SystemMenuItem { }

	[Obsolete("This class was moved to WPFHelper", true)]
	public class WindowBehavior { }
}