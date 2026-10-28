using System.Windows;
using Microsoft.Data.SqlClient;

namespace ClinicApp;

public partial class LoginWindow : Window
{
    public UserInfo? User { get; private set; }
    public LoginWindow() => InitializeComponent();

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        { MessageBox.Show("Введите логин и пароль."); return; }
        try
        {
            var table = Db.Query("SELECT id_polzovatelya,login,role FROM [19.10_polzovateli] WHERE login=@login AND password=@password AND is_active=1",
                new SqlParameter("@login", LoginBox.Text.Trim()), new SqlParameter("@password", PasswordBox.Password));
            if (table.Rows.Count == 0) { MessageBox.Show("Неверный логин или пароль.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var row = table.Rows[0];
            User = new UserInfo((int)row[0], row[1].ToString()!, row[2].ToString()!);
            Db.Execute("INSERT [19.10_audit](login,deystvie) VALUES(@l,N'Вход в систему')", new SqlParameter("@l", User.Login));
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show("Не удалось подключиться к базе данных. Сначала выполните Database\\19.10_schema.sql в SSMS.\n\n" + ex.Message, "Ошибка SQL Server", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
