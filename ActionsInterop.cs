using System;

namespace SharedClasses
{
	public static class ActionsInterop
	{
		public static bool DoAllActionsAndHandleError(Action<string> onError, params Action[] actions)
		{
			bool hasError = false;
			foreach (var act in actions)
			{
				try
				{
					act();
				}
				catch (Exception exc)
				{
					if (onError != null)
						onError(exc.Message);
					hasError = true;
				}
			}
			return !hasError;
		}

		public static bool DoActionsAndHandleError_StopOnFirstFail(Action<string, int> onErrorActionNum, params Action[] actions)
		{
			int count = 0;
			if (onErrorActionNum == null)
				onErrorActionNum = delegate { };
			foreach (var act in actions)
			{
				try
				{
					act();
					count++;
				}
				catch (Exception exc)
				{
					onErrorActionNum(exc.Message, count);
					return false;
				}
			}
			return true;
		}
	}
}