using System;
using System.Windows.Forms;

public class UserMessages
{
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
			MessageBox.Show(owner, Message, Title, MessageBoxButtons.OK, icon);
			if (useTempForm && topmostForm != null && !topmostForm.IsDisposed) topmostForm.Dispose();
			((Form)owner).TopMost = ownerOriginalTopmostState;
		};

		if (((Form)owner).InvokeRequired)
			((Form)owner).Invoke(showConfirmAction, new object[] { });
		else showConfirmAction();
		return true;
	}

	public static bool ShowErrorMessage(string Message, string Title = "Error", IWin32Window owner = null, bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Error, AlwaysOnTop);
		return true;
	}

	public static bool ShowWarningMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Warning, AlwaysOnTop);
		return true;
	}

	public static bool ShowInfoMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Information, AlwaysOnTop);
		return true;
	}

	public static bool ShowMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwaysOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.None, AlwaysOnTop);
		return true;
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, IWin32Window owner = null, bool AlwaysOnTop = true)
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

		bool result = false;
		Action showConfirmAction = delegate
		{
			bool ownerOriginalTopmostState = ((Form)owner).TopMost; 
			((Form)owner).TopMost = AlwaysOnTop;
			result = MessageBox.Show(owner, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
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