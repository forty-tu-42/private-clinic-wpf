using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;

namespace ClinicApp;

public sealed class EditDialog : Window
{
    private readonly Dictionary<string, Control> controls = new();
    private byte[]? image;
    public IReadOnlyDictionary<string, string> Values => controls.ToDictionary(x => x.Key, x => x.Value is PasswordBox p ? p.Password : ((TextBox)x.Value).Text);
    public byte[]? ImageBytes => image;

    public EditDialog(string title, IEnumerable<(string Key, string Label, string Value, bool Password)> fields, byte[]? currentImage = null)
    {
        Title = title; Width = 560; Height = 680; Owner = Application.Current.MainWindow; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        image = currentImage;
        var root = new DockPanel { Margin = new Thickness(18) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var save = new Button { Content = "Сохранить", IsDefault = true, MinWidth = 110 };
        var cancel = new Button { Content = "Отмена", IsCancel = true, MinWidth = 110 };
        save.Click += (_, _) => { DialogResult = true; };
        buttons.Children.Add(save); buttons.Children.Add(cancel); DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel();
        foreach (var field in fields)
        {
            panel.Children.Add(new TextBlock { Text = field.Label, Margin = new Thickness(4, 7, 4, 0) });
            Control input;
            if (field.Password) input = new PasswordBox { Password = field.Value };
            else input = new TextBox { Text = field.Value, AcceptsReturn = field.Key is "simptomy" or "diagnoz" or "predpisaniya" or "deystvie" or "pobochnye_effekty", TextWrapping = TextWrapping.Wrap };
            controls[field.Key] = input; panel.Children.Add(input);
        }
        if (title.Contains("пациент", StringComparison.OrdinalIgnoreCase) || title.Contains("врач", StringComparison.OrdinalIgnoreCase))
        {
            var photo = new Button { Content = image == null ? "Выбрать фотографию…" : "Заменить фотографию…" };
            photo.Click += (_, _) => { var d = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" }; if (d.ShowDialog() == true) { image = File.ReadAllBytes(d.FileName); photo.Content = "Фотография выбрана: " + Path.GetFileName(d.FileName); } };
            panel.Children.Add(new TextBlock { Text = "Фотография", Margin = new Thickness(4, 7, 4, 0) }); panel.Children.Add(photo);
        }
        scroll.Content = panel; root.Children.Add(scroll); Content = root;
    }
}
