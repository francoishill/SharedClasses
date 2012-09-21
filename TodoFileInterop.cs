using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace SharedClasses
{
	public class TodoFile : INotifyPropertyChanged
	{
		private static Timer saveFilesTimer = null;
		private static List<TodoFile> createdTodoFiles = new List<TodoFile>();
		private static TimeSpan tickIntervalToSaveFiles = TimeSpan.FromSeconds(5);
		private static TimeSpan minDurationToSaveAfterLastModified = TimeSpan.FromSeconds(5);

		private const string dataformatFileExtension = "yyyyMMddHHmmssfff";
		private bool IgnoreTodolineUpdate = false;

		private readonly DateTime creationDate = DateTime.Now;
		public string FileName { get { return Path.GetFileNameWithoutExtension(FullFilePath); } }
		private string _filecontent;
		public string FileContent
		{
			get
			{
				if (_filecontent == null)
				{
					if (!File.Exists(FullFilePath)) return "";
					_filecontent = File.ReadAllText(FullFilePath);
				}
				return _filecontent;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value)) return;
				_filecontent = value;
				HasUnsavedChanges = true;
				if (!IgnoreTodolineUpdate)
					UpdateTodoLines();
				UpdateLastModified();
				OnPropertyChanged("FileContent");
			}
		}

		public ObservableCollection<TodoLine> TodoLines { get; set; }
		public string FullFilePath;
		private bool _hasunsavedchanges;
		public bool HasUnsavedChanges { get { return _hasunsavedchanges; } private set { _hasunsavedchanges = value; OnPropertyChanged("HasUnsavedChanges"); } }
		private DateTime _lastmodified;
		public DateTime LastModified { get { return _lastmodified; } private set { _lastmodified = value; OnPropertyChanged("LastChange"); } }
		public bool HasDueItems { get { foreach (TodoLine tl in TodoLines) if (tl.IsDue) return true; return false; } }

		private void UpdateLastModified()
		{
			LastModified = DateTime.Now;
		}

		private static void EnsureSaveFilesTimerStarted()
		{
			if (saveFilesTimer == null)
				saveFilesTimer = new System.Threading.Timer(
				delegate
				{
					foreach (TodoFile tf in createdTodoFiles)
						if (tf.HasUnsavedChanges && DateTime.Now.Subtract(tf.LastModified) >= minDurationToSaveAfterLastModified)
							tf.SaveChanges();
				},
				null,
				TimeSpan.FromSeconds(0),
				tickIntervalToSaveFiles);
		}

		public TodoFile(string FullFilePath)
		{
			createdTodoFiles.Add(this);
			EnsureSaveFilesTimerStarted();
			this.FullFilePath = FullFilePath;
			UpdateTodoLines();
			HasUnsavedChanges = false;
		}
		~TodoFile()
		{
			if (this.HasUnsavedChanges)
				this.SaveChanges();
			if (File.Exists(FullFilePath) && string.IsNullOrWhiteSpace(FileContent))
			{
				ActionsInterop.DoAllActionsAndHandleError(
					err => UserMessages.ShowErrorMessage(err),
					delegate { File.Delete(FullFilePath); });
			}
			if (createdTodoFiles.Contains(this))
				createdTodoFiles.Remove(this);
		}

		public void SaveChanges()//string value)
		{
			string backupFilePath = GetDateFilenameNow();
			bool saveSuccess = ActionsInterop.DoAllActionsAndHandleError(
				(err) => UserMessages.ShowErrorMessage("Cannot save file: " + err),
				delegate { File.WriteAllText(backupFilePath, File.ReadAllText(FullFilePath)); },
				//new FileInfo(FullFilePath).Compress(backupFilePath);
				delegate { File.SetAttributes(backupFilePath, FileAttributes.System | FileAttributes.Hidden); },
				delegate { File.WriteAllText(FullFilePath, _filecontent); });

			if (saveSuccess)
			{
				HasUnsavedChanges = false;
				_filecontent = null;
			}
			OnPropertyChanged("FileContent");
		}

		private void UpdateTodoLines()
		{
			if (!string.IsNullOrEmpty(FileContent))
				this.TodoLines = new ObservableCollection<TodoLine>(FileContent.Split(new string[] { "\r\n" }, StringSplitOptions.None).Select(l => new TodoLine(l)));
			else
				this.TodoLines = new ObservableCollection<TodoLine>();
			OnPropertyChanged("TodoLines");
			SetTodolineCompleteEvents();
		}

		private void SetTodolineCompleteEvents()
		{
			UnsetTodolineCompleteEvents();
			foreach (TodoLine tl in this.TodoLines)
				tl.PropertyChanged += new PropertyChangedEventHandler(TodoLine_PropertyChanged);
		}

		private void UnsetTodolineCompleteEvents()
		{
			foreach (TodoLine tl in this.TodoLines)
				tl.PropertyChanged -= new PropertyChangedEventHandler(TodoLine_PropertyChanged);
		}

		private void TodoLine_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (
				e.PropertyName.Equals("IsComplete")
				|| e.PropertyName.Equals("LineText")
				|| e.PropertyName.Equals("DueDate"))
				RefreshFileContentFromTodoLines();
			else if (e.PropertyName.Equals("IsDue"))
				OnPropertyChanged("HasDueItems");
		}

		private void RefreshFileContentFromTodoLines()
		{
			string tmpContents = "";
			foreach (TodoLine tl in this.TodoLines)
				tmpContents += (tmpContents.Length > 0 ? Environment.NewLine : "") + tl.GetFullLineText();
			try
			{
				IgnoreTodolineUpdate = true;
				this.FileContent = tmpContents;
			}
			finally { IgnoreTodolineUpdate = false; }
			tmpContents = null;
		}

		public string GetDateFilenameNow()
		{
			return string.Format("{0}.{1}", FullFilePath, DateTime.Now.ToString(dataformatFileExtension));
			//return string.Format("{0}.{1}.gz", FullFilePath, DateTime.Now.ToString(dataformatFileExtension));
		}

		public void Purge()
		{
			File.Move(FullFilePath, GetDateFilenameNow());
		}

		public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}

	public class TodoLine : INotifyPropertyChanged
	{
		public static TimeSpan DurationBetweenIsDueChecks = TimeSpan.FromSeconds(1);//TimeSpan.FromMinutes(5);
		private static DateTime NoDueDateValue = DateTime.MinValue;

		private string _linetext;
		public string LineText { get { return _linetext; } set { _linetext = value; OnPropertyChanged("LineText"); } }

		private bool _iscomplete;
		public bool IsComplete { get { return _iscomplete; } set { _iscomplete = value; OnPropertyChanged("IsComplete", "DueDate", "HasDueDate", "IsDue"); } }

		private DateTime _duedate;
		public DateTime DueDate { get { return _duedate; } set { if (_duedate.Equals(value)) return; _duedate = value; _reminderdate = value; OnPropertyChanged("DueDate", "HasDueDate", "IsDue", "ReminderDate"); } }

		public bool HasDueDate { get { return !DueDate.Equals(DateTime.MinValue); } }

		private DateTime _reminderdate;
		public DateTime ReminderDate { get { return _reminderdate; } set { _reminderdate = value; OnPropertyChanged("ReminderDate"); } }

		public TodoLine(string LineText)//, bool IsComplete)
		{
			this.IsComplete = LineText.Trim().StartsWith("//");

			//Check if has date in string
			string tmpline = LineText.TrimStart('/');
			DateTime tmpdate;
			if (tmpline.StartsWith("[") && tmpline.IndexOf(']') != -1)
			{
				int closebracketPos = tmpline.IndexOf(']');
				if (GetDate(tmpline.Substring(1, closebracketPos - 1), out tmpdate))
				{
					this.DueDate = tmpdate;
					this.LineText = tmpline.Substring(closebracketPos + 1);
					return;
				}
			}

			//Did not find date in string, just checking for completeness
			this.LineText = LineText.TrimStart('/');
		}

		public bool IsDue { get { OnPropertyChanged("IdDue"); if (!HasDueDate || IsComplete) return false; return DateTime.Now.Subtract(DueDate).Add(DurationBetweenIsDueChecks).TotalSeconds >= 0; } }
		public bool IsReminderDue { get { OnPropertyChanged("IsReminderDue"); if (!HasDueDate || IsComplete) return false; return DateTime.Now.Subtract(ReminderDate).Add(DurationBetweenIsDueChecks).TotalSeconds >= 0; } }

		private const string cSaveDateFormat = "[yyyy-MM-dd HH:mm]";
		public string GetFullLineText()
		{
			return
				(this.IsComplete ? "//" : "")
				+ (this.HasDueDate ? DueDate.ToString(cSaveDateFormat) : "")
				+ this.LineText;
		}

		public override string ToString()
		{
			return GetFullLineText();
		}

		public static bool GetDate(string stringIn, out DateTime datetime)
		{
			datetime = DateTime.MaxValue;
			if (string.IsNullOrWhiteSpace(stringIn))
				return false;
			string[] splits = stringIn.Split('-', ' ', 'h', '/', ':');
			if (splits.Length != 5)
				return false;
			int tmpint;
			int[] FiveInts = new int[5];
			for (int i = 0; i < splits.Length; i++)
				if (!int.TryParse(splits[i], out tmpint))
					return false;
				else
					FiveInts[i] = tmpint;
			try
			{
				DateTime tmpdate = new DateTime(FiveInts[0], FiveInts[1], FiveInts[2], FiveInts[3], FiveInts[4], 0);
				datetime = tmpdate;
				return true;
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Unable to convert string '" + stringIn + "' to datetime: " + exc.Message);
				return false;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged = new PropertyChangedEventHandler(delegate { });
		public void OnPropertyChanged(params string[] propertyNames) { foreach (string propertyName in propertyNames) PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}
}