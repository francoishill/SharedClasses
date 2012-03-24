using System;
using Google.Apis.Util;
using Google.Apis.Tasks.v1;
using Google.Apis.Authentication;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Authentication.OAuth2;
using DotNetOpenAuth.OAuth2;
using System.Linq;
using System.Net;
using System.IO;
//using TestFrana.Properties;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Authentication;
using Google.Apis.Samples.Helper.Forms;
using Google.Apis.Samples.Helper;
using System.Security.Cryptography;
using System.Resources;
using Google.Apis.Plus.v1;

namespace SharedClasses
{
	public class GoogleApiInterop
	{
		public static ErrorEventHandler OnError = new ErrorEventHandler(delegate { });

		/*Additional dependencies and sample code:
		Minimum winforms
		Full framework
		Class: EncodeAndDecodeInterop
		Class: FileSystemInterop
		Class: SettingsInterop
		Class: SharedClassesSettings
		Winforms: Form: InputBox
		WPF: Window: InputBoxWPF
		WPF: Class: WPFdraggableCanvas
		File: GoogleAPIs\Google.Apis.Tasks.v1.cs
		File: GoogleAPIs\Google.Apis.Plus.v1.cs
		The following assemblies should be located in c:\francois\other\dlls\GoogleApis:
		Assembly: System.Security
		Assembly own: DotNetOpenAuth
		Assembly own: Google.Apis.Authentication.OAuth2
		Assembly own: Google.Apis
		Assembly own: Google.Apis.Samples.Helper
		Assembly own: Newtonsoft.JSon.Net35

		//Sample code for using this class:
		//TasksService and PlusService
		private void TestTaskList()
		{
			//Tasks
			TasksService Service = GoogleApiInterop.CreateNewTasksService(err => MessageBox.Show("Error: " + err));
			foreach (TaskList tasklist in Service.Tasklists.List().Fetch().Items)
			{
				var tasksInList = Service.Tasks.List(tasklist.Id).Fetch();

				Console.WriteLine(tasklist.Title);
				foreach (Task task in tasksInList.Items)
					Console.WriteLine("\task" + task.Title);
			}
		 
			//Plus
			PlusService plusService = GoogleApiInterop.CreateNewPlusService(err => MessageBox.Show("Error: " + err));
			ActivitiesResource.ListRequest listreq = plusService.Activities.List("111454751750373519745", ActivitiesResource.Collection.Public);
			listreq.MaxResults = 100;
			listreq.Alt = ActivitiesResource.Alt.Json;
			try
			{
				string nextpage = null;
				Google.Apis.Plus.v1.Data.ActivityFeed feed;
				while (true)
				{
					feed = listreq.Fetch();

					if (nextpage == feed.NextPageToken)
						break;

					if (nextpage == null)
						nextpage = feed.NextPageToken;
					foreach (var act in feed.Items)
						MessageBox.Show(act.Title);
				}
			}
			catch (Exception exc)
			{
				Console.WriteLine("Exception: " + exc.Message);
			}
		}
		*/

		public enum ServiceType { Tasks, Plus };

		public static TasksService CreateNewTasksService(Action<string> ActionOnError)
		{
			OnError += (s, e) => { ActionOnError(e.GetException().Message); };
			return new TasksService(CreateAuthenticator(ServiceType.Tasks));
		}

		public static PlusService CreateNewPlusService(Action<string> ActionOnError)
		{
			OnError += (s, e) => { ActionOnError(e.GetException().Message); };
			return new PlusService(CreateAuthenticator(ServiceType.Plus));
		}

		private static IAuthenticator CreateAuthenticator(ServiceType serviceType)
		{
			var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description);
			provider.ClientIdentifier = ClientCredentials.ClientID;
			provider.ClientSecret = ClientCredentials.ClientSecret;
			if (serviceType == ServiceType.Tasks)
				return new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization_Tasks);
			else if (serviceType == ServiceType.Plus)
				return new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization_Plus);
			else
				throw new Exception("Cannot use undefined service type: " + serviceType.ToString());
		}

		private static IAuthorizationState GetAuthorization_Tasks(NativeApplicationClient client) { return GetAuthorization(client, ServiceType.Tasks); }
		private static IAuthorizationState GetAuthorization_Plus(NativeApplicationClient client) { return GetAuthorization(client, ServiceType.Plus); }

		private static IAuthorizationState GetAuthorization(NativeApplicationClient client, ServiceType serviceType)
		{
			// You should use a more secure way of storing the key here as
			// .NET applications can be disassembled using a reflection tool.
			//const
			string STORAGE = "google.samples.dotnet."
				+ (
				serviceType == ServiceType.Tasks ? "tasksInList" :
				serviceType == ServiceType.Plus ? "activities" :
				""
				);
			const string KEY = "y},drdzf11x9;87";
			string scope =
				serviceType == ServiceType.Tasks ? TasksService.Scopes.Tasks.GetStringValue() :
				serviceType == ServiceType.Plus ? PlusService.Scopes.PlusMe.GetStringValue() + "+" + PlusService.Scopes.UserinfoEmail.GetStringValue() :
				"";

			// Check if there is a cached refresh token available.
			IAuthorizationState state = AuthorizationMgr.GetCachedRefreshToken(STORAGE, KEY);
			if (state != null)
			{
				try
				{
					client.RefreshToken(state);
					return state; // Yes - we are done.
				}
				catch (DotNetOpenAuth.Messaging.ProtocolException ex)
				{
					//CommandLine.WriteError("Using existing refresh token failed: " + ex.Message);
					OnError(null, new ErrorEventArgs(new Exception("Using existing refresh token failed: " + ex)));
				}
			}

			// Retrieve the authorization from the user.
			state = AuthorizationMgr.RequestNativeAuthorization(client, scope);
			AuthorizationMgr.SetCachedRefreshToken(STORAGE, KEY, state);
			return state;
		}
	}

	/// <summary>
	/// This class provides the client credentials for all the samples in this solution.
	/// In order to run all of the samples, you have to enable API access for every API 
	/// you want to use, enter your credentials here.
	/// 
	/// You can find your credentials here:
	///  https://code.google.com/apis/console/#:access
	/// 
	/// For your own application you should find a more secure way than just storing your client secret inside a string,
	/// as it can be lookup up easily using a reflection tool.
	/// </summary>
	internal static class ClientCredentials
	{
		/// <summary>
		/// The OAuth2.0 Client ID of your project.
		/// </summary>
		public static string ClientID { get { return SharedClasses.GlobalSettings.GoogleApiInteropSettings.Instance.ClientID; } }

		/// <summary>
		/// The OAuth2.0 Client secret of your project.
		/// </summary>
		public static string ClientSecret { get { return EncodeAndDecodeInterop.DecodeString(GlobalSettings.GoogleApiInteropSettings.Instance.ClientSecretEncrypted, GenericSettings.EncodingType); } }

		/// <summary>
		/// Your Api/Developer key.
		/// </summary>
		public static string ApiKey { get { return SharedClasses.GlobalSettings.GoogleApiInteropSettings.Instance.ApiKey; } }

		#region Verify Credentials
		static ClientCredentials()
		{
			ReflectionUtils.VerifyCredentials(typeof(ClientCredentials));
		}
		#endregion
	}

	/// <summary>
	/// Reflection Helper
	/// </summary>
	public static class ReflectionUtils
	{
		/// <summary>
		/// Tries to return a descriptive name for the specified member info. 
		/// Uses the DescriptionAttribute if available.
		/// </summary>
		/// <returns>Description from DescriptionAttriute or name of the MemberInfo</returns>
		public static string GetDescriptiveName(MemberInfo info)
		{
			// If available: Return the description set in the DescriptionAttribute
			foreach (DescriptionAttribute attribute in info.GetCustomAttributes(typeof(DescriptionAttribute), true))
			{
				return attribute.Description;
			}

			// Otherwise: Return the name of the member
			return info.Name;
		}

		/// <summary>
		/// Selects all type members from the collection which have the specified argument.
		/// </summary>
		/// <typeparam name="TMemberInfo">The type of the member the collection is made of.</typeparam>
		/// <typeparam name="TAttribute">The attribute to look for.</typeparam>
		/// <param name="collection">The collection select from.</param>
		/// <returns>Only the TypeMembers which haev the specified argument defined.</returns>
		public static IEnumerable<KeyValuePair<TMemberInfo, TAttribute>> WithAttribute<TMemberInfo, TAttribute>(
			this IEnumerable<TMemberInfo> collection)
			where TAttribute : Attribute
			where TMemberInfo : MemberInfo
		{
			Type attributeType = typeof(TAttribute);
			return from TMemberInfo info in collection
				   let attribute = info.GetCustomAttributes(attributeType, true).SingleOrDefault() as TAttribute
				   where attribute != null
				   select new KeyValuePair<TMemberInfo, TAttribute>(info, attribute);
		}

		/// <summary>
		/// Returns the value of the static field specified by the given name, 
		/// or the default(T) if the field is not found.
		/// </summary>
		/// <typeparam name="T">The type of the field.</typeparam>
		/// <param name="type">The type containing the field.</param>
		/// <param name="fieldName">The name of the field.</param>
		/// <returns>The value of this field.</returns>
		public static T GetStaticField<T>(Type type, string fieldName)
		{
			var field = type.GetField(fieldName);
			if (field == null)
			{
				return default(T);
			}
			return (T)field.GetValue(null);
		}

		/// <summary>
		/// Verifies that the ClientID/ClientSecret/DeveloperKey is set in the specified class.
		/// </summary>
		/// <param name="type">ClientCredentials.cs class.</param>
		public static void VerifyCredentials(Type type)
		{
			var regex = new Regex("<.+>");

			var errors = (from fieldName in new[] { "ClientID", "ClientSecret", "ApiKey", "BucketPath" }
						  let field = GetStaticField<string>(type, fieldName)
						  where field != null && regex.IsMatch(field)
						  select "- " + fieldName + " is currently not set.").ToList();

			if (errors.Count > 0)
			{
				errors.Insert(0, "Please modify the ClientCredentials.cs:");
				errors.Add("You can find this information on the Google API Console.");
				string msg = String.Join(Environment.NewLine, errors.ToArray());
				//CommandLine.WriteError(msg);
				GoogleApiInterop.OnError(null, new ErrorEventArgs(new Exception(msg)));
				//TODO: Show message to enter credentials
				//MessageBox.Show(msg, "Please enter your credentials!", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(0);
			}
		}
	}

	/// <summary>
	/// Authorization helper for Native Applications.
	/// </summary>
	internal static class AuthorizationMgr
	{
		private static readonly INativeAuthorizationFlow[] NativeFlows = new INativeAuthorizationFlow[]
                                                                        {
                                                                            new LoopbackServerAuthorizationFlow(), 
                                                                            new WindowTitleNativeAuthorizationFlow() 
                                                                        };

		/// <summary>
		/// Requests authorization on a native client by using a predefined set of authorization flows.
		/// </summary>
		/// <param name="client">The client used for authentication.</param>
		/// <param name="authState">The requested authorization state.</param>
		/// <returns>The authorization code, or null if cancelled by the user.</returns>
		/// <exception cref="NotSupportedException">Thrown if no supported flow was found.</exception>
		public static string RequestNativeAuthorization(NativeApplicationClient client, IAuthorizationState authState)
		{
			// Try each available flow until we get an authorization / error.
			foreach (INativeAuthorizationFlow flow in NativeFlows)
			{
				try
				{
					return flow.RetrieveAuthorization(client, authState);
				}
				catch (NotSupportedException) { /* Flow unsupported on this environment */ }
			}

			throw new NotSupportedException("Found no supported native authorization flow.");
		}

		/// <summary>
		/// Requests authorization on a native client by using a predefined set of authorization flows.
		/// </summary>
		/// <param name="client">The client used for authorization.</param>
		/// <param name="scopes">The requested set of scopes.</param>
		/// <returns>The authorized state.</returns>
		/// <exception cref="AuthenticationException">Thrown if the request was cancelled by the user.</exception>
		public static IAuthorizationState RequestNativeAuthorization(NativeApplicationClient client,
																	 params string[] scopes)
		{
			IAuthorizationState state = new AuthorizationState(scopes);
			string authCode = RequestNativeAuthorization(client, state);

			if (string.IsNullOrEmpty(authCode))
			{
				throw new AuthenticationException("The authentication request was cancelled by the user.");
			}

			return client.ProcessUserAuthorization(authCode, state);
		}

		/// <summary>
		/// Returns a cached refresh token for this application, or null if unavailable.
		/// </summary>
		/// <param name="storageName">The file name (without extension) used for storage.</param>
		/// <param name="key">The key to decrypt the data with.</param>
		/// <returns>The authorization state containing a Refresh Token, or null if unavailable</returns>
		public static AuthorizationState GetCachedRefreshToken(string storageName,
															   string key)
		{
			string file = storageName + ".auth";
			byte[] contents = AppData.ReadFile(file);

			if (contents == null)
			{
				return null; // No cached token available.
			}

			byte[] salt = Encoding.Unicode.GetBytes(Assembly.GetEntryAssembly().FullName + key);
			byte[] decrypted = ProtectedData.Unprotect(contents, salt, DataProtectionScope.CurrentUser);
			string[] content = Encoding.Unicode.GetString(decrypted).Split(new[] { "\r\n" }, StringSplitOptions.None);

			// Create the authorization state.
			string[] scopes = content[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string refreshToken = content[1];
			return new AuthorizationState(scopes) { RefreshToken = refreshToken };
		}

		/// <summary>
		/// Saves a refresh token to the specified storage name, and encrypts it using the specified key.
		/// </summary>
		public static void SetCachedRefreshToken(string storageName,
												 string key,
												 IAuthorizationState state)
		{
			// Create the file content.
			string scopes = state.Scope.Aggregate("", (left, append) => left + " " + append);
			string content = scopes + "\r\n" + state.RefreshToken;

			// Encrypt it.
			byte[] salt = Encoding.Unicode.GetBytes(Assembly.GetEntryAssembly().FullName + key);
			byte[] encrypted = ProtectedData.Protect(
				Encoding.Unicode.GetBytes(content), salt, DataProtectionScope.CurrentUser);

			// Save the data to the auth file.
			string file = storageName + ".auth";
			AppData.WriteFile(file, encrypted);
		}
	}

	/// <summary>
	/// An authorization flow is the process of obtaining an AuthorizationCode 
	/// when provided with an IAuthorizationState.
	/// </summary>
	internal interface INativeAuthorizationFlow
	{
		/// <summary>
		/// Retrieves the authorization of the user for the given AuthorizationState.
		/// </summary>
		/// <param name="client">The client used for authentication.</param>
		/// <param name="authorizationState">The state requested.</param>
		/// <returns>The authorization code, or null if the user cancelled the request.</returns>
		/// <exception cref="NotSupportedException">Thrown if this flow is not supported.</exception>
		string RetrieveAuthorization(UserAgentClient client, IAuthorizationState authorizationState);
	}

	/// <summary>
	/// A native authorization flow which uses a listening local loopback socket to fetch the authorization code.
	/// </summary>
	/// <remarks>Might not work if blocked by the system firewall.</remarks>
	public class LoopbackServerAuthorizationFlow : INativeAuthorizationFlow
	{
		private const string LoopbackCallback = "http://localhost:{0}/{1}/authorize/";

		/// <summary>
		/// Returns a random, unused port.
		/// </summary>
		private static int GetRandomUnusedPort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			try
			{
				listener.Start();
				return ((IPEndPoint)listener.LocalEndpoint).Port;
			}
			finally
			{
				listener.Stop();
			}
		}

		/// <summary>
		/// Handles an incoming WebRequest.
		/// </summary>
		/// <param name="context">The request to handle.</param>
		/// <param name="appName">Name of the application handling the request.</param>
		/// <returns>The authorization code, or null if the process was cancelled.</returns>
		private string HandleRequest(HttpListenerContext context)
		{
			try
			{
				// Check whether we got a successful response:
				string code = context.Request.QueryString["code"];
				if (!string.IsNullOrEmpty(code))
				{
					return code;
				}

				// Check whether we got an error response:
				string error = context.Request.QueryString["error"];
				if (!string.IsNullOrEmpty(error))
				{
					return null; // Request cancelled by user.
				}

				// The response is unknown to us. Choose a different authentication flow.
				throw new NotSupportedException(
					"Received an unknown response: " + Environment.NewLine + context.Request.RawUrl);
			}
			finally
			{
				// Write a response.
				using (var writer = new StreamWriter(context.Response.OutputStream))
				{
					string response = GetLoopbackServerHtmlResponse().Replace("{APP}", Util.ApplicationName);
					writer.WriteLine(response);
					writer.Flush();
				}
				context.Response.OutputStream.Close();
				context.Response.Close();
			}
		}

		private static string GetLoopbackServerHtmlResponse()
		{
			//return Resources.LoopbackServerHtmlResponse;
			return
@"<html>
  <head>
    <title>{APP} - OAuth Authentication</title>
  </head>
  <body>
    <h1>Authorization for {APP}</h1>
    <p>The application has received your response. You can close this window now.</p>
    <script type='text/javascript'>
      window.setTimeout(function() { window.open('', '_self', ''); window.close(); }, 100);
      if (window.opener) { window.opener.checkToken(); }
     </script>
  </body>
</html>";
		}

		public string RetrieveAuthorization(UserAgentClient client, IAuthorizationState authorizationState)
		{
			if (!HttpListener.IsSupported)
			{
				throw new NotSupportedException("HttpListener is not supported by this platform.");
			}

			// Create a HttpListener for the specified url.
			string url = string.Format(LoopbackCallback, GetRandomUnusedPort(), Util.ApplicationName);
			authorizationState.Callback = new Uri(url);
			var webserver = new HttpListener();
			webserver.Prefixes.Add(url);

			// Retrieve the authorization url.
			Uri authUrl = client.RequestUserAuthorization(authorizationState);

			try
			{
				// Start the webserver.
				webserver.Start();

				// Open the browser.
				Process.Start(authUrl.ToString());

				// Wait for the incoming connection, then handle the request.
				return HandleRequest(webserver.GetContext());
			}
			catch (HttpListenerException ex)
			{
				throw new NotSupportedException("The HttpListener threw an exception.", ex);
			}
			finally
			{
				// Stop the server after handling the one request.
				webserver.Stop();
			}
		}
	}

	/// <summary>
	/// Describes a flow which captures the authorization code out of the window title of the browser.
	/// </summary>
	/// <remarks>Works on Windows, but not on Unix. Will failback to copy/paste mode if unsupported.</remarks>
	internal class WindowTitleNativeAuthorizationFlow : INativeAuthorizationFlow
	{
		private const string OutOfBandCallback = "urn:ietf:wg:oauth:2.0:oob";

		public string RetrieveAuthorization(UserAgentClient client, IAuthorizationState authorizationState)
		{
			// Create the Url.
			authorizationState.Callback = new Uri(OutOfBandCallback);
			Uri url = client.RequestUserAuthorization(authorizationState);

			// Show the dialog.
			//if (!Application.RenderWithVisualStyles)
			//{
			//    Application.EnableVisualStyles();
			//}

			//Application.DoEvents();
			string authCode = OAuth2AuthorizationDialog.ShowDialog(url);
			//Application.DoEvents();

			if (string.IsNullOrEmpty(authCode))
			{
				return null; // User cancelled the request.
			}

			return authCode;
		}
	}
}