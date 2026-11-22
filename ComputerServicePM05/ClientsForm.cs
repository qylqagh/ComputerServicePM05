using System;
using System.Data;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class ClientsForm : EntityListForm
{
    public ClientsForm(UserSession session)
        : base(
            "Клиенты",
            session,
            "В базе пока нет клиентов. Нажмите «Добавить».",
            "По запросу \"{0}\" ничего не найдено.")
    {
        AddToolbarButton("AddClientButton", "Добавить", UiKit.Accent, Color.White, () => OpenClientCard(null), 1);
        AddToolbarButton("EditClientButton", "Изменить", UiKit.Neutral, UiKit.TextDark, OpenSelectedClient, 2);

        if (IsAdmin)
        {
            AddToolbarButton("DeleteClientButton", "Удалить", UiKit.Neutral, UiKit.DangerFore, DeleteSelectedClient, 3);
        }

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
    client_id,
    full_name AS "ФИО",
    phone AS "Телефон",
    COALESCE(email, '') AS "Email"
FROM clients
WHERE full_name LIKE @pattern OR phone LIKE @pattern
ORDER BY full_name;
""";

            command.Parameters.AddWithValue("@pattern", "%" + SearchTextBox.Text.Trim() + "%");

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            Grid.DataSource = table;

            var idColumn = Grid.Columns["client_id"];
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

    private void OpenClientCard(long? clientId)
    {
        using var cardForm = new ClientCardForm(clientId);

        if (cardForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void OpenSelectedClient()
    {
        var clientId = GetSelectedId("client_id", "клиент");

        if (clientId is not null)
        {
            OpenClientCard(clientId);
        }
    }

    private void DeleteSelectedClient()
    {
        var clientId = GetSelectedId("client_id", "клиент");

        if (clientId is null)
        {
            return;
        }

        var clientName = GetSelectedText("ФИО");

        if (!UiKit.Confirm(this, $"Удалить клиента {clientName}? Удаление необратимо.", "Подтверждение удаления"))
        {
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM clients WHERE client_id = @id;";
            command.Parameters.AddWithValue("@id", clientId.Value);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, "Клиент удален из базы.", "Готово");
            LoadData();
        }
        catch (SqliteException)
        {
            UiKit.ShowWarning(this, "Невозможно удалить клиента: по нему оформлены заявки на ремонт.", "Операция запрещена");
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }
}