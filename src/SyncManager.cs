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
					Dictionary<string, ITask> tasks;
					Dictionary<string, ICategory> categories;
					
					// Refresh the tasks
					Application.Backend.Refresh();

					// Read Categories and populate them in the localCache
					categories = Application.Backend.Categories;
					Logger.Debug("Populating local cache with categories.");
					foreach(ICategory cat in categories.Values)
					{
						Logger.Debug("Category: {0}", cat.Name);
					}
					
					// Read Tasks and populate them into the localCache
					tasks = Application.Backend.Tasks;
					Logger.Debug("Populating local cache with tasks.");
					foreach(ITask task in tasks.Values)
					{
						CreateLocalTask(task);
					}
				}
			
				Logger.Debug("SyncThreadLoop done!");
			}
		}

		private void CreateLocalTask(ITask task)
		{
			Logger.Debug("Creating Local Task:");
			Logger.Debug("Name: {0}", task.Name);
			Logger.Debug("DueDate: {0}", task.DueDate);
			Logger.Debug("CompletionDate: {0}", task.CompletionDate);
			Logger.Debug("Priority: {0}", task.Priority);
			Logger.Debug("State: {0}", task.State);
			Logger.Debug("Category: {0}", task.Category);
			Logger.Debug("External ID: {0}", task.Id);
			if (Application.LocalCache.Database.ExternalTaskExists(task.Id)) {
				Logger.Debug("Task not added.");
				Logger.Debug("External ID {0} already exists", task.Id);
				return;
			}
			Task local_task = new Task (Application.LocalCache, task.Name, task.DueDate,
						    task.CompletionDate, task.Priority, task.State,
						    task.Category as Category, task.Id);
		}
	}
}
