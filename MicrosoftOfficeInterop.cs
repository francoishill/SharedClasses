using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Windows.Forms;

public class MicrosoftOfficeInterop
{
	public static void CreateNewOutlookMessage(string To, string Subject, string Body, TextFeedbackEventHandler textFeedbackEvent = null)
	{
		ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
		{
			if (Process.GetProcessesByName("Outlook").Length == 0)
			{
				TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Starting Outlook, please wait...");
				//TextFeedbackEventArgs.RaiseTextFeedbackEvent_Ifnotnull(textFeedbackEvent, "Starting Outlook, please wait...");
				Process p = System.Diagnostics.Process.Start("Outlook");
			}

			// Creates a new Outlook Application Instance
			Microsoft.Office.Interop.Outlook.Application objOutlook = new Outlook.Application();
			// Creating a new Outlook Message from the Outlook Application Instance
			Outlook.MailItem mic = (Outlook.MailItem)(objOutlook.CreateItem(Outlook.OlItemType.olMailItem));
			mic.To = To;
			mic.Subject = Subject;
			mic.Body = Body;
			//form.TopMost = false;
			mic.Display(true);
			//form.TopMost = true;
			//WindowsInterop.ShowAndActivateForm(form);
		});
	}
}