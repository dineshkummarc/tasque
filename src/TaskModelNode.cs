// TaskModelNode.cs created with MonoDevelop
// User: calvin at 4:29 PM 3/14/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

namespace Tasque
{	
	public class TaskModelNode
	{
		private Task task;
		private string name;
		
		public bool IsSeparator
		{
			get { return (task == null); }
		}
		
		public Task Task
		{
			get { return task; }
		}
		
		public string Name
		{
			get {
				if(task == null)
					return name;
				else
					return task.Name;
			}
		}
	
	
		public TaskModelNode(Task task)
		{
			this.task = task;
		}
		
		public TaskModelNode(string name)
		{
			this.task = null;
			this.name = name;
		}
	}
}
