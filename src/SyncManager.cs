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

		private void Pause(int timeout)
		{
			Thread.Sleep(timeout);
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
					Dictionary<string, ITask> remoteTasks;
					Dictionary<string, Task> localTasks;
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
					remoteTasks = Application.Backend.Tasks;
					Logger.Debug("Populating local cache with remote tasks.");
					foreach(ITask task in remoteTasks.Values)
					{
						CreateLocalTask(task);
					}

					// "Upload" any local tasks that haven't been
					// uploaded
					// localTasks = Application.LocalCache.Tasks;
					Logger.Debug("Sending local tasks to remote backend");
					foreach(Task task in Application.LocalCache.TasksNew.Values)
					{
						CreateRemoteTask(task);
					}

				}
			
				Logger.Debug("SyncThreadLoop done!");
			}
		}

		// Check each local task for an external ID, if the external ID does not exist
		// send that task to the remote backend
		private void CreateRemoteTask(Task task)
		{
			Logger.Debug("Creating Remote Task:");
			Logger.Debug("Name: {0}", task.Name);
			Logger.Debug("DueDate: {0}", task.DueDate);
			Logger.Debug("CompletionDate: {0}", task.CompletionDate);
			Logger.Debug("Priority: {0}", task.Priority);
			Logger.Debug("State: {0}", task.State);
			Logger.Debug("Category: {0}", task.Category);
			Logger.Debug("ID: {0}", task.Id);
			Logger.Debug("External ID: {0}", task.ExternalId);
			if (task.ExternalId != "" && Application.LocalCache.Database.ExternalTaskExists(task.ExternalId)) {
				Logger.Debug("Remote task not created");
				Logger.Debug("Task already has an external ID ({0})", task.ExternalId);
				return;
			} 
			// XXX: use a more intelligent timeout.  also, i think a good system would be
			// to add a timeout time to the backend interface so each backend has a 
			// unique timeout time.  maybe allow this to be overridden by the user?
			this.Pause(1000);
			Logger.Debug("Creating remote task...");
			ITask remoteTask = Application.Backend.CreateTask(task.Name, task.Category as ICategory);
			task.ExternalId = remoteTask.Id;
		}

		// Get the external ID from each task (from the backend).  If there is already a task
		// in the local database with the retrieved external ID, ignore the external task.  
		// Otherwise, add the external task to the local database.
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
				Logger.Debug("Local task not created.");
				Logger.Debug("External ID {0} already exists", task.Id);
				return;
			}
			Task local_task = new Task (Application.LocalCache, task.Name, task.DueDate,
						    task.CompletionDate, task.Priority, task.State,
						    task.Category as Category, task.Id);
		}
	}
}
