using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
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
		public static string GetHtmlFromUrl(string url, NetworkCredential credentials = null)
		{
			using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
			{
				if (credentials != null)
					client.Credentials = credentials;
				string returnString = "";
				ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
				{
					returnString = client.DownloadString(url);
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