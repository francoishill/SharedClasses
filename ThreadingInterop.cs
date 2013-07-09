﻿using System;
using System.Threading;
using System.Windows.Forms;

public class ThreadingInterop
{
	public static bool ForceExitAllTreads = false;

	private static bool AlreadyAttachedToApplicationExitEvent = false;
	//Have a look at this function (automatically queues to a thread) - System.Threading.ThreadPool.QueueUserWorkItem()
	//PerformVoidFunctionSeperateThread(() => { MessageBox.Show("Test"); MessageBox.Show("Test1"); });
	public static Thread PerformOneArgFunctionSeperateThread(Action<object> action, object arg, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true, ApartmentState? apartmentState = null)
	{
		if (AttachForceExitToFormClose)
			if (!AlreadyAttachedToApplicationExitEvent)
			{
				if (Application.OpenForms.Count > 0)
					Application.OpenForms[0].FormClosing += delegate
					{
						ForceExitAllTreads = true;
					};
				//Application.ApplicationExit += delegate
				//{
				//  ForceExitAllTreads = true;
				//};
				AlreadyAttachedToApplicationExitEvent = true;
			}

		Thread th = new Thread(() =>
		{
			if (CheckInvokeRequired && controlToCheckInvokeRequired != null)
				controlToCheckInvokeRequired.Invoke((Action)delegate { action(arg); });
			else
				action(arg);
		});
		th.Name = ThreadName;
		th.IsBackground = true;
		if (apartmentState.HasValue)
			th.SetApartmentState(apartmentState.Value);
		th.Start();
		//th.Join();
		if (WaitUntilFinish)
		{
			while (th.IsAlive && !ForceExitAllTreads) { Application.DoEvents(); }
			//th.Join();Cannot use this, makes QuickAccess not work (window does not want to show when clicking on tray icon)
			th.Abort();
			th = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			return null;
		}
		else
		{
			Application.DoEvents();
			return th;
		}
	}
	//public static Thread PerformOneArgFunctionSeperateThread<T>(T arg, Action<T> action, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true, ApartmentState? apartmentState = null)
	//{
	//    return PerformOneArgFunctionSeperateThread(action, arg, WaitUntilFinish, ThreadName, CheckInvokeRequired, controlToCheckInvokeRequired, AttachForceExitToFormClose, apartmentState);
	//}
	public static Thread DoAction(MethodInvoker method, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true, ApartmentState? apartmentState = null)
	{
		return PerformOneArgFunctionSeperateThread(
			(Action<object>)delegate(object arg) { method(); },
			null,
			WaitUntilFinish,
			ThreadName,
			CheckInvokeRequired,
			controlToCheckInvokeRequired,
			AttachForceExitToFormClose,
			apartmentState);
	}
	public static Thread PerformVoidFunctionSeperateThread(MethodInvoker method, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true, ApartmentState? apartmentState = null)
	{
		return PerformOneArgFunctionSeperateThread(
			(Action<object>)delegate(object arg) { method(); },
			null,
			WaitUntilFinish,
			ThreadName,
			CheckInvokeRequired,
			controlToCheckInvokeRequired,
			AttachForceExitToFormClose,
			apartmentState);
	}
	public static Thread PerformOneArgFunctionSeperateThread<ArgType>(Action<ArgType> action, object arg, bool WaitUntilFinish = true, string ThreadName = "UnknownName", bool CheckInvokeRequired = false, Control controlToCheckInvokeRequired = null, bool AttachForceExitToFormClose = true, ApartmentState? apartmentState = null)
	{
		return PerformOneArgFunctionSeperateThread((obj) => action((ArgType)obj), arg, WaitUntilFinish, ThreadName, CheckInvokeRequired, controlToCheckInvokeRequired, AttachForceExitToFormClose, apartmentState);
	}

	public static void UpdateGuiFromThread(Control controlToUpdate, Action action)
	{
		if (controlToUpdate == null) return;
		if (controlToUpdate.InvokeRequired)
		{
			try
			{
				controlToUpdate.Invoke(action);//, new object[] { });
			}
			catch { }
		}
		else
		{
			try
			{
				action();
			}
			catch { }
		}
	}

	delegate void AutocompleteCallback(ComboBox txtBox, String text);
	delegate void ClearAutocompleteCallback(ComboBox txtBox);
	public static void ClearTextboxAutocompleteCustomSource(ComboBox txtBox)
	{
		if (txtBox.InvokeRequired)
		{
			ClearAutocompleteCallback d = new ClearAutocompleteCallback(ClearTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Clear();
		}
	}

	public static void AddTextboxAutocompleteCustomSource(ComboBox txtBox, string textToAdd)
	{
		if (txtBox.InvokeRequired)
		{
			AutocompleteCallback d = new AutocompleteCallback(AddTextboxAutocompleteCustomSource);
			txtBox.Invoke(d, new object[] { txtBox, textToAdd });
		}
		else
		{
			txtBox.AutoCompleteCustomSource.Add(textToAdd);
		}
	}

	public static bool ActionWithTimeout(Action action, int timeoutMilliseconds, Action<string> actionOnError)
	{
		return ActionWithTimeout<string>(delegate { action(); }, timeoutMilliseconds, "", actionOnError);
	}

	public static bool ActionWithTimeout<T>(Action<T> action, int timeoutMilliseconds, T arg, Action<string> actionOnError)
	{
		bool succeeded = false;
		Action wrappedAction = () =>
		{
			try
			{
				action(arg);
				succeeded = true;
			}
			catch (Exception ex)
			{
				actionOnError(ex.Message);
			}
		};

		IAsyncResult result = wrappedAction.BeginInvoke(null, null);

		if (result.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
		{
			//It did not timeout but the action completed before the duration
			wrappedAction.EndInvoke(result);
			return succeeded;
		}
		//The action timed out
		return false;
	}

	public static void ActionAfterDelay(Action action, TimeSpan delay, Action<string> actionOnError)
	{
		var timer = new System.Windows.Forms.Timer();
		timer.Interval = (int)delay.TotalMilliseconds;
		timer.Tag = action;
		timer.Tick += (snder, evt) =>
		{
			try
			{
				var snderAsTimer = snder as System.Windows.Forms.Timer;
				snderAsTimer.Stop();//Because we must only fire this once
				((Action)snderAsTimer.Tag)();
				snderAsTimer.Dispose();
				snderAsTimer = null;
			}
			catch (Exception exc)
			{
				actionOnError(exc.Message);
			}
		};
		timer.Start();

		/*new System.Threading.Timer(
			(actionPassedAsState) =>
			{
				try
				{
					((Action)actionPassedAsState)();
				}
				catch (Exception exc)
				{
					actionOnError(exc.Message);
				}
			},
			action,
			delay,
			TimeSpan.FromMilliseconds(-1));*/
	}

	public static bool RetryActionIfFails(Action action, int maximumRetryCount, out Exception exceptionIfFailed)
	{
		int currentRetryCount = 0;
		retryhere:
		try
		{
			action();
			exceptionIfFailed = null;
			return true;
		}
		catch (Exception exc)
		{
			if (currentRetryCount++ < maximumRetryCount)
				goto retryhere;

			exceptionIfFailed = exc;
			return false;
		}
	}
}