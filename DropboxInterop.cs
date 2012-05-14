using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DropNet;
using System.Windows.Forms;

namespace SharedClasses
{
	public class DropboxInterop
	{
		public class FranDropboxClient : DropNetClient
		{

			public bool SuccessfullyAuthenticated = false;
			private WebBrowser webBrowser1;

			public FranDropboxClient(string apiKey, string appSecret)
				: base(apiKey, appSecret)
			{
				this.webBrowser1 = new WebBrowser() { ScriptErrorsSuppressed = true };
				this.UseSandbox = true;
				this.GetTokenAsync(userToken =>
				{
					var tokenUrl = this.BuildAuthorizeUrl("http://dkdevelopment.net/BoxShotLogin.htm");
					webBrowser1.Navigated += new WebBrowserNavigatedEventHandler(webBrowser1_Navigated);
					if (webBrowser1.InvokeRequired)
						webBrowser1.Invoke((Action)delegate { webBrowser1.Navigate(new Uri(tokenUrl)); });
					else
						webBrowser1.Navigate(new Uri(tokenUrl));
				},
				(error) =>
				{
					MessageBox.Show(error.Message);
				});
			}

			private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
			{
				if (e.Url.AbsolutePath == "/BoxShotLogin.htm")
				{
					this.GetAccessTokenAsync(response =>
					{
						this.SuccessfullyAuthenticated = true;
					},
					(error) =>
					{
						MessageBox.Show("Error with authentication: " + error.Message);
					});
				}
				else
				{
					HtmlElement head = webBrowser1.Document.GetElementsByTagName("head")[0];
					HtmlElement scriptEl = webBrowser1.Document.CreateElement("script");
					dynamic element = scriptEl.DomElement;
					element.text = "function clickAllow() { document.getElementsByName('allow_access')[0].click(); }";
					head.AppendChild(scriptEl);
					webBrowser1.Document.InvokeScript("clickAllow");
				}
			}
		}
	}
}