using System;

namespace MatterHackers.Agg.PlatformAbstract
{
	public class OpenFileDialogParams : FileDialogParams
	{
		public bool MultiSelect { get; set; }

		/// <summary>
		/// These are the parameters passed to an open file dialog
		/// </summary>
		public OpenFileDialogParams(String fileTypeFilter, String initialDirectory = "", bool multiSelect = false, string title = "", string actionButtonLabel = "")
			: base(fileTypeFilter, initialDirectory, title, actionButtonLabel)
		{
			this.MultiSelect = multiSelect;
		}
	}
}