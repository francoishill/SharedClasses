using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;

namespace SharedClasses
{
	public static class DeveloperCommunication
	{
		public static void RunMailto(string subject = null, string body = null)
		{
			//If null used, will just not be used in MailTo command
			string postfixAfterMailtoAddress = "";
			if (subject != null && body != null)
				postfixAfterMailtoAddress = string.Format("?subject={0}&body={1}", subject, body);
			else if (subject != null)
				postfixAfterMailtoAddress = string.Format("?subject={0}", subject);
			else if (body != null)
				postfixAfterMailtoAddress = string.Format("?body={0}", body);
			Process.Start("mailto:developer@firepuma.com" + postfixAfterMailtoAddress);
		}

		private static string cDeveloperGmailAddress_FromAddress = "devhillapps@gmail.com";
		private static string cDeveloperEmailAddress_ToAddress = "developer@firepuma.com";
		public static bool SendEmailToDeveloperViaSMTP(string subject, string body, bool bodyIsHtml = true, string attachmentFilepath = null)
		{
			SmtpClient client = new SmtpClient("smtp.gmail.com", 587);//465
			client.EnableSsl = true;
			client.Timeout = 10000;
			client.DeliveryMethod = SmtpDeliveryMethod.Network;
			client.UseDefaultCredentials = false;
			client.Credentials = new NetworkCredential(cDeveloperGmailAddress_FromAddress, "passdevhillapps@7");//CredentialCache.DefaultNetworkCredentials;

			Attachment data = null;
			try
			{
				MailMessage message = new MailMessage(
				   cDeveloperGmailAddress_FromAddress,
				   cDeveloperEmailAddress_ToAddress,
				   subject,
				   body);
				message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
				message.IsBodyHtml = bodyIsHtml;

				if (attachmentFilepath != null)
				{
					data = new Attachment(attachmentFilepath, MediaTypeNames.Application.Octet);
					ContentDisposition disposition = data.ContentDisposition;
					disposition.CreationDate = System.IO.File.GetCreationTime(attachmentFilepath);
					disposition.ModificationDate = System.IO.File.GetLastWriteTime(attachmentFilepath);
					disposition.ReadDate = System.IO.File.GetLastAccessTime(attachmentFilepath);
					message.Attachments.Add(data);
				}

				client.Send(message);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception caught in CreateMessageWithAttachment(): {0}",
					  ex.ToString());
				return false;
			}
			finally
			{
				if (data != null)
					data.Dispose();
			}
		}
	}
}