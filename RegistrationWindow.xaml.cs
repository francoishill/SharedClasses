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
		//public string publicKey;
		public string applicationName;
		private string currentUniqueSignature;
		private string licenseKeyObtainedOnline = null;
		private string publicKeyObtainedOnline = null;

		private RegistrationWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(textboxOwnerEmail.Text)
				&& !string.IsNullOrWhiteSpace(textboxOrderCode.Text))//Probably the user used the Url Protocol link from his Order Email
			{
				string errorIfFailed;
				if (Register(out errorIfFailed))
					this.DialogResult = true;
				else
					UserMessages.ShowErrorMessage(errorIfFailed);
			}
		}

		private bool Register(out string errorIfFailed)
		{
			buttonRegister.IsEnabled = false;
			buttonRegister.UpdateLayout();

			try
			{
				if (!UpdateUniqueSignature(out errorIfFailed))
				{
					return false;
				}

				if (!LicensingInterop_Client.GetLicenseAndPublicKeyFromLicenseServer(currentUniqueSignature, out licenseKeyObtainedOnline, out publicKeyObtainedOnline))
				{
					errorIfFailed = licenseKeyObtainedOnline;
					licenseKeyObtainedOnline = null;
					publicKeyObtainedOnline = null;
					return false;
				}

				/*if (string.IsNullOrWhiteSpace(textboxLicenseKeyXML.Text))
				{
					UserMessages.ShowWarningMessage("Please enter (paste) your license key into the box.");
					textboxLicenseKeyXML.Focus();
					return false;
				}*/

				try
				{
					new StringLicenseValidator(this.publicKeyObtainedOnline, licenseKeyObtainedOnline)//textboxLicenseKeyXML.Text)
						.AssertValidLicense();//This will validate the license
				}
				catch (LicenseNotFoundException)// licexc)
				{
					errorIfFailed = "License validation failed.";
					return false;
				}
				catch (Exception exc)
				{
					errorIfFailed = "License validation failed: " + exc.Message;
					return false;
				}

				errorIfFailed = null;
				return true;
			}
			finally
			{
				buttonRegister.IsEnabled = true;
			}
		}

		private void buttonRegister_Click(object sender, RoutedEventArgs e)
		{
			string errorIfFailed;
			if (Register(out errorIfFailed))
				this.DialogResult = true;
			else
				UserMessages.ShowErrorMessage(errorIfFailed);
		}

		private bool UpdateUniqueSignature(out string errorIfFailed)
		{
			if (string.IsNullOrWhiteSpace(textboxOwnerEmail.Text))
			{
				errorIfFailed = "Please enter an owner email first.";
				textboxOwnerEmail.Focus();
				return false;
			}

			//We reversed the code (in php) in the email sent to the user after paypal purchase (this is just a safety measure).
			//We KEEP it reversed and pass it to the License server, he will unreverse it and check it against our firepuma db
			string reversedOrderCode = textboxOrderCode.Text;

			currentUniqueSignature = LicensingInterop_Shared.GetMachineUIDForApplicationAndMachine(
				   applicationName,
				   textboxOwnerEmail.Text,
				   reversedOrderCode,
				   LicensingInterop_Client.GetThisPcMachineSignature(),
				   err => UserMessages.ShowErrorMessage(err));
			errorIfFailed = null;
			return true;
		}

		private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			//Do not dynamically change it, only when we click Register
			//UpdateUniqueSignature();
		}

		public static bool RegisterApplication(string applicationName, out string licenseXmlText, out string publicKey, string predefinedEmail = null, string predefinedOrdercode = null)
		{
			RegistrationWindow win = new RegistrationWindow();
			//win.publicKey = publicKey;
			win.applicationName = applicationName;
			win.Title = "Registration for " + applicationName;
			//win.textboxMachineSignature.Text = LicensingInterop_Client.GetUniqueSignatureForApplicationAndMachine(
			//applicationName,				);

			if (!string.IsNullOrWhiteSpace(predefinedEmail))
				win.textboxOwnerEmail.Text = predefinedEmail;
			if (!string.IsNullOrWhiteSpace(predefinedOrdercode))
				win.textboxOrderCode.Text = predefinedOrdercode;

			bool success = win.ShowDialog() == true;
			if (success)
			{
				licenseXmlText = win.licenseKeyObtainedOnline;
				publicKey = win.publicKeyObtainedOnline;
			}
			else
			{
				licenseXmlText = null;
				publicKey = null;
			}
			return success;
		}

		private string EncodeStringForMailto(string str)
		{
			return str
				.Replace(" ", "%20")
				.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n", "%0d%0a");
		}
		//private void Button_Click_1(object sender, RoutedEventArgs e)
		//{
		//    if (UpdateUniqueSignature())
		//    {
		//        DeveloperCommunication.RunMailto("Please provide license XML", EncodeStringForMailto("Unique signature:\r\n\r\n" + currentUniqueSignature.Text));
		//    }
		//}
	}
}
