using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace QuickAccess
{
	public class MicrosoftOfficeInterop
	{
		public static void CreateNewOutlookMessage(Form1 form1, string To, string Subject, string Body)
		{
			ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
			{
				if (Process.GetProcessesByName("Outlook").Length == 0)
				{
					Logging.appendLogTextbox_OfPassedTextbox(form1.textBox_Messages, "Starting Outlook, please wait...");
					Process p = System.Diagnostics.Process.Start("Outlook");
				}

				// Creates a new Outlook Application Instance
				Microsoft.Office.Interop.Outlook.Application objOutlook = new Outlook.Application();
				// Creating a new Outlook Message from the Outlook Application Instance
				Outlook.MailItem mic = (Outlook.MailItem)(objOutlook.CreateItem(Outlook.OlItemType.olMailItem));
				mic.To = To;
				mic.Subject = Subject;
				mic.Body = Body;
				form1.TopMost = false;
				mic.Display(true);
				form1.TopMost = true;
				form1.ShowAndActivateThisForm();
			});
		}
	}
}
