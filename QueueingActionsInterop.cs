using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
public class QueueingActionsInterop
{
	//TODO: Maybe later on have a look at thread queueing (System.Threading.ThreadPool.QueueUserWorkItem()) or even WaitHandle in System.Threading

	private class CustomQueue : Queue<Action>
	{
		static object s_Lock = new object();        // needed because the counter is accessed from multiple threads 
		static AutoResetEvent s_TestFinished = new AutoResetEvent(false);        // signals when the test is finished 

		public new void Enqueue(Action item)
		{
			base.Enqueue(item);
			EnsureQueueIsProcessing();
		}

		private void EnsureQueueIsProcessing()
		{
			while (this.Count > 0)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(InvokeAction));
				s_TestFinished.WaitOne();        // we wait until the last task has finished 
			}
		}

		private void InvokeAction(object state)
		{
			// This method executes on a threadpool thread
			//lock (s_Lock)
			{
				this.Dequeue().Invoke();
				s_TestFinished.Set();
			}
		}
	}

	private static CustomQueue QueuedActions = new CustomQueue();

	public static void EnqueueAction(Action action)
	{
		QueuedActions.Enqueue(action);
	}
}