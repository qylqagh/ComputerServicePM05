using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class PartsPickForm : Form
{
    private readonly long _orderId;
    private readonly ComboBox _partComboBox;
    private readonly NumericUpDown _quantityNumeric;
    private readonly TableLayoutPanel _cardLayout;
    private readonly List<long> _partIds = new List<long>();
    private readonly Dictionary<long, long> _stockById = new Dictionary<long, long>();

    public PartsPickForm(long orderId)
    {
        _orderId = orderId;

        Text = "Списание запчасти со склада";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "PartsPickLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = UiKit.Surface,
            Padding = new Padding(28, 30, 28, 28),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        for (int i = 0; i < 5; i++)
        {
            _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var titleLabel = new Label
        {
            Name = "PartsPickTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        _partComboBox = new ComboBox
        {
            Name = "PartComboBox",
            Width = 420,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 0
        };

        _partComboBox.SelectedIndexChanged += (_, _) => UpdateQuantityLimit();

        _quantityNumeric = new NumericUpDown
        {
            Name = "QuantityNumeric",
            Width = 420,
            Minimum = 1,
            Maximum = 1,
            Value = 1,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            Margin = new Padding(0, 0, 0, 22),
            TabIndex = 1
        };

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "PartsPickButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("PartsPickSaveButton", "Списать", UiKit.Accent, Color.White, 120, 2);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("PartsPickCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 3);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        _cardLayout.Controls.Add(titleLabel, 0, 0);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("PartLabel", "Запчасть (доступны только остатки > 0)"), 0, 1);
        _cardLayout.Controls.Add(_partComboBox, 0, 2);
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("QuantityLabel", "Количество"), 0, 3);
        _cardLayout.Controls.Add(_quantityNumeric, 0, 4);
        _cardLayout.Controls.Add(buttonsFlow, 0, 5);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        LoadParts();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ClientSize = new Size(_cardLayout.Width + 90, _cardLayout.Height + 80);

        _cardLayout.Location = new Point(
            (ClientSize.Width - _cardLayout.Width) / 2,
            (ClientSize.Height - _cardLayout.Height) / 2);
    }

    private void LoadParts()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT part_id, part_name, article, quantity FROM parts WHERE quantity > 0 ORDER BY part_name;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var partId = reader.GetInt64(0);
                var stock = reader.GetInt64(3);

                _partIds.Add(partId);
                _stockById[partId] = stock;
                _partComboBox.Items.Add($"{reader.GetString(1)} ({reader.GetString(2)}) — остаток: {stock}");
            }

            if (_partComboBox.Items.Count == 0)
            {
                _partComboBox.Items.Add("На складе нет запчастей с остатком");
                _partComboBox.SelectedIndex = 0;
                _partComboBox.Enabled = false;
                _quantityNumeric.Enabled = false;
            }
            else
            {
                _partComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void UpdateQuantityLimit()
    {
        if (_partComboBox.SelectedIndex < 0 || _partComboBox.SelectedIndex >= _partIds.Count)
        {
            return;
        }

        var partId = _partIds[_partComboBox.SelectedIndex];
        _quantityNumeric.Maximum = Math.Max(1, _stockById[partId]);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_partComboBox.SelectedIndex < 0 || _partComboBox.SelectedIndex >= _partIds.Count)
        {
            UiKit.ShowWarning(this, "На складе нет запчастей для списания. Добавьте запчасть в разделе «Запчасти».", "Операция запрещена");
            return;
        }

        var partId = _partIds[_partComboBox.SelectedIndex];
        var quantity = (long)_quantityNumeric.Value;

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            using var existingCommand = connection.CreateCommand();
            existingCommand.Transaction = transaction;
            existingCommand.CommandText = "SELECT quantity, price_at_use FROM repair_order_parts WHERE repair_order_id = @orderId AND part_id = @partId;";
            existingCommand.Parameters.AddWithValue("@orderId", _orderId);
            existingCommand.Parameters.AddWithValue("@partId", partId);

            long existingQuantity = 0;
            var hasExisting = false;
            double priceAtUse = 0;

            using (var reader = existingCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    hasExisting = true;
                    existingQuantity = reader.GetInt64(0);
                    priceAtUse = reader.GetDouble(1);
                }
            }

            if (quantity > _stockById[partId])
            {
                UiKit.ShowWarning(this, $"Недостаточно запчастей на складе: доступно {_stockById[partId]}.", "Операция запрещена");
                return;
            }

            using var stockCommand = connection.CreateCommand();
            stockCommand.Transaction = transaction;
            stockCommand.CommandText = "UPDATE parts SET quantity = quantity - @quantity WHERE part_id = @partId;";
            stockCommand.Parameters.AddWithValue("@quantity", quantity);
            stockCommand.Parameters.AddWithValue("@partId", partId);
            stockCommand.ExecuteNonQuery();

            using var linkCommand = connection.CreateCommand();
            linkCommand.Transaction = transaction;

            if (hasExisting)
            {
                linkCommand.CommandText = "UPDATE repair_order_parts SET quantity = @newQuantity WHERE repair_order_id = @orderId AND part_id = @partId;";
                linkCommand.Parameters.AddWithValue("@newQuantity", existingQuantity + quantity);
            }
            else
            {
                linkCommand.CommandText = "INSERT INTO repair_order_parts (repair_order_id, part_id, quantity, price_at_use) VALUES (@orderId, @partId, @quantity, @price);";
                linkCommand.Parameters.AddWithValue("@quantity", quantity);

                using var priceCommand = connection.CreateCommand();
                priceCommand.Transaction = transaction;
                priceCommand.CommandText = "SELECT price FROM parts WHERE part_id = @partId;";
                priceCommand.Parameters.AddWithValue("@partId", partId);
                priceAtUse = Convert.ToDouble(priceCommand.ExecuteScalar());

                linkCommand.Parameters.AddWithValue("@price", priceAtUse);
            }

            linkCommand.Parameters.AddWithValue("@orderId", _orderId);
            linkCommand.Parameters.AddWithValue("@partId", partId);
            linkCommand.ExecuteNonQuery();

            transaction.Commit();

            UiKit.ShowInfo(this, "Запчасть списана со склада и добавлена в заявку.", "Готово");

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