﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using CefSharp;

namespace PdfSorter
{
    public partial class App
    {
        private App()
        {
            var settings = new CefSettings();
            Cef.Initialize(settings);
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Current.DispatcherUnhandledException += AppDispatcherUnhandledException;
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            string errorMessage =
                string.Format(
                    "An application error occurred.\nPlease check whether your data is correct and repeat the action. If this error occurs again there seems to be a more serious malfunction in the application, and you better close it.\n\nError:{0}\n\nDo you want to continue?\n(if you click Yes you will continue with your work, if you click No the application will close)",

                    e.Exception.Message + (e.Exception.InnerException != null
                        ? "\n" +
                          e.Exception.InnerException.Message
                        : null));

            if (
                MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) ==
                MessageBoxResult.No)
            {
                if (MessageBox.Show(
                    "WARNING: The application will close. Any changes will not be saved!\nDo you really want to close it?",
                    "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) ==
                    MessageBoxResult.Yes)
                {
                    Current.Shutdown();
                }
            }
        }
    }
}