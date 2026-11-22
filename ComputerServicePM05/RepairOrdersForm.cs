using System;
using System.Collections.Generic;
using System.Data;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class RepairOrdersForm : EntityListForm
{
    private readonly ComboBox _statusFilterComboBox;
    private readonly List<long?> _statusFilterIds = new List<long?>();

    public RepairOrdersForm(UserSession session)
        : base(
            "Заявки на ремонт",
            session,
            "Заявок на ремонт пока нет. Нажмите «Добавить».",
            "По запросу \"{0}\" ничего не найдено.")
    {
        _statusFilterComboBox = new ComboBox
        {
            Name = "StatusFilterComboBox",
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0, 2, 12, 0),
            TabIndex = 1
        };

        _statusFilterComboBox.SelectedIndexChanged += (_, _) => LoadData();
        ToolbarActionsPanel.Controls.Add(_statusFilterComboBox);

        AddToolbarButton("AddOrderButton", "Добавить", UiKit.Accent, Color.White, () => OpenOrderCard(null), 2);
        AddToolbarButton("EditOrderButton", "Открыть", UiKit.Neutral, UiKit.TextDark, OpenSelectedOrder, 3);

        if (IsAdmin)
        {
            AddToolbarButton("DeleteOrderButton", "Удалить", UiKit.Neutral, UiKit.DangerFore, DeleteSelectedOrder, 4);
        }

        Grid.CellDoubleClick += (_, _) => OpenSelectedOrder();

        LoadStatusFilter();
        LoadData();
    }

    private void LoadStatusFilter()
    {
        _statusFilterComboBox.Items.Add("Все статусы");
        _statusFilterIds.Add(null);

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT repair_status_id, status_name FROM repair_statuses ORDER BY repair_status_id;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                _statusFilterIds.Add(reader.GetInt64(0));
                _statusFilterComboBox.Items.Add(reader.GetString(1));
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }

        _statusFilterComboBox.SelectedIndex = 0;
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
    ro.repair_order_id,
    c.full_name AS "Клиент",
    d.manufacturer || ' ' || d.model AS "Устройство",
    rs.status_name AS "Статус",
    COALESCE(u.full_name, '—') AS "Сотрудник",
    substr(ro.created_at, 1, 10) AS "Создана",
    ro.total_cost AS "Стоимость"
FROM repair_orders AS ro
INNER JOIN clients AS c ON c.client_id = ro.client_id
INNER JOIN devices AS d ON d.device_id = ro.device_id
INNER JOIN repair_statuses AS rs ON rs.repair_status_id = ro.status_id
LEFT JOIN users AS u ON u.user_id = ro.assigned_user_id
WHERE (@statusId IS NULL OR ro.status_id = @statusId)
    AND (c.full_name LIKE @pattern OR d.manufacturer LIKE @pattern OR d.model LIKE @pattern)
ORDER BY ro.repair_order_id DESC;
""";

            var statusId = _statusFilterComboBox.SelectedIndex <= 0 ? null : _statusFilterIds[_statusFilterComboBox.SelectedIndex];

            command.Parameters.AddWithValue("@statusId", statusId is null ? DBNull.Value : statusId.Value);
            command.Parameters.AddWithValue("@pattern", "%" + SearchTextBox.Text.Trim() + "%");

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            Grid.DataSource = table;

            var idColumn = Grid.Columns["repair_order_id"];
            if (idColumn is not null)
            {
                idColumn.Visible = false;
            }

            var costColumn = Grid.Columns["Стоимость"];
            if (costColumn is not null)
            {
                costColumn.DefaultCellStyle.Format = "0.00";
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

    private void OpenOrderCard(long? orderId)
    {
        using var cardForm = new RepairOrderCardForm(orderId);

        if (cardForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void OpenSelectedOrder()
    {
        var orderId = GetSelectedId("repair_order_id", "заявка");

        if (orderId is not null)
        {
            OpenOrderCard(orderId);
        }
    }

    private void DeleteSelectedOrder()
    {
        var orderId = GetSelectedId("repair_order_id", "заявка");

        if (orderId is null)
        {
            return;
        }

        if (!UiKit.Confirm(this, $"Удалить заявку №{orderId.Value} вместе со списанными запчастями? Удаление необратимо.", "Подтверждение удаления"))
        {
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            using var returnCommand = connection.CreateCommand();
            returnCommand.Transaction = transaction;
            returnCommand.CommandText = """
UPDATE parts
SET quantity = quantity + rop.quantity
FROM repair_order_parts AS rop
WHERE rop.part_id = parts.part_id AND rop.repair_order_id = @id;
""";
            returnCommand.Parameters.AddWithValue("@id", orderId.Value);
            returnCommand.ExecuteNonQuery();

            using var deletePartsCommand = connection.CreateCommand();
            deletePartsCommand.Transaction = transaction;
            deletePartsCommand.CommandText = "DELETE FROM repair_order_parts WHERE repair_order_id = @id;";
            deletePartsCommand.Parameters.AddWithValue("@id", orderId.Value);
            deletePartsCommand.ExecuteNonQuery();

            using var deleteOrderCommand = connection.CreateCommand();
            deleteOrderCommand.Transaction = transaction;
            deleteOrderCommand.CommandText = "DELETE FROM repair_orders WHERE repair_order_id = @id;";
            deleteOrderCommand.Parameters.AddWithValue("@id", orderId.Value);
            deleteOrderCommand.ExecuteNonQuery();

            transaction.Commit();

            UiKit.ShowInfo(this, "Заявка удалена, запчасти возвращены на склад.", "Готово");
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