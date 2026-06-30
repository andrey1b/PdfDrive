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

            // Окно «Спросить у ИИ»
            ["ai.button"]      = "🤖  Спросить у ИИ",
            ["ai_title"]       = "Спросить у ИИ",
            ["ai_question"]    = "Вопрос:",
            ["ai_ask"]         = "▶  Спросить",
            ["ai_save_all"]    = "Сохранить все",
            ["ai_clear"]       = "Очистить",
            ["ai_api_keys"]    = "⚙ API ключи",
            ["ai_save"]        = "Сохранить",
            ["ai_copy"]        = "Копировать",
            ["ai_copied"]      = "Скопировано в буфер",
            ["ai_quick"]       = "Быстрые вопросы:",
            ["ai_qb_compress"] = "Сжать PDF",
            ["ai_qb_merge"]    = "Объединить файлы",
            ["ai_qb_convert"]  = "Конвертировать",
            ["ai_q_compress"]  = "Как уменьшить размер PDF-файла без заметной потери качества? Дай пошаговую инструкцию.",
            ["ai_q_merge"]     = "Как объединить несколько PDF и изображений в один файл? Подскажи простые способы.",
            ["ai_q_convert"]   = "Как конвертировать PDF в Word (или наоборот) с сохранением форматирования?",
            ["ai_empty_title"] = "Вопрос пуст",
            ["ai_empty_msg"]   = "Введите вопрос.",
            ["ai_nothing_msg"] = "Нет ответа для сохранения.",
            ["ai_status_wait"] = "⌛ Жду ответы от ИИ…",
            ["ai_status_done"] = "✓ Готово!",
            ["ai_status_browser"]="🌐 Открыт(ы) в браузере: {list}",
            ["ai_status_none"] = "Нет выбранных ИИ.",
            ["ai_browser_note"]= "🌐 Вопрос открыт в браузере.\nВопрос скопирован в буфер — вставьте (Ctrl+V) в чат.\nСкопируйте ответ сюда после получения.",
            ["ai_req_to"]      = "⌛ Запрос к {name}…",
            ["ai_err"]         = "❌ Ошибка",
            ["ai_stats"]       = "Символов: {chars} | Слов: {words}",
            ["ai_save_header"] = "Ответы ИИ",
            ["ai_keys_title"]  = "API ключи для ИИ",
            ["ai_keys_save"]   = "Сохранить",
            ["ai_key_claude_link"]     = "Получить на console.anthropic.com",
            ["ai_key_gemini_link"]     = "Бесплатно на aistudio.google.com",
            ["ai_key_deepseek_link"]   = "Получить на platform.deepseek.com",
            ["ai_key_perplexity_link"] = "Получить на perplexity.ai/settings/api",
            ["btn.add"]          = "➕  Добавить файлы",
            ["btn.up"]           = "▲",
            ["btn.down"]         = "▼",
            ["btn.remove"]       = "✕",
            ["btn.clear"]        = "Очистить список",
            ["btn.save"]         = "💾  Сохранить как один PDF",
            ["btn.preview"]      = "👁  Предпросмотр",
            ["btn.theme"]        = "🌓 Тема",
            ["chk.openafter"]    = "Открыть после сохранения",
            ["status.preview"]   = "Создаю предпросмотр…",
            ["status.previewdone"] = "Предпросмотр открыт во внешней программе.",
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

            // Ask-AI window
            ["ai.button"]      = "🤖  Ask AI",
            ["ai_title"]       = "Ask AI",
            ["ai_question"]    = "Question:",
            ["ai_ask"]         = "▶  Ask",
            ["ai_save_all"]    = "Save all",
            ["ai_clear"]       = "Clear",
            ["ai_api_keys"]    = "⚙ API keys",
            ["ai_save"]        = "Save",
            ["ai_copy"]        = "Copy",
            ["ai_copied"]      = "Copied to clipboard",
            ["ai_quick"]       = "Quick questions:",
            ["ai_qb_compress"] = "Compress PDF",
            ["ai_qb_merge"]    = "Merge files",
            ["ai_qb_convert"]  = "Convert",
            ["ai_q_compress"]  = "How can I reduce a PDF file size without noticeable quality loss? Give step-by-step instructions.",
            ["ai_q_merge"]     = "How can I merge several PDFs and images into one file? Suggest simple ways.",
            ["ai_q_convert"]   = "How can I convert PDF to Word (or vice versa) while keeping the formatting?",
            ["ai_empty_title"] = "Empty question",
            ["ai_empty_msg"]   = "Enter a question.",
            ["ai_nothing_msg"] = "No answer to save.",
            ["ai_status_wait"] = "⌛ Waiting for AI…",
            ["ai_status_done"] = "✓ Done!",
            ["ai_status_browser"]="🌐 Opened in browser: {list}",
            ["ai_status_none"] = "No AI selected.",
            ["ai_browser_note"]= "🌐 Question opened in the browser.\nIt is copied to the clipboard — paste (Ctrl+V) into the chat.\nCopy the answer back here.",
            ["ai_req_to"]      = "⌛ Request to {name}…",
            ["ai_err"]         = "❌ Error",
            ["ai_stats"]       = "Chars: {chars} | Words: {words}",
            ["ai_save_header"] = "AI answers",
            ["ai_keys_title"]  = "API keys for AI",
            ["ai_keys_save"]   = "Save",
            ["ai_key_claude_link"]     = "Get it at console.anthropic.com",
            ["ai_key_gemini_link"]     = "Free at aistudio.google.com",
            ["ai_key_deepseek_link"]   = "Get it at platform.deepseek.com",
            ["ai_key_perplexity_link"] = "Get it at perplexity.ai/settings/api",
            ["btn.add"]          = "➕  Add files",
            ["btn.up"]           = "▲",
            ["btn.down"]         = "▼",
            ["btn.remove"]       = "✕",
            ["btn.clear"]        = "Clear list",
            ["btn.save"]         = "💾  Save as one PDF",
            ["btn.preview"]      = "👁  Preview",
            ["btn.theme"]        = "🌓 Theme",
            ["chk.openafter"]    = "Open after saving",
            ["status.preview"]   = "Building preview…",
            ["status.previewdone"] = "Preview opened in the external viewer.",
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
