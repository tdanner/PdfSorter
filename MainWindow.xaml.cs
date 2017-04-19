using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using CefSharp;

namespace PdfSorter
{
    public partial class MainWindow
    {
        private const string BasePath = @"C:\Users\Tim\Dropbox\My ScanSnap\";

        private readonly List<string> _candidates;
        private List<string> _sortedCandidates;
        private readonly List<string> _files;
        private readonly int _filesPosition;

        public MainWindow()
        {
            InitializeComponent();

            _candidates =
                new List<string>(Directory.GetDirectories(BasePath, "*", SearchOption.AllDirectories)
                    .Where(d => !d.Contains(".organizer"))
                    .Select(d => d.Substring(BasePath.Length))) {"Trash"};

            _files = Directory.GetFiles(BasePath, "*.pdf").ToList();
            _filesPosition = 0;
            
            ShowCurrentFile();
            
            FindBestMatch();
        }

        private void FindBestMatch()
        {
            _sortedCandidates =
                _candidates.Select(p => new {Value = p, Quality = MatchQuality(p, TypeAheadTextBox.Text)})
                    .Where(x => x.Quality < int.MaxValue)
                    .OrderBy(x => x.Quality)
                    .Select(x => x.Value)
                    .ToList();
            CandidateListBox.ItemsSource = _sortedCandidates;
            CandidateListBox.SelectedIndex = 0;
        }

        private static int MatchQuality(string str, string pattern)
        {
            int strPos = 0, patternPos = 0;
            while (strPos < str.Length && patternPos < pattern.Length)
            {                
                if (char.ToUpper(str[strPos]) == char.ToUpper(pattern[patternPos]))
                    patternPos++;

                strPos++;
            }

            if (patternPos == pattern.Length)
                return strPos;

            return int.MaxValue - patternPos;
        }

        private void Log(string format, params object[] objs)
        {
            Trace.WriteLine(string.Format(format, objs));
        }

        private void ChromiumWebBrowser_OnFrameLoadStart(object sender, FrameLoadStartEventArgs e)
        {
            Log("OnFrameLoadStart: {0}", e.Url);
        }

        private void ChromiumWebBrowser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            Log("OnFrameLoadEnd: {0} {1}", e.HttpStatusCode, e.Url);
        }

        private void TypeAheadTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            FindBestMatch();
        }

        private void TypeAheadTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    CandidateListBox.SelectedIndex -= 1;
                    e.Handled = true;
                    break;
                case Key.Down:
                    CandidateListBox.SelectedIndex += 1;
                    e.Handled = true;
                    break;
                case Key.Return:
                    CommitMove();
                    e.Handled = true;
                    break;
            }
        }

        private void CommitMove()
        {
            string sourcePath = _files[_filesPosition];
            string targetDirectoryName = (string)CandidateListBox.SelectedItem;

            if (targetDirectoryName == "Trash")
            {
                Log("Deleting {0}", sourcePath);
                File.Delete(sourcePath);
            }
            else
            {
                string targetDirectoryPath = Path.Combine(BasePath, targetDirectoryName);

                string fileName = Path.GetFileName(sourcePath);

                if (fileName == null)
                    throw new ApplicationException(string.Format("Can't move path {0} which has no filename.", sourcePath));

                string targetPath = Path.Combine(targetDirectoryPath, fileName);

                Log("Moving {0} to {1}", sourcePath, targetPath);
                File.Move(sourcePath, targetPath);
            }

            _files.RemoveAt(_filesPosition);
            ShowCurrentFile();

            TypeAheadTextBox.SelectAll();
        }

        private void ShowCurrentFile()
        {
            if (_files.Count > _filesPosition)
            {
                PreviewBrowser.Address = _files[_filesPosition];
                Title = string.Format("{0} ({1} files)", Path.GetFileName(_files[_filesPosition]), _files.Count);
            }
            else
            {
                PreviewBrowser.Address = "about:blank";
                Title = "No files";
            }
        }
    }
}
