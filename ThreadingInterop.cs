using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public class ThreadingInterop
{
	//PerformVoidFunctionSeperateThread(() => { MessageBox.Show("Test"); MessageBox.Show("Test1"); });
	public static void PerformVoidFunctionSeperateThread(MethodInvoker method, bool WaitUntilFinish = true)
	{
		System.Threading.Thread th = new System.Threading.Thread(() =>
		{
			method.Invoke();
		});
		th.Start();
		//th.Join();
		if (WaitUntilFinish)
			while (th.IsAlive) { Application.DoEvents(); }
	}

	public static void UpdateGuiFromThread(Control controlToUpdate, Action action)
	{
		if (controlToUpdate.InvokeRequired)
			controlToUpdate.Invoke(action, new object[] { });
		else action.Invoke();
	}

	delegate void AutocompleteCallback(ComboBox txtBox, String text);
	delegate void ClearAutocompleteCallback(ComboBox txtBox);
	public static void ClearTextboxAutocompleteCustomSource(ComboBox txtBox)
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

	public static void AddTextboxAutocompleteCustomSource(ComboBox txtBox, string textToAdd)
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