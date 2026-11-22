using System;
using System.Data;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class DevicesForm : EntityListForm
{
    public DevicesForm(UserSession session)
        : base(
            "Устройства",
            session,
            "В базе пока нет устройств. Нажмите «Добавить».",
            "По запросу \"{0}\" ничего не найдено.")
    {
        AddToolbarButton("AddDeviceButton", "Добавить", UiKit.Accent, Color.White, () => OpenDeviceCard(null), 1);
        AddToolbarButton("EditDeviceButton", "Изменить", UiKit.Neutral, UiKit.TextDark, OpenSelectedDevice, 2);

        if (IsAdmin)
        {
            AddToolbarButton("DeleteDeviceButton", "Удалить", UiKit.Neutral, UiKit.DangerFore, DeleteSelectedDevice, 3);
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
    d.device_id,
    dc.category_name AS "Категория",
    d.manufacturer AS "Производитель",
    d.model AS "Модель",
    COALESCE(d.serial_number, '') AS "Серийный номер"
FROM devices AS d
INNER JOIN device_categories AS dc ON dc.device_category_id = d.device_category_id
WHERE d.manufacturer LIKE @pattern
    OR d.model LIKE @pattern
    OR COALESCE(d.serial_number, '') LIKE @pattern
ORDER BY d.manufacturer, d.model;
""";

            command.Parameters.AddWithValue("@pattern", "%" + SearchTextBox.Text.Trim() + "%");

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            Grid.DataSource = table;

            var idColumn = Grid.Columns["device_id"];
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

    private void OpenDeviceCard(long? deviceId)
    {
        using var cardForm = new DeviceCardForm(deviceId);

        if (cardForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void OpenSelectedDevice()
    {
        var deviceId = GetSelectedId("device_id", "устройство");

        if (deviceId is not null)
        {
            OpenDeviceCard(deviceId);
        }
    }

    private void DeleteSelectedDevice()
    {
        var deviceId = GetSelectedId("device_id", "устройство");

        if (deviceId is null)
        {
            return;
        }

        var deviceName = $"{GetSelectedText("Производитель")} {GetSelectedText("Модель")}";

        if (!UiKit.Confirm(this, $"Удалить устройство {deviceName}? Удаление необратимо.", "Подтверждение удаления"))
        {
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM devices WHERE device_id = @id;";
            command.Parameters.AddWithValue("@id", deviceId.Value);
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, "Устройство удалено из базы.", "Готово");
            LoadData();
        }
        catch (SqliteException)
        {
            UiKit.ShowWarning(this, "Невозможно удалить устройство: по нему оформлены заявки на ремонт.", "Операция запрещена");
        }
        catch (Exception)
        {
            UiKit.ShowUnexpectedError(this);
        }
    }
}