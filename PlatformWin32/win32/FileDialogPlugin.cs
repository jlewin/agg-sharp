using System;
using System.IO;
﻿using MatterHackers.Agg.UI;
using System;
﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using MatterHackers.Agg.PlatformAbstract;
using System.Collections.Generic;

namespace MatterHackers.Agg.WindowsFileDialogs
{
    public class FileDialogPlugin : FileDialogProvider
    {
		// Resolve not needed on non-Mac platforms
		public override string ResolveFilePath(string path)
		{
			return path;
		}

		// TODO: Investigate if this signature is used and/or why it's missing for the lastest
		public override IEnumerable<string> ResolveFilePaths(IEnumerable<string> filePaths)
		{
			// Only perform Mac file reference resoltion when the string starts with the expected token
			return filePaths.Select(path => !path.StartsWith("/.file") ? path : this.ResolveFilePath(path));
		}

		public override bool OpenFileDialog(OpenFileDialogParams openParams, OpenFileDialogDelegate callback)
			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = true;
			openParams.FileName = "";
			openParams.FileNames = null;
			OpenFileDialog openFileDialog1 = new OpenFileDialog();

			openFileDialog1.InitialDirectory = openParams.InitialDirectory;
			openFileDialog1.Filter = openParams.Filter;
			openFileDialog1.Multiselect = openParams.MultiSelect;
			openFileDialog1.Title = openParams.Title;

			openFileDialog1.FilterIndex = openParams.FilterIndex;
			openFileDialog1.RestoreDirectory = true;

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				openParams.FileNames = openFileDialog1.FileNames;
				openParams.FileName = openFileDialog1.FileName;
			}

			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = false;

			UiThread.RunOnIdle((state) =>
			{
				OpenFileDialogParams openParamsIn = state as OpenFileDialogParams;
				if (openParamsIn != null)
				{
					callback(openParamsIn);
				}
			}, openParams);
			return true;
		}

		public override bool SelectFolderDialog(SelectFolderDialogParams folderParams, SelectFolderDialogDelegate callback)
		{
			SelectFolderDialog(ref folderParams);
			UiThread.RunOnIdle(() =>
			{
				callback(folderParams);
			});
			return true;
		}

		private string SelectFolderDialog(ref SelectFolderDialogParams folderParams)
		{
			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = true;

			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			folderBrowserDialog.Description = folderParams.Description;
			switch (folderParams.RootFolder)
			{
				case SelectFolderDialogParams.RootFolderTypes.MyComputer:
					folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
					break;

				default:
					throw new NotImplementedException();
			}
			folderBrowserDialog.ShowNewFolderButton = folderParams.ShowNewFolderButton;

			folderBrowserDialog.ShowDialog();

			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = false;
			folderParams.FolderPath = folderBrowserDialog.SelectedPath;

			return folderBrowserDialog.SelectedPath;
		}

		public override bool SaveFileDialog(SaveFileDialogParams saveParams, SaveFileDialogDelegate callback)
		{
			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = true;
			SaveFileDialogParams SaveFileDialogDialogParams = saveParams;

			SaveFileDialog saveFileDialog1 = new SaveFileDialog();

			saveFileDialog1.InitialDirectory = SaveFileDialogDialogParams.InitialDirectory;
			saveFileDialog1.Filter = saveParams.Filter;
			saveFileDialog1.FilterIndex = saveParams.FilterIndex;
			saveFileDialog1.RestoreDirectory = true;
			saveFileDialog1.AddExtension = true;
			saveFileDialog1.FileName = saveParams.FileName;

			saveFileDialog1.Title = saveParams.Title;
			saveFileDialog1.ShowHelp = false;
			saveFileDialog1.OverwritePrompt = true;
			saveFileDialog1.CheckPathExists = true;
			saveFileDialog1.SupportMultiDottedExtensions = true;
			saveFileDialog1.ValidateNames = false;

			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				SaveFileDialogDialogParams.FileName = saveFileDialog1.FileName;
			}

			WidgetForWindowsFormsAbstract.MainWindowsFormsWindow.ShowingSystemDialog = false;

			UiThread.RunOnIdle(() =>
			{
				callback(saveParams);
			});

			return true;
		}
	}
}