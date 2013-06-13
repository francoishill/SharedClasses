using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SharedClasses;
#if CONSOLE
#else
using System.Drawing;
using System.Windows.Forms;
#endif

namespace SharedClasses
{
	public partial class UserMessages : Form
	{
		private const string FJHmainFolderNameForLoggingMessages = "_UserMessages";

		//public static Dictionary<string, UserMessages> ListOfShowingMessages = new Dictionary<string, UserMessages>(StringComparer.InvariantCultureIgnoreCase);
		public static ConcurrentDictionary<string, UserMessages> ListOfShowingMessages = new ConcurrentDictionary<string, UserMessages>(StringComparer.InvariantCultureIgnoreCase);

		public MessageBoxButtons CurrentButtons;

		[ThreadStatic]
		private static Dictionary<MessageBoxIcon, Image> _mappedIcons;
		private static Dictionary<MessageBoxIcon, Image> MappedIcons
		{
			get
			{
				if (_mappedIcons == null)
				{
					_mappedIcons = new Dictionary<MessageBoxIcon, Image>();

					_mappedIcons.Add(MessageBoxIcon.Error, SystemIcons.Error.ToBitmap());
					_mappedIcons.Add(MessageBoxIcon.Warning, SystemIcons.Warning.ToBitmap());
					_mappedIcons.Add(MessageBoxIcon.Information, SystemIcons.Information.ToBitmap());
					_mappedIcons.Add(MessageBoxIcon.Question, SystemIcons.Question.ToBitmap());
				}
				return _mappedIcons;
			}
		}

		private UserMessages()
		{
			InitializeComponent();
			labelCountRepeatedCount.Text = "0 times";
			EnsureClosingEventAttached();
		}

		private bool alreadyAttached = false;
		private void EnsureClosingEventAttached()
		{
			if (!alreadyAttached)
			{
				if (Application.OpenForms.Count > 0)
					Application.OpenForms[0].FormClosing += delegate
					{
						var keys = ListOfShowingMessages.Keys.ToArray();
						for (int i = keys.Length - 1; i >= 0; i--)
						{
							//ListOfShowingMessages[keys[i]].Close();
							UserMessages tmpMbox;
							while (!ListOfShowingMessages.TryGetValue(keys[i], out tmpMbox))
								System.Threading.Thread.Sleep(200);

							Action action = (Action)delegate { tmpMbox.Close(); };
							if (tmpMbox.InvokeRequired)
								tmpMbox.Invoke(action);
							else
								action();
						}
					};
				alreadyAttached = true;
			}
		}

		private static bool VisualStylesAlreadyEnabled = false;
		public static DialogResult ShowUserMessage(IWin32Window owner, string Message, string Title, MessageBoxIcon icon, bool AlwaysOnTop, params string[] argumentsIfMessageStringIsFormatted)
		{
			if (!VisualStylesAlreadyEnabled)
			{
				Application.EnableVisualStyles();
				VisualStylesAlreadyEnabled = true;
			}

			if (argumentsIfMessageStringIsFormatted.Length > 0)
				Message = string.Format(Message, argumentsIfMessageStringIsFormatted);

			if (ListOfShowingMessages.ContainsKey(Message))
			{
				Action showConfirmAction = delegate
				{
					ListOfShowingMessages[Message].labelCountRepeatedCount.Visible = true;
					int curCount;
					if (int.TryParse(ListOfShowingMessages[Message].labelCountRepeatedCount.Text.Substring(0, ListOfShowingMessages[Message].labelCountRepeatedCount.Text.IndexOf(' ')), out curCount))
						ListOfShowingMessages[Message].labelCountRepeatedCount.Text = (curCount + 1).ToString() + " times";
				};
				if (ListOfShowingMessages[Message].InvokeRequired)
					ListOfShowingMessages[Message].Invoke(showConfirmAction);
				else
					showConfirmAction();
				return DialogResult.Cancel;//Might cause issues as the 2nd, 3rd, 4th, etc occurance will always return Cancel
			}

			UserMessages umb = new UserMessages();
			umb.TopMost = AlwaysOnTop;
			ListOfShowingMessages.AddOrUpdate(Message, umb, (s, u) => { return null; });

			if (icon == MessageBoxIcon.None)
				umb.pictureBox1.Visible = false;
			else
				umb.pictureBox1.Image = MappedIcons[icon];
			umb.CurrentButtons = MessageBoxButtons.OK;
			umb.labelMessage.Text = Message;
			umb.Text = Title;
			//umb.TopMost = AlwaysOnTop;

			Logging.LogTypes logtype = Logging.LogTypes.Info;
			switch (icon)
			{
				case MessageBoxIcon.Error:
					logtype = Logging.LogTypes.Error;
					break;
				case MessageBoxIcon.Information:
					logtype = Logging.LogTypes.Info;
					break;
				case MessageBoxIcon.Question:
					logtype = Logging.LogTypes.Info;
					break;
				case MessageBoxIcon.Warning:
					logtype = Logging.LogTypes.Warning;
					break;
				default:
					logtype = Logging.LogTypes.Info;
					break;
			}

			string thisAppname = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			Logging.LogMessageToFile(Message, logtype, Logging.ReportingFrequencies.Daily, FJHmainFolderNameForLoggingMessages, thisAppname);

			return umb.ShowDialog(owner);
		}

		private void GlobalKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Escape)
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}

		private bool labelLocationChanged = false;
		private void UserMessages_SizeChanged(object sender, EventArgs e)
		{
			if (this.Size.Width != 138 || this.Size.Height != 170)
			{
				if (this.Size.Height > 170)
					if (!labelLocationChanged)
					{
						labelMessage.Location = new Point(labelMessage.Location.X, labelMessage.Location.Y - 9);
						labelLocationChanged = true;
					}
				if (CurrentButtons == MessageBoxButtons.OK)
				{
					Button acceptButton = new Button()
					{
						Text = "&OK",
						Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
						DialogResult = System.Windows.Forms.DialogResult.OK
					};
					this.panel2.Controls.Add(acceptButton);
					acceptButton.PreviewKeyDown += GlobalKeyDown;
					acceptButton.Location = new Point(this.panel2.Width - acceptButton.Width - 5, (this.panel2.Height - acceptButton.Height) / 2);
				}
			}
		}

		private void UserMessages_FormClosing(object sender, FormClosingEventArgs e)
		{
			UserMessages tmpMbox;
			while (!ListOfShowingMessages.TryRemove(this.labelMessage.Text, out tmpMbox))
				System.Threading.Thread.Sleep(200);
		}


#if CONSOLE
	public static void ShowWarningMessage(string message)
	{
		Console.WriteLine(string.Format("Warning: {0}", message));
	}
#else
		public static Icon iconForMessages;

		/*All methods to show messages that is of type boolean (except Confirm) will return true
		Reason being is that it makes it easy to show a message in a method which would should exit when the message is shown*/
		private static bool ShowMsg(IWin32Window owner, string Message, string Title, MessageBoxIcon icon, bool AlwaysOnTop, params string[] argumentsIfMessageStringIsFormatted)
		{
			//AlwayOnTop is currently broken if no owner assigned
			UserMessages.ShowUserMessage(owner, Message, Title, icon, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
			/*if (owner == null && Form.ActiveForm != null && AlwaysOnTop) owner = Form.ActiveForm;
			bool useTempForm = false;
			//Form tempForm = null;
			Form topmostForm = null;
			if (owner == null && AlwaysOnTop)
			{
				useTempForm = true;
				topmostForm = GetTopmostForm();// new Form();
				owner = topmostForm;
			}

			Action showConfirmAction = delegate
			{
				bool ownerOriginalTopmostState = false;
				if (owner != null)
				{
					ownerOriginalTopmostState = ((Form)owner).TopMost;
					((Form)owner).TopMost = AlwaysOnTop;
				}
				if (iconForMessages != null) ((Form)owner).Icon = iconForMessages;
				//MessageBox.Show(owner, Message, Title, MessageBoxButtons.OK, icon);
				UserMessages.ShowUserMessage(owner, Message, Title, icon, AlwaysOnTop);
				if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
				if (owner != null)
					((Form)owner).TopMost = ownerOriginalTopmostState;
			};

				if (owner != null && ((Form)owner).InvokeRequired)
					((Form)owner).Invoke(showConfirmAction, new object[] { });
				else showConfirmAction();
			return true;*/
		}

		public static bool ShowErrorMessage(string Message, string Title = "Error", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowErrorMessage(null, Message, Title, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}
		public static bool ShowErrorMessage(IWin32Window owner, string Message, string Title = "Error", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowMsg(owner, Message, Title, MessageBoxIcon.Error, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}

		public static bool ShowWarningMessage(string Message, string Title = "Warning", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowWarningMessage(null, Message, Title, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}
		public static bool ShowWarningMessage(IWin32Window owner, string Message, string Title = "Warning", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowMsg(owner, Message, Title, MessageBoxIcon.Warning, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}

		public static bool ShowInfoMessage(string Message, string Title = "Info", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowInfoMessage(null, Message, Title, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}
		public static bool ShowInfoMessage(IWin32Window owner, string Message, string Title = "Info", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowMsg(owner, Message, Title, MessageBoxIcon.Information, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}

		public static bool ShowMessage(string Message, string Title = "Message", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowMessage(null, Message, Title, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}
		public static bool ShowMessage(IWin32Window owner, string Message, string Title = "Message", bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			ShowMsg(owner, Message, Title, MessageBoxIcon.None, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
			return true;
		}

		public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			return Confirm(null, Message, Title, DefaultYesButton, AlwaysOnTop, argumentsIfMessageStringIsFormatted);
		}
		public static bool Confirm(IWin32Window owner, string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			return ConfirmNullable(owner, Message, Title, DefaultYesButton, AlwaysOnTop, false, argumentsIfMessageStringIsFormatted) == true;
			////DialogResult result = MessageBox.Show(topmostForm, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
			////topmostForm.Dispose(); // clean it up all the way

			//if (owner == null && Form.ActiveForm != null) owner = Form.ActiveForm;
			//bool useTempForm = false;
			////Form tempForm = null;
			//Form topmostForm = null;
			//if (owner == null)
			//{
			//	useTempForm = true;
			//	//tempForm = new Form();
			//	topmostForm = GetTopmostForm();
			//	owner = topmostForm;
			//}

			//bool result = false;
			//Action showConfirmAction = delegate
			//{
			//	bool ownerOriginalTopmostState = ((Form)owner).TopMost; 
			//	((Form)owner).TopMost = AlwaysOnTop;
			//	result = MessageBox.Show(owner, Message, Title, IsAnswerNullable ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
			//	if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
			//	((Form)owner).TopMost = ownerOriginalTopmostState;
			//};

			//if (((Form)owner).InvokeRequired)
			//	((Form)owner).Invoke(showConfirmAction, new object[] { });
			//else showConfirmAction();
			//return result;
		}

		public static bool? ConfirmNullable(string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, bool AnswerIsNullable = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			return ConfirmNullable(null, Message, Title, DefaultYesButton, AlwaysOnTop, AnswerIsNullable, argumentsIfMessageStringIsFormatted);
		}

		delegate void Action();
		public static bool? ConfirmNullable(IWin32Window owner, string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, bool AnswerIsNullable = true, params string[] argumentsIfMessageStringIsFormatted)
		{
			//DialogResult result = MessageBox.Show(topmostForm, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
			//topmostForm.Dispose(); // clean it up all the way

			if (owner == null && Form.ActiveForm != null) owner = Form.ActiveForm;
			bool useTempForm = false;
			//Form tempForm = null;
			Form topmostForm = null;
			if (owner == null)
			{
				useTempForm = true;
				//tempForm = new Form();
				topmostForm = GetTopmostForm();
				owner = topmostForm;
			}

			bool? result = false;
			Action showConfirmAction = delegate
			{
				bool ownerOriginalTopmostState = ((Form)owner).TopMost;
				((Form)owner).TopMost = AlwaysOnTop;
				DialogResult tmpDialogResult = MessageBox.Show(
					owner,
					argumentsIfMessageStringIsFormatted.Length > 0 ? string.Format(Message, argumentsIfMessageStringIsFormatted) : Message,
					Title,
					AnswerIsNullable ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo,
					MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
				if (tmpDialogResult == DialogResult.Yes)
					result = true;
				else if (tmpDialogResult == DialogResult.No)
					result = false;
				else
					result = null;
				if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
				((Form)owner).TopMost = ownerOriginalTopmostState;
			};

			Logging.LogTypes logtype = Logging.LogTypes.Info;
			string thisAppname = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			Logging.LogMessageToFile(Message, logtype, Logging.ReportingFrequencies.Daily, FJHmainFolderNameForLoggingMessages, thisAppname);

			if (((Form)owner).InvokeRequired)
				((Form)owner).Invoke(showConfirmAction, new object[] { });
			else showConfirmAction();
			return result;
		}

		private static Form GetTopmostForm()
		{
			Form topmostForm = new Form();
			// We do not want anyone to see this window so position it off the
			// visible screen and make it as small as possible
			topmostForm.Size = new System.Drawing.Size(1, 1);
			topmostForm.StartPosition = FormStartPosition.Manual;
			System.Drawing.Rectangle rect = SystemInformation.VirtualScreen;
			topmostForm.Location = new System.Drawing.Point(rect.Bottom + 10, rect.Right + 10);
			topmostForm.Show();
			// Make this form the active form and make it TopMost

			topmostForm.Focus();
			topmostForm.BringToFront();
			topmostForm.TopMost = true;
			return topmostForm;
			// Finally show the MessageBox with the form just created as its owner
		}

		//Forms
		public static string ChooseDirectory(string UserMessage, string SelectedPath = null, Environment.SpecialFolder RootFolder = Environment.SpecialFolder.SendTo, bool ShowNewFolderButton = false, IWin32Window owner = null)
		{
			bool useTempForm = false;
			Form topmostForm = null;
			if (owner == null)
			{
				useTempForm = true;
				topmostForm = GetTopmostForm();// new Form();
				owner = topmostForm;
			}

			bool ownerOriginalTopmostState = ((Form)owner).TopMost;
			((Form)owner).TopMost = true;
			if (iconForMessages != null) ((Form)owner).Icon = iconForMessages;

			try
			{
				FolderBrowserDialog fbd = new FolderBrowserDialog();
				fbd.Description = UserMessage;
				if (SelectedPath != null) fbd.SelectedPath = SelectedPath;
				if (RootFolder != Environment.SpecialFolder.SendTo) fbd.RootFolder = RootFolder;
				fbd.ShowNewFolderButton = ShowNewFolderButton;
				if (fbd.ShowDialog(owner) == DialogResult.OK)
					return fbd.SelectedPath;
				return null;
			}
			finally
			{
				if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
				((Form)owner).TopMost = ownerOriginalTopmostState;
			}
		}
#endif
	}

	/*public static class UserMessagesExtensions
	{
		public static void ShowErr(this System.Windows.Window window, string err)
		{
			UserMessages.ShowErrorMessage(err);
		}
	}*/
}