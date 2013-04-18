using System;

namespace SharedClasses
{
	public interface IWpfApp
	{
		string ApplicationName { get; }
		void RunApp();
		bool CloseApp(out string reasonIfNotAbleToClose);
	}
}