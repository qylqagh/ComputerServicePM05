using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class RepairOrderCardForm : Form
{
    private long? _orderId;
    private readonly ComboBox _clientComboBox;
    private readonly ComboBox _deviceComboBox;
    private readonly ComboBox _statusComboBox;
    private readonly ComboBox _employeeComboBox;
    private readonly TextBox _defectTextBox;
    private readonly TextBox _costTextBox;
    private readonly TextBox _notesTextBox;
    private readonly DataGridView _partsGrid;
    private readonly Label _totalLabel;
    private readonly TableLayoutPanel _cardLayout;
    private readonly List<long?> _employeeIds = new List<long?>();
    private string? _existingClosedAt;
    private decimal _partsSum;

    public RepairOrderCardForm(long? orderId)
    {
        _orderId = orderId;

        Text = orderId is null ? "Новая заявка на ремонт" : $"Заявка №{orderId.Value}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "OrderCardLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            BackColor = UiKit.Surface,
            Padding = new Padding(28, 30, 28, 28),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var row = 0;

        var titleLabel = new Label
        {
            Name = "OrderCardTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(titleLabel, 0, row);
        _cardLayout.SetColumnSpan(titleLabel, 2);
        row++;

        _clientComboBox = UiKit.CreateFieldComboBox("ClientComboBox", 0, 16);
        _statusComboBox = UiKit.CreateFieldComboBox("StatusComboBox", 3, 16);
        _deviceComboBox = UiKit.CreateFieldComboBox("DeviceComboBox", 1, 16);
        _employeeComboBox = UiKit.CreateFieldComboBox("EmployeeComboBox", 4, 16);

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("ClientLabel", "Клиент"), 0, row);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("StatusLabel", "Статус"), 1, row);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_clientComboBox, 0, row);
        _cardLayout.Controls.Add(_statusComboBox, 1, row);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("DeviceLabel", "Устройство"), 0, row);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("EmployeeLabel", "Ответственный сотрудник"), 1, row);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_deviceComboBox, 0, row);
        _cardLayout.Controls.Add(_employeeComboBox, 1, row);
        row++;

        _defectTextBox = new TextBox
        {
            Name = "DefectTextBox",
            Width = 700,
            Height = 70,
            Multiline = true,
            MaxLength = 500,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 2
        };

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("DefectLabel", "Заявленная неисправность"), 0, row);
        _cardLayout.SetColumnSpan(_cardLayout.GetControlFromPosition(0, row)!, 2);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_defectTextBox, 0, row);
        _cardLayout.SetColumnSpan(_defectTextBox, 2);
        row++;

        _costTextBox = UiKit.CreateFieldTextBox("CostTextBox", 12, 5, 16);
        _costTextBox.Text = "0";
        _costTextBox.KeyPress += DecimalTextBox_KeyPress;
        _costTextBox.TextChanged += (_, _) => RefreshTotalLabel();

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("CostLabel", "Стоимость работ, руб"), 0, row);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_costTextBox, 0, row);
        row++;

        _notesTextBox = new TextBox
        {
            Name = "NotesTextBox",
            Width = 700,
            Height = 60,
            Multiline = true,
            MaxLength = 500,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 6
        };

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("NotesLabel", "Примечание (необязательно)"), 0, row);
        _cardLayout.SetColumnSpan(_cardLayout.GetControlFromPosition(0, row)!, 2);
        row++;

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_notesTextBox, 0, row);
        _cardLayout.SetColumnSpan(_notesTextBox, 2);
        row++;

        if (orderId is not null)
        {
            _partsGrid = new DataGridView
            {
                Name = "OrderPartsGrid",
                Dock = DockStyle.Fill,
                Height = 180,
                Margin = new Padding(0, 0, 0, 10)
            };

            UiKit.StyleGrid(_partsGrid);

            AddRow(SizeType.Absolute, 190F);
            _cardLayout.Controls.Add(_partsGrid, 0, row);
            _cardLayout.SetColumnSpan(_partsGrid, 2);
            row++;

            var partsButtonsFlow = new FlowLayoutPanel
            {
                Name = "OrderPartsButtonsPanel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 14)
            };

            var addPartButton = UiKit.CreateButton("AddPartButton", "Списать запчасть", UiKit.Accent, Color.White, 160, 7);
            addPartButton.Click += (_, _) => AddPartToOrder();

            var removePartButton = UiKit.CreateButton("RemovePartButton", "Вернуть запчасть", UiKit.Neutral, UiKit.DangerFore, 160, 8);
            removePartButton.Click += (_, _) => RemovePartFromOrder();

            partsButtonsFlow.Controls.Add(addPartButton);
            partsButtonsFlow.Controls.Add(removePartButton);

            AddRow(SizeType.AutoSize);
            _cardLayout.Controls.Add(partsButtonsFlow, 0, row);
            _cardLayout.SetColumnSpan(partsButtonsFlow, 2);
            row++;
        }

        _totalLabel = new Label
        {
            Name = "TotalLabel",
            Text = "Итого: 0.00",
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(_totalLabel, 0, row);
        _cardLayout.SetColumnSpan(_totalLabel, 2);
        row++;

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "OrderCardButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("OrderSaveButton", "Сохранить", UiKit.Accent, Color.White, 140, 9);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("OrderCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 10);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        AddRow(SizeType.AutoSize);
        _cardLayout.Controls.Add(buttonsFlow, 0, row);
        _cardLayout.SetColumnSpan(buttonsFlow, 2);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        LoadComboBoxSources();

        if (orderId is not null)
        {
            LoadOrder();
            LoadParts();
        }

        RefreshTotalLabel();
    }

    private void AddRow(SizeType sizeType, float height = 0F)
    {
        _cardLayout.RowStyles.Add(sizeType == SizeType.Absolute
            ? new RowStyle(SizeType.Absolute, height)
            : new RowStyle(SizeType.AutoSize));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ClientSize = new Size(_cardLayout.Width + 90, _cardLayout.Height + 80);

        _cardLayout.Location = new Point(
            (ClientSize.Width - _cardLayout.Width) / 2,
            (ClientSize.Height - _cardLayout.Height) / 2);
    }

    private void LoadComboBoxSources()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var clientsCommand = connection.CreateCommand();
            clientsCommand.CommandText = "SELECT client_id, full_name FROM clients ORDER BY full_name;";
            var clientsTable = new DataTable();
            clientsTable.Load(clientsCommand.ExecuteReader());
            _clientComboBox.DataSource = clientsTable;
            _clientComboBox.DisplayMember = "full_name";
            _clientComboBox.ValueMember = "client_id";

            using var devicesCommand = connection.CreateCommand();
            devicesCommand.CommandText = "SELECT device_id, manufacturer || ' ' || model AS device_name FROM devices ORDER BY manufacturer, model;";
            var devicesTable = new DataTable();
            devicesTable.Load(devicesCommand.ExecuteReader());
            _deviceComboBox.DataSource = devicesTable;
            _deviceComboBox.DisplayMember = "device_name";
            _deviceComboBox.ValueMember = "device_id";

            using var statusesCommand = connection.CreateCommand();
            statusesCommand.CommandText = "SELECT repair_status_id, status_name FROM repair_statuses ORDER BY repair_status_id;";
            var statusesTable = new DataTable();
            statusesTable.Load(statusesCommand.ExecuteReader());
            _statusComboBox.DataSource = statusesTable;
            _statusComboBox.DisplayMember = "status_name";
            _statusComboBox.ValueMember = "repair_status_id";

            _employeeComboBox.Items.Add("— не назначен —");
            _employeeIds.Add(null);

            using var usersCommand = connection.CreateCommand();
            usersCommand.CommandText = "SELECT user_id, full_name FROM users WHERE is_active = 1 ORDER BY full_name;";

            using var usersReader = usersCommand.ExecuteReader();

            while (usersReader.Read())
            {
                _employeeIds.Add(usersReader.GetInt64(0));
                _employeeComboBox.Items.Add(usersReader.GetString(1));
            }

            _employeeComboBox.SelectedIndex = 0;
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void LoadOrder()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT client_id, device_id, status_id, assigned_user_id, reported_defect, total_cost, notes, closed_at FROM repair_orders WHERE repair_order_id = @id;";
            command.Parameters.AddWithValue("@id", _orderId.Value);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                _clientComboBox.SelectedValue = reader.GetInt64(0);
                _deviceComboBox.SelectedValue = reader.GetInt64(1);
                _statusComboBox.SelectedValue = reader.GetInt64(2);

                var employeeId = reader.IsDBNull(3) ? (long?)null : reader.GetInt64(3);
                var employeeIndex = _employeeIds.IndexOf(employeeId);
                _employeeComboBox.SelectedIndex = employeeIndex < 0 ? 0 : employeeIndex;

                _defectTextBox.Text = reader.GetString(4);
                _costTextBox.Text = reader.GetDouble(5).ToString("0.##", CultureInfo.InvariantCulture);
                _notesTextBox.Text = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                _existingClosedAt = reader.IsDBNull(7) ? null : reader.GetString(7);
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void LoadParts()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
SELECT
    rop.repair_order_part_id,
    rop.part_id,
    p.part_name AS "Запчасть",
    rop.quantity AS "Кол-во",
    rop.price_at_use AS "Цена",
    rop.quantity * rop.price_at_use AS "Сумма"
FROM repair_order_parts AS rop
INNER JOIN parts AS p ON p.part_id = rop.part_id
WHERE rop.repair_order_id = @id
ORDER BY p.part_name;
""";
            command.Parameters.AddWithValue("@id", _orderId.Value);

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            _partsGrid.DataSource = table;

            foreach (var columnName in new[] { "repair_order_part_id", "part_id" })
            {
                var column = _partsGrid.Columns[columnName];
                if (column is not null)
                {
                    column.Visible = false;
                }
            }

            var priceColumn = _partsGrid.Columns["Цена"];
            if (priceColumn is not null)
            {
                priceColumn.DefaultCellStyle.Format = "0.00";
            }

            var sumColumn = _partsGrid.Columns["Сумма"];
            if (sumColumn is not null)
            {
                sumColumn.DefaultCellStyle.Format = "0.00";
            }

            using var sumCommand = connection.CreateCommand();
            sumCommand.CommandText = "SELECT COALESCE(SUM(quantity * price_at_use), 0) FROM repair_order_parts WHERE repair_order_id = @id;";
            sumCommand.Parameters.AddWithValue("@id", _orderId.Value);
            _partsSum = Convert.ToDecimal(sumCommand.ExecuteScalar());
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void DecimalTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        var textBox = (TextBox)sender!;

        var allowed = char.IsDigit(e.KeyChar)
            || char.IsControl(e.KeyChar)
            || ((e.KeyChar == '.' || e.KeyChar == ',') && !textBox.Text.Contains('.') && !textBox.Text.Contains(','));

        if (!allowed)
        {
            e.Handled = true;
        }
    }

    private void RefreshTotalLabel()
    {
        var labor = ParseCost();
        _totalLabel.Text = $"Итого: {labor + _partsSum:0.00}";
    }

    private decimal ParseCost()
    {
        return decimal.TryParse(_costTextBox.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var labor) && labor >= 0
            ? labor
            : 0M;
    }

    private void AddPartToOrder()
    {
        using var pickForm = new PartsPickForm(_orderId!.Value);

        if (pickForm.ShowDialog(this) == DialogResult.OK)
        {
            LoadParts();
            RefreshTotalLabel();
            PersistTotal();
        }
    }

    private void RemovePartFromOrder()
    {
        if (_partsGrid.SelectedRows.Count == 0)
        {
            UiKit.ShowWarning(this, "Сначала выберите запчасть в таблице списания.", "Нет выделения");
            return;
        }

        var rowId = Convert.ToInt64(_partsGrid.SelectedRows[0].Cells["repair_order_part_id"].Value);
        var partId = Convert.ToInt64(_partsGrid.SelectedRows[0].Cells["part_id"].Value);
        var quantity = Convert.ToInt64(_partsGrid.SelectedRows[0].Cells["Кол-во"].Value);
        var partName = _partsGrid.SelectedRows[0].Cells["Запчасть"].Value?.ToString();

        if (!UiKit.Confirm(this, $"Вернуть запчасть {partName} (кол-во {quantity}) на склад?", "Подтверждение"))
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
            returnCommand.CommandText = "UPDATE parts SET quantity = quantity + @quantity WHERE part_id = @partId;";
            returnCommand.Parameters.AddWithValue("@quantity", quantity);
            returnCommand.Parameters.AddWithValue("@partId", partId);
            returnCommand.ExecuteNonQuery();

            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM repair_order_parts WHERE repair_order_part_id = @rowId;";
            deleteCommand.Parameters.AddWithValue("@rowId", rowId);
            deleteCommand.ExecuteNonQuery();

            transaction.Commit();

            UiKit.ShowInfo(this, "Запчасть возвращена на склад.", "Готово");

            LoadParts();
            RefreshTotalLabel();
            PersistTotal();
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

    private void PersistTotal()
    {
        if (_orderId is null)
        {
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE repair_orders SET total_cost = @total WHERE repair_order_id = @id;";
            command.Parameters.AddWithValue("@total", (double)(ParseCost() + _partsSum));
            command.Parameters.AddWithValue("@id", _orderId.Value);
            command.ExecuteNonQuery();
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var defect = _defectTextBox.Text.Trim();

        if (_clientComboBox.SelectedValue is null)
        {
            UiKit.ShowWarning(this, "Выберите клиента из списка.", "Проверка данных");
            _clientComboBox.Focus();
            return;
        }

        if (_deviceComboBox.SelectedValue is null)
        {
            UiKit.ShowWarning(this, "Выберите устройство из списка.", "Проверка данных");
            _deviceComboBox.Focus();
            return;
        }

        if (_statusComboBox.SelectedValue is null)
        {
            UiKit.ShowWarning(this, "Выберите статус заявки из списка.", "Проверка данных");
            _statusComboBox.Focus();
            return;
        }

        if (defect.Length < 5)
        {
            UiKit.ShowWarning(this, "Опишите неисправность: минимум 5 символов.", "Проверка данных");
            _defectTextBox.Focus();
            return;
        }

        var statusId = Convert.ToInt64(_statusComboBox.SelectedValue);
        var isClosed = statusId == 6 || statusId == 7;
        var closedAt = isClosed ? _existingClosedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : null;
        var employeeIndex = _employeeComboBox.SelectedIndex;
        var employeeId = employeeIndex < 0 ? null : _employeeIds[employeeIndex];
        var notes = _notesTextBox.Text.Trim();

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            if (_orderId is null)
            {
                command.CommandText = """
INSERT INTO repair_orders (client_id, device_id, assigned_user_id, reported_defect, status_id, closed_at, total_cost, notes)
VALUES (@clientId, @deviceId, @employeeId, @defect, @statusId, @closedAt, @total, @notes);
""";
            }
            else
            {
                command.CommandText = """
UPDATE repair_orders
SET client_id = @clientId,
    device_id = @deviceId,
    assigned_user_id = @employeeId,
    reported_defect = @defect,
    status_id = @statusId,
    closed_at = @closedAt,
    notes = @notes
WHERE repair_order_id = @id;
""";
                command.Parameters.AddWithValue("@id", _orderId.Value);
            }

            command.Parameters.AddWithValue("@clientId", Convert.ToInt64(_clientComboBox.SelectedValue));
            command.Parameters.AddWithValue("@deviceId", Convert.ToInt64(_deviceComboBox.SelectedValue));
            command.Parameters.AddWithValue("@employeeId", employeeId is null ? DBNull.Value : employeeId.Value);
            command.Parameters.AddWithValue("@defect", defect);
            command.Parameters.AddWithValue("@statusId", statusId);
            command.Parameters.AddWithValue("@closedAt", closedAt is null ? DBNull.Value : closedAt);
            command.Parameters.AddWithValue("@total", (double)(ParseCost() + _partsSum));
            command.Parameters.AddWithValue("@notes", notes.Length == 0 ? DBNull.Value : notes);
            command.ExecuteNonQuery();

            if (_orderId is null)
            {
                using var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid();";
                _orderId = Convert.ToInt64(idCommand.ExecuteScalar());
                Text = $"Заявка №{_orderId.Value}";
            }

            UiKit.ShowInfo(this, _orderId is null ? "Заявка создана." : "Изменения сохранены.", "Готово");

            DialogResult = DialogResult.OK;
            Close();
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