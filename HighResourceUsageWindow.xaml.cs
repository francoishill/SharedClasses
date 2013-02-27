using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for HighResourceUsageWindow.xaml
	/// </summary>
	public partial class HighResourceUsageWindow : Window
	{
		public enum ReturnResult { ForceCloseNow, IgnoreOnce, IgnoreUntilClose };
		private Dictionary<HighResourceUsageWindow, string> _listOfThisAppOpenWindowsWithWorkspaceFiles = new Dictionary<HighResourceUsageWindow, string>();
		private ReturnResult _dialogResult = ReturnResult.IgnoreOnce;

		public HighResourceUsageWindow(string messageText, string messageTitle)
		{
			InitializeComponent();

			textblockTitle.Text = messageTitle;
			textblockMessage.Text = messageText;

			GetWorkspaceAndPositionWindow();
		}

		~HighResourceUsageWindow()
		{
			RemoveSelfFromListAndDeleteWorkspaceFile();
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			this.UpdateLayout();
			WriteWorkspaceFile();
		}

		private void Window_Closed_1(object sender, EventArgs e)
		{
			RemoveSelfFromListAndDeleteWorkspaceFile();
		}

		private void RemoveSelfFromListAndDeleteWorkspaceFile()
		{
			if (_listOfThisAppOpenWindowsWithWorkspaceFiles.ContainsKey(this))
			{
				try
				{
					File.Delete(_listOfThisAppOpenWindowsWithWorkspaceFiles[this]);
				}
				catch { }
				_listOfThisAppOpenWindowsWithWorkspaceFiles.Remove(this);
			}
		}

		private void WriteWorkspaceFile()
		{
			string workspaceFile = SharedClasses.SettingsInterop.GetFullFilePathInLocalAppdata(
				string.Format("{0}_{1}.fjset", Process.GetCurrentProcess().Id, _listOfThisAppOpenWindowsWithWorkspaceFiles.Count + 1),
				"SharedClasses",
				"HighResourceUsageWindow\\Workspaces");
			File.WriteAllLines(
				workspaceFile,
				new string[]
				{ 
					this.Left.ToString(), 
					this.Top.ToString() 
				});
			_listOfThisAppOpenWindowsWithWorkspaceFiles.Add(this, workspaceFile);
		}

		private bool IsProcessRunning(int processID)
		{
			try
			{
				Process proc = Process.GetProcessById(processID);
				return proc != null;
			}
			catch
			{
				return false;
			}
		}

		private double GetWindowHeight() { this.UpdateLayout(); return this.Height; }
		private double GetWindowWidth() { this.UpdateLayout(); return this.Width; }

		private void GetWorkspaceAndPositionWindow()
		{
			var dirWithAllWorkspaces = SharedClasses.SettingsInterop.GetFullFolderPathInLocalAppdata(
				"Workspaces",
				"SharedClasses",
				"HighResourceUsageWindow");

			int tmpint;
			var processWorkspaceFilenames = Directory.GetFiles(dirWithAllWorkspaces, "*.fjset", SearchOption.TopDirectoryOnly)
				.Where(fp => Path.GetFileNameWithoutExtension(fp).Split('_').Length == 2)
				.Where(fp =>
					int.TryParse(Path.GetFileNameWithoutExtension(fp).Split('_')[0], out tmpint)//Process id
					&& int.TryParse(Path.GetFileNameWithoutExtension(fp).Split('_')[1], out tmpint));//Window number of process

			double minimumWorkspaceXofRunningProcs = SystemParameters.WorkArea.Right - GetWindowWidth();
			double minimumWorkspaceYofRunningProcs = SystemParameters.WorkArea.Bottom - 20;//The 20 is for KillWadiso6 button

			foreach (var filepath in processWorkspaceFilenames)
			{
				//File format (each line): x, y
				try
				{
					int processId = int.Parse(Path.GetFileNameWithoutExtension(filepath).Split('_')[0]);
					int windowNum = int.Parse(Path.GetFileNameWithoutExtension(filepath).Split('_')[1]);
					if (!IsProcessRunning(processId))
						File.Delete(filepath);
					else
					{
						var fileLines = File.ReadAllLines(filepath)
							.Where(fl => !string.IsNullOrWhiteSpace(fl))
							.ToList();
						//Assume file in correct format, otherwise should crash with exception which will be logged below
						double tmpx = double.Parse(fileLines[0]);
						double tmpy = double.Parse(fileLines[1]);
						if (tmpx < minimumWorkspaceXofRunningProcs)
							minimumWorkspaceXofRunningProcs = tmpx;
						if (tmpy < minimumWorkspaceYofRunningProcs)
							minimumWorkspaceYofRunningProcs = tmpy;

						fileLines.Clear();
						fileLines = null;
					}
				}
				catch (Exception exc)
				{
					SharedClasses.Logging.LogErrorToFile("Error in determining process workspaces: " + exc.Message,
						SharedClasses.Logging.ReportingFrequencies.Daily, "SharedClasses", "HighResourceUsageWindow");
				}
			}

			if (minimumWorkspaceYofRunningProcs - GetWindowHeight() >= 0)
			{
				this.Top = minimumWorkspaceYofRunningProcs - GetWindowHeight();
				this.Left = minimumWorkspaceXofRunningProcs;
			}
			else
			{
				this.Top = SystemParameters.WorkArea.Bottom - GetWindowHeight();
				this.Left = minimumWorkspaceXofRunningProcs - GetWindowWidth();
			}
		}

		private void buttonForceCloseNow_Click(object sender, RoutedEventArgs e)
		{
			this._dialogResult = ReturnResult.ForceCloseNow;
			this.Close();
		}

		private void buttonIgnoreOnce_Click(object sender, RoutedEventArgs e)
		{
			this._dialogResult = ReturnResult.IgnoreOnce;
			this.Close();
		}

		private void buttonIgnoreUntilClose_Click(object sender, RoutedEventArgs e)
		{
			this._dialogResult = ReturnResult.IgnoreUntilClose;
			this.Close();
		}

		public static ReturnResult ShowHighResourceUsageWindowReturnResult(string messageText, string messageTitle)
		{
			var tmpwin = new HighResourceUsageWindow(messageText, messageTitle);
			tmpwin.ShowDialog();
			return tmpwin._dialogResult;
		}

		private void textblockEditSettings_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var tmpwin = new HighResourceUsageSettingsWindow();
			tmpwin.ShowDialog();
		}
	}
}
