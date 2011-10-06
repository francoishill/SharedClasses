using System.Windows.Forms;
public class UserMessages
{
	private static void ShowMsg(IWin32Window Owner, string Message, string Title, MessageBoxIcon icon)
	{
		MessageBox.Show(Owner, Message, Title, MessageBoxButtons.OK, icon);
	}

	public static void ShowErrorMessage(string Message, string Title = "Error", IWin32Window owner = null)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Error);
	}

	public static void ShowWarningMessage(string Message, string Title = "Warning", IWin32Window owner = null)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Warning);
	}

	public static void ShowInfoMessage(string Message, string Title = "Warning", IWin32Window owner = null)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.Information);
	}

	public static void ShowMessage(string Message, string Title = "Warning", IWin32Window owner = null)
	{
		ShowMsg(owner, Message, Title, MessageBoxIcon.None);
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false, IWin32Window owner = null)
	{
		return MessageBox.Show(owner, Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
	}
}