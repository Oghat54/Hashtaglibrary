using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace HashtagCopier;

public partial class ImportExportDialog : Window
{
    public bool DataChanged { get; private set; } = false;

    public ImportExportDialog()
    {
        InitializeComponent();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Экспорт базы",
            Filter = "JSON (*.json)|*.json",
            FileName = $"hashtagcopier_{DateTime.Now:yyyy-MM-dd}.json"
        };
        if (dlg.ShowDialog() != true) return;

        var data = DataService.Load();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dlg.FileName, json);
        MessageBox.Show($"Сохранено:\n{dlg.FileName}");
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Импорт базы",
            Filter = "JSON (*.json)|*.json"
        };
        if (dlg.ShowDialog() != true) return;

        AppData imported;
        try
        {
            var json = File.ReadAllText(dlg.FileName);
            imported = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось прочитать файл:\n" + ex.Message);
            return;
        }

        // Спросим: заменить или объединить
        var res = MessageBox.Show(
            "Заменить текущую базу?\n\n" +
            "«Да» — заменить полностью.\n" +
            "«Нет» — объединить (франшизы с одинаковым именем сложатся, персонажи добавятся).\n" +
            "«Отмена» — ничего не делать.",
            "Импорт", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        if (res == MessageBoxResult.Cancel) return;

        var current = DataService.Load();

        if (res == MessageBoxResult.Yes)
        {
            current = imported;
        }
        else // Merge
        {
            foreach (var fImp in imported.Franchises)
            {
                var fCur = current.Franchises
                    .FirstOrDefault(x => x.Name.Equals(fImp.Name, StringComparison.OrdinalIgnoreCase));

                if (fCur == null)
                {
                    current.Franchises.Add(fImp);
                }
                else
                {
                    // Франшиза есть — добавляем только новых персонажей
                    foreach (var cImp in fImp.Characters)
                    {
                        if (!fCur.Characters.Any(c => c.Name.Equals(cImp.Name, StringComparison.OrdinalIgnoreCase)))
                            fCur.Characters.Add(cImp);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(imported.DefaultCommunityTag))
                current.DefaultCommunityTag = imported.DefaultCommunityTag;
        }

        DataService.Save(current);
        DataChanged = true;
        MessageBox.Show("Импорт завершён.");
    }
}