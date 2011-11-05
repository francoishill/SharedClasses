using UriProtocol = VisualStudioInteropSettings.UriProtocol;

public class SharedClassesSettings
{
	public static VisualStudioInteropSettings visualStudioInterop { get; set; }
	private static string RootApplicationNameForSharedClasses = "SharedClasses";

	public static void EnsureAllSharedClassesSettingsNotNullCreateDefault()
	{
		string VisualStudioSettingsFileName = "VisualStudioInteropSettings.xml";
		if (SharedClassesSettings.visualStudioInterop == null)
			SharedClassesSettings.visualStudioInterop = SettingsInterop.GetSettings<VisualStudioInteropSettings>(
				VisualStudioSettingsFileName,
				RootApplicationNameForSharedClasses);
		if (SharedClassesSettings.visualStudioInterop == null)
			SharedClassesSettings.visualStudioInterop = new VisualStudioInteropSettings();
		SettingsInterop.FlushSettings<VisualStudioInteropSettings>(
			SharedClassesSettings.visualStudioInterop,
			VisualStudioSettingsFileName,
			RootApplicationNameForSharedClasses);
	}
}

public class VisualStudioInteropSettings
{
	public enum UriProtocol { Http, Ftp }

	public string BaseUri { get; set; }
	public string RelativeRootUriAFTERvspublishing { get; set; }
	public UriProtocol? UriProtocolForAFTERvspublishing { get; set; }
	public string RelativeRootUriForVsPublishing { get; set; }
	public UriProtocol? UriProtocolForVsPublishing { get; set; }

	public VisualStudioInteropSettings()
	{
		BaseUri = "127.0.0.1";
		RelativeRootUriAFTERvspublishing = "/ownapplications";
		UriProtocolForAFTERvspublishing = UriProtocol.Http;
		RelativeRootUriForVsPublishing = "/francois/websites/firepuma/ownapplications";
		UriProtocolForVsPublishing = UriProtocol.Ftp;
	}
	public VisualStudioInteropSettings(string BaseUriIn, string RelativeRootUriAFTERvspublishingIn, UriProtocol UriProtocolForAFTERvspublishingIn, string RelativeRootUriForVsPublishingIn, UriProtocol UriProtocolForVsPublishingIn)
	{
		BaseUri = BaseUriIn;
		RelativeRootUriAFTERvspublishing = RelativeRootUriAFTERvspublishingIn;
		UriProtocolForAFTERvspublishing = UriProtocolForAFTERvspublishingIn;
		RelativeRootUriForVsPublishing = RelativeRootUriForVsPublishingIn;
		UriProtocolForVsPublishing = UriProtocolForVsPublishingIn;
	}

	public string GetCombinedUriForAFTERvspublishing()
	{
		return UriProtocolForAFTERvspublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriAFTERvspublishing;
	}

	public string GetCombinedUriForVsPublishing()
	{
		return UriProtocolForVsPublishing.ToString().ToLower() + "://" + BaseUri + RelativeRootUriForVsPublishing;
	}
}