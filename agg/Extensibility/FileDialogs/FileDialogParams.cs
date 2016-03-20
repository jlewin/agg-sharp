using System;

namespace MatterHackers.Agg.PlatformAbstract
{
	public abstract class FileDialogParams
	{
		public FileDialogParams(String fileTypeFilter, String initialDirectory, string title, string actionButtonLabel)
		{
			this.Filter = fileTypeFilter;
			this.InitialDirectory = initialDirectory;
			this.Title = title;
			this.ActionButtonLabel = actionButtonLabel;
		}

		public int FilterIndex { get; set; }

		/// <summary>
		/// The title of the dialog window. If not set will show 'Open' or 'Save' as appropriate
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// This does not show on Windows (but does on mac.
		/// </summary>
		public string ActionButtonLabel { get; set; }

		/// <summary>
		/// The following are complete examples of valid Filter string values:
		/// All Files|*.*
		/// Word Documents|*.doc|All Files|*.*
		/// </summary>
		public String Filter { get; set; }

		public String InitialDirectory { get; set; }

		public String FileName { get; set; }

		public String[] FileNames { get; set; }
	}
}
