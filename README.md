# Информационная система «Частная поликлиника»

Учебное настольное WPF-приложение для ведения пациентов, врачей, медицинских осмотров, лекарств и назначений.

## Технологии

- C# и WPF/XAML;
- .NET 9;
- Microsoft.Data.SqlClient / ADO.NET;
- Microsoft SQL Server.

## Возможности

- авторизация и роли администратора, руководителя и оператора-врача;
- ведение пациентов, врачей, осмотров, лекарств и назначений;
- поиск, фотографии, печатные формы и отчёт по загруженности врачей;
- журналирование действий пользователей.

## Подготовка базы данных

1. Создайте или выберите базу Microsoft SQL Server.
2. Выполните скрипт `Database/19.10_schema.sql`.
3. Задайте строку подключения в переменной окружения `CLINIC_DB_CONNECTION`.

Пример для PowerShell:

```powershell
$env:CLINIC_DB_CONNECTION = "Server=SERVER;Database=DATABASE;User Id=USER;Password=PASSWORD;TrustServerCertificate=True"
```

## Запуск

```powershell
dotnet run --project .\ClinicApp\ClinicApp.csproj
```

Демонстрационные учётные записи создаются SQL-скриптом и описаны в нём.
