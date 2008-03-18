// RtmBackend.cs created with MonoDevelop
// User: boyd at 7:10 AM 2/11/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Mono.Unix;
using Tasque.Backends;
using RtmNet;
using System.Threading;
using System.Collections.Generic;

namespace Tasque.Backends.RtmBackend
{
	public class RtmBackend : IBackend
	{
		private const string apiKey = "b29f7517b6584035d07df3170b80c430";
		private const string sharedSecret = "93eb5f83628b2066";

		private Dictionary<string, ITask> tasks;
		private Dictionary<string, ICategory> categories;

		private Rtm rtm;
		private string frob;
		private Auth rtmAuth;
		private string timeline;
		
		private bool initialized;
		private bool configured;

		public RtmBackend ()
		{
			initialized = false;
			configured = false;

			tasks = new Dictionary<string,Tasque.ITask> ();
			categories = new Dictionary<string, ICategory> ();
		}

		#region Public Properties
		public string Name
		{
			get { return "Remember the Milk"; }
		}
		
		/// <value>
		/// All the tasks including ITaskDivider items.
		/// </value>
		public Dictionary<string,ITask> Tasks
		{
			get { return tasks; }
		}

		/// <value>
		/// This returns all the task lists (categories) that exist.
		/// </value>
		public Dictionary<string,ICategory> Categories
		{
			get { return categories; }
		}

		public string RtmUserName
		{
			get {
				if( (rtmAuth != null) && (rtmAuth.User != null) ) {
					return rtmAuth.User.Username;
				} else
					return null;
			}
		}
		
		/// <value>
		/// Indication that the rtm backend is configured
		/// </value>
		public bool Configured
		{
			get { return configured; }
		}
		
		/// <value>
		/// Inidication that the backend is initialized
		/// </value>
		public bool Initialized
		{
			get { return initialized; }
		}
		
#endregion // Public Properties

#region Public Methods
		public ITask CreateTask (string taskName, ICategory category)
		{
			string categoryID;
			RtmTask rtmTask = null;
			
			if(category is Tasque.AllCategory)
				categoryID = null;
			else
				categoryID = (category as RtmCategory).Id;	

			if(rtm != null) {
				try {
					List list;
					
					if(categoryID == null)
						list = rtm.TasksAdd(timeline, taskName);
					else
						list = rtm.TasksAdd(timeline, taskName, categoryID);

					rtmTask = UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set create task: " + taskName);
					Logger.Debug(e.ToString());
				}
			}
			else
				throw new Exception("Unable to communicate with Remember The Milk");
				
			return rtmTask;
		}
		
		public void DeleteTask(ITask task)
		{
			RtmTask rtmTask = task as RtmTask;
			if(rtm != null) {
				try {
					rtm.TasksDelete(timeline, rtmTask.ListID, rtmTask.SeriesTaskID, rtmTask.TaskID);

					if(tasks.ContainsKey(rtmTask.Id) ) {
						tasks.Remove(rtmTask.Id);
					}
				} catch(Exception e) {
					Logger.Debug("Unable to delete task: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}
			else
				throw new Exception("Unable to communicate with Remember The Milk");
		}
		
		public void Refresh()
		{
			Logger.Debug("RtmBackend Refreshing data...");

			UpdateCategories();			
			UpdateTasks();

			Logger.Debug("RtmBackend refreshing data!");
		}

		public void Initialize()
		{
			// *************************************
			// AUTHENTICATION to Remember The Milk
			// *************************************
			string authToken =
				Application.Preferences.Get (Preferences.AuthTokenKey);
			if (authToken != null ) {
				Logger.Debug("Found AuthToken, checking credentials...");
				try {
					rtm = new Rtm(apiKey, sharedSecret, authToken);
					rtmAuth = rtm.AuthCheckToken(authToken);
					timeline = rtm.TimelineCreate();
					Logger.Debug("RTM Auth Token is valid!");
					Logger.Debug("Setting configured status to true");
					configured = true;
				} catch (Exception e) {
					Application.Preferences.Set (Preferences.AuthTokenKey, null);
					Application.Preferences.Set (Preferences.UserIdKey, null);
					Application.Preferences.Set (Preferences.UserNameKey, null);
					rtm = null;
					rtmAuth = null;
					Logger.Error("Exception authenticating, reverting" + e.Message);
				}
			}

			if(rtm == null)
				rtm = new Rtm(apiKey, sharedSecret);
			
			initialized = true;
		}

		public void Cleanup()
		{
		}

		public Gtk.Widget GetPreferencesWidget ()
		{
			return new RtmPreferencesWidget (this);
		}

		public string GetAuthUrl()
		{
			frob = rtm.AuthGetFrob();
			string url = rtm.AuthCalcUrl(frob, AuthLevel.Delete);
			return url;
		}

		public void FinishedAuth()
		{
			rtmAuth = rtm.AuthGetToken(frob);
			if (rtmAuth != null) {
				Preferences prefs = Application.Preferences;
				prefs.Set (Preferences.AuthTokenKey, rtmAuth.Token);
				if (rtmAuth.User != null) {
					prefs.Set (Preferences.UserNameKey, rtmAuth.User.Username);
					prefs.Set (Preferences.UserIdKey, rtmAuth.User.UserId);
				}
			}
			
			string authToken =
				Application.Preferences.Get (Preferences.AuthTokenKey);
			if (authToken != null ) {
				Logger.Debug("Found AuthToken, checking credentials...");
				try {
					rtm = new Rtm(apiKey, sharedSecret, authToken);
					rtmAuth = rtm.AuthCheckToken(authToken);
					timeline = rtm.TimelineCreate();
					Logger.Debug("RTM Auth Token is valid!");
					Logger.Debug("Setting configured status to true");
					configured = true;
//					Refresh();
				} catch (Exception e) {
					rtm = null;
					rtmAuth = null;				
					Logger.Error("Exception authenticating, reverting" + e.Message);
				}	
			}
		}

		public void UpdateTaskName(RtmTask task)
		{
			if(rtm != null) {
				try {
					List list = rtm.TasksSetName(timeline, task.ListID, task.SeriesTaskID, task.TaskID, task.Name);		
					UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set name on task: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}
		}
		
		public void UpdateTaskDueDate(RtmTask task)
		{
			if(rtm != null) {
				try {
					List list;
					if(task.DueDate == DateTime.MinValue)
						list = rtm.TasksSetDueDate(timeline, task.ListID, task.SeriesTaskID, task.TaskID);
					else	
						list = rtm.TasksSetDueDate(timeline, task.ListID, task.SeriesTaskID, task.TaskID, task.DueDateString);
					UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set due date on task: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}
		}
		
		public void UpdateTaskCompleteDate(RtmTask task)
		{
			UpdateTask(task);
		}
		
		public void UpdateTaskPriority(RtmTask task)
		{
			if(rtm != null) {
				try {
					List list = rtm.TasksSetPriority(timeline, task.ListID, task.SeriesTaskID, task.TaskID, task.PriorityString);
					UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set priority on task: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}
		}
		
		public void UpdateTaskActive(RtmTask task)
		{
			if(task.State == TaskState.Completed)
			{
				if(rtm != null) {
					try {
						List list = rtm.TasksUncomplete(timeline, task.ListID, task.SeriesTaskID, task.TaskID);
						UpdateTaskFromResult(list);
					} catch(Exception e) {
						Logger.Debug("Unable to set Task as completed: " + task.Name);
						Logger.Debug(e.ToString());
					}
				}
			}
			else
				UpdateTask(task);
		}
		
		public void UpdateTaskInactive(RtmTask task)
		{
			UpdateTask(task);
		}	

		public void UpdateTaskCompleted(RtmTask task)
		{
			if(rtm != null) {
				try {
					List list = rtm.TasksComplete(timeline, task.ListID, task.SeriesTaskID, task.TaskID);
					UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set Task as completed: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}
		}	

		public void UpdateTaskDeleted(RtmTask task)
		{
			UpdateTask(task);
		}
		

		public void MoveTaskCategory(RtmTask task, string id)
		{
			if(rtm != null) {
				try {
					List list = rtm.TasksMoveTo(timeline, task.ListID, id, task.SeriesTaskID, task.TaskID);
					UpdateTaskFromResult(list);
				} catch(Exception e) {
					Logger.Debug("Unable to set Task as completed: " + task.Name);
					Logger.Debug(e.ToString());
				}
			}					
		}
		
		
		public void UpdateTask(RtmTask task)
		{
			if(tasks.ContainsKey(task.Id))
			{
				tasks[task.Id] = task;
			}
		}
		
		public RtmTask UpdateTaskFromResult(List list)
		{
			TaskSeries ts = list.TaskSeriesCollection[0];
			if(ts != null) {
				RtmTask rtmTask = new RtmTask(ts, this, list.ID);
				tasks[rtmTask.Id] = rtmTask;
				return rtmTask;				
			}
			return null;
		}
		
		public ICategory GetCategory(string id)
		{
			if(categories.ContainsKey(id))
				return categories[id];
			else
				return null;
		}
		
		public RtmNote CreateNote (RtmTask rtmTask, string text)
		{
			RtmNet.Note note = null;
			RtmNote rtmNote = null;
			
			if(rtm != null) {
				try {
					note = rtm.NotesAdd(timeline, rtmTask.ListID, rtmTask.SeriesTaskID, rtmTask.TaskID, String.Empty, text);
					rtmNote = new RtmNote(note);
				} catch(Exception e) {
					Logger.Debug("RtmBackend.CreateNote: Unable to create a new note");
					Logger.Debug(e.ToString());
				}
			}
			else
				throw new Exception("Unable to communicate with Remember The Milk");
				
			return rtmNote;
		}


		public void DeleteNote (RtmTask rtmTask, RtmNote note)
		{
			if(rtm != null) {
				try {
					rtm.NotesDelete(timeline, note.ID);
				} catch(Exception e) {
					Logger.Debug("RtmBackend.DeleteNote: Unable to delete note");
					Logger.Debug(e.ToString());
				}
			}
			else
				throw new Exception("Unable to communicate with Remember The Milk");
		}

		public void SaveNote (RtmTask rtmTask, RtmNote note)
		{
			if(rtm != null) {
				try {
					rtm.NotesEdit(timeline, note.ID, String.Empty, note.Text);
				} catch(Exception e) {
					Logger.Debug("RtmBackend.SaveNote: Unable to save note");
					Logger.Debug(e.ToString());
				}
			}
			else
				throw new Exception("Unable to communicate with Remember The Milk");
		}

#endregion // Public Methods

#region Private Methods
		/// <summary>
		/// Update the model to match what is in RTM
		/// FIXME: This is a lame implementation and needs to be optimized
		/// </summary>		
		private void UpdateCategories()
		{
			Logger.Debug("RtmBackend.UpdateCategories was called");
			
			categories.Clear();
			
			try {
				Lists lists = rtm.ListsGetList();
				foreach(List list in lists.listCollection)
				{
					RtmCategory rtmCategory = new RtmCategory(list);

					categories[rtmCategory.Id] = rtmCategory;
				}
			} catch (Exception e) {
				Logger.Debug("Exception in fetch " + e.Message);
			}
			Logger.Debug("RtmBackend.UpdateCategories is done");			
		}

		/// <summary>
		/// Update the model to match what is in RTM
		/// FIXME: This is a lame implementation and needs to be optimized
		/// </summary>		
		private void UpdateTasks()
		{
			Logger.Debug("RtmBackend.UpdateTasks was called");
			
			tasks.Clear();

			try {
			
				Lists lists = rtm.ListsGetList();
				foreach(List list in lists.listCollection)
				{
					Tasks tasksList = null;
					try {
						tasksList = rtm.TasksGetList(list.ID);
					} catch (Exception tglex) {
						Logger.Debug("Exception calling TasksGetList(list.ListID) " + tglex.Message);
					}

					if(tasksList != null) {
						foreach(List tList in tasksList.ListCollection)
						{
							foreach(TaskSeries ts in tList.TaskSeriesCollection)
							{
								RtmTask rtmTask = new RtmTask(ts, this, list.ID);
								
								tasks[rtmTask.Id] = rtmTask;
							}
						}
					}
				}
			} catch (Exception e) {
				Logger.Debug("Exception in fetch " + e.Message);
				Logger.Debug(e.ToString());
			}
			Logger.Debug("RtmBackend.UpdateTasks is done");			
		}

		
#endregion // Private Methods

#region Event Handlers
#endregion // Event Handlers
	}
}
