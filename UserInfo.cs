namespace ClinicApp;

public record UserInfo(int Id, string Login, string Role)
{
    public bool IsAdmin => Role == "Администратор";
    public bool IsDirector => Role == "Руководитель поликлиники";
    public bool IsDoctor => Role == "Оператор-врач";
}
