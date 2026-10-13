using System.Windows;
using System.Windows.Input;

namespace GoidaLauncher;

public partial class TextInputWindow : Window
{
    public string Value => InputBox.Text.Trim();

    public TextInputWindow(string prompt, string defaultValue = "")
    {
        InitializeComponent();
        SourceInitialized += (_, _) => Services.WindowChromeService.ApplyDarkTitleBar(this);
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
        Loaded += (_, _) => { InputBox.Focus(); InputBox.SelectAll(); };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text))
        {
            MessageBox.Show(this, "Введите непустое значение.", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OkButton_Click(sender, e);
        else if (e.Key == Key.Escape) CancelButton_Click(sender, e);
    }
}
