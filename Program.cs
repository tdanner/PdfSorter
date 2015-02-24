using System;
using System.Windows.Forms;
using CefSharp;

namespace PdfSorter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            InitChromium();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void InitChromium()
        {
            var settings = new CefSettings();
            if (!Cef.Initialize(settings))
            {
                throw new ApplicationException("Unable to intialize Chromium Embedding Framework");
            }
        }
    }
}
