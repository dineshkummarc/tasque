// LocalCache.cs created with MonoDevelop
// User: boyd at 1:35 PM 3/14/2008
//

using System;
using System.Collections.Generic;
using Mono.Unix;
using Tasque.Backends;
using Mono.Data.Sqlite;

namespace Tasque
{
	public class LocalCache
	{
		private Dictionary<int, Gtk.TreeIter> taskIters;
		private Gtk.TreeStore taskStore;
		private Gtk.TreeModelSort sortedTasksModel;
		private bool initialized;
		private bool configured = true;
		
		private Database db;
		
		private Gtk.ListStore categoryListStore;
		private Gtk.TreeModelSort sortedCategoriesModel;

		public event BackendInitializedHandler BackendInitialized;
		public event BackendSyncStartedHandler BackendSyncStarted;
		public event BackendSyncFinishedHandler BackendSyncFinished;

		private DateTime overdueRangeStart;
		private DateTime overdueRangeEnd;

		private DateTime todayRangeStart;
		private DateTime todayRangeEnd;

		private DateTime tomorrowRangeStart;
		private DateTime tomorrowRangeEnd;

		private DateTime sevenDaysRangeStart;
		private DateTime sevenDaysRangeEnd;

		private DateTime futureRangeStart;
		private DateTime futureRangeEnd;
		
		private Gtk.TreeIter overdueIter;
		private Gtk.TreeIter todayIter;
		private Gtk.TreeIter tomorrowIter;
		private Gtk.TreeIter nextSevenDaysIter;
		private Gtk.TreeIter futureIter;
		private Gtk.TreeIter completedTaskIter;

		Category defaultCategory;
		
		public LocalCache ()
		{
			initialized = false;
			taskIters = new Dictionary<int, Gtk.TreeIter> (); 
			taskStore = new Gtk.TreeStore (typeof (TaskModelNode));
			
			sortedTasksModel = new Gtk.TreeModelSort (taskStore);
			sortedTasksModel.SetSortFunc (0, new Gtk.TreeIterCompareFunc (CompareTasksSortFunc));
			sortedTasksModel.SetSortColumnId (0, Gtk.SortType.Ascending);
			
			categoryListStore = new Gtk.ListStore (typeof (Category));
			
			sortedCategoriesModel = new Gtk.TreeModelSort (categoryListStore);
			sortedCategoriesModel.SetSortFunc (0, new Gtk.TreeIterCompareFunc (CompareCategorySortFunc));
			sortedCategoriesModel.SetSortColumnId (0, Gtk.SortType.Ascending);
		}
		
		#region Public Properties
		/// <value>
		/// All the tasks including ITaskDivider items.
		/// </value>
		public Gtk.TreeModel Tasks
		{
			get { return sortedTasksModel; }
		}
		
		/// <value>
		/// This returns all the task lists (categories) that exist.
		/// </value>
		public Gtk.TreeModel Categories
		{
			get { return sortedCategoriesModel; }
		}
		
		/// <value>
		/// Indication that the Sqlite backend is configured
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
		
		public Database Database
		{
			get { return db; }
		}
		#endregion // Public Properties
		
		#region Public Methods
		public Task CreateTask (string taskName, Category category)		
		{
			// not sure what to do here with the category
			Task task = new Task (this, taskName);
			
			// Determine and set the task category
			if (category == null || category is Tasque.AllCategory)
				task.Category = defaultCategory; // Default to work
			else
				task.Category = category;

			Gtk.TreeIter parentIter = GetParentIter(task);
			Gtk.TreeIter iter = taskStore.AppendNode(parentIter);
			taskStore.SetValue (iter, 0, new TaskModelNode(task));
			taskIters [task.Id] = iter;		

			return task;
		}
		
		public void DeleteTask(Task task)
		{}
		
		public void Refresh()
		{}
		
		public void Initialize()
		{
			if(db == null)
				db = new Database();
				
			db.Open();
			
			//
			// Add in the "All" Category
			//
			AllCategory allCategory = new Tasque.AllCategory ();
			Gtk.TreeIter iter = categoryListStore.Append ();
			categoryListStore.SetValue (iter, 0, allCategory);
			
			RefreshDates();
			RefreshCategories();
			RefreshTasks();		

		
			initialized = true;
			if(BackendInitialized != null) {
				BackendInitialized();
			}		
		}

		public void Cleanup()
		{
			this.categoryListStore.Clear();
			this.taskStore.Clear();
			this.taskIters.Clear();

			db.Close();
			db = null;
			initialized = false;		
		}
		#endregion // Public Methods
		
		#region Private Methods
		static int CompareTasksSortFunc (Gtk.TreeModel model,
										 Gtk.TreeIter a,
										 Gtk.TreeIter b)
		{
			TaskModelNode taskModelNodeA = model.GetValue (a, 0) as TaskModelNode;
			TaskModelNode taskModelNodeB = model.GetValue (b, 0) as TaskModelNode;
			
			if (taskModelNodeA == null || taskModelNodeB == null || taskModelNodeA.Task == null || taskModelNodeB.Task == null)
				return 0;
			
			return (taskModelNodeA.Task.CompareTo (taskModelNodeB.Task));
		}
		
		static int CompareCategorySortFunc (Gtk.TreeModel model,
											Gtk.TreeIter a,
											Gtk.TreeIter b)
		{
			Category categoryA = model.GetValue (a, 0) as Category;
			Category categoryB = model.GetValue (b, 0) as Category;
			
			if (categoryA == null || categoryB == null)
				return 0;
			
			if (categoryA is Tasque.AllCategory)
				return -1;
			else if (categoryB is Tasque.AllCategory)
				return 1;
			
			return (categoryA.Name.CompareTo (categoryB.Name));
		}

		
		public void UpdateTask (Task task)
		{
			// Set the task in the store so the model will update the UI.
			Gtk.TreeIter iter;
			Gtk.TreeIter parentIter;
			
			Logger.Debug("Update task was called");
			
			if (taskIters.ContainsKey (task.Id) == false)
				return;
				
			iter = taskIters [task.Id];
			
			if (task.State == TaskState.Deleted) {
				taskIters.Remove (task.Id);
				if (taskStore.Remove (ref iter) == false) {
					Logger.Debug ("Successfully deleted from taskStore: {0}",
						task.Name);
				} else {
					Logger.Debug ("Problem removing from taskStore: {0}",
						task.Name);
				}
			} else {
				parentIter = GetParentIter(task);
				
				if(!taskStore.IsAncestor(parentIter, iter))
				{
					Logger.Debug("Task needs to be re-parented...");

					taskStore.Remove(ref iter);
					iter = taskStore.AppendNode(parentIter);
					taskStore.SetValue (iter, 0, new TaskModelNode(task));					
					taskIters [task.Id] = iter;
				} else {
					taskStore.SetValue (iter, 0, new TaskModelNode(task));
				}
			}
		}
		
		
		
		public void RefreshCategories()
		{
			Gtk.TreeIter iter;
			Category newCategory;
			bool hasValues = false;
			
			string command = "SELECT id FROM Categories";
        	SqliteCommand cmd = db.Connection.CreateCommand();
        	cmd.CommandText = command;
        	SqliteDataReader dataReader = cmd.ExecuteReader();
        	while(dataReader.Read()) {
			    int id = dataReader.GetInt32(0);
				hasValues = true;
				
				newCategory = new Category (this, id);
				if( (defaultCategory == null) || (newCategory.Name.CompareTo("Work") == 0) )
					defaultCategory = newCategory;
				iter = categoryListStore.Append ();
				categoryListStore.SetValue (iter, 0, newCategory);				
        	}

        	dataReader.Close();
        	cmd.Dispose();

			if(!hasValues)
			{
				defaultCategory = newCategory = new Category (this, "Work");
				iter = categoryListStore.Append ();
				categoryListStore.SetValue (iter, 0, newCategory);

				newCategory = new Category (this, "Personal");
				iter = categoryListStore.Append ();
				categoryListStore.SetValue (iter, 0, newCategory);
				
				newCategory = new Category (this, "Family");
				iter = categoryListStore.Append ();
				categoryListStore.SetValue (iter, 0, newCategory);		

				newCategory = new Category (this, "Project");
				iter = categoryListStore.Append ();
				categoryListStore.SetValue (iter, 0, newCategory);		
			}
		}
		


		public void RefreshTasks()
		{
			Gtk.TreeIter iter;
        	Gtk.TreeIter parentIter;
        	Task newTask;
			bool hasValues = false;

			overdueIter = taskStore.AppendNode();
			taskStore.SetValue(overdueIter, 0, new TaskModelNode(Catalog.GetString("Overdue")));
			
			todayIter = taskStore.AppendNode();
			taskStore.SetValue(todayIter, 0, new TaskModelNode(Catalog.GetString("Today")));
			
			tomorrowIter = taskStore.AppendNode();
			taskStore.SetValue(tomorrowIter, 0, new TaskModelNode(Catalog.GetString("Tomorrow")));
			
			nextSevenDaysIter = taskStore.AppendNode();
			taskStore.SetValue(nextSevenDaysIter, 0, new TaskModelNode(Catalog.GetString("Next 7 Days")));
			
			futureIter = taskStore.AppendNode();
			taskStore.SetValue(futureIter, 0, new TaskModelNode(Catalog.GetString("Future")));
			
			completedTaskIter = taskStore.AppendNode();
			taskStore.SetValue(completedTaskIter, 0, new TaskModelNode(Catalog.GetString("Completed")));

			string command = "SELECT id FROM Tasks";
        	SqliteCommand cmd = db.Connection.CreateCommand();
        	cmd.CommandText = command;
        	SqliteDataReader dataReader = cmd.ExecuteReader();
        	while(dataReader.Read()) {
			    int id = dataReader.GetInt32(0);
				hasValues = true;
				
				newTask = new Task(this, id);
				parentIter = GetParentIter(newTask);
				iter = taskStore.AppendNode(parentIter);
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));
				taskIters [newTask.Id] = iter;				
        	}

        	dataReader.Close();
        	cmd.Dispose();

			if(!hasValues)
			{
				newTask = new Task (this, "Enter tasks into Tasque");
				newTask.Category = defaultCategory;
				newTask.DueDate = DateTime.Now;
				newTask.Priority = TaskPriority.High;
				parentIter = GetParentIter(newTask);
				iter = taskStore.AppendNode(parentIter);
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));
				taskIters [newTask.Id] = iter;
				
				newTask = new Task (this, "Get things done");
				newTask.Category = defaultCategory;
				newTask.DueDate = DateTime.Now;
				newTask.Priority = TaskPriority.Medium;
				parentIter = GetParentIter(newTask);
				iter = taskStore.AppendNode(parentIter);
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));
				taskIters [newTask.Id] = iter;				

				newTask = new Task (this, "Enjoy new found freedom");
				newTask.Category = defaultCategory;
				newTask.DueDate = DateTime.Now;
				newTask.Priority = TaskPriority.Low;
				parentIter = GetParentIter(newTask);
				iter = taskStore.AppendNode(parentIter);
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));
				taskIters [newTask.Id] = iter;				

			}
		}

		private Gtk.TreeIter GetParentIter(Task task)
		{
			Gtk.TreeIter iter;
			
			if(task.LocalState == TaskState.Completed) {
				Logger.Debug("Parent is Complete");
				iter = completedTaskIter;
			}
			else if( InRange(overdueRangeStart, overdueRangeEnd, task) ) {
				Logger.Debug("Parent is Overdue");
				iter = overdueIter;
			}
			else if( InRange(todayRangeStart, todayRangeEnd, task) ) {
				Logger.Debug("Parent is Today");
				iter = todayIter;
			}
			else if( InRange(tomorrowRangeStart, tomorrowRangeEnd, task) ) {
				Logger.Debug("Parent is Tomorrow");
				iter = tomorrowIter;
			}
			else if( InRange(sevenDaysRangeStart, sevenDaysRangeEnd, task) ) {
				Logger.Debug("Parent is Next Seven Days");
				iter = nextSevenDaysIter;
			}
			else { 
				Logger.Debug("Parent is Future");
				iter = futureIter;
			}
			
			return iter;
		}
		
		
		private bool InRange (DateTime rangeStart, DateTime rangeEnd, Task task)
		{
			if (task == null)
				return false;
				
			if (task.DueDate < rangeStart || task.DueDate > rangeEnd)
				return false;
			
			return true;
		}
		
		
		private void RefreshDates()
		{
			// Overdue
			overdueRangeStart = DateTime.MinValue.AddSeconds(1); // min is one second more than min
			overdueRangeEnd = DateTime.Now.AddDays (-1);
			overdueRangeEnd = new DateTime (overdueRangeEnd.Year, overdueRangeEnd.Month, overdueRangeEnd.Day,
									 23, 59, 59);		

			// Today
			todayRangeStart = DateTime.Now;
			todayRangeStart = new DateTime (todayRangeStart.Year, todayRangeStart.Month,
									   todayRangeStart.Day, 0, 0, 0);
			todayRangeEnd = DateTime.Now;
			todayRangeEnd = new DateTime (todayRangeEnd.Year, todayRangeEnd.Month,
									 todayRangeEnd.Day, 23, 59, 59);

			// Tomorrow
			tomorrowRangeStart = DateTime.Now.AddDays (1);
			tomorrowRangeStart = new DateTime (tomorrowRangeStart.Year, tomorrowRangeStart.Month,
									   tomorrowRangeStart.Day, 0, 0, 0);
			tomorrowRangeEnd = DateTime.Now.AddDays (1);
			tomorrowRangeEnd = new DateTime (tomorrowRangeEnd.Year, tomorrowRangeEnd.Month,
									 tomorrowRangeEnd.Day, 23, 59, 59);

			// Next Seven Days
			sevenDaysRangeStart = DateTime.Now.AddDays (2);
			sevenDaysRangeStart = new DateTime (sevenDaysRangeStart.Year, sevenDaysRangeStart.Month,
									   sevenDaysRangeStart.Day, 0, 0, 0);
			sevenDaysRangeEnd = DateTime.Now.AddDays (6);
			sevenDaysRangeEnd = new DateTime (sevenDaysRangeEnd.Year, sevenDaysRangeEnd.Month,
									 sevenDaysRangeEnd.Day, 23, 59, 59);

			// Future
			futureRangeStart = DateTime.Now.AddDays (7);
			futureRangeStart = new DateTime (futureRangeStart.Year, futureRangeStart.Month,
									   futureRangeStart.Day, 0, 0, 0);
			futureRangeEnd = DateTime.MaxValue;
		}

		#endregion // Private Methods
		
		#region Event Handlers
		#endregion // Event Handlers
	}
}
