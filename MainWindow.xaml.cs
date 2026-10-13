using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HashtagCopier;

public partial class MainWindow : Window
{
    private AppData _data;
    private Franchise? _currentFranchise;

    public MainWindow()
    {
        InitializeComponent();
        _data = DataService.Load();
        ShowFranchises();
        InitializeComponent();
        Loaded += (_, _) =>
    {
    // Регистрируем хоткей, когда окно уже создано (есть HWND)
    HotkeyManager.Register(this, ToggleWindow);

    // Убедимся, что фокус на поиске
    SearchBox.Focus();
    };

    Closing += (_, _) =>
    {
    // Снимаем хоткей при закрытии
    HotkeyManager.Unregister(this);
    };
    _data = DataService.Load();

    // ПКМ по кнопке «Обязательная часть»
    var menu = new ContextMenu();
    var editItem = new MenuItem { Header = "✏ Изменить" };
    editItem.Click += (_, _) => EditMandatoryPart();
    menu.Items.Add(editItem);
    BtnMandatory.ContextMenu = menu;

    UpdateMandatoryUI();

    ShowFranchises();
    Loaded += (_, _) => SearchBox.Focus();
    }

    private void ToggleWindow()
{
    if (WindowState == WindowState.Minimized || !IsVisible || !IsActive)
    {
        // Показать и активировать
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;     // временно поднять
        Topmost = TopmostCheck.IsChecked == true;   // вернуть как было
        SearchBox.Focus();
        SearchBox.SelectAll();
    }
    else
    {
        // Спрятать (свернуть)
        WindowState = WindowState.Minimized;
    }
}
    // ================= ОТРИСОВКА =================
     private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
{
    if (_currentFranchise == null) ShowFranchises();
    else ShowCharacters(_currentFranchise);
}
private void ShowFranchises()
{
    _currentFranchise = null;
    ButtonsPanel.Children.Clear();

    string filter = SearchBox.Text.Trim().ToLowerInvariant();

    var sorted = _data.Franchises
        .Where(f => string.IsNullOrEmpty(filter) || f.Name.ToLowerInvariant().Contains(filter))
        .OrderByDescending(f => f.ClickCount)
        .ToList();

    foreach (var f in sorted)
        ButtonsPanel.Children.Add(
            MakeButton(f.Name, f.ClickCount, () => OnFranchiseClick(f), BuildFranchiseMenu(f)));

    UpdateStats();
    BtnAdd.Content = "Добавить франшизу";
}

    private void ShowCharacters(Franchise franchise)
{
    _currentFranchise = franchise;
    ButtonsPanel.Children.Clear();

    string filter = SearchBox.Text.Trim().ToLowerInvariant();

    var sorted = franchise.Characters
        .Where(c => string.IsNullOrEmpty(filter) || c.Name.ToLowerInvariant().Contains(filter))
        .OrderByDescending(c => c.ClickCount)
        .ToList();

    foreach (var c in sorted)
        ButtonsPanel.Children.Add(
            MakeButton(c.Name, c.ClickCount, () => OnCharacterClick(franchise, c), BuildCharacterMenu(franchise, c)));

    UpdateStats();
    SearchBox.Focus();
    BtnAdd.Content = $"Добавить персонажа";
}
    // ================= КЛИКИ =================

    private void OnFranchiseClick(Franchise f)
{
    f.ClickCount++;
    Clipboard.SetText(DataService.FranchiseHashtag(f));
    DataService.Save(_data);
    SearchBox.Text = "";           // ← добавили
    ShowCharacters(f);
}

    private void OnCharacterClick(Franchise f, Character c)
    {
        c.ClickCount++;
        Clipboard.SetText(DataService.CharacterHashtag(c));
        DataService.Save(_data);
        ShowCharacters(f);
    }

    // ================= ДОБАВЛЕНИЕ ФРАНШИЗЫ =================
    private void Add_Click(object sender, RoutedEventArgs e)
{
    if (_currentFranchise == null)
        AddFranchise();
    else
        AddCharacter(_currentFranchise);
}
   private void AddFranchise()
{
    var dlg = new InputDialog(
        title: "Новая франшиза",
        label1: "Название франшизы", default1: "",
        label2: "Хештег франшизы", default2: "",
        hint: "Без #, например: genshin_impact",
        autoFillSecond: true
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    if (_data.Franchises.Any(f => f.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)))
    {
        MessageBox.Show("Такая франшиза уже есть.");
        return;
    }

    _data.Franchises.Add(new Franchise
    {
        Name = dlg.Value1,
        Hashtag = DataService.SanitizeTag(dlg.Value2)
    });
    DataService.Save(_data);
    ShowFranchises();
}

    // ================= ДОБАВЛЕНИЕ ПЕРСОНАЖА =================

    private void AddCharacter(Franchise f)
{
    var dlg = new InputDialog(
        title: $"Новый персонаж — {f.Name}",
        label1: "Имя персонажа", default1: "",
        label2: "Хештег персонажа", default2: "",
        label3: "Тег сообщества", default3: _data.DefaultCommunityTag,
        hint: "Вставится как #хештег@тег",
        autoFillSecond: true
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    if (f.Characters.Any(c => c.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)))
    {
        MessageBox.Show("Такой персонаж уже есть.");
        return;
    }

    f.Characters.Add(new Character
    {
        Name = dlg.Value1,
        Hashtag = DataService.SanitizeTag(dlg.Value2),
        CommunityTag = DataService.SanitizeTag(dlg.Value3)
    });
    DataService.Save(_data);
    ShowCharacters(f);
}

    // ================= НАСТРОЙКИ =================

    private void EditDefaultCommunityTag_Click(object sender, RoutedEventArgs e)
{
    var dlg = new InputDialog(
        title: "Тег сообщества по умолчанию",
        label1: "Тег (без @)", default1: _data.DefaultCommunityTag,
        hint: "Например: chay_s_pechenikami"
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    _data.DefaultCommunityTag = DataService.SanitizeTag(dlg.Value1);
    DataService.Save(_data);
    MessageBox.Show("Сохранено.");
}

    // ================= ESC =================

    protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Escape && _currentFranchise != null)
    {
        SearchBox.Text = "";       // ← добавили
        ShowFranchises();
        e.Handled = true;
    }
    base.OnKeyDown(e);
}

    // ================= ВСПОМОГАТЕЛЬНЫЕ =================

    private static Button MakeButton(string text, int count, Action onClick, ContextMenu? menu = null)
{
    var main = new TextBlock
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 20, 0)   // место под счётчик справа
    };

    var grid = new Grid();
    grid.Children.Add(main);

    if (count > 0)
    {
        var counter = new TextBlock
        {
            Text = count.ToString(),
            FontSize = 10,
            Opacity = 0.6,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 6, 4),
            IsHitTestVisible = false
        };
        grid.Children.Add(counter);
    }

    var btn = new Button
    {
        Content = grid,
        Margin = new Thickness(6),
        Padding = new Thickness(14, 10, 14, 10),
        MinWidth = 140,
        FontSize = 14,
        Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
        Foreground = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
        Cursor = Cursors.Hand,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,   // ← ключевое
        VerticalContentAlignment = VerticalAlignment.Stretch         // ← ключевое
    };
    btn.Click += (_, _) => onClick();
    if (menu != null) btn.ContextMenu = menu;
    return btn;
}

    private ContextMenu BuildFranchiseMenu(Franchise f)
{
    var menu = new ContextMenu();

    menu.Items.Add(new MenuItem
    {
        Header = $"Нажатий: {f.ClickCount}",
        IsEnabled = false
    });
    menu.Items.Add(new Separator());

    var edit = new MenuItem { Header = "✏ Изменить" };
    edit.Click += (_, _) => EditFranchise(f);
    menu.Items.Add(edit);

    var reset = new MenuItem { Header = "🔄 Сбросить счётчик" };
    reset.Click += (_, _) =>
    {
        f.ClickCount = 0;
        DataService.Save(_data);
        ShowFranchises();
    };
    menu.Items.Add(reset);

    menu.Items.Add(new Separator());

    var del = new MenuItem { Header = "🗑 Удалить" };
    del.Click += (_, _) => DeleteFranchise(f);
    menu.Items.Add(del);

    return menu;
}

private ContextMenu BuildCharacterMenu(Franchise f, Character c)
{
    var menu = new ContextMenu();

    menu.Items.Add(new MenuItem
    {
        Header = $"Нажатий: {c.ClickCount}",
        IsEnabled = false
    });
    menu.Items.Add(new Separator());

    var edit = new MenuItem { Header = "✏ Изменить" };
    edit.Click += (_, _) => EditCharacter(f, c);
    menu.Items.Add(edit);

    var reset = new MenuItem { Header = "🔄 Сбросить счётчик" };
    reset.Click += (_, _) =>
    {
        c.ClickCount = 0;
        DataService.Save(_data);
        ShowCharacters(f);
    };
    menu.Items.Add(reset);

    menu.Items.Add(new Separator());

    var del = new MenuItem { Header = "🗑 Удалить" };
    del.Click += (_, _) => DeleteCharacter(f, c);
    menu.Items.Add(del);

    return menu;
}
private void EditFranchise(Franchise f)
{
    var dlg = new InputDialog(
        title: $"Изменить: {f.Name}",
        label1: "Название франшизы", default1: f.Name,
        label2: "Хештег франшизы", default2: f.Hashtag,
        hint: "Без #",
        autoFillSecond: false   // при редактировании не перезаписываем хештег
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    if (!f.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)
        && _data.Franchises.Any(x => x.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)))
    {
        MessageBox.Show("Франшиза с таким названием уже есть.");
        return;
    }

    f.Name = dlg.Value1;
    f.Hashtag = DataService.SanitizeTag(dlg.Value2);
    DataService.Save(_data);
    ShowFranchises();
}

private void DeleteFranchise(Franchise f)
{
    var res = MessageBox.Show(
        $"Удалить франшизу «{f.Name}» и всех её персонажей ({f.Characters.Count})?",
        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

    if (res != MessageBoxResult.Yes) return;

    _data.Franchises.Remove(f);
    DataService.Save(_data);
    ShowFranchises();
}

private void EditCharacter(Franchise f, Character c)
{
    var dlg = new InputDialog(
        title: $"Изменить: {c.Name}",
        label1: "Имя персонажа", default1: c.Name,
        label2: "Хештег персонажа", default2: c.Hashtag,
        label3: "Тег сообщества", default3: c.CommunityTag,
        hint: "Вставится как #хештег@тег",
        autoFillSecond: false
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    if (!c.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)
        && f.Characters.Any(x => x.Name.Equals(dlg.Value1, StringComparison.OrdinalIgnoreCase)))
    {
        MessageBox.Show("Персонаж с таким именем уже есть.");
        return;
    }

    c.Name = dlg.Value1;
    c.Hashtag = DataService.SanitizeTag(dlg.Value2);
    c.CommunityTag = DataService.SanitizeTag(dlg.Value3);
    DataService.Save(_data);
    ShowCharacters(f);
}

private void DeleteCharacter(Franchise f, Character c)
{
    var res = MessageBox.Show(
        $"Удалить персонажа «{c.Name}»?",
        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

    if (res != MessageBoxResult.Yes) return;

    f.Characters.Remove(c);
    DataService.Save(_data);
    ShowCharacters(f);
}
private void UpdateStats()
{
    if (_currentFranchise == null)
    {
        // топ-3 франшизы
        var top = _data.Franchises
            .OrderByDescending(f => f.ClickCount)
            .Take(3)
            .Where(f => f.ClickCount > 0)
            .ToList();

        if (top.Count == 0)
        {
            StatsBlock.Text = "пока пусто";
            return;
        }

        StatsBlock.Text = string.Join("\n",
            top.Select((f, i) => $"{i + 1}. {f.Name} — {f.ClickCount}"));
    }
    else
    {
        // топ-3 персонажа внутри франшизы
        var top = _currentFranchise.Characters
            .OrderByDescending(c => c.ClickCount)
            .Take(3)
            .Where(c => c.ClickCount > 0)
            .ToList();

        if (top.Count == 0)
        {
            StatsBlock.Text = "пока пусто";
            return;
        }

        StatsBlock.Text = string.Join("\n",
            top.Select((c, i) => $"{i + 1}. {c.Name} — {c.ClickCount}"));
    }
}
private void OpenImportExport_Click(object sender, RoutedEventArgs e)
{
    var dlg = new ImportExportDialog { Owner = this };
    dlg.ShowDialog();

    if (dlg.DataChanged)
    {
        _data = DataService.Load();
        SearchBox.Text = "";
        ShowFranchises();
    }
}
private void CopyMandatory_Click(object sender, RoutedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(_data.MandatoryPart))
    {
        MessageBox.Show("Обязательная часть не задана. ПКМ по кнопке → Изменить.");
        return;
    }
    Clipboard.SetText(_data.MandatoryPart);
}
private void EditMandatoryPart()
{
    var dlg = new InputDialog(
        title: "Обязательная часть",
        label1: "Текст (можно в несколько строк)", default1: _data.MandatoryPart,
        hint: "Первая строка — заголовок, ниже — хештеги",
        multiline: true
    ) { Owner = this };

    dlg.ShowDialog();
    if (dlg.DialogResult != true) return;

    _data.MandatoryPart = dlg.Value1.Trim();
    DataService.Save(_data);
    BtnMandatory.Tag = _data.MandatoryPart;
}
private void UpdateMandatoryUI()
{
    // Тултип показывает, что именно копируется (в многострочном виде)
    BtnMandatory.Tag = _data.MandatoryPart;

    // Если хочешь видеть под кнопкой первые строки — раскомментируй
    // MandatoryHint.Text = _data.MandatoryPart;
}
private void TopmostCheck_Changed(object sender, RoutedEventArgs e)
{
    Topmost = TopmostCheck.IsChecked == true;
}
}