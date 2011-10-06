using System.Windows.Forms;
public class UserMessages
{
	private static void ShowMsg(IWin32Window Owner, string Message, string Title, MessageBoxIcon icon, bool AlwayOnTop)
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
	}

	public static void ShowErrorMessage(string Message, string Title = "Error", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Error, AlwayOnTop);
	}

	public static void ShowWarningMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Warning, AlwayOnTop);
	}

	public static void ShowInfoMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Information, AlwayOnTop);
	}

	public static void ShowMessage(string Message, string Title = "Warning", IWin32Window owner = null, bool AlwayOnTop = true)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.None, AlwayOnTop);
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, IWin32Window owner = null)
	{
		return MessageBox.Show(owner, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
	}
}