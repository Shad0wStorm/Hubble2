using ClientSupport;
using System;
using System.Windows.Forms;

namespace CobraBay
{
    /// <summary>
    /// Provides a directory selector using a Windows Forms dialog.
    /// </summary>
    class FormsDirectorySelector : DirectorySelector
    {
        /// <summary>
        /// Selects a directory using a folder browser dialog.
        /// </summary>
        /// <param name="initial">The initial path to be shown in the dialog.</param>
        /// <returns>The selected directory path, or null if canceled.</returns>
        public override string SelectDirectory(string initial)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.SelectedPath = initial;
                folderBrowserDialog.ShowNewFolderButton = true;

                DialogResult result = folderBrowserDialog.ShowDialog();

                return result == DialogResult.OK ? folderBrowserDialog.SelectedPath : null;
            }
        }
    }
}
