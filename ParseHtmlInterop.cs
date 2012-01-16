using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;

namespace SharedClasses
{
	public class TorrentSearchResult: INotifyPropertyChanged
	{
		public string Name { get; set; }
		public string Uri { get; set; }
		private Visibility visibility;
		public Visibility Visibility
		{
			get { return visibility; }
			set { visibility = value; OnPropertyChanged("Visibility"); }
		}
		private Visibility loadingAnimationVisible;
		public Visibility LoadingAnimationVisible
		{
			get { return loadingAnimationVisible; }
			set { loadingAnimationVisible = value; OnPropertyChanged("LoadingAnimationVisible"); }
		}

		public TorrentSearchResult(string Name, string Uri)
		{
			this.Name = Name;
			this.Uri = Uri;
			this.Visibility = System.Windows.Visibility.Visible;
			this.LoadingAnimationVisible = System.Windows.Visibility.Hidden;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ParseHtmlInterop
	{
		public static bool SetAllowUnsafeHeaderParsing20()
		{
			//Get the assembly that contains the internal class
			Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
			if (aNetAssembly != null)
			{
				//Use the assembly in order to get the internal type for the internal class
				Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
				if (aSettingsType != null)
				{
					//Use the internal static property to get an instance of the internal settings class.
					//If the static instance isn't created allready the property will create it for us.
					object anInstance = aSettingsType.InvokeMember("Section",
						BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });

					if (anInstance != null)
					{
						//Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
						FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
						if (aUseUnsafeHeaderParsing != null)
						{
							aUseUnsafeHeaderParsing.SetValue(anInstance, true);
							return true;
						}
					}
				}
			}
			return false;
		}

		private static bool AlreadySetAllowUnsafeHeaderParsing20 = false;
		public static string GetHtmlFromUrl(string url, NetworkCredential credentials = null)
		{

			if (!AlreadySetAllowUnsafeHeaderParsing20)
			{
				if (!SetAllowUnsafeHeaderParsing20())
					UserMessages.ShowWarningMessage("Unable to set 'Allow Unsafe Header Parsing' to true, this may cause issues with network communications");
				else
					AlreadySetAllowUnsafeHeaderParsing20 = true;
			}

			using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
			{
				if (credentials != null)
					client.Credentials = credentials;
				string returnString = "";
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					try
					{
						returnString = client.DownloadString(url);
					}
					catch (Exception exc)
					{
						UserMessages.ShowErrorMessage("Error trying to obtain Html From Url: " + exc.Message);
					}
				},
				ThreadName: "GetHtmlFromUrl");
				return returnString;
			}
		}

		public static ObservableCollection<TorrentSearchResult> GetResultsForTorrentzSearch(string searchQuery)
		{
			ObservableCollection<TorrentSearchResult> tmplist = new ObservableCollection<TorrentSearchResult>();

			//string filename = @"C:\Users\francois\Documents\halo torrent search.htm";

			HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
			htmlDocument.LoadHtml(GetHtmlFromUrl("http://torrentz.eu/search?f=" + searchQuery));//File.ReadAllText(filename));
			HtmlAgilityPack.HtmlNode bodyNodes = htmlDocument.DocumentNode.ChildNodes["html"].ChildNodes["body"];
			HtmlAgilityPack.HtmlNode divSponsoredClass = bodyNodes.ChildNodes["div"];
			if (divSponsoredClass != null && (!divSponsoredClass.Attributes.Contains("class") || divSponsoredClass.Attributes["class"].Value != "results"))
			{
				divSponsoredClass = divSponsoredClass.NextSibling;
				while (divSponsoredClass != null && (divSponsoredClass.Name != "div" || !divSponsoredClass.Attributes.Contains("class") || divSponsoredClass.Attributes["class"].Value != "results"))
					divSponsoredClass = divSponsoredClass.NextSibling;
				if (divSponsoredClass != null)
				{
					foreach (HtmlAgilityPack.HtmlNode tmpDlResultNode in divSponsoredClass.ChildNodes)
						if (tmpDlResultNode.Name == "dl")
						{
							HtmlAgilityPack.HtmlNode hyperlinkToResult = tmpDlResultNode.ChildNodes["dt"].ChildNodes["a"];
							string url = hyperlinkToResult.Attributes["href"].Value;
							string name = hyperlinkToResult.InnerText.Replace("\r", "").Replace("\n", "").Replace("\t", "");
							tmplist.Add(
								new TorrentSearchResult(
									name,
									(url[0] == '/' ? "http://torrentz.eu" : "") + url
									));
						}
				}
			}

			return tmplist;
		}
	}
}