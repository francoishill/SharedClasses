using System;

namespace SharedClasses
{
	public abstract class Singleton<T> where T : new()
	{
		private static T instance;
		private static object lockingObject = new Object();
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					lock (lockingObject)
					{
						if (instance == null)
							instance = new T();//PropertyInterceptor<T>.Create();
					}
				}
				return instance;
			}
		}
	}
}