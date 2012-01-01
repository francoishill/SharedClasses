using System;
using System.Diagnostics;
namespace SharedClasses
{
	public class DebugInterop
	{
		[Conditional("debug")]// only possible for void methods
		public static void Assert(bool ok, string errorMsg)
		{
			if (!ok)
			{
				Console.WriteLine(errorMsg);
				System.Environment.Exit(0);// graceful program termination
			}
		}
	}
}