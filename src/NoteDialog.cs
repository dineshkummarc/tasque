// NoteDialog.cs created with MonoDevelop
// User: boyd at 5:24 PM 2/13/2008

using System;
using System.Collections.Generic;
using Mono.Unix;

namespace Tasque
{
	public class NoteDialog : Gtk.Dialog
	{
		private Task task;
		
		Gtk.VBox targetVBox;
		
		#region Constructors
		public NoteDialog (Gtk.Window parentWindow, Task task)
			: base ()
		{
			this.ParentWindow = parentWindow.GdkWindow;
			this.task = task;
			this.Title = String.Format(Catalog.GetString("Notes for: {0:s}"), task.Name);
			this.HasSeparator = false;
			this.SetSizeRequest(350,320);
			this.Icon = Utilities.GetIcon ("tasque-16", 16);
			//this.Flags = Gtk.DialogFlags.DestroyWithParent;
			
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.HscrollbarPolicy = Gtk.PolicyType.Never;

			sw.BorderWidth = 0;
			sw.CanFocus = true;
			sw.Show ();
			
			Gtk.EventBox innerEb = new Gtk.EventBox();
			innerEb.BorderWidth = 0;
			innerEb.ModifyBg (Gtk.StateType.Normal, 
						new Gdk.Color(255,255,255));
			innerEb.ModifyBase (Gtk.StateType.Normal, 
						new Gdk.Color(255,255,255));

			targetVBox = new Gtk.VBox();
			targetVBox.BorderWidth = 5;
			targetVBox.Show ();
			innerEb.Add(targetVBox);
			innerEb.Show ();
			
			if(task.Notes != null) {
				foreach (INote note in task.Notes) {
					NoteWidget noteWidget = new NoteWidget (note);
					noteWidget.TextChanged += OnNoteTextChanged;
					noteWidget.DeleteButtonClicked += OnDeleteButtonClicked;
					noteWidget.Show ();
					targetVBox.PackStart (noteWidget, false, false, 0);
				}
			}
			
			sw.AddWithViewport(innerEb);
			sw.Show ();
			
			VBox.PackStart (sw, true, true, 0);

			if(task.SupportsMultipleNotes) {
				Gtk.Button button = new Gtk.Button(Gtk.Stock.Add);
				button.Show();
				this.ActionArea.PackStart(button);
				button.Clicked += OnAddButtonClicked;
			}
			
			AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close);
					
			Response += delegate (object sender, Gtk.ResponseArgs args) {
				// Hide the window.  The TaskWindow watches for when the
				// dialog is hidden and will take care of the rest.
				Hide ();
			};
		}
		#endregion // Constructors
		
		#region Properties
		public Task Task
		{
			get { return task; }
		}
		#endregion // Properties
		
		#region Private Methods
		#endregion // PrivateMethods
		
		#region Event Handlers
		void OnAddButtonClicked (object sender, EventArgs args)
		{
			Logger.Debug("Add button clicked in dialog");
			NoteWidget noteWidget = new NoteWidget (null);
			noteWidget.TextChanged += OnNoteTextChanged;
			noteWidget.DeleteButtonClicked += OnDeleteButtonClicked;
			noteWidget.Show ();
			targetVBox.PackStart (noteWidget, false, false, 0);
			
			// TODO: Implement NoteDialog.OnAddButtonClicked
		}

		
		void OnDeleteButtonClicked (object sender, EventArgs args)
		{
			NoteWidget nWidget = sender as NoteWidget;
			try {
				task.DeleteNote(nWidget.Note);
				targetVBox.Remove (nWidget);
			} catch(Exception e) {
				Logger.Debug("Unable to delete the note");
				Logger.Debug(e.ToString());
			}
		}
		
		void OnNoteTextChanged (object sender, EventArgs args)
		{
			NoteWidget nWidget = sender as NoteWidget;

			// if null, add a note, else, modify it
			if(nWidget.Note == null) {
				try {
					INote note = task.CreateNote(nWidget.Text);
					nWidget.Note = note;
				} catch(Exception e) {
					Logger.Debug("Unable to create a note");
					Logger.Debug(e.ToString());
				}
			} else {
				try {
					task.SaveNote(nWidget.Note);
				} catch(Exception e) {
					Logger.Debug("Unable to save note");
					Logger.Debug(e.ToString());
				}
			}
		}
		#endregion // Event Handlers
	}
}
