using System;
using System.Collections.Generic;

namespace PdfDrive.Services;

/// <summary>
/// Локализация уровня приложения. Словари RU/EN эмбеддены в код.
/// </summary>
public static class Loc
{
    public const string FallbackLang = "ru";

    public static IReadOnlyDictionary<string, string> SupportedLangs { get; } =
        new Dictionary<string, string>
        {
            ["ru"] = "Русский",
            ["en"] = "English",
        };

    private static string _lang = FallbackLang;
    public static string CurrentLang => _lang;

    /// <summary>Подписка на смену языка — окна перерисовывают подписи.</summary>
    public static event Action? LanguageChanged;

    public static void SetLang(string lang)
    {
        if (string.IsNullOrEmpty(lang) || !_dictionaries.ContainsKey(lang)) return;
        if (_lang == lang) return;
        _lang = lang;
        LanguageChanged?.Invoke();
    }

    /// <summary>Получить локализованную строку по ключу.</summary>
    public static string T(string key, params (string name, object value)[] args)
    {
        var dict = _dictionaries.TryGetValue(_lang, out var d) ? d : _dictionaries[FallbackLang];
        if (!dict.TryGetValue(key, out var text))
            text = _dictionaries[FallbackLang].GetValueOrDefault(key, key);

        foreach (var (n, v) in args)
            text = text.Replace("{" + n + "}", v?.ToString() ?? "");

        return text;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new()
    {
        ["ru"] = new Dictionary<string, string>
        {
            ["app.title"]        = "PDF Drive",
            ["app.subtitle"]     = "Сборка PDF, изображений, текста и Word в один файл",
            ["btn.add"]          = "➕  Добавить файлы",
            ["btn.up"]           = "▲",
            ["btn.down"]         = "▼",
            ["btn.remove"]       = "✕",
            ["btn.clear"]        = "Очистить список",
            ["btn.save"]         = "💾  Сохранить как один PDF",
            ["btn.theme"]        = "🌓 Тема",
            ["list.empty"]       = "Список пуст. Нажмите «Добавить файлы» или перетащите PDF, изображения, текст и Word сюда.",
            ["status.ready"]     = "Готово. Файлов в списке: {n}.",
            ["status.merging"]   = "Сборка…",
            ["status.done"]      = "Готово! Создан файл: {path}",
            ["status.empty"]     = "Добавьте хотя бы один файл.",
            ["status.error"]     = "Ошибка: {msg}",
            ["dlg.add.title"]    = "Выберите файлы (PDF, изображения, текст, Word)",
            ["dlg.save.title"]   = "Сохранить итоговый PDF",
            ["dlg.savepdf.filter"] = "Файл PDF (*.pdf)|*.pdf",
            ["filter.supported"] = "Поддерживаемые файлы (PDF, изображения, текст, Word)",
            ["filter.pdf"]       = "Файлы PDF",
            ["filter.images"]    = "Изображения",
            ["filter.text"]      = "Текстовые файлы",
            ["filter.docx"]      = "Документы Word (DOCX)",
            ["filter.all"]       = "Все файлы",
            ["col.name"]         = "Имя файла",
            ["col.pages"]        = "Стр.",
            ["pages.unknown"]    = "?",
            ["save.default"]     = "Обследование",
        },
        ["en"] = new Dictionary<string, string>
        {
            ["app.title"]        = "PDF Drive",
            ["app.subtitle"]     = "Combine PDFs, images, text and Word into one file",
            ["btn.add"]          = "➕  Add files",
            ["btn.up"]           = "▲",
            ["btn.down"]         = "▼",
            ["btn.remove"]       = "✕",
            ["btn.clear"]        = "Clear list",
            ["btn.save"]         = "💾  Save as one PDF",
            ["btn.theme"]        = "🌓 Theme",
            ["list.empty"]       = "List is empty. Click “Add files” or drag PDFs, images, text and Word here.",
            ["status.ready"]     = "Ready. Files in list: {n}.",
            ["status.merging"]   = "Building…",
            ["status.done"]      = "Done! File created: {path}",
            ["status.empty"]     = "Add at least one file.",
            ["status.error"]     = "Error: {msg}",
            ["dlg.add.title"]    = "Select files (PDF, images, text, Word)",
            ["dlg.save.title"]   = "Save resulting PDF",
            ["dlg.savepdf.filter"] = "PDF file (*.pdf)|*.pdf",
            ["filter.supported"] = "Supported files (PDF, images, text, Word)",
            ["filter.pdf"]       = "PDF files",
            ["filter.images"]    = "Images",
            ["filter.text"]      = "Text files",
            ["filter.docx"]      = "Word documents (DOCX)",
            ["filter.all"]       = "All files",
            ["col.name"]         = "File name",
            ["col.pages"]        = "Pages",
            ["pages.unknown"]    = "?",
            ["save.default"]     = "Examination",
        },
    };
}
