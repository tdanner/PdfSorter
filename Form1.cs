using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CefSharp.WinForms;

namespace PdfSorter
{
    public partial class Form1 : Form
    {
        private const string Path = @"C:\Users\Tim\Dropbox\My ScanSnap\";
        private const string AboutBlank = "about:blank";

        private string _typeAhead = string.Empty;
        private readonly HashSet<string> _candidates;
        private string _currentFile;
        private string _targetPath;
        private readonly ChromiumWebBrowser _webBrowser1;

        public Form1()
        {
            InitializeComponent();

            _webBrowser1 = new ChromiumWebBrowser(AboutBlank);
            _webBrowser1.Dock = DockStyle.Fill;
            _webBrowser1.StatusMessage += _webBrowser1_StatusMessage;
            _webBrowser1.LoadError += _webBrowser1_LoadError;
            _webBrowser1.NavStateChanged += _webBrowser1_NavStateChanged;
            _webBrowser1.FrameLoadStart += _webBrowser1_FrameLoadStart;
            _webBrowser1.FrameLoadEnd += _webBrowser1_FrameLoadEnd;
            _webBrowser1.ConsoleMessage += _webBrowser1_ConsoleMessage;

            splitContainer1.Panel2.Controls.Add(_webBrowser1);

            _candidates = new HashSet<string>(Directory.GetDirectories(Path, "*", SearchOption.AllDirectories).Select(d => d.Substring(Path.Length)),
                StringComparer.OrdinalIgnoreCase);
        }

        void _webBrowser1_ConsoleMessage(object sender, CefSharp.ConsoleMessageEventArgs e)
        {
            Log("_webBrowser1_ConsoleMessage: {0} at {1}:{2}", e.Message, e.Source, e.Line);
        }

        void _webBrowser1_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            Log("_webBrowser1_FrameLoadEnd: {0}", e.Url);
        }

        void _webBrowser1_FrameLoadStart(object sender, CefSharp.FrameLoadStartEventArgs e)
        {
            Log("_webBrowser1_FrameLoadStart: {0}", e.Url);
        }

        void _webBrowser1_NavStateChanged(object sender, CefSharp.NavStateChangedEventArgs e)
        {
            Log("_webBrowser1_NavStateChanged: CanGoBack={0}, CanGoForward={1}, CanReload={2}", e.CanGoBack, e.CanGoForward, e.CanReload);
        }

        void _webBrowser1_LoadError(object sender, CefSharp.LoadErrorEventArgs e)
        {
            Log("_webBrowser1_LoadError: {0} - {1} loading {2}", e.ErrorCode, e.ErrorText, e.FailedUrl);
        }

        void _webBrowser1_StatusMessage(object sender, CefSharp.StatusMessageEventArgs e)
        {
            Log("_webBrowser1_StatusMessage: {0}", e.Value);
        }

        private static void Log(string format, params object[] objs)
        {
            Trace.WriteLine(string.Format(format, objs));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void ShowNextFile()
        {
            var files = Directory.GetFiles(Path, "*.pdf");
            _currentFile = files[0];
            _webBrowser1.Load(_currentFile);
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
            _webBrowser1.Load(AboutBlank);
            while (_webBrowser1.IsLoading)
            {
                Application.DoEvents();
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
            var fileName = System.IO.Path.GetFileName(_currentFile);
            if (fileName == null)
                throw new ApplicationException(string.Format("Can't move path {0} which has no filename.", _currentFile));
            File.Move(_currentFile, System.IO.Path.Combine(_targetPath, fileName));
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

        private void Form1_Shown(object sender, EventArgs e)
        {
            ShowNextFile();
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            ShowNextFile();
        }
    }
}
