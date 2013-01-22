using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Threading;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for UnhandledExceptionsWindow.xaml
	/// </summary>
	public partial class UnhandledExceptionsWindow : Window
	{
		/*	Additional dependencies for this file:
			Class: AppTypeIndependant
			Class: DeveloperCommunication
			Class: ReflectionInterop
			Assembly: WindowsBase
			Assembly: PresentationCore
			Assembly: PresentationFramework
			Assembly: System.Xaml*/

		public UnhandledExceptionsWindow(Exception exception)
		{
			InitializeComponent();

			this.DataContext = exception;
		}

		private void UnhandledExceptionsWindow1_Loaded(object sender, RoutedEventArgs e)
		{
			this.MaxHeight = SystemParameters.WorkArea.Height - 100;
		}

		private bool alreadyBusySending = false;
		private string tmpSubject = null;
		private string tmpBody = null;
		private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			textblockBusySendingMessage.Visibility = System.Windows.Visibility.Visible;
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

			Exception exc = this.DataContext as Exception;
			if (exc == null)
			{
				AppTypeIndependant.ShowErrorMessage("Unable to get the Exception from this.DataContext");
				return;
			}

			if (alreadyBusySending)
			{
				UserMessages.ShowInfoMessage("Already busy reporting error, please be patient...");
				return;
			}
			alreadyBusySending = true;

			GetSubjectAndBodyFromException(exc, out tmpSubject, out tmpBody);

			ThreadingInterop.DoAction(delegate
			{
				bool successfullySent = false;

				try
				{
					successfullySent = SendEmailToDeveloper(tmpSubject, tmpBody);
				}
				finally
				{
					this.Dispatcher.Invoke((Action)delegate
					{
						if (successfullySent)
						{
							textblockClickToReportToDeveloper.Visibility = System.Windows.Visibility.Collapsed;
							//textblockBusySendingMessage.Visibility = System.Windows.Visibility.Collapsed;
							textblockBusySendingMessage.Text = "Successfully reported the error, the application may now be closed.";
							textblockBusySendingMessage.Foreground = Brushes.LightGreen;
						}
						alreadyBusySending = false;
					});
				}
			},
			false);
		}

		private static void GetSubjectAndBodyFromException(Exception exc, out string subject, out string body)
		{
			string thisAppname = System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			string appVersion = FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).FileVersion;
			string username = Environment.UserName;
			string computerGuid = SettingsInterop.GetComputerGuidAsString();//Note this is not the same as fingerprint for Licensing

			subject = "Unhandled exception in '" + thisAppname + "' v" + appVersion;
			body = "Username = " + username
						+ Environment.NewLine
						+ "computerGuid = " + computerGuid
						+ Environment.NewLine
						+ Environment.NewLine
						+ "Exception message: " + exc.Message
						+ Environment.NewLine
						+ Environment.NewLine
						+ "Method Name: " + exc.TargetSite
						+ Environment.NewLine
						+ Environment.NewLine
						+ "Stack Trace:"
						+ Environment.NewLine
						+ exc.StackTrace;
		}

		private static bool SendEmailToDeveloper(string subject, string body)
		{
			bool successfullySent = false;
			try
			{
				int maxRetryTimes = 3;
				int currentRetriesTimes = 0;

				List<string> errorsCaught = new List<string>();
			retrysendemail:
				errorsCaught.Clear();
				successfullySent = DeveloperCommunication.SendEmailToDeveloperViaSMTP(subject, body, false, null,
					err => errorsCaught.Add(err));
				if (!successfullySent)
				{
					if (currentRetriesTimes++ < maxRetryTimes)
					{
						Thread.Sleep(1000);//Wait a second
						goto retrysendemail;
					}
					else if (errorsCaught.Count > 0)
						UserMessages.ShowErrorMessage(string.Format("Error(s) in reporting to developer (already retried {0} times): {1}", maxRetryTimes, Environment.NewLine + string.Join(Environment.NewLine, errorsCaught)));
					else
						UserMessages.ShowErrorMessage("Unknown error sending to developer, retried already {0} times", argumentsIfMessageStringIsFormatted: maxRetryTimes.ToString());
				}
			}
			catch (Exception exc1)
			{
				//This should actually never occur
				UserMessages.ShowErrorMessage("Exception occurred inside UnhandledExceptions handler: " + exc1.Message);
			}
			return successfullySent;
		}

		private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) return;
			(sender as TextBox).SelectAll();
		}

		private static string GetApplicationName()
		{
			return Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
		}

		private static readonly string cIsAutoSendingEnabledFilePath = SettingsInterop.GetFullFilePathInLocalAppdata("AutoSendingEnabled.fjbool", GetApplicationName(), "Settings");
		private static bool GetIsAutoSendingEnabled() { return File.Exists(cIsAutoSendingEnabledFilePath); }
		private static void SetIsAutoSendingEnabled() { try { File.Create(cIsAutoSendingEnabledFilePath).Close(); } catch (Exception exc) { UserMessages.ShowErrorMessage("Could not enable auto sending of reports: " + exc.Message); } }

		public static void ShowUnHandledException(Exception exc)
		{
			if (!GetIsAutoSendingEnabled())
			{
				UnhandledExceptionsWindow uew = new UnhandledExceptionsWindow(exc);
				uew.labelMainMessage.Content = uew.labelMainMessage.Content.ToString().Replace("[ApplicationName]",
					GetApplicationName());
				uew.ShowDialog();
			}
			else
			{
				string subject, body;
				GetSubjectAndBodyFromException(exc, out subject, out body);
				SendEmailToDeveloper(subject, body);
			}
		}

		private void checkboxAutomaticallyReport_Checked(object sender, RoutedEventArgs e)
		{
			checkboxAutomaticallyReport.IsEnabled = false;
			SetIsAutoSendingEnabled();
		}

		private void storyboardCheckboxFadeout_Completed(object sender, EventArgs e)
		{
			checkboxAutomaticallyReport.Visibility = System.Windows.Visibility.Collapsed;
		}
	}

	public static class NavigationService
	{
		// Copied from http://geekswithblogs.net/casualjim/archive/2005/12/01/61722.aspx
		private static readonly Regex RE_URL = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");

		public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
				"Text",
				typeof(string),
				typeof(NavigationService),
				new PropertyMetadata(null, OnTextChanged)
		);

		public static string GetText(DependencyObject d)
		{ return d.GetValue(TextProperty) as string; }

		public static void SetText(DependencyObject d, string value)
		{ d.SetValue(TextProperty, value); }

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var text_block = d as TextBlock;
			if (text_block == null)
				return;

			text_block.Inlines.Clear();

			var new_text = (string)e.NewValue;
			if (string.IsNullOrEmpty(new_text))
				return;

			// Find all URLs using a regular expression
			int last_pos = 0;
			foreach (Match match in RE_URL.Matches(new_text))
			{
				// Copy raw string from the last position up to the match
				if (match.Index != last_pos)
				{
					var raw_text = new_text.Substring(last_pos, match.Index - last_pos);
					text_block.Inlines.Add(new Run(raw_text));
				}

				// Create a hyperlink for the match
				var link = new Hyperlink(new Run(match.Value))
				{
					NavigateUri = new Uri(match.Value)
				};
				link.Click += OnUrlClick;

				text_block.Inlines.Add(link);

				// Update the last matched position
				last_pos = match.Index + match.Length;
			}

			// Finally, copy the remainder of the string
			if (last_pos < new_text.Length)
				text_block.Inlines.Add(new Run(new_text.Substring(last_pos)));
		}

		private static void OnUrlClick(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			// Do something with link.NavigateUri
		}
	}
}