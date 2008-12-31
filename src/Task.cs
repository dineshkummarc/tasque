// Task.cs created with MonoDevelop
// User: boyd at 1:34 PM 3/14/2008
//

using System;
using System.Collections.Generic;

namespace Tasque
{
	public class Task
	{
		private LocalCache cache;
		private int id;
		private uint timerID = 0;
		
		public Task(LocalCache cache, string name)
		{
			this.cache = cache;
			string command = String.Format("INSERT INTO Tasks (Name, DueDate, CompletionDate, Priority, State, Category, ExternalID) values ('{0}','{1}', '{2}','{3}', '{4}', '{5}', '{6}')", 
								name, Database.FromDateTime(DateTime.MinValue), Database.FromDateTime(DateTime.MinValue), 
								((int)(TaskPriority.None)), ((int)TaskState.Active), 0, string.Empty );
			cache.Database.ExecuteScalar(command);
			this.id = cache.Database.Connection.LastInsertRowId;
		}

		// The constructor for external tasks
		public Task(LocalCache cache, string name, DateTime dueDate, DateTime completionDate, TaskPriority priority, TaskState state, Category category, string externalID)
		{
			this.cache = cache;
			// XXX: Handle the category correctly
                        string command = String.Format("INSERT INTO Tasks (Name, DueDate, CompletionDate, Priority, State, Category, ExternalID) values ('{0}','{1}', '{2}','{3}', '{4}', '{5}', '{6}')",
                                                                name, Database.FromDateTime(dueDate), Database.FromDateTime(completionDate), ((int)(TaskPriority)priority), ((int)(TaskState)state), 0, externalID);
                        cache.Database.ExecuteScalar(command);
                        this.id = cache.Database.Connection.LastInsertRowId;
		}
		
		public Task (LocalCache cache, int id)
		{
			this.cache = cache;
			this.id = id;
		}		
		
		#region Public Properties
		
		public int Id
		{
			get { return id; }
			set { id = value; }
		}
		
		public string Name
		{
			get {
				string command = String.Format("SELECT Name FROM Tasks where ID='{0}'", id);
				return cache.Database.GetSingleString(command);
			}
			set {
				string command = String.Format("UPDATE Tasks set Name='{0}' where ID='{1}'", value, id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);
			}
		}
		
		public DateTime DueDate
		{
			get {
				string command = String.Format("SELECT DueDate FROM Tasks where ID='{0}'", id);
				return cache.Database.GetDateTime(command);
			}
			set {
				string command = String.Format("UPDATE Tasks set DueDate='{0}' where ID='{1}'", Database.FromDateTime(value), id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);				
			}
		}
		
		
		public DateTime CompletionDate
		{
			get {
				string command = String.Format("SELECT CompletionDate FROM Tasks where ID='{0}'", id);
				return cache.Database.GetDateTime(command);
			}
			set {
				string command = String.Format("UPDATE Tasks set CompletionDate='{0}' where ID='{1}'", Database.FromDateTime(value), id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);				
			}
		}		
		
		
		public bool IsComplete
		{
			get {
				if (CompletionDate == DateTime.MinValue)
					return false;
				
				return true;
			}
		}
		
		public TaskPriority Priority
		{
			get {
				string command = String.Format("SELECT Priority FROM Tasks where ID='{0}'", id);
				return (TaskPriority)cache.Database.GetSingleInt(command);
			}
			set {
				string command = String.Format("UPDATE Tasks set Priority='{0}' where ID='{1}'", ((int)value), id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);				
			}
		}

		public bool HasNotes
		{
			get { return false; }
		}
		
		public bool SupportsMultipleNotes
		{
			get { return false; }
		}
		
		public TaskState State
		{
			get { return LocalState; }
		}
		
		public TaskState LocalState
		{
			get {
				string command = String.Format("SELECT State FROM Tasks where ID='{0}'", id);
				return (TaskState)cache.Database.GetSingleInt(command);
			}
			set {
				string command = String.Format("UPDATE Tasks set State='{0}' where ID='{1}'", ((int)value), id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);				
			}
		}

		public Category Category
		{
			get {
				string command = String.Format("SELECT Category FROM Tasks where ID='{0}'", id);
				int catID = cache.Database.GetSingleInt(command);
				Category sqCat = new Category(cache, catID);
				return sqCat;
			}
			set {
				string command = String.Format("UPDATE Tasks set Category='{0}' where ID='{1}'", ((int)(value as Category).ID), id);
				cache.Database.ExecuteScalar(command);
				cache.UpdateTask(this);
			}
		}
		
		public List<INote> Notes
		{
			get { return null; }
		}		

		/// <value>
		/// The ID of the timer used to complete a task after being marked
		/// inactive.
		/// </value>
		public uint TimerID
		{
			get { return timerID; }
			set { timerID = value; }
		}		
		#endregion // Public Properties
		
		#region Public Methods
		public void Activate ()
		{
			// Logger.Debug ("Task.Activate ()");
			CompletionDate = DateTime.MinValue;
			LocalState = TaskState.Active;
			cache.UpdateTask (this);
		}
		
		public void Inactivate ()
		{
			// Logger.Debug ("Task.Inactivate ()");
			CompletionDate = DateTime.Now;
			LocalState = TaskState.Inactive;
			cache.UpdateTask (this);
		}
		
		public void Complete ()
		{
			//Logger.Debug ("Task.Complete ()");
			CompletionDate = DateTime.Now;
			LocalState = TaskState.Completed;
			cache.UpdateTask (this);
		}
		
		public void Delete ()
		{
			//Logger.Debug ("Task.Delete ()");
			LocalState = TaskState.Deleted;
			cache.UpdateTask (this);
		}
		
		public INote CreateNote(string text)
		{
			return null;
		}
		
		public void DeleteNote(INote note)
		{
		}

		public void SaveNote(INote note)
		{
		}
		
		public int CompareTo (Task task)
		{
			bool isSameDate = true;
			if (DueDate.Year != task.DueDate.Year
					|| DueDate.DayOfYear != task.DueDate.DayOfYear)
				isSameDate = false;
			
			if (isSameDate == false) {
				if (DueDate == DateTime.MinValue) {
					// No due date set on this task. Since we already tested to see
					// if the dates were the same above, we know that the passed-in
					// task has a due date set and it should be "higher" in a sort.
					return 1;
				} else if (task.DueDate == DateTime.MinValue) {
					// This task has a due date and should be "first" in sort order.
					return -1;
				}
				
				int result = DueDate.CompareTo (task.DueDate);
				
				if (result != 0) {
					return result;
				}
			}
			
			// The due dates match, so now sort based on priority and name
			return CompareByPriorityAndName (task);
		}
		
		public int CompareToByCompletionDate (Task task)
		{
			bool isSameDate = true;
			if (CompletionDate.Year != task.CompletionDate.Year
					|| CompletionDate.DayOfYear != task.CompletionDate.DayOfYear)
				isSameDate = false;
			
			if (isSameDate == false) {
				if (CompletionDate == DateTime.MinValue) {
					// No completion date set for some reason.  Since we already
					// tested to see if the dates were the same above, we know
					// that the passed-in task has a CompletionDate set, so the
					// passed-in task should be "higher" in the sort.
					return 1;
				} else if (task.CompletionDate == DateTime.MinValue) {
					// "this" task has a completion date and should evaluate
					// higher than the passed-in task which doesn't have a
					// completion date.
					return -1;
				}
				
				return CompletionDate.CompareTo (task.CompletionDate);
			}
			
			// The completion dates are the same, so no sort based on other
			// things.
			return CompareByPriorityAndName (task);
		}
		#endregion // Public Methods
		
		#region Private Methods
		private int CompareByPriorityAndName (Task task)
		{
			// The due dates match, so now sort based on priority
			if (Priority != task.Priority) {
				switch (Priority) {
				case TaskPriority.High:
					return -1;
				case TaskPriority.Medium:
					if (task.Priority == TaskPriority.High) {
						return 1;
					} else {
						return -1;
					}
				case TaskPriority.Low:
					if (task.Priority == TaskPriority.None) {
						return -1;
					} else {
						return 1;
					}
				case TaskPriority.None:
					return 1;
				}
			}
			
			// Due dates and priorities match, now sort by name
			return Name.CompareTo (task.Name);
		}
		#endregion // Private Methods
	}
}
