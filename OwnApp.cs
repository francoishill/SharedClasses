using System;

namespace SharedClasses
{
	public interface IOwnApp
	{
		void ApplicationEntryPoint();
	}

	/*public abstract class OwnApp
	{
		public abstract void ApplicationEntryPoint();
	}*/

	public class WpfApp : System.Windows.Application, IOwnApp
	{
		/// <summary>
		/// We explicitly make the set; accessor private, so we cannot set it in App.xaml
		/// </summary>
		public new Uri StartupUri { get; private set; }

		protected override void OnStartup(System.Windows.StartupEventArgs e)
		{
			//System.Windows.MessageBox.Show("Hallo");
			AutoUpdating.CheckForUpdates_ExceptionHandler();
		}

		public void ApplicationEntryPoint()
		{
		}
	}

	public sealed class WinformsApp : IOwnApp
	{
		public void ApplicationEntryPoint()
		{
			throw new NotImplementedException();
		}
	}

	public sealed class ConsoleApp : IOwnApp
	{
		public void ApplicationEntryPoint()
		{
			throw new NotImplementedException();
		}
	}
}