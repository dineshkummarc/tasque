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
		
		//private TaskGroup overdueGroup;
		//private TaskGroup todayGroup;
		//private TaskGroup tomorrowGroup;
		//private TaskGroup nextSevenDaysGroup;
		//private TaskGroup futureGroup;
		//private CompletedTaskGroup completedTaskGroup;
		
		private Gtk.TreeIter overdueIter;
		private Gtk.TreeIter todayIter;
		private Gtk.TreeIter tomorrowIter;
		private Gtk.TreeIter nextSevenDaysIter;
		private Gtk.TreeIter futureIter;
		private Gtk.TreeIter completedTaskIter;


		
		Category defaultCategory;
		//Category workCategory;
		//Category projectsCategory;
		
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
			
			Gtk.TreeIter iter = taskStore.AppendNode ();
			taskStore.SetValue (iter, 0, task);
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

		public Gtk.Widget GetPreferencesWidget ()
		{
			// TODO: Replace this with returning null once things are going
			// so that the Preferences Dialog doesn't waste space.
			return new Gtk.Label ("Local file requires no configuration.");
		}
		#endregion // Public Methods
		
		#region Private Methods
		static int CompareTasksSortFunc (Gtk.TreeModel model,
										 Gtk.TreeIter a,
										 Gtk.TreeIter b)
		{
			Task taskA = model.GetValue (a, 0) as Task;
			Task taskB = model.GetValue (b, 0) as Task;
			
			if (taskA == null || taskB == null)
				return 0;
			
			return (taskA.CompareTo (taskB));
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
				taskStore.SetValue (iter, 0, task);
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
			Task newTask;
			bool hasValues = false;

			overdueIter = taskStore.AppendNode();
			taskStore.SetValue(overdueIter, 0, new TaskModelNode(Catalog.GetString("Overdue")));
			
			todayIter = taskStore.AppendNode();
			taskStore.SetValue(todayIter, 0, new TaskModelNode(Catalog.GetString("Today")));
			
			tomorrowIter = taskStore.AppendNode();
			taskStore.SetValue(overdueIter, 0, new TaskModelNode(Catalog.GetString("Tomorrow")));
			
			nextSevenDaysIter = taskStore.AppendNode();
			taskStore.SetValue(tomorrowIter, 0, new TaskModelNode(Catalog.GetString("Next 7 Days")));
			
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
				iter = taskStore.AppendNode(overdueIter);
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));				
        	}

        	dataReader.Close();
        	cmd.Dispose();

			if(!hasValues)
			{
				newTask = new Task (this, "Create some tasks");
				newTask.Category = defaultCategory;
				newTask.DueDate = DateTime.Now;
				newTask.Priority = TaskPriority.Medium;
				iter = taskStore.AppendNode ();
				taskStore.SetValue (iter, 0, new TaskModelNode(newTask));	
				taskIters [newTask.Id] = iter;
			}
		}

		#endregion // Private Methods
		
		#region Event Handlers
		#endregion // Event Handlers
	}
}
