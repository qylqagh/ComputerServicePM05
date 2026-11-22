using System;
using System.Drawing;
using System.Windows.Forms;
using ComputerServicePM05.Models;
using Microsoft.Data.Sqlite;

namespace ComputerServicePM05;

public sealed class LoginForm : Form
{
    private readonly TextBox _loginTextBox;
    private readonly TextBox _passwordTextBox;
    private readonly Button _loginButton;
    private readonly TableLayoutPanel _cardLayout;

    public LoginForm()
    {
        Text = "Авторизация";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(241, 245, 249);
        Font = new Font("Segoe UI", 9.5F);

        _cardLayout = new TableLayoutPanel
        {
            Name = "LoginCardLayout",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = Color.White,
            Padding = new Padding(36, 40, 36, 40),
            Anchor = AnchorStyles.None
        };

        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        for (int i = 0; i < 7; i++)
        {
            _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var titleLabel = new Label
        {
            Name = "LoginTitleLabel",
            Text = "Сервисный центр",
            AutoSize = true,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Margin = new Padding(0, 0, 0, 6)
        };

        var subtitleLabel = new Label
        {
            Name = "LoginSubtitleLabel",
            Text = "Войдите с учетными данными",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 0, 0, 26)
        };

        var loginLabel = new Label
        {
            Name = "LoginFieldLabel",
            Text = "Логин",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(0, 0, 0, 6)
        };

        _loginTextBox = new TextBox
        {
            Name = "LoginTextBox",
            Width = 320,
            MaxLength = 50,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 11F),
            Margin = new Padding(0, 0, 0, 18),
            TabIndex = 0
        };

        var passwordLabel = new Label
        {
            Name = "PasswordFieldLabel",
            Text = "Пароль",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(0, 0, 0, 6)
        };

        _passwordTextBox = new TextBox
        {
            Name = "PasswordTextBox",
            Width = 320,
            MaxLength = 100,
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 11F),
            Margin = new Padding(0, 0, 0, 26),
            TabIndex = 1
        };

        _loginButton = new Button
        {
            Name = "LoginButton",
            Text = "Войти",
            Width = 320,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(59, 130, 246),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            AutoSize = false,
            TabIndex = 2
        };

        _loginButton.FlatAppearance.BorderSize = 0;
        _loginButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 99, 235);
        _loginButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);

        _loginButton.Click += LoginButton_Click;

        _cardLayout.Controls.Add(titleLabel, 0, 0);
        _cardLayout.Controls.Add(subtitleLabel, 0, 1);
        _cardLayout.Controls.Add(loginLabel, 0, 2);
        _cardLayout.Controls.Add(_loginTextBox, 0, 3);
        _cardLayout.Controls.Add(passwordLabel, 0, 4);
        _cardLayout.Controls.Add(_passwordTextBox, 0, 5);
        _cardLayout.Controls.Add(_loginButton, 0, 6);

        Controls.Add(_cardLayout);
        AcceptButton = _loginButton;

        Shown += (_, _) => _loginTextBox.Focus();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        ClientSize = new Size(_cardLayout.Width + 120, _cardLayout.Height + 96);

        _cardLayout.Location = new Point(
            (ClientSize.Width - _cardLayout.Width) / 2,
            (ClientSize.Height - _cardLayout.Height) / 2);

        MinimumSize = Size;
    }

    private void LoginButton_Click(object? sender, EventArgs e)
    {
        var login = _loginTextBox.Text.Trim();
        var password = _passwordTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                this,
                "Заполните логин и пароль.",
                "Проверка данных",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var session = Authenticate(login, password);

            if (session is null)
            {
                MessageBox.Show(
                    this,
                    "Вы ввели неверный логин или пароль. Пожалуйста проверьте ещё раз введенные данные",
                    "Ошибка входа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Hide();

            var mainForm = new MainForm(session);

            mainForm.FormClosed += (_, _) =>
            {
                if (mainForm.LogoutRequested)
                {
                    _loginTextBox.Clear();
                    _passwordTextBox.Clear();
                    _loginTextBox.Focus();
                    Show();
                }
                else
                {
                    Application.Exit();
                }
            };

            mainForm.Show();
        }
        catch (SqliteException)
        {
            MessageBox.Show(
                this,
                "Не удалось подключиться к базе данных.",
                "Ошибка базы данных",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception)
        {
            MessageBox.Show(
                this,
                "Произошла непредвиденная ошибка.",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static UserSession? Authenticate(string login, string password)
    {
        using var connection = DatabaseManager.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
SELECT
    u.user_id,
    u.login,
    u.full_name,
    r.role_name,
    u.is_active
FROM users AS u
INNER JOIN roles AS r ON r.role_id = u.role_id
WHERE u.login = @login AND u.password = @password;
""";

        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", password);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        var isActive = reader.GetInt64(4) == 1;

        if (!isActive)
            return null;

        return new UserSession(
            (int)reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3));
    }
}