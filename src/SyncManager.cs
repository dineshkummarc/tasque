// SyncManager.cs created with MonoDevelop
// User: calvin at 12:23 PM 3/18/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Threading;
using System.Collections.Generic;

namespace Tasque
{
	public class SyncManager
	{
		
		private Thread syncThread;
		private bool runningSyncThread;
		private AutoResetEvent runSyncEvent;
		
		
		public SyncManager()
		{
			runSyncEvent = new AutoResetEvent(false);
			
			runningSyncThread = false;
			syncThread  = new Thread(SyncThreadLoop);
			
		}

		public void Sync()
		{
			runSyncEvent.Set();
		}		

		public void Start()
		{
			runningSyncThread = true;
			syncThread.Start();
		}

		public void Stop()
		{
			runningSyncThread = false;
			runSyncEvent.Set();
			syncThread.Abort();
		}		

		
		private void SyncThreadLoop()
		{
			while(runningSyncThread) {
				runSyncEvent.WaitOne();

				if(!runningSyncThread)
					return;

				runSyncEvent.Reset();

				Logger.Debug("SyncThreadLoop running...");

				if( (Application.Backend != null) && 
					(Application.Backend.Configured) &&
					(Application.Backend.Initialized) )
				{
					// Refresh the tasks
					Application.Backend.Refresh();

					// Read Categories and populate them in the localCache
					
					// Read Tasks and populate them into the localCache
				}
			
				Logger.Debug("SyncThreadLoop done!");
			}
		}
		
	}
}
