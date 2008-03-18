// Category.cs created with MonoDevelop
// User: boyd at 1:34 PM 3/14/2008
//

using System;

namespace Tasque
{
	public class Category
	{
		private int id;
		LocalCache cache;
		
		public int ID
		{
			get { return id; }
		}
		
		public virtual string Name
		{
			get {
				string command = String.Format("SELECT Name FROM Categories where ID='{0}'", id);
				return cache.Database.GetSingleString(command);
			}
			set {
				string command = String.Format("UPDATE Categories set Name='{0}' where ID='{0}'", value, id);
				cache.Database.ExecuteScalar(command);
			}
		}
		
		public string ExternalID
		{
			get {
				string command = String.Format("SELECT ExternalID FROM Categories where ID='{0}'", id);
				return cache.Database.GetSingleString(command);
			}
			set {
				string command = String.Format("UPDATE Categories set ExternalID='{0}' where ID='{0}'", value, id);
				cache.Database.ExecuteScalar(command);
			}
		}
		
		public Category (LocalCache cache, string name)
		{
			this.cache = cache;
			string command = String.Format("INSERT INTO Categories (Name, ExternalID) values ('{0}', '{1}')", name, string.Empty);
			cache.Database.ExecuteScalar(command);
			this.id = cache.Database.Connection.LastInsertRowId;
			//Logger.Debug("Inserted category named: {0} with id {1}", name, id);
		}
		
		public Category (LocalCache cache, int id)
		{
			this.cache = cache;
			this.id = id;
		}

		internal Category ()
		{
		}

		
		public virtual bool ContainsTask(Task task)
		{
			if(task.Category is Category)
				return ((task.Category as Category).ID == id);

			return false;
		}
		
	}
}
