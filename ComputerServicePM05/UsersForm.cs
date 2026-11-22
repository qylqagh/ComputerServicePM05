using System;
using System.Data;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class UsersForm : EntityListForm
{
    public UsersForm(UserSession session)
        : base(
            "Пользователи",
            session,
            "В базе пока нет пользователей.",
            "По запросу \"{0}\" ничего не найдено.")
    {
        AddToolbarButton("AddUserButton", "Добавить", UiKit.Accent, Color.White, () => OpenUserCard(null), 1);
        AddToolbarButton("EditUserButton", "Изменить", UiKit.Neutral, UiKit.TextDark, OpenSelectedUser, 2);
        AddToolbarButton("ToggleUserButton", "Статус", UiKit.Neutral, UiKit.TextDark, ToggleSelectedUser, 3);

        LoadData();
    }

    protected override void LoadData()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
SELECT
    u.user_id,
    u.login AS "Логин",
    u.full_name AS "ФИО",
    r.role_name AS "Роль",
    CASE WHEN u.is_active = 1 THEN 'да' ELSE 'нет' END AS "Активен"
FROM users AS u
INNER JOIN roles AS r ON r.role_id = u.role_id
WHERE u.login LIKE @pattern OR u.full_name LIKE @pattern
ORDER BY u.login;
""";

            command.Parameters.AddWithValue("@pattern", "%" + SearchTextBox.Text.Trim() + "%");

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            Grid.DataSource = table;

            var idColumn = Grid.Columns["user_id"];
            if (idColumn is not null)
            {
                idColumn.Visible = false;
            }

            ApplyEmptyState(table.Rows.Count > 0);
        }
        catch (SqliteException)
        {
            UiKit.ShowDatabaseError(this);
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }

    private void OpenUserCard(long? userId)
    {
        using var cardForm = new UserCardForm(userId);

        if (cardForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void OpenSelectedUser()
    {
        var userId = GetSelectedId("user_id", "пользователь");

        if (userId is not null)
        {
            OpenUserCard(userId);
        }
    }

    private void ToggleSelectedUser()
    {
        var userId = GetSelectedId("user_id", "пользователь");

        if (userId is null)
        {
            return;
        }

        var login = GetSelectedText("Логин");

        if (login == Session.Login)
        {
            UiKit.ShowWarning(this, "Нельзя изменить статус собственной учетной записи.", "Операция запрещена");
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE users SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END WHERE user_id = @id;";
            command.Parameters.AddWithValue("@id", userId.Value);
            command.ExecuteNonQuery();

            using var readCommand = connection.CreateCommand();
            readCommand.CommandText = "SELECT is_active FROM users WHERE user_id = @id;";
            readCommand.Parameters.AddWithValue("@id", userId.Value);

            var isActive = Convert.ToInt64(readCommand.ExecuteScalar()) == 1;

            UiKit.ShowInfo(this, isActive ? "Пользователь активирован." : "Пользователь отключен от системы.", "Готово");
            LoadData();
        }
        catch (SqliteException)
        {
            UiKit.ShowDatabaseError(this);
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }
}