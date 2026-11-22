using System;
using System.Data;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class PartsForm : EntityListForm
{
    public PartsForm(UserSession session)
        : base(
            "Запчасти",
            session,
            session.Role == "Администратор" ? "Склад пуст. Нажмите «Добавить»." : "Склад пуст.",
            "По запросу \"{0}\" ничего не найдено.")
    {
        if (IsAdmin)
        {
            AddToolbarButton("AddPartButton", "Добавить", UiKit.Accent, Color.White, () => OpenPartCard(null), 1);
            AddToolbarButton("EditPartButton", "Изменить", UiKit.Neutral, UiKit.TextDark, OpenSelectedPart, 2);
            AddToolbarButton("DeletePartButton", "Удалить", UiKit.Neutral, UiKit.DangerFore, DeleteSelectedPart, 3);
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
    part_id,
    article AS "Артикул",
    part_name AS "Название",
    price AS "Цена",
    quantity AS "Остаток"
FROM parts
WHERE article LIKE @pattern OR part_name LIKE @pattern
ORDER BY part_name;
""";

            command.Parameters.AddWithValue("@pattern", "%" + SearchTextBox.Text.Trim() + "%");

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            Grid.DataSource = table;

            var idColumn = Grid.Columns["part_id"];
            if (idColumn is not null)
            {
                idColumn.Visible = false;
            }

            var priceColumn = Grid.Columns["Цена"];
            if (priceColumn is not null)
            {
                priceColumn.DefaultCellStyle.Format = "0.00";
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

    private void OpenPartCard(long? partId)
    {
        using var cardForm = new PartCardForm(partId);

        if (cardForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void OpenSelectedPart()
    {
        var partId = GetSelectedId("part_id", "запчасть");

        if (partId is not null)
        {
            OpenPartCard(partId);
        }
    }

    private void DeleteSelectedPart()
    {
        var partId = GetSelectedId("part_id", "запчасть");

        if (partId is null)
        {
            return;
        }

        var partName = GetSelectedText("Название");

        if (!UiKit.Confirm(this, $"Удалить запчасть {partName}? Удаление необратимо.", "Подтверждение удаления"))
        {
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM parts WHERE part_id = @id;";
            command.Parameters.AddWithValue("@id", partId.Value);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, "Запчасть удалена со склада.", "Готово");
            LoadData();
        }
        catch (SqliteException)
        {
            UiKit.ShowWarning(this, "Невозможно удалить запчасть: она списана в заявки на ремонт.", "Операция запрещена");
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }
}