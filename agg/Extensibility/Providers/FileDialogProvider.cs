using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MatterHackers.Agg.PlatformAbstract
{
    public abstract class FileDialogProvider
    {

		public abstract IEnumerable<string> ResolveFilePaths(IEnumerable<string> filePaths);
		public abstract string ResolveFilePath(string path);
		
        public delegate void OpenFileDialogDelegate(OpenFileDialogParams openParams);
        public delegate void SelectFolderDialogDelegate(SelectFolderDialogParams folderParams);
        public delegate void SaveFileDialogDelegate(SaveFileDialogParams saveParams);

        public abstract bool OpenFileDialog(OpenFileDialogParams openParams, OpenFileDialogDelegate callback);
        public abstract bool SelectFolderDialog(SelectFolderDialogParams folderParams, SelectFolderDialogDelegate callback);
        public abstract bool SaveFileDialog(SaveFileDialogParams saveParams, SaveFileDialogDelegate callback);



    }
}
