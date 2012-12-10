using System.Windows;
using Rhino.Licensing;
using System;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for RegistrationWindow.xaml
	/// </summary>
	public partial class RegistrationWindow : Window
	{
		public string publicKey;

		private RegistrationWindow()
		{
			InitializeComponent();
		}

		private bool Register()
		{
			if (string.IsNullOrWhiteSpace(textboxLicenseKeyXML.Text))
			{
				UserMessages.ShowWarningMessage("Please enter (paste) your license key into the box.");
				textboxLicenseKeyXML.Focus();
				return false;
			}

			try
			{
				new StringLicenseValidator(this.publicKey, textboxLicenseKeyXML.Text)
					.AssertValidLicense();//This will validate the license
			}
			catch (LicenseNotFoundException)// licexc)
			{
				UserMessages.ShowWarningMessage("License validation failed.");
				return false;
			}
			catch (Exception exc)
			{
				UserMessages.ShowWarningMessage("License validation failed: " + exc.Message);
				return false;
			}

			return true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (Register())
				this.DialogResult = true;
		}


		public static bool RegisterApplication(string applicationName, string publicKey, out string licenseXmlText)
		{
			RegistrationWindow win = new RegistrationWindow();
			win.publicKey = publicKey;
			win.Title = "Registration for " + applicationName;
			bool success = win.ShowDialog() == true;
			licenseXmlText = win.textboxLicenseKeyXML.Text;
			return success;
		}
	}
}
