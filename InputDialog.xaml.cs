using System.Windows;
using System.Windows.Controls;

namespace HashtagCopier;

public partial class InputDialog : Window
{
    public string Value1 { get; private set; } = "";
    public string Value2 { get; private set; } = "";
    public string Value3 { get; private set; } = "";

    private readonly bool _use1;
    private readonly bool _use2;
    private readonly bool _use3;
    private bool _suppressAuto = false;

    public InputDialog(
        string title,
        string? label1 = null, string default1 = "",
        string? label2 = null, string default2 = "",
        string? label3 = null, string default3 = "",
        string? hint = null,
        bool autoFillSecond = false,
        bool multiline = false)      // ← новый параметр
    {
        InitializeComponent();
        Title = title;

        // --- Поле 1 ---
        _use1 = label1 != null;
        Label1.Text = label1 ?? "";
        Box1.Text = default1;
        Label1.Visibility = _use1 ? Visibility.Visible : Visibility.Collapsed;
        Box1.Visibility   = _use1 ? Visibility.Visible : Visibility.Collapsed;

        if (_use1 && multiline)
    {
        Box1.AcceptsReturn = true;
        Box1.TextWrapping = TextWrapping.Wrap;
        Box1.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Box1.Height = 120;
        Box1.VerticalContentAlignment = VerticalAlignment.Top;

        // Снимаем IsDefault с кнопки OK, чтобы Enter вставлял перевод строки
        OkButton.IsDefault = false;
    }

        // --- Поле 2 ---
        _use2 = label2 != null;
        Label2.Text = label2 ?? "";
        Box2.Text = default2;
        Label2.Visibility = _use2 ? Visibility.Visible : Visibility.Collapsed;
        Box2.Visibility   = _use2 ? Visibility.Visible : Visibility.Collapsed;

        // --- Поле 3 ---
        _use3 = label3 != null;
        Label3.Text = label3 ?? "";
        Box3.Text = default3;
        Label3.Visibility = _use3 ? Visibility.Visible : Visibility.Collapsed;
        Box3.Visibility   = _use3 ? Visibility.Visible : Visibility.Collapsed;

        // --- Подсказка ---
        if (string.IsNullOrEmpty(hint))
            Hint.Visibility = Visibility.Collapsed;
        else
        {
            Hint.Text = hint;
            Hint.Visibility = Visibility.Visible;
        }

        // --- Автозаполнение второго поля из первого ---
        if (autoFillSecond && _use2)
        {
            bool userEdited = false;
            Box2.TextChanged += (_, _) => { if (!_suppressAuto) userEdited = true; };
            Box1.TextChanged += (_, _) =>
            {
                if (!userEdited)
                {
                    _suppressAuto = true;
                    Box2.Text = DataService.SanitizeTag(Box1.Text);
                    _suppressAuto = false;
                }
            };
        }

        Loaded += (_, _) =>
        {
            if (_use1) Box1.Focus();
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        // При многострочном режиме Enter должен вставлять перевод строки,
        // а не срабатывать как «нажатие OK». Поэтому проверяем вручную.
        Value1 = _use1 ? Box1.Text.Trim() : "";
        Value2 = _use2 ? Box2.Text.Trim() : "";
        Value3 = _use3 ? Box3.Text.Trim() : "";

        if (_use1 && string.IsNullOrWhiteSpace(Value1)) { Warn("Первое поле пустое.");  return; }
        if (_use2 && string.IsNullOrWhiteSpace(Value2)) { Warn("Второе поле пустое.");  return; }
        if (_use3 && string.IsNullOrWhiteSpace(Value3)) { Warn("Третье поле пустое."); return; }

        DialogResult = true;
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "Ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
}