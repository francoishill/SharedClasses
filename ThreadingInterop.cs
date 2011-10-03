using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public class ThreadingInterop
{
	public static void PerformVoidFunctionSeperateThread(MethodInvoker method)
	{
		System.Threading.Thread th = new System.Threading.Thread(() =>
		{
			method.Invoke();
		});
		th.Start();
		//th.Join();
		while (th.IsAlive) { Application.DoEvents(); }
	}

	public static void UpdateGuiFromThread(Control controlToUpdate, Action action)
	{
		if (controlToUpdate.InvokeRequired)
			controlToUpdate.Invoke(action, new object[] { });
		else action.Invoke();
	}

	delegate void AutocompleteCallback(TextBox txtBox, String text);
	delegate void ClearAutocompleteCallback(TextBox txtBox);
	public static void ClearTextboxAutocompleteCustomSource(TextBox txtBox)
	{
		if (txtBox.InvokeRequired)
		{
			ClearAutocompleteCallback d = new ClearAutocompleteCallback(ClearTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Clear();
		}
	}

	public static void AddTextboxAutocompleteCustomSource(TextBox txtBox, string textToAdd)
	{
		if (txtBox.InvokeRequired)
		{
			AutocompleteCallback d = new AutocompleteCallback(AddTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox, textToAdd });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Add(textToAdd);
		}
	}
}