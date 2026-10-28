using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;

namespace ClinicApp;

public partial class MainWindow : Window
{
    private readonly UserInfo user;
    private string page = "patients";
    private DataRowView? selected;

    public MainWindow(UserInfo user)
    {
        InitializeComponent(); this.user = user;
        UserText.Text = $"{user.Login}\n{user.Role}";
        UsersButton.Visibility = AuditButton.Visibility = user.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        LoadPage();
    }

    private void Menu_Click(object sender, RoutedEventArgs e) { page = (string)((Button)sender).Tag; SearchBox.Clear(); selected = null; LoadPage(); }
    private void Search_Click(object sender, RoutedEventArgs e) => LoadPage();
    private void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadPage(); }

    private void LoadPage()
    {
        try
        {
            string q = "%" + SearchBox.Text.Trim() + "%";
            string sql = page switch
            {
                "patients" => "SELECT id_pacienta AS [Код],fio AS [ФИО],pol AS [Пол],data_rozhdeniya AS [Дата рождения],seriya_pasporta+' '+nomer_pasporta AS [Паспорт],polis_oms AS [Полис ОМС],adres AS [Адрес],telefon AS [Телефон] FROM [19.10_pacienty] WHERE fio LIKE @q OR polis_oms LIKE @q ORDER BY fio",
                "doctors" => "SELECT id_vracha AS [Код],fio AS [ФИО],specialnost AS [Специальность],telefon AS [Телефон],kabinet AS [Кабинет],stazh AS [Стаж] FROM [19.10_vrachi] WHERE fio LIKE @q OR specialnost LIKE @q ORDER BY fio",
                "visits" => "SELECT o.id_osmotra AS [Код],p.fio AS [Пациент],v.fio AS [Врач],o.data_osmotra AS [Дата],o.mesto_osmotra AS [Место],o.simptomy AS [Симптомы],o.diagnoz AS [Диагноз],o.predpisaniya AS [Предписания] FROM [19.10_osmotry] o JOIN [19.10_pacienty] p ON p.id_pacienta=o.id_pacienta JOIN [19.10_vrachi] v ON v.id_vracha=o.id_vracha WHERE p.fio LIKE @q OR v.fio LIKE @q OR o.diagnoz LIKE @q ORDER BY o.data_osmotra DESC",
                "medicines" => "SELECT id_lekarstva AS [Код],nazvanie AS [Название],sposob_priema AS [Способ приёма],dozirovka AS [Дозировка],deystvie AS [Действие],pobochnye_effekty AS [Побочные эффекты] FROM [19.10_lekarstva] WHERE nazvanie LIKE @q ORDER BY nazvanie",
                "prescriptions" => "SELECT n.id_naznacheniya AS [Код],p.fio AS [Пациент],l.nazvanie AS [Лекарство],n.dozirovka AS [Дозировка],n.sposob_priema AS [Способ приёма],n.kolichestvo AS [Количество],o.data_osmotra AS [Дата осмотра] FROM [19.10_naznacheniya] n JOIN [19.10_osmotry] o ON o.id_osmotra=n.id_osmotra JOIN [19.10_pacienty] p ON p.id_pacienta=o.id_pacienta JOIN [19.10_lekarstva] l ON l.id_lekarstva=n.id_lekarstva WHERE p.fio LIKE @q OR l.nazvanie LIKE @q ORDER BY o.data_osmotra DESC",
                "reports" => "SELECT v.fio AS [Врач],v.specialnost AS [Специальность],COUNT(o.id_osmotra) AS [Количество осмотров],SUM(CASE WHEN o.mesto_osmotra=N'Вызов на дом' THEN 1 ELSE 0 END) AS [Вызовов на дом] FROM [19.10_vrachi] v LEFT JOIN [19.10_osmotry] o ON o.id_vracha=v.id_vracha GROUP BY v.fio,v.specialnost ORDER BY [Количество осмотров] DESC",
                "users" => "SELECT id_polzovatelya AS [Код],login AS [Логин],role AS [Роль],is_active AS [Активен] FROM [19.10_polzovateli] WHERE login LIKE @q OR role LIKE @q ORDER BY login",
                _ => "SELECT id_zapisi AS [Код],login AS [Пользователь],deystvie AS [Действие],data_deystviya AS [Дата и время] FROM [19.10_audit] ORDER BY data_deystviya DESC"
            };
            var table = Db.Query(sql, new SqlParameter("@q", q)); MainGrid.ItemsSource = table.DefaultView;
            TitleText.Text = PageTitle(); StatusText.Text = $"Записей: {table.Rows.Count}"; DetailsText.Text = "Выберите запись для просмотра подробностей"; PhotoImage.Source = null;
            ApplyPermissions();
        }
        catch (Exception ex) { MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message, "SQL Server", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private string PageTitle() => page switch { "patients" => "Пациенты", "doctors" => "Врачи", "visits" => "Осмотры", "medicines" => "Лекарства", "prescriptions" => "Назначения", "reports" => "Отчёт: загруженность врачей", "users" => "Пользователи", _ => "Журнал аудита" };
    private void ApplyPermissions()
    {
        bool editable = user.IsAdmin || (user.IsDoctor && page is "patients" or "visits" or "prescriptions");
        bool add = editable; bool edit = editable; bool delete = editable;
        if (page is "reports" or "audit") add = edit = delete = false;
        if (user.IsDoctor && page == "patients") delete = false;
        if (user.IsDirector) add = edit = delete = false;
        AddButton.IsEnabled = add; EditButton.IsEnabled = edit; DeleteButton.IsEnabled = delete;
    }

    private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selected = MainGrid.SelectedItem as DataRowView; PhotoImage.Source = null;
        if (selected == null) return;
        DetailsText.Text = string.Join("\n", selected.Row.Table.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName}: {selected[c.ColumnName]}"));
        if (page is "patients" or "doctors") LoadPhoto(Convert.ToInt32(selected["Код"]));
    }

    private void LoadPhoto(int id)
    {
        string table = page == "patients" ? "[19.10_pacienty]" : "[19.10_vrachi]"; string key = page == "patients" ? "id_pacienta" : "id_vracha";
        var t = Db.Query($"SELECT foto FROM {table} WHERE {key}=@id", new SqlParameter("@id", id));
        if (t.Rows.Count == 0 || t.Rows[0][0] is DBNull) return;
        var bitmap = new BitmapImage(); using var ms = new MemoryStream((byte[])t.Rows[0][0]); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = ms; bitmap.EndInit(); PhotoImage.Source = bitmap;
    }

    private void Add_Click(object sender, RoutedEventArgs e) => EditRecord(false);
    private void Edit_Click(object sender, RoutedEventArgs e) { if (selected == null) { MessageBox.Show("Выберите запись."); return; } EditRecord(true); }

    private void EditRecord(bool edit)
    {
        try
        {
            int id = edit ? Convert.ToInt32(selected!["Код"]) : 0;
            var data = edit ? GetRawRecord(id) : new DataTable(); byte[]? photo = edit && data.Rows.Count > 0 && data.Columns.Contains("foto") && data.Rows[0]["foto"] is byte[] b ? b : null;
            string Get(string key) => edit && data.Rows.Count > 0 && data.Columns.Contains(key) && data.Rows[0][key] != DBNull.Value ? data.Rows[0][key] is DateTime dt ? dt.ToString("yyyy-MM-dd") : data.Rows[0][key].ToString()! : "";
            var fields = FieldsForPage(Get); if (fields.Count == 0) return;
            var dialog = new EditDialog((edit ? "Изменить: " : "Добавить: ") + PageTitle().ToLower(), fields, photo);
            if (dialog.ShowDialog() != true) return;
            SaveRecord(edit, id, dialog.Values, dialog.ImageBytes); Audit((edit ? "Изменена запись: " : "Добавлена запись: ") + PageTitle()); LoadPage();
        }
        catch (Exception ex) { MessageBox.Show("Не удалось сохранить запись. Проверьте заполнение полей.\n\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private List<(string, string, string, bool)> FieldsForPage(Func<string, string> g) => page switch
    {
        "patients" => new() { ("fio","ФИО",g("fio"),false),("pol","Пол",g("pol"),false),("data_rozhdeniya","Дата рождения (ГГГГ-ММ-ДД)",g("data_rozhdeniya"),false),("seriya_pasporta","Серия паспорта",g("seriya_pasporta"),false),("nomer_pasporta","Номер паспорта",g("nomer_pasporta"),false),("polis_oms","Полис ОМС",g("polis_oms"),false),("adres","Адрес",g("adres"),false),("telefon","Телефон",g("telefon"),false) },
        "doctors" => new() { ("fio","ФИО",g("fio"),false),("specialnost","Специальность",g("specialnost"),false),("telefon","Телефон",g("telefon"),false),("kabinet","Кабинет",g("kabinet"),false),("stazh","Стаж (лет)",g("stazh"),false) },
        "visits" => new() { ("id_pacienta","Код пациента",g("id_pacienta"),false),("id_vracha","Код врача",g("id_vracha"),false),("data_osmotra","Дата осмотра (ГГГГ-ММ-ДД)",g("data_osmotra"),false),("mesto_osmotra","Место (Амбулаторно / Вызов на дом)",g("mesto_osmotra"),false),("simptomy","Симптомы",g("simptomy"),false),("diagnoz","Диагноз",g("diagnoz"),false),("predpisaniya","Предписания",g("predpisaniya"),false) },
        "medicines" => new() { ("nazvanie","Название",g("nazvanie"),false),("sposob_priema","Способ приёма",g("sposob_priema"),false),("dozirovka","Дозировка",g("dozirovka"),false),("deystvie","Предполагаемое действие",g("deystvie"),false),("pobochnye_effekty","Побочные эффекты",g("pobochnye_effekty"),false) },
        "prescriptions" => new() { ("id_osmotra","Код осмотра",g("id_osmotra"),false),("id_lekarstva","Код лекарства",g("id_lekarstva"),false),("dozirovka","Дозировка",g("dozirovka"),false),("sposob_priema","Способ приёма",g("sposob_priema"),false),("kolichestvo","Количество / длительность",g("kolichestvo"),false) },
        "users" => new() { ("login","Логин",g("login"),false),("password","Пароль",g("password"),true),("role","Роль",g("role"),false),("is_active","Активен (1/0)",g("is_active"),false) }, _ => new()
    };

    private DataTable GetRawRecord(int id)
    {
        var (table, key) = TableAndKey(); return Db.Query($"SELECT * FROM {table} WHERE {key}=@id", new SqlParameter("@id", id));
    }
    private (string Table, string Key) TableAndKey() => page switch { "patients" => ("[19.10_pacienty]","id_pacienta"), "doctors" => ("[19.10_vrachi]","id_vracha"), "visits" => ("[19.10_osmotry]","id_osmotra"), "medicines" => ("[19.10_lekarstva]","id_lekarstva"), "prescriptions" => ("[19.10_naznacheniya]","id_naznacheniya"), _ => ("[19.10_polzovateli]","id_polzovatelya") };

    private void SaveRecord(bool edit, int id, IReadOnlyDictionary<string,string> v, byte[]? photo)
    {
        var (table, key) = TableAndKey(); var columns = v.Keys.ToList(); if (page is "patients" or "doctors") columns.Add("foto");
        string sql = edit ? $"UPDATE {table} SET {string.Join(",", columns.Select(c => c + "=@" + c))} WHERE {key}=@id" : $"INSERT {table}({string.Join(",",columns)}) VALUES({string.Join(",",columns.Select(c => "@"+c))})";
        var parameters = new List<SqlParameter>();
        foreach (var c in v.Keys)
        {
            object value = v[c];
            if (c.StartsWith("id_") || c is "stazh" or "is_active") value = int.Parse(v[c], CultureInfo.InvariantCulture);
            else if (c.StartsWith("data_")) value = DateTime.ParseExact(v[c], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            parameters.Add(new SqlParameter("@" + c, value));
        }
        if (page is "patients" or "doctors") parameters.Add(new SqlParameter("@foto", SqlDbType.VarBinary, -1) { Value = photo ?? (object)DBNull.Value });
        if (edit) parameters.Add(new SqlParameter("@id", id)); Db.Execute(sql, parameters.ToArray());
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (selected == null) { MessageBox.Show("Выберите запись."); return; }
        if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { var (table,key)=TableAndKey(); Db.Execute($"DELETE FROM {table} WHERE {key}=@id",new SqlParameter("@id",selected["Код"])); Audit("Удалена запись: "+PageTitle()); LoadPage(); }
        catch(Exception ex) { MessageBox.Show("Запись связана с другими данными и не может быть удалена.\n"+ex.Message); }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (MainGrid.Items.Count == 0) return;
        var document = new FlowDocument { PagePadding = new Thickness(45), ColumnWidth = double.PositiveInfinity, FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 10 };
        document.Blocks.Add(new Paragraph(new Run("Частная поликлиника\n" + PageTitle())) { FontSize=18, FontWeight=FontWeights.Bold, TextAlignment=TextAlignment.Center });
        document.Blocks.Add(new Paragraph(new Run($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\nПользователь: {user.Login} ({user.Role})")));
        if (MainGrid.ItemsSource is not DataView view || view.Table == null) return;
        var table = new Table { CellSpacing = 0 }; foreach (DataColumn _ in view.Table.Columns) table.Columns.Add(new TableColumn());
        var group = new TableRowGroup(); var header = new TableRow(); foreach (DataColumn c in view.Table.Columns) header.Cells.Add(new TableCell(new Paragraph(new Run(c.ColumnName))) { Background = System.Windows.Media.Brushes.LightBlue, Padding = new Thickness(3) }); group.Rows.Add(header);
        foreach (DataRowView row in view) { var tr = new TableRow(); foreach (DataColumn c in view.Table.Columns) tr.Cells.Add(new TableCell(new Paragraph(new Run(row[c.ColumnName]?.ToString() ?? ""))) { Padding = new Thickness(3), BorderBrush=System.Windows.Media.Brushes.Gray, BorderThickness=new Thickness(.3) }); group.Rows.Add(tr); }
        table.RowGroups.Add(group); document.Blocks.Add(table); var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) { document.PageHeight=dialog.PrintableAreaHeight; document.PageWidth=dialog.PrintableAreaWidth; dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, PageTitle()); Audit("Напечатан отчёт: "+PageTitle()); }
    }

    private void Audit(string action) { try { Db.Execute("INSERT [19.10_audit](login,deystvie) VALUES(@l,@a)",new SqlParameter("@l",user.Login),new SqlParameter("@a",action)); } catch { } }
    private void Logout_Click(object sender, RoutedEventArgs e) { Audit("Выход из системы"); Application.Current.Shutdown(); }
}
