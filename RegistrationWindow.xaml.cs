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
		public string applicationName;

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

		private bool UpdateUniqueSignature()
		{
			if (string.IsNullOrWhiteSpace(textboxOwnerName.Text))
			{
				UserMessages.ShowWarningMessage("Please enter an owner name first.");
				textboxOwnerName.Focus();
				return false;
			}

			textboxUniqueSignature.Text = LicensingInterop_Shared.GetMachineUIDForApplicationAndMachine(
				   applicationName,
				   textboxOwnerName.Text,
				   LicensingInterop_Client.GetThisPcMachineSignature(),
				   err => UserMessages.ShowErrorMessage(err));
			return true;
		}

		private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			UpdateUniqueSignature();
		}

		public static bool RegisterApplication(string applicationName, string publicKey, out string licenseXmlText)
		{
			RegistrationWindow win = new RegistrationWindow();
			win.publicKey = publicKey;
			win.applicationName = applicationName;
			win.Title = "Registration for " + applicationName;
			//win.textboxMachineSignature.Text = LicensingInterop_Client.GetUniqueSignatureForApplicationAndMachine(
			//applicationName,				);
			bool success = win.ShowDialog() == true;
			if (success)
				licenseXmlText = win.textboxLicenseKeyXML.Text;
			else
				licenseXmlText = null;
			return success;
		}

		private string EncodeStringForMailto(string str)
		{
			return str
				.Replace(" ", "%20")
				.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "%0d%0a");
		}
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if (UpdateUniqueSignature())
			{
				DeveloperCommunication.RunMailto("Please provide license XML", EncodeStringForMailto("Unique signature:\r\n\r\n" + textboxUniqueSignature.Text));
			}
		}
	}
}
