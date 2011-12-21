using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class UserMessages
{
	public static Icon iconForMessages;

	/*All methods to show messages that is of type boolean (except Confirm) will return true
	Reason being is that it makes it easy to show a message in a method which would should exit when the message is shown*/
	private static bool ShowMsg(IWin32Window owner, string Message, string Title, MessageBoxIcon icon, bool AlwaysOnTop)
	{
		if (owner == null && Form.ActiveForm != null) owner = Form.ActiveForm;
		bool useTempForm = false;
		//Form tempForm = null;
		Form topmostForm = null;
		if (owner == null)
		{
			useTempForm = true;
			topmostForm = GetTopmostForm();// new Form();
			owner = topmostForm;
		}

		Action showConfirmAction = delegate
		{
			bool ownerOriginalTopmostState = ((Form)owner).TopMost; 
			((Form)owner).TopMost = AlwaysOnTop;
			if (iconForMessages != null) ((Form)owner).Icon = iconForMessages;
			MessageBox.Show(owner, Message, Title, MessageBoxButtons.OK, icon);
			if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
			((Form)owner).TopMost = ownerOriginalTopmostState;
		};

		if (((Form)owner).InvokeRequired)
			((Form)owner).Invoke(showConfirmAction, new object[] { });
		else showConfirmAction();
		return true;
	}

	public static bool ShowErrorMessage(string Message, string Title = "Error", bool AlwaysOnTop = true)
	{
		ShowErrorMessage(null, Message, Title, AlwaysOnTop);
		return true;
	}
	public static bool ShowErrorMessage(IWin32Window owner, string Message, string Title = "Error", bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Error, AlwaysOnTop);
		return true;
	}

	public static bool ShowWarningMessage(string Message, string Title = "Warning", bool AlwaysOnTop = true)
	{
		ShowWarningMessage(null, Message, Title, AlwaysOnTop);
		return true;
	}
	public static bool ShowWarningMessage(IWin32Window owner, string Message, string Title = "Warning", bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Warning, AlwaysOnTop);
		return true;
	}

	public static bool ShowInfoMessage(string Message, string Title = "Warning", bool AlwaysOnTop = true)
	{
		ShowInfoMessage(null, Message, Title, AlwaysOnTop);
		return true;
	}
	public static bool ShowInfoMessage(IWin32Window owner, string Message, string Title = "Warning", bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Information, AlwaysOnTop);
		return true;
	}

	public static bool ShowMessage(string Message, string Title = "Warning", bool AlwaysOnTop = true)
	{
		ShowMessage(null, Message, Title, AlwaysOnTop);
		return true;
	}
	public static bool ShowMessage(IWin32Window owner, string Message, string Title = "Warning",bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.None, AlwaysOnTop);
		return true;
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true)
	{
		return Confirm(null, Message, Title, DefaultYesButton, AlwaysOnTop);
	}
	public static bool Confirm(IWin32Window owner, string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true)
	{
		return ConfirmNullable(owner, Message, Title, DefaultYesButton, AlwaysOnTop, false) == true;
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

	public static bool? ConfirmNullable(string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, bool AnswerIsNullable = true)
	{
		return ConfirmNullable(null, Message, Title, DefaultYesButton, AlwaysOnTop, AnswerIsNullable);
	}

	public static bool? ConfirmNullable(IWin32Window owner, string Message, string Title = "Confirm", bool DefaultYesButton = false, bool AlwaysOnTop = true, bool AnswerIsNullable = true)
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
			DialogResult tmpDialogResult = MessageBox.Show(owner, Message, Title, AnswerIsNullable ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);
			if (tmpDialogResult == DialogResult.Yes)
				result = true;
			else if (tmpDialogResult == DialogResult.No)
				result = false;
			else
				result = null;
			if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
			((Form)owner).TopMost = ownerOriginalTopmostState;
		};

		if (((Form)owner).InvokeRequired)
			((Form)owner).Invoke(showConfirmAction, new object[] { });
		else showConfirmAction();
		return result;
	}

	public static string Prompt(string Message, string Title = "Prompt", string DefaultResponse = "")
	{
		return Microsoft.VisualBasic.Interaction.InputBox(Message, Title, DefaultResponse);
	}

	public static T PickItem<T>(Array itemArray, string Message, T defaultItem)
	{
		return PickItem<T>(null, itemArray, Message, defaultItem);
	}
	public static object PickItem(Type ObjectType, Array itemArray, string Message, object defaultItem)
	{
		return PickItem(ObjectType, null, itemArray, Message, defaultItem);
	}
	public static T PickItem<T>(IWin32Window owner, Array itemArray, string Message, T defaultItem)
	{
		//return PickItemForm.PickItem<T>(itemArray, Message, defaultItem, owner);
		return (T)PickItem(typeof(T), owner, itemArray, Message, defaultItem);
	}
	public static object PickItem(Type ObjectType, IWin32Window owner, Array itemArray, string Message, object defaultItem)
	{
		return PickItemForm.PickItem(ObjectType, itemArray, Message, defaultItem, owner);
	}

	public static T PickItem<T>(List<T> itemList, string Message, T defaultItem)
	{
		return PickItem<T>(null, itemList, Message, defaultItem);
	}
	public static T PickItem<T>(IWin32Window owner, List<T> itemList, string Message, T defaultItem)
	{
		return PickItemForm.PickItem<T>(itemList.ToArray(), Message, defaultItem, owner);
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
}