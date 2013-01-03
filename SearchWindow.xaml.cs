using System.Windows;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Threading;
using System.ComponentModel;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for SearchWindow.xaml
	/// </summary>
	public partial class SearchWindow : Window
	{
		ObservableCollection<FoundSearchItem> foundItems = new ObservableCollection<FoundSearchItem>();

		private string SearchText;
		private string fileText;
		private string RootDirectoryForSearching;
		private bool PauseActivationPasting = true;

		private string SearchButtonText_BeforeSearching;//= "_Search again";
		private const string SearchButtonText_WhileSearching = "C_ancel search";

		public SearchWindow(string textToSearchFor, string RootDirectoryForSearching)
		{
			InitializeComponent();

			this.SearchText = textToSearchFor;
			this.SearchButtonText_BeforeSearching = buttonSearchAgain.Content.ToString();
			this.RootDirectoryForSearching = RootDirectoryForSearching;

			textboxTextToSearchFor.Text = textToSearchFor;
			textblockRootDirectoryForSearching.Text = RootDirectoryForSearching;
		}

		private void SearchWindow1_Loaded(object sender, RoutedEventArgs e)
		{
			this.listboxFoundInFiles.ItemsSource = foundItems;
			this.listboxFoundFileExtensionsToBeIgnored.ItemsSource = FoundSearchItem.CurrentListOfExtensions;

			PerformSearch();
		}

		private bool hideTaskbarProgressOnNextActivation = false;
		private void ShowProgress(bool resetValueToZero = true)
		{
			hideTaskbarProgressOnNextActivation = false;
			TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
			progressBar.Value = 0;
			progressBar.Visibility = System.Windows.Visibility.Visible;
		}

		private void UpdateProgess(int progressPercentage)
		{
			Action updateAction = new Action(delegate
			{
				if (progressBar.Value != progressPercentage)
				{
					TaskbarManager.Instance.SetProgressValue(progressPercentage, 100);
					progressBar.Value = progressPercentage;
					//Application.DoEvents();
				}
			});
			this.Dispatcher.Invoke(updateAction);
		}

		private void HideProgress(bool resetValueToZero = true)
		{
			if (Win32Api.GetForegroundWindow() == this.GetHandle())
			{
				TaskbarManager.Instance.SetProgressValue(0, 100);
				TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
			}
			else
				hideTaskbarProgressOnNextActivation = true;
			progressBar.Value = 0;
			progressBar.Visibility = System.Windows.Visibility.Hidden;
		}

		private void UpdateProgressOfLoop(int loopVal, int loopMax)
		{
			UpdateProgess((int)Math.Truncate((double)100 * (double)loopVal++ / (double)loopMax));
		}

		private bool CancelSearch = false;
		private bool isBusySearching = false;
		private void PerformSearch()
		{
			CancelSearch = false;
			if (isBusySearching)
				return;
			isBusySearching = true;

			textblockRootDirectoryForSearching.IsEnabled = false;
			textboxTextToSearchFor.IsEnabled = false;
			//buttonSearchAgain.Enabled = false;
			buttonSearchAgain.Content = SearchButtonText_WhileSearching;
			//Application.DoEvents();

			foundItems.Clear();
			listboxFoundInFiles.Tag = null;
			textblockRootDirectoryForSearching.Text = RootDirectoryForSearching;
			textblockStatus.Text = string.Format(
				"Searching for \"{0}\" files in folder: {1}",
				textboxTextToSearchFor.Text,
				RootDirectoryForSearching);
			ShowProgress(true);

			listboxFoundInFiles.Tag = SearchText;
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				try
				{
					var files = Directory.GetFiles(RootDirectoryForSearching, "*", SearchOption.AllDirectories);
					int fileCount = files.Length;
					int totalDone = 0;
					foreach (string filepath in files)
					{
						if (CancelSearch)
							break;

						if (SettingsSimple.SearchInFilesSettings.Instance.ExcludeFileTypes.Contains(Path.GetExtension(filepath), StringComparer.InvariantCultureIgnoreCase))
							continue;
						if (Path.GetFileName(filepath).StartsWith("TemporaryGeneratedFile_", StringComparison.InvariantCultureIgnoreCase))
							continue;

						if (filepath.IndexOf(".svn", StringComparison.InvariantCultureIgnoreCase) != -1)
						{
							UpdateProgressOfLoop(totalDone++, fileCount);
							continue;
						}

						if (filepath.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) != -1)
							this.Dispatcher.Invoke((Action)delegate
							{
								AddNodeResultPath(filepath);
							});
						else
						{
							try
							{
								fileText = File.ReadAllText(filepath);
								if (fileText.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) != -1)
								{
									this.Dispatcher.Invoke((Action)delegate
									{
										AddNodeResultPath(filepath);
									});
								}
							}
							catch (Exception fileReadException)
							{
								UserMessages.ShowWarningMessage(string.Format("Error reading file: '{0}':{1}{2}", filepath, Environment.NewLine, fileReadException.Message));
							}
						}
						UpdateProgressOfLoop(totalDone++, fileCount);
					}
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Exception occurred: " + exc.Message
						+ Environment.NewLine + Environment.NewLine
						+ exc.StackTrace);
				}
				finally
				{
					Action afterSearchAction = new Action(delegate
					{
						textblockRootDirectoryForSearching.IsEnabled = true;
						textboxTextToSearchFor.IsEnabled = true;
						//buttonSearchAgain.Enabled = true;
						buttonSearchAgain.Content = SearchButtonText_BeforeSearching;
						//Application.DoEvents();
						HideProgress(true);
					});
					this.Dispatcher.Invoke(afterSearchAction);
					isBusySearching = false;
				}
			},
			false);
		}

		private void AddNodeResultPath(string path)
		{
			var displaytext = path;
			if (displaytext.StartsWith(textblockRootDirectoryForSearching.Text, StringComparison.InvariantCultureIgnoreCase))
				displaytext = ".." + displaytext.Substring(textblockRootDirectoryForSearching.Text.Length);
			FoundSearchItem fi = new FoundSearchItem(displaytext, path);
			foundItems.Add(fi);
			RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled();
		}

		private void textblockRootDirectoryForSearching_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			fbd.SelectedPath = RootDirectoryForSearching;
			fbd.Description = "Choose new root folder for searching...";
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				RootDirectoryForSearching = fbd.SelectedPath;
				textblockRootDirectoryForSearching.Text = RootDirectoryForSearching;
			}
		}

		private void textboxTextToSearchFor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			if (SearchText != textboxTextToSearchFor.Text)
				SearchText = textboxTextToSearchFor.Text;
		}

		private void buttonSearchAgain_Click(object sender, RoutedEventArgs e)
		{
			if (buttonSearchAgain.Content.ToString().Equals(SearchButtonText_BeforeSearching))
				PerformSearch();
			else
				CancelSearch = true;
		}

		private string lastClipboard = null;
		private void SearchWindow1_Activated(object sender, EventArgs e)
		{
			if (hideTaskbarProgressOnNextActivation)
			{
				hideTaskbarProgressOnNextActivation = false;
				HideProgress(true);
			}
			if (!PauseActivationPasting)
			{
				if (!isBusySearching)
				{
					var clipboardText = System.Windows.Forms.Clipboard.GetText();
					if (clipboardText != lastClipboard && !string.IsNullOrEmpty(clipboardText))
					{
						textboxTextToSearchFor.Text = clipboardText;
						SearchText = clipboardText;
						textboxTextToSearchFor.Focus();
						textblockStatus.Text = "Pasted text into search box: " + clipboardText;
					}
					lastClipboard = clipboardText;
				}
			}
		}

		private void SearchWindow1_Deactivated(object sender, EventArgs e)
		{
			PauseActivationPasting = false;
		}

		private void textboxTextToSearchFor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
				PerformSearch();
		}

		private void borderItemMain_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)//Double clicked
			{
				FrameworkElement fe = sender as FrameworkElement;
				if (fe == null) return;
				FoundSearchItem item = fe.DataContext as FoundSearchItem;
				if (item == null) return;
				if (File.Exists(item.FullFilepath))
					Process.Start("explorer", "/select,\"" + item.FullFilepath + "\"");
				else
					UserMessages.ShowWarningMessage("Cannot find file: " + item.FullFilepath);
			}
		}

		Dictionary<string, Paragraph> dictFullfilepathHighlightedParagraph = new Dictionary<string, Paragraph>();
		private void listboxFoundInFiles_SelectedItemChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			richTextBoxFileContents.Document.Blocks.Clear();
			richTextBoxFileContents.IsEnabled = false;
			buttonNextInFile.IsEnabled = false;

			if (e.AddedItems.Count != 1)//We have none or multiple selected
				return;

			FoundSearchItem item = e.AddedItems[0] as FoundSearchItem;
			if (item == null) return;

			richTextBoxFileContents.IsEnabled = true;
			buttonNextInFile.IsEnabled = true;

			string fullFilepath = item.FullFilepath;
			string textToSearchFor = textboxTextToSearchFor.Text;

			if (!File.Exists(fullFilepath))
				UserMessages.ShowWarningMessage("Cannot find file: " + fullFilepath);
			else
			{
				string fileContents;
				List<int> foundIndexesInFile = item.ObtainFoundIndexes(textToSearchFor, out fileContents);

				if (dictFullfilepathHighlightedParagraph.ContainsKey(fullFilepath))
					richTextBoxFileContents.Document.Blocks.Add(dictFullfilepathHighlightedParagraph[fullFilepath]);
				else
				{
					Paragraph tmppar = new Paragraph();
					if (foundIndexesInFile.Count == 0)
						tmppar.Inlines.Add(new Run(fileContents));
					else
					{
						if (foundIndexesInFile[0] > 0)//The first found item is not in character position == 0
							tmppar.Inlines.Add(new Run(fileContents.Substring(0, foundIndexesInFile[0]))
							{
								Foreground = Brushes.Black
							});

						for (int i = 0; i < foundIndexesInFile.Count; i++)
						{
							tmppar.Inlines.Add(new Run(fileContents.Substring(foundIndexesInFile[i], textToSearchFor.Length))
							{
								Foreground = Brushes.White,
								Background = Brushes.DarkBlue
							});

							int tmpstartpos = foundIndexesInFile[i] + textToSearchFor.Length;
							string textBetweenThisAndNextMatch =
								i == foundIndexesInFile.Count - 1
								? fileContents.Substring(tmpstartpos)//Need to give no endpoint as we use up to end if it is the last match 
								: fileContents.Substring(tmpstartpos, foundIndexesInFile[i + 1] - tmpstartpos);
							tmppar.Inlines.Add(new Run(textBetweenThisAndNextMatch)
							{
								Foreground = Brushes.Black
							});
						}

						dictFullfilepathHighlightedParagraph.Add(fullFilepath, tmppar);
						richTextBoxFileContents.Document.Blocks.Add(tmppar);
					}
				}

				//richTextBoxFileContents.Text = File.ReadAllText(filepath);
				//richTextBoxFileContents.LoadFile(fullFilepath, RichTextBoxStreamType.PlainText);
				//richTextBoxFileContents.Find(textBoxSearchText.Text, 0);
			}
		}

		private void SearchWindow1_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//listboxFoundFileExtensionsToBeIgnored.Width =
			//    (listboxFoundFileExtensionsToBeIgnored.Parent as FrameworkElement).ActualWidth - 200;
		}

		private void checkboxExtensionsEnabled_Checked(object sender, RoutedEventArgs e)
		{
			RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled();
		}

		private void checkboxExtensionsEnabled_Unchecked(object sender, RoutedEventArgs e)
		{
			RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled();
		}

		private void RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled()
		{
			var currentlyEnabledExtensions =
				FoundSearchItem.CurrentListOfExtensions
				.Where(ex => ex.IsEnabled)
				.Select(ex => ex.ExtensionString.ToLower())
				.OrderBy(ex => ex);

			foreach (var fi in foundItems)
				fi.CurrentVisibility =
					currentlyEnabledExtensions.Contains(Path.GetExtension(fi.FullFilepath).ToLower())
					? System.Windows.Visibility.Visible
					: System.Windows.Visibility.Collapsed;
		}

		private void enabledExtensionsSelectAll_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			foreach (var ext in FoundSearchItem.CurrentListOfExtensions)
				ext.IsEnabled = true;
			RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled();
		}

		private void enabledExtensionsSelectNone_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			foreach (var ext in FoundSearchItem.CurrentListOfExtensions)
				ext.IsEnabled = false;
			RefreshFoundItemsVisibilityBasedOnFileExtensionsEnabled();
		}
	}

	public class FoundSearchItem : INotifyPropertyChanged
	{
		public class FileExtension : INotifyPropertyChanged
		{
			public string ExtensionString { get; private set; }
			
			private bool _isenabled;
			public bool IsEnabled { get { return _isenabled; } set { _isenabled = value; OnPropertyChanged("IsEnabled"); } }

			public FileExtension(string ExtensionString, bool IsEnabled)
			{
				this.ExtensionString = ExtensionString;
				this.IsEnabled = IsEnabled;
			}

			public event PropertyChangedEventHandler PropertyChanged = delegate { };
			public void OnPropertyChanged(params string[] propertyNames) { foreach (var pn in propertyNames) PropertyChanged(this, new PropertyChangedEventArgs(pn)); }
		}

		public static ObservableCollection<FileExtension> CurrentListOfExtensions = new ObservableCollection<FileExtension>();
		private static ObservableCollection<FoundSearchItem> CurrentlyCreatedList = new ObservableCollection<FoundSearchItem>();

		public string DisplayFilepath { get; private set; }
		public string FullFilepath { get; private set; }
		private Visibility _currentvisibility;
		public Visibility CurrentVisibility { get { return _currentvisibility; } set { _currentvisibility = value; OnPropertyChanged("CurrentVisibility"); } }

		private string searchForTextUsedToObtainIndexes = null;
		private string fileMD5hashWhenObtainedIndexes = null;
		private List<int> alreadyFoundSearchIndexes;//Just for temporary storing

		public FoundSearchItem(string DisplayFilepath, string FullFilepath)
		{
			this.DisplayFilepath = DisplayFilepath;
			this.FullFilepath = FullFilepath;
			CurrentlyCreatedList.Add(this);
			RefreshCurrentListOfExtensions();
		}
		~FoundSearchItem()
		{
			RefreshCurrentListOfExtensions(this);
		}
		private static void RefreshCurrentListOfExtensions(FoundSearchItem itemToIgnore_IfBusyDisposing = null)
		{
			var allExistingExtensions =
				CurrentlyCreatedList
				   .Where(fi => fi != itemToIgnore_IfBusyDisposing)
				   .Select(fi => Path.GetExtension(fi.FullFilepath).ToLower())
				   .Distinct()
				   .OrderBy(s => s);

			Dispatcher.CurrentDispatcher.Invoke((Action)delegate
			{
						string[] defaultDisabledExtensions = new string[]
						{
							".baml",
							".cache",
							".config",
							".dll",
							".exe",
							".force",
							".ico",
							".lref",
							".manifest",
							".resx",
							".settings",
							".user"
						};

				CurrentListOfExtensions.Clear();
				foreach (var ext in allExistingExtensions)
					CurrentListOfExtensions.Add(new FileExtension(ext, !defaultDisabledExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase)));
			});
		}

		public List<int> ObtainFoundIndexes(string textToSearchFor, out string fileContents)
		{
			fileContents = File.ReadAllText(this.FullFilepath);//Just get this at the beginning of this method as it is an OUT parameter

			string fileMD5 = this.FullFilepath.FileToMD5Hash();
			//We double-check that the same 'textToSearchFor' is used and that the file did not change
			if (fileMD5.Equals(this.fileMD5hashWhenObtainedIndexes)
				&& textToSearchFor.Equals(this.searchForTextUsedToObtainIndexes)
				&& this.alreadyFoundSearchIndexes != null)
				return alreadyFoundSearchIndexes;

			this.searchForTextUsedToObtainIndexes = textToSearchFor;
			this.fileMD5hashWhenObtainedIndexes = fileMD5;
			this.alreadyFoundSearchIndexes = new List<int>();

			if (-1 == fileContents.IndexOf(textToSearchFor, StringComparison.InvariantCultureIgnoreCase))
				return this.alreadyFoundSearchIndexes;//Just return the empty list, this means no matches found

			int tmpStartindex = 0;
			int tmpIndex = fileContents.IndexOf(textToSearchFor, tmpStartindex, StringComparison.InvariantCultureIgnoreCase);
			while (-1 != tmpIndex)
			{
				this.alreadyFoundSearchIndexes.Add(tmpIndex);
				tmpStartindex = tmpIndex + 1;
				if (tmpStartindex >= fileContents.Length)
					break;
				tmpIndex = fileContents.IndexOf(textToSearchFor, tmpStartindex, StringComparison.InvariantCultureIgnoreCase);
			}

			return this.alreadyFoundSearchIndexes;
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(params string[] propertyNames) { foreach (var pn in propertyNames) PropertyChanged(this, new PropertyChangedEventArgs(pn)); }
	}
}