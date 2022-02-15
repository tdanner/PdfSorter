using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.ClipperLib;
using Microsoft.ML;

namespace PdfSorter;

public partial class MainWindow
{
    private const string BasePath = @"C:\Users\Tim\OneDrive\ScanSnap\";
    private const string SourcePath = @"C:\Users\Tim\OneDrive\ScanSnap2\";
    private const string ModelPath = @"C:\Users\tim\OneDrive\PdfSorterModel.zip";

    public static readonly RoutedCommand NewFolderCommand = new();

    private readonly List<string> _candidates;
    private readonly DocLabelService _docLabelService;
    private readonly List<string> _files;
    private readonly int _filesPosition;

    private readonly MLContext _mlContext = new();
    private List<string> _sortedCandidates;

    public MainWindow()
    {
        InitializeComponent();

        NewFolderCommand.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));

        var stopwatch = Stopwatch.StartNew();
        _docLabelService = new DocLabelService(_mlContext);
        _docLabelService.LoadModelFromFile(ModelPath);
        Debug.WriteLine($"Loaded model in {stopwatch.ElapsedMilliseconds} ms");

        _candidates =
            new List<string>(Directory.GetDirectories(BasePath, "*", SearchOption.AllDirectories)
                .Where(d => !d.Contains(".organizer"))
                .Select(d => d[BasePath.Length..])) { "Trash" };

        _files = Directory.GetFiles(SourcePath, "*.pdf").ToList();
        _filesPosition = 0;

        InitBrowser();
        FindBestMatch();
    }

    private async void InitBrowser()
    {
        await PreviewBrowser.EnsureCoreWebView2Async();
        ShowCurrentFile();
    }

    private static string LoadTextFromPdf(string pdfPath)
    {
        PdfReader pdfReader = new(pdfPath);
        PdfDocument pdfDoc = new(pdfReader);
        int pageCount = pdfDoc.GetNumberOfPages();
        StringBuilder pdfText = new();
        for (int pageNum = 1; pageNum <= pageCount; pageNum++)
        {
            try
            {
                pdfText.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(pageNum)));
            }
            catch (ClipperException)
            {
                // no text from this page I guess
            }
        }

        pdfDoc.Close();
        pdfReader.Close();

        return pdfText.ToString();
    }

    private static IEnumerable<Doc> LoadTrainingData()
    {
        const string sourceDir = @"C:\Users\tim\OneDrive\ScanSnap";
        foreach (string path in Directory.EnumerateFiles(sourceDir, "*.pdf", SearchOption.AllDirectories))
        {
            string directoryName = Path.GetDirectoryName(path);
            Debug.Assert(directoryName != null);
            if (directoryName == sourceDir)
            {
                continue;
            }

            string category = directoryName[(sourceDir.Length + 1)..];

            if (category.Contains(".organizer"))
            {
                continue;
            }

            string text = LoadTextFromPdf(path);

            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"No text found in {path}");
                continue;
            }

            yield return new Doc { Path = path, Category = category, Text = LoadTextFromPdf(path) };
        }
    }

    private static void DoTraining()
    {
        MLContext mlContext = new();
        Console.WriteLine($"{DateTime.Now:T} Loading training data...");
        IEnumerable<Doc> trainingData = LoadTrainingData().ToList();
        Console.WriteLine($"{DateTime.Now:T} Training data loaded. Starting training...");
        DocTrainingService docTrainingService = new(mlContext);
        ITransformer model = docTrainingService.AutoTrain(trainingData, TimeSpan.FromMinutes(10));
        Console.WriteLine($"{DateTime.Now:T} Training complete. Saving model...");
        docTrainingService.SaveModel(@"C:\Users\tim\Desktop\doc-model.dat", model);
    }

    private void FindBestMatch()
    {
        if (_files.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(TypeAheadTextBox.Text))
        {
            _sortedCandidates =
                _candidates.Select(p => new { Value = p, Quality = MatchQuality(p, TypeAheadTextBox.Text) })
                    .Where(x => x.Quality < int.MaxValue)
                    .OrderBy(x => x.Quality)
                    .Select(x => x.Value)
                    .ToList();
        }
        else
        {
            string pdfPath = _files[_filesPosition];
            string pdfText = LoadTextFromPdf(pdfPath);
            var pdfDoc = new Doc { Path = pdfPath, Text = pdfText };
            DocPrediction prediction = _docLabelService.Predict(pdfDoc);
            _sortedCandidates = _docLabelService.GetSortedCategories(prediction);
        }

        CandidateListBox.ItemsSource = _sortedCandidates;
        CandidateListBox.SelectedIndex = 0;
    }

    private static int MatchQuality(string str, string pattern)
    {
        int strPos = 0, patternPos = 0;
        while (strPos < str.Length && patternPos < pattern.Length)
        {
            if (char.ToUpper(str[strPos]) == char.ToUpper(pattern[patternPos]))
            {
                patternPos++;
            }

            strPos++;
        }

        if (patternPos == pattern.Length)
        {
            return strPos;
        }

        return int.MaxValue - patternPos;
    }

    private static void Log(string format, params object[] args)
    {
        Trace.WriteLine(string.Format(format, args));
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
            {
                throw new ApplicationException($"Can't move path {sourcePath} which has no filename.");
            }

            string targetPath = Path.Combine(targetDirectoryPath, fileName);

            Log("Moving {0} to {1}", sourcePath, targetPath);
            File.Move(sourcePath, targetPath);
        }

        _files.RemoveAt(_filesPosition);
        ShowCurrentFile();

        TypeAheadTextBox.Text = string.Empty;

        FindBestMatch();
    }

    private void ShowCurrentFile()
    {
        if (_files.Count > _filesPosition)
        {
            PreviewBrowser.CoreWebView2.Navigate(_files[_filesPosition]);
            Title = $"{Path.GetFileName(_files[_filesPosition])} ({_files.Count} files)";
        }
        else
        {
            PreviewBrowser.CoreWebView2.Navigate("about:blank");
            Title = "No files";
        }
    }

    private void NewFolderCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        string newDir = Path.Combine(BasePath, TypeAheadTextBox.Text);
        Directory.CreateDirectory(newDir);
        _candidates.Add(TypeAheadTextBox.Text);
        FindBestMatch();
    }
}
