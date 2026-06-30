using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PdfDrive.Models;
using PdfDrive.Services;

namespace PdfDrive;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<PdfFileItem> _files = new();
    private bool _initialized;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        FilesList.ItemsSource = _files;
        _files.CollectionChanged += (_, _) => UpdateUiState();

        OpenAfterSaveChk.IsChecked = _settings.OpenAfterSave;
        _initialized = true;

        // Список языков
        foreach (var kv in Loc.SupportedLangs)
            LangCombo.Items.Add(new ComboBoxItem { Content = kv.Value, Tag = kv.Key });
        SelectLangInCombo(Loc.CurrentLang);

        Loc.LanguageChanged += ApplyTexts;
        ThemeService.ThemeChanged += () => UpdateThemeButton();

        ApplyTexts();
        UpdateThemeButton();
        UpdateUiState();
    }

    // ──────────────────────────────────────────────────────────── тексты
    private void ApplyTexts()
    {
        TitleLabel.Text = Loc.T("app.title");
        SubLabel.Text   = Loc.T("app.subtitle");
        AddBtn.Content   = Loc.T("btn.add");
        ClearBtn.Content = Loc.T("btn.clear");
        SaveBtn.Content  = Loc.T("btn.save");
        PreviewBtn.Content = Loc.T("btn.preview");
        OpenAfterSaveChk.Content = Loc.T("chk.openafter");
        EmptyHint.Text   = Loc.T("list.empty");
        AskAiBtn.Content = Loc.T("ai.button");
        UpdateThemeButton();
        UpdateUiState();
    }

    private void UpdateThemeButton() => ThemeBtn.Content = Loc.T("btn.theme");

    private void AskAiBtn_Click(object sender, RoutedEventArgs e)
    {
        var w = new Views.AskAiWindow { Owner = this };
        w.Show();
    }

    // ──────────────────────────────────────────────────────────── состояние UI
    private void UpdateUiState()
    {
        EmptyHint.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SaveBtn.IsEnabled = _files.Count >= 1;
        PreviewBtn.IsEnabled = _files.Count >= 1;
        ClearBtn.IsEnabled = _files.Count > 0;
        if (_files.Count > 0)
            StatusLabel.Text = Loc.T("status.ready", ("n", _files.Count));
        else
            StatusLabel.Text = "";
    }

    // ──────────────────────────────────────────────────────────── язык / тема
    private void SelectLangInCombo(string lang)
    {
        foreach (ComboBoxItem item in LangCombo.Items)
            if ((string)item.Tag == lang) { LangCombo.SelectedItem = item; return; }
    }

    private void LangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LangCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            Loc.SetLang(lang);
            _settings.Lang = lang;
            _settings.Save();
        }
    }

    private void ThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        _settings.Theme = ThemeService.Toggle();
        _settings.Save();
    }

    // ──────────────────────────────────────────────────────────── добавление
    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = Loc.T("dlg.add.title"),
            Filter = FileTypes.OpenFilter,
            Multiselect = true,
        };
        if (dlg.ShowDialog(this) == true)
            AddFiles(dlg.FileNames);
    }

    private void AddFiles(System.Collections.Generic.IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            var kind = FileTypes.Detect(path);
            if (kind == null) continue;  // неподдерживаемый формат — пропускаем
            if (_files.Any(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            var item = new PdfFileItem(path, kind.Value)
            {
                // У изображения всегда 1 страница; у PDF — best-effort подсчёт.
                Pages = kind == FileKind.Image ? 1 : PdfMerger.TryCountPages(path),
            };
            _files.Add(item);
        }
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e) => _files.Clear();

    // ──────────────────────────────────────────────────────────── порядок / удаление
    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PdfFileItem item)
        {
            int i = _files.IndexOf(item);
            if (i > 0) _files.Move(i, i - 1);
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PdfFileItem item)
        {
            int i = _files.IndexOf(item);
            if (i >= 0 && i < _files.Count - 1) _files.Move(i, i + 1);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PdfFileItem item)
            _files.Remove(item);
    }

    // ──────────────────────────────────────────────────────────── drag & drop
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            AddFiles(paths);
    }

    // ──────────────────────────────────────────────────────────── «открыть после сохранения»
    private void OpenAfterSaveChk_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _settings.OpenAfterSave = OpenAfterSaveChk.IsChecked == true;
        _settings.Save();
    }

    // ──────────────────────────────────────────────────────────── предпросмотр
    private void PreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count < 1)
        {
            StatusLabel.Text = Loc.T("status.empty");
            return;
        }

        try
        {
            StatusLabel.Text = Loc.T("status.preview");
            // Собираем во временный файл и открываем во внешней программе просмотра PDF.
            string tmp = Path.Combine(Path.GetTempPath(),
                "PdfDrive_preview_" + Guid.NewGuid().ToString("N") + ".pdf");
            PdfMerger.Build(_files.ToList(), tmp);
            OpenFile(tmp);
            StatusLabel.Text = Loc.T("status.previewdone");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = Loc.T("status.error", ("msg", ex.Message));
            MessageBox.Show(this, ex.Message, Loc.T("app.title"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ──────────────────────────────────────────────────────────── сохранение
    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count < 1)
        {
            StatusLabel.Text = Loc.T("status.empty");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = Loc.T("dlg.save.title"),
            Filter = Loc.T("dlg.savepdf.filter"),
            FileName = Loc.T("save.default") + ".pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
        };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            StatusLabel.Text = Loc.T("status.merging");
            PdfMerger.Build(_files.ToList(), dlg.FileName);
            StatusLabel.Text = Loc.T("status.done", ("path", Path.GetFileName(dlg.FileName)));

            if (_settings.OpenAfterSave)
                OpenFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = Loc.T("status.error", ("msg", ex.Message));
            MessageBox.Show(this, ex.Message, Loc.T("app.title"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Открывает файл в программе по умолчанию (системный просмотрщик PDF).</summary>
    private static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
