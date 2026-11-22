using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class UserCardForm : Form
{
    private readonly long? _userId;
    private readonly TextBox _loginTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly TextBox _fullNameTextBox;
    private readonly ComboBox _roleComboBox;
    private readonly CheckBox _activeCheckBox;
    private readonly TableLayoutPanel _cardLayout;

    public UserCardForm(long? userId)
    {
        _userId = userId;

        Text = userId is null ? "Новый пользователь" : "Изменение пользователя";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiKit.Background;
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "UserCardLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = UiKit.Surface,
            Padding = new Padding(28, 30, 28, 28),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var row = 0;

        var titleLabel = new Label
        {
            Name = "UserCardTitleLabel",
            Text = Text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiKit.TextDark,
            Margin = new Padding(0, 0, 0, 18)
        };

        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _cardLayout.Controls.Add(titleLabel, 0, row);
        row++;

        _loginTextBox = UiKit.CreateFieldTextBox("UserLoginTextBox", 50, 0, 16);
        _passwordTextBox = UiKit.CreateFieldTextBox("UserPasswordTextBox", 100, 1, 16);
        _passwordTextBox.UseSystemPasswordChar = true;
        _fullNameTextBox = UiKit.CreateFieldTextBox("UserFullNameTextBox", 100, 3, 16);
        _roleComboBox = UiKit.CreateFieldComboBox("RoleComboBox", 4, 16);

        var showPasswordCheckBox = new CheckBox
        {
            Name = "ShowPasswordCheckBox",
            Text = "Показать пароль",
            AutoSize = true,
            ForeColor = UiKit.TextMuted,
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 2
        };

        showPasswordCheckBox.CheckedChanged += (_, _) => _passwordTextBox.UseSystemPasswordChar = !showPasswordCheckBox.Checked;

        _activeCheckBox = new CheckBox
        {
            Name = "ActiveCheckBox",
            Text = "Учетная запись активна",
            AutoSize = true,
            ForeColor = UiKit.TextSlate,
            Margin = new Padding(0, 0, 0, 16),
            TabIndex = 5
        };

        var buttonsFlow = new FlowLayoutPanel
        {
            Name = "UserCardButtonsPanel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        var saveButton = UiKit.CreateButton("UserSaveButton", "Сохранить", UiKit.Accent, Color.White, 140, 6);
        saveButton.Click += SaveButton_Click;

        var cancelButton = UiKit.CreateButton("UserCancelButton", "Отмена", UiKit.Neutral, UiKit.TextDark, 110, 7);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonsFlow.Controls.Add(saveButton);
        buttonsFlow.Controls.Add(cancelButton);

        AddRow();
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("UserLoginLabel", "Логин"), 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(_loginTextBox, 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("UserPasswordLabel", "Пароль"), 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(_passwordTextBox, 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(showPasswordCheckBox, 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("UserFullNameLabel", "ФИО"), 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(_fullNameTextBox, 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(UiKit.CreateFieldLabel("UserRoleLabel", "Роль"), 0, row);
        row++;

        AddRow();
        _cardLayout.Controls.Add(_roleComboBox, 0, row);
        row++;

        if (userId is not null)
        {
            AddRow();
            _cardLayout.Controls.Add(_activeCheckBox, 0, row);
            row++;
        }

        AddRow();
        _cardLayout.Controls.Add(buttonsFlow, 0, row);

        Controls.Add(_cardLayout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        LoadRoles();

        if (userId is not null)
        {
            LoadUser();
        }
    }

    private void AddRow()
    {
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ClientSize = new Size(_cardLayout.Width + 90, _cardLayout.Height + 80);

        _cardLayout.Location = new Point(
            (ClientSize.Width - _cardLayout.Width) / 2,
            (ClientSize.Height - _cardLayout.Height) / 2);
    }

    private void LoadRoles()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT role_id, role_name FROM roles ORDER BY role_id;";

            var table = new DataTable();
            table.Load(command.ExecuteReader());

            _roleComboBox.DataSource = table;
            _roleComboBox.DisplayMember = "role_name";
            _roleComboBox.ValueMember = "role_id";

            if (_userId is null && _roleComboBox.Items.Count > 1)
            {
                _roleComboBox.SelectedIndex = 1;
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void LoadUser()
    {
        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT login, password, full_name, role_id, is_active FROM users WHERE user_id = @id;";
            command.Parameters.AddWithValue("@id", _userId.Value);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                _loginTextBox.Text = reader.GetString(0);
                _passwordTextBox.Text = reader.GetString(1);
                _fullNameTextBox.Text = reader.GetString(2);
                _roleComboBox.SelectedValue = reader.GetInt64(3);
                _activeCheckBox.Checked = reader.GetInt64(4) == 1;
            }
        }
        catch (Exception)
        {
            UiKit.ShowDatabaseError(this);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        var login = _loginTextBox.Text.Trim();
        var password = _passwordTextBox.Text;
        var fullName = _fullNameTextBox.Text.Trim();

        if (login.Length < 3)
        {
            UiKit.ShowWarning(this, "Укажите логин: минимум 3 символа.", "Проверка данных");
            _loginTextBox.Focus();
            return;
        }

        if (password.Length < 4)
        {
            UiKit.ShowWarning(this, "Укажите пароль: минимум 4 символа.", "Проверка данных");
            _passwordTextBox.Focus();
            return;
        }

        if (fullName.Length < 2)
        {
            UiKit.ShowWarning(this, "Укажите ФИО пользователя: минимум 2 символа.", "Проверка данных");
            _fullNameTextBox.Focus();
            return;
        }

        if (_roleComboBox.SelectedValue is null)
        {
            UiKit.ShowWarning(this, "Выберите роль пользователя из списка.", "Проверка данных");
            _roleComboBox.Focus();
            return;
        }

        try
        {
            using var connection = DatabaseManager.CreateConnection();
            connection.Open();

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM users WHERE login = @login AND (@id IS NULL OR user_id <> @id);";
            checkCommand.Parameters.AddWithValue("@login", login);
            checkCommand.Parameters.AddWithValue("@id", _userId is null ? DBNull.Value : _userId.Value);

            if (Convert.ToInt64(checkCommand.ExecuteScalar()) > 0)
            {
                UiKit.ShowWarning(this, "Пользователь с таким логином уже существует. Выберите другой логин.", "Проверка данных");
                _loginTextBox.Focus();
                return;
            }

            using var command = connection.CreateCommand();

            if (_userId is null)
            {
                command.CommandText = "INSERT INTO users (login, password, full_name, role_id, is_active) VALUES (@login, @password, @fullName, @roleId, 1);";
            }
            else
            {
                command.CommandText = "UPDATE users SET login = @login, password = @password, full_name = @fullName, role_id = @roleId, is_active = @isActive WHERE user_id = @id;";
                command.Parameters.AddWithValue("@id", _userId.Value);
                command.Parameters.AddWithValue("@isActive", _activeCheckBox.Checked ? 1 : 0);
            }

            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@fullName", fullName);
            command.Parameters.AddWithValue("@roleId", Convert.ToInt64(_roleComboBox.SelectedValue));
            command.ExecuteNonQuery();

            UiKit.ShowInfo(this, _userId is null ? "Пользователь добавлен в систему." : "Изменения сохранены.", "Готово");

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