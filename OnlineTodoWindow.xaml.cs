using System;
using System.Windows;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;

namespace SharedClasses
{
	public partial class OnlineTodoWindow : Window
	{
		TextFeedbackEventHandler textFeedbackEvent;
		TextFeedbackEventHandler updateStatusEvent;
		ObservableCollection<TodoItem> todoList = new ObservableCollection<TodoItem>();

		public OnlineTodoWindow()
		{
			InitializeComponent();

			textFeedbackEvent += (s, e) => { appendLogTextbox(e.FeedbackText); };
			updateStatusEvent += (s, e) => { SetStatus(e.FeedbackText); };
		}

		private void OnlineTodoWindow1_Loaded(object sender, RoutedEventArgs e)
		{
			GetCurrentTodolist();
		}

		private void GetCurrentTodolist()
		{
			todoList.Clear();
			string tmpresult = TodoItem.GetResultOfPerformingDesktopAppDoTask(textFeedbackEvent, updateStatusEvent, PhpInterop.Username, "getlist", new List<string>());
			if (tmpresult != null)
			{
				appendLogTextbox("Successfully obtained todo list");
				todoList = null;
				todoList = new ObservableCollection<TodoItem>(tmpresult.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
					.Where(line => line.Trim('\t', '\n', '\r', ' ', '\0').Length > 0)
					.Select(line => TodoItem.CreateFromTabseparatedLine(line, textFeedbackEvent, updateStatusEvent)));
			}
			listBoxTodoList.ItemsSource = todoList;
			//treeviewTodoList.ItemsSource = todoList;
		}

		private void SetStatus(string s)
		{
			Console.WriteLine("Status: " + s);
		}

		private void appendLogTextbox(string p)
		{
			Console.WriteLine("Appended message: " + p);
		}

		private void listBoxTodoList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
				listBoxTodoList.SelectedIndex = -1;
		}
	}

	public class TodoItem
	{
		private const string MySQLdateformat = "yyyy-MM-dd HH:mm:ss";
		private static DateTime mindate = new DateTime(1800, 1, 1, 0, 0, 0);

		private TextFeedbackEventHandler textFeedbackEventHandler;
		private TextFeedbackEventHandler updateStatusEventHandler;

		public string Category { get; set; }
		public string SubCategory { get; set; }
		public string Items { get; set; }
		public string Description { get; set; }
		public bool Completed { get; set; }
		public DateTime Due { get; set; }
		public DateTime Created { get; set; }
		public int RemindedCount { get; set; }
		private bool _stopsnooze;
		public bool StopSnooze { get { return _stopsnooze; } set { _stopsnooze = value; SetStopSnoozeOnline(value); } }

		public int AutosnoozeInterval { get; set; }

		private static bool IsInitialSet = false;
		public TodoItem(string Category, string SubCategory, string Items, string Description, bool Completed, DateTime Due, DateTime Created, int RemindedCount, bool StopSnooze, int AutosnoozeInterval, TextFeedbackEventHandler textFeedbackEventHandler, TextFeedbackEventHandler updateStatusEventHandler)
		{
			IsInitialSet = true;
			this.textFeedbackEventHandler = textFeedbackEventHandler ?? new TextFeedbackEventHandler(delegate { });
			this.updateStatusEventHandler = updateStatusEventHandler ?? new TextFeedbackEventHandler(delegate { });
			this.Category = Category;
			this.SubCategory = SubCategory;
			this.Items = Items;
			this.Description = Description;
			this.Completed = Completed;
			this.Due = Due;
			this.Created = Created;
			this.RemindedCount = RemindedCount;
			this.StopSnooze = StopSnooze;
			this.AutosnoozeInterval = AutosnoozeInterval;
			IsInitialSet = false;
		}

		private bool SetStopSnoozeOnline(bool newValue)
		{
			bool oldval = this.StopSnooze;
			bool result = PerformDesktopAppDoTask(
				this.textFeedbackEventHandler,
				this.updateStatusEventHandler,
				PhpInterop.Username,
				"updatestopsnooze",
				new List<string>() { this.Category, this.SubCategory, this.Items, this.StopSnooze ? "1" : "0" },
				true,
				"1");
			if (!result)
				StopSnooze = oldval;
			return result;
		}

		private static bool PerformDesktopAppDoTask(TextFeedbackEventHandler textFeedbackEventHandler, TextFeedbackEventHandler updateStatusEventHandler, string UsernameIn, string TaskName, List<string> ArgumentList, bool CheckForSpecificResult = false, string SuccessSpecificResult = "", bool MustWriteResultToLogsTextbox = false)
		{
			if (IsInitialSet)
				return true;
			if (textFeedbackEventHandler == null)
				textFeedbackEventHandler = new TextFeedbackEventHandler(delegate { });
			if (updateStatusEventHandler == null)
				updateStatusEventHandler = new TextFeedbackEventHandler(delegate { });

			string result = GetResultOfPerformingDesktopAppDoTask(textFeedbackEventHandler, updateStatusEventHandler, UsernameIn, TaskName, ArgumentList, MustWriteResultToLogsTextbox);
			if (CheckForSpecificResult && result == SuccessSpecificResult)
				return true;
			return false;
		}

		public static string GetResultOfPerformingDesktopAppDoTask(TextFeedbackEventHandler textFeedbackEventHandler, TextFeedbackEventHandler updateStatusEventHandler, string UsernameIn, string TaskName, List<string> ArgumentList, bool MustWriteResultToLogsTextbox = false)
		{
			string tmpkey = GetPrivateKey(textFeedbackEventHandler, updateStatusEventHandler);

			if (textFeedbackEventHandler == null)
				textFeedbackEventHandler = new TextFeedbackEventHandler(delegate { });
			if (updateStatusEventHandler == null)
				updateStatusEventHandler = new TextFeedbackEventHandler(delegate { });

			if (tmpkey != null)
			{
				HttpWebRequest addrequest = null;
				HttpWebResponse addresponse = null;
				StreamReader input = null;

				try
				{
					if (UsernameIn != null && UsernameIn.Length > 0
																 && tmpkey != null && tmpkey.Length > 0)
					{
						string encryptedstring;
						string decryptedstring = "";
						bool mustreturn = false;
						ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
						{
							string ArgumentListTabSeperated = "";
							foreach (string s in ArgumentList)
								ArgumentListTabSeperated += (ArgumentListTabSeperated.Length > 0 ? "\t" : "") + s;

							addrequest = (HttpWebRequest)WebRequest.Create(PhpInterop.doWorkAddress + "/dotask/" +
									PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(UsernameIn, "123456789abcdefghijklmno") + "/" +
									PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(TaskName, tmpkey) + "/" +
									PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(ArgumentListTabSeperated, tmpkey));// + "/");
							//appendLogTextbox(addrequest.RequestUri.ToString());
							try
							{
								addresponse = (HttpWebResponse)addrequest.GetResponse();
								input = new StreamReader(addresponse.GetResponseStream());
								encryptedstring = input.ReadToEnd();
								//appendLogTextbox("Encrypted response: " + encryptedstring);

								decryptedstring = PhpInterop.PhpEncryption.SimpleTripleDesDecrypt(encryptedstring, tmpkey);
								//appendLogTextbox("Decrypted response: " + decryptedstring);
								decryptedstring = decryptedstring.Replace("\0", "").Trim();
								//MessageBox.Show(this, decryptedstring);
								if (MustWriteResultToLogsTextbox)
									textFeedbackEventHandler(null, new TextFeedbackEventArgs("Result for " + TaskName + ": " + decryptedstring));
								mustreturn = true;
							}
							catch (Exception exc) { UserMessages.ShowErrorMessage("Exception:" + exc.Message, "Exception"); }
						});
						if (mustreturn) return decryptedstring;
					}
				}
				catch (Exception exc)
				{
					textFeedbackEventHandler(null, new TextFeedbackEventArgs("Obtain php: " + exc.Message));
				}
				finally
				{
					if (addresponse != null) addresponse.Close();
					if (input != null) input.Close();
				}
			}
			return null;
		}

		private static string GetPrivateKey(TextFeedbackEventHandler textFeedbackEventHandler, TextFeedbackEventHandler updateStatusEventHandler)
		{
			if (textFeedbackEventHandler == null)
				textFeedbackEventHandler = new TextFeedbackEventHandler(delegate { });
			if (updateStatusEventHandler == null)
				updateStatusEventHandler = new TextFeedbackEventHandler(delegate { });

			try
			{
				updateStatusEventHandler(null, new TextFeedbackEventArgs("Obtaining pvt key..."));
				string tmpkey = null;

				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					tmpkey = PhpInterop.PostPHP(textFeedbackEventHandler, PhpInterop.ServerAddress + "/generateprivatekey.php", "username=" + PhpInterop.Username + "&password=" + PhpInterop.Password);
				});

				string tmpSuccessKeyString = "Success: Key=";
				if (tmpkey != null && tmpkey.Length > 0 && tmpkey.ToUpper().StartsWith(tmpSuccessKeyString.ToUpper()))
				{
					tmpkey = tmpkey.Substring(tmpSuccessKeyString.Length).Replace("\n", "").Replace("\r", "");
					updateStatusEventHandler(null, new TextFeedbackEventArgs(tmpkey));
				}
				return tmpkey;
			}
			catch (Exception exc)
			{
				textFeedbackEventHandler(null, new TextFeedbackEventArgs("Obtain private key exception: " + exc.Message));
				return null;
			}
		}

		public static TodoItem CreateFromTabseparatedLine(string line, TextFeedbackEventHandler textFeedbackEventHandler, TextFeedbackEventHandler updateStatusEventHandler)
		{
			if (line.Contains("\t") && line.Split('\t').Length >= 4)
			{
				string tmpCategory = line.Split('\t')[0];
				string tmpSubcat = line.Split('\t')[1];
				string tmpItems = line.Split('\t')[2];
				string tmpDescription = line.Split('\t')[3];
				bool tmpCompleted = line.Split('\t')[4] == "1";
				DateTime tmpDue = line.Split('\t')[5].Length > 0 ? DateTime.ParseExact(line.Split('\t')[5], MySQLdateformat, CultureInfo.InvariantCulture) : mindate;
				DateTime tmpCreated = line.Split('\t')[6].Length > 0 ? DateTime.ParseExact(line.Split('\t')[6], MySQLdateformat, CultureInfo.InvariantCulture) : mindate;
				int tmpRemindedCount = Convert.ToInt32(line.Split('\t')[7]);
				bool tmpStopSnooze = line.Split('\t')[8] == "1";
				int tmpAutosnoozeInterval = Convert.ToInt32(line.Split('\t')[9]);
				return new TodoItem(tmpCategory, tmpSubcat, tmpItems, tmpDescription, tmpCompleted, tmpDue, tmpCreated, tmpRemindedCount, tmpStopSnooze, tmpAutosnoozeInterval, textFeedbackEventHandler, updateStatusEventHandler);
			}
			else UserMessages.ShowWarningMessage("The following line is invalid todo line: " + line);
			return null;
		}
	}
}