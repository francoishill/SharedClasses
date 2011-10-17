using System.Windows.Forms;
public class UserMessages
{
	/*All methods to show messages that is of type boolean (except Confirm) will return true
	Reason being is that it makes it easy to show a message in a method which would should exit when the message is shown*/
	private static bool ShowMsg(IWin32Window Owner, string Message, string Title, MessageBoxIcon icon, bool AlwayOnTop)
	{
		if (Owner == null && Form.ActiveForm != null) Owner = Form.ActiveForm;
		bool useTempForm = false;
		Form tempForm = null;
		if (Owner == null)
		{
			useTempForm = true;
			tempForm = new Form();
			Owner = tempForm;
		}
		if (AlwayOnTop) ((Form)Owner).TopMost = true;
		MessageBox.Show(Owner, Message, Title, MessageBoxButtons.OK, icon);
		if (useTempForm && tempForm != null && !tempForm.IsDisposed) tempForm.Dispose();
		return true;
	}

	public static bool ShowErrorMessage(string Message, string Title = "Error", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Error, AlwayOnTop);
		return true;
	}

	public static bool ShowWarningMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Warning, AlwayOnTop);
		return true;
	}

	public static bool ShowInfoMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Information, AlwayOnTop);
		return true;
	}

	public static bool ShowMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.None, AlwayOnTop);
		return true;
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, IWin32Window owner = null)
	{
		return MessageBox.Show(owner, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
	}
}