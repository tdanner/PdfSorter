using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PdfSorter
{
    public partial class Form1 : Form
    {
        const string Path = @"C:\Users\Tim\Dropbox\My ScanSnap\";

        private string _typeAhead = string.Empty;
        private readonly HashSet<string> _candidates;
        private string _currentFile;
        private string _targetPath;

        public Form1()
        {
            InitializeComponent();
            _candidates = new HashSet<string>(Directory.GetDirectories(Path, "*", SearchOption.AllDirectories).Select(d => d.Substring(Path.Length)),
                StringComparer.OrdinalIgnoreCase);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ShowNextFile();
        }

        private void ShowNextFile()
        {
            var files = Directory.GetFiles(Path, "*.pdf");
            _currentFile = files[0];
            webBrowser1.Navigate(_currentFile);
        }

        private void FindBestMatch()
        {
            string bestMatch = _candidates.OrderBy(p => MatchQuality(p, _typeAhead)).First();
            textBox1.Text = bestMatch;
            _targetPath = System.IO.Path.Combine(Path, bestMatch);
        }

        private static int MatchQuality(string str, string pattern)
        {
            int matchIndex = str.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
                matchIndex = int.MaxValue;
            return matchIndex;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            _typeAhead += e.KeyChar;
            FindBestMatch();
        }

        private void moveButton_Click(object sender, EventArgs e)
        {
            webBrowser1.Navigate("about:blank");
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
            File.Move(_currentFile, System.IO.Path.Combine(_targetPath, System.IO.Path.GetFileName(_currentFile)));
            ShowNextFile();
            _typeAhead = string.Empty;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                _typeAhead = string.Empty;
                _targetPath = string.Empty;
                textBox1.Text = string.Empty;
            }
        }
    }
}
