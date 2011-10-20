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
		//static AutoResetEvent s_TestFinished = new AutoResetEvent(false);        // signals when the test is finished 

		public new void Enqueue(Action item)
		{
			base.Enqueue(item);
			EnsureQueueIsProcessing();
		}

		private void EnsureQueueIsProcessing()
		{
			if (this.Count > 0)
			{
				//InvokeAction(null);
				//Action action = this.Dequeue();
				ThreadPool.QueueUserWorkItem(new WaitCallback(InvokeAction));//, action);
				//s_TestFinished.WaitOne();        // we wait until the last task has finished 
				//s_TestFinished.WaitOne(1000);        // we wait until the last task has finished 
			}
		}

		private void InvokeAction(object ActionToInvoke)
		{
			// This method executes on a threadpool thread
			lock (s_Lock)
			{
				//(ActionToInvoke as Action)();
				//bool isbusy = true;
				//if (this.Count > 0) this.Dequeue()();
				this.Dequeue()();
				//  .BeginInvoke(
				//  delegate
				//  {
				//    isbusy = false;

				//  }, null);//.DynamicInvoke();//.Invoke();
				//while (isbusy) { }				
			}
			//s_TestFinished.Set();
		}
	}

	private static CustomQueue QueuedActions = new CustomQueue();

	public static void EnqueueAction(Action action)
	{
		QueuedActions.Enqueue(action);
	}
}

/*public class TestAnotherQueueingActionsInterop
{
	Queue<Action> actionQueue = new Queue<Action>();

	public void EnqueueJointPosition(Position position)
	{
		var vector = DefineVector(Axis.Robot, Guid.NewGuid().ToString());
		vector.Position = position;

		SetJoints(vector, vector.Position, PointType.AbsoluteJointCoordinate);

		actionQueue.Enqueue(() => MoveJoint(vector));
	}

	public void EnqueueAction(Action<RobotController> item)
	{
		actionQueue.Enqueue(() => item(this));
	}

	//private ManualResetEvent trigger;
	private bool canMove = false;

	public void RunMovementQueue()
	{
		//trigger = new ManualResetEvent(true);

		MotionEnded += RobotController_MotionEnded;
		MotionStarted += RobotController_MotionStarted;

		canMove = true;

		while (actionQueue.Count > 0)
		{
			if (canMove)
			{
				canMove = false;

				var action = actionQueue.Dequeue();
				action();
			}
		}

		MotionStarted -= RobotController_MotionStarted;
		MotionEnded -= RobotController_MotionEnded;
	}

	private void RobotController_MotionStarted(object sender, MotionEventArgs e)
	{
		canMove = false;
	}

	private void RobotController_MotionEnded(object sender, MotionEventArgs e)
	{
		canMove = true;
	}
}*/