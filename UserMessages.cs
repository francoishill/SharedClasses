using System.Windows.Forms;
public class UserMessages
{
	public static void ShowErrorMessage(string Message, string Title = "Error")
	{
		MessageBox.Show(Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	public static bool Confirm(string Message, string Title = "Confirm", bool DefaultYesButton = false)
	{
		return MessageBox.Show(Message, Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, DefaultYesButton ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) == DialogResult.Yes;
	}
}