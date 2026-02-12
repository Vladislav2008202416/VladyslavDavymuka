using ReactiveUI;
using MySqlConnector;
using System;
using System.Collections.ObjectModel; 

namespace MyThirdApp.ViewModels;

public class ExpenseItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public string TimeDisplay { get; set; } = ""; // Тільки час (14:30)
}

public class MainWindowViewModel : ViewModelBase
{
    // ⚠️ Твій пароль
    private const string ConnString = "Server=localhost;User=root;Password=VLad2008202416;Database=shop1;";

    // --- ЕКРАНИ ---
    private bool _isLoginVisible = true; public bool IsLoginVisible { get => _isLoginVisible; set => this.RaiseAndSetIfChanged(ref _isLoginVisible, value); }
    private bool _isRegisterVisible = false; public bool IsRegisterVisible { get => _isRegisterVisible; set => this.RaiseAndSetIfChanged(ref _isRegisterVisible, value); }
    private bool _isAccountVisible = false; public bool IsAccountVisible { get => _isAccountVisible; set => this.RaiseAndSetIfChanged(ref _isAccountVisible, value); }

    // --- ДАНІ КОРИСТУВАЧА ---
    private string _email = ""; public string Email { get => _email; set => this.RaiseAndSetIfChanged(ref _email, value); }
    private string _password = ""; public string Password { get => _password; set => this.RaiseAndSetIfChanged(ref _password, value); }
    private string _name = ""; public string Name { get => _name; set => this.RaiseAndSetIfChanged(ref _name, value); }
    private string _message = ""; public string Message { get => _message; set => this.RaiseAndSetIfChanged(ref _message, value); }
    private string _currentUserName = ""; public string CurrentUserName { get => _currentUserName; set => this.RaiseAndSetIfChanged(ref _currentUserName, value); }

    // --- ТРЕКЕР ---
    public ObservableCollection<ExpenseItem> MyExpenses { get; } = new();
    
    // --- КАЛЕНДАР (Виправлений) ---
    // Ми використовуємо звичайний DateTime, щоб не було помилок
    private DateTime _selectedDate = DateTime.Now; 
    public DateTime SelectedDate 
    { 
        get => _selectedDate; 
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedDate, value);
            // Оновлюємо текст на кнопці календаря
            this.RaisePropertyChanged(nameof(DateButtonText));
            // Завантажуємо список для нової дати
            LoadExpenses(); 
        }
    }

    // Текст, який буде написано на кнопці (напр. "12.02.2026")
    public string DateButtonText => SelectedDate.ToString("dd.MM.yyyy");

    private string _newTitle = ""; public string NewTitle { get => _newTitle; set => this.RaiseAndSetIfChanged(ref _newTitle, value); }
    private string _newAmount = ""; public string NewAmount { get => _newAmount; set => this.RaiseAndSetIfChanged(ref _newAmount, value); }
    private decimal _totalSum = 0; public decimal TotalSum { get => _totalSum; set => this.RaiseAndSetIfChanged(ref _totalSum, value); }

    // --- ЛОГІКА ВХОДУ ---
    public void LoginCommand()
    {
        try
        {
            using (var conn = new MySqlConnection(ConnString))
            {
                conn.Open();
                string sql = "SELECT name FROM users WHERE email = @uEmail AND password = @uPass";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uEmail", Email);
                    cmd.Parameters.AddWithValue("@uPass", Password);
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        CurrentUserName = result.ToString();
                        LoadExpenses(); 
                        ShowAccountScreen();
                    }
                    else Message = "Невірний логін ❌";
                }
            }
        }
        catch (Exception ex) { Message = "Помилка: " + ex.Message; }
    }

    public void RegisterCommand()
    {
        try
        {
            using (var conn = new MySqlConnection(ConnString))
            {
                conn.Open();
                string sql = "INSERT INTO users (email, password, name) VALUES (@uEmail, @uPass, @uName)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uEmail", Email);
                    cmd.Parameters.AddWithValue("@uPass", Password);
                    cmd.Parameters.AddWithValue("@uName", Name);
                    cmd.ExecuteNonQuery();
                }
            }
            CurrentUserName = Name;
            LoadExpenses(); 
            ShowAccountScreen();
        }
        catch (Exception ex) { Message = "Помилка: " + ex.Message; }
    }

    // --- ЗАВАНТАЖЕННЯ ---
    public void LoadExpenses()
    {
        MyExpenses.Clear(); TotalSum = 0;       
        using (var conn = new MySqlConnection(ConnString))
        {
            conn.Open();
            // Беремо записи тільки за обраний день
            // DATE(date) відрізає години, залишає тільки рік-місяць-день
            string sql = "SELECT id, title, amount, date FROM expenses WHERE user_email = @uEmail AND DATE(date) = DATE(@uDate) ORDER BY id DESC";
            
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@uEmail", Email);
                cmd.Parameters.AddWithValue("@uDate", SelectedDate); 

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        MyExpenses.Add(new ExpenseItem 
                        { 
                            Id = reader.GetInt32("id"), 
                            Title = reader.GetString("title"), 
                            // Категорії тут більше немає
                            Amount = reader.GetDecimal("amount"),
                            TimeDisplay = reader.GetDateTime("date").ToString("HH:mm") // Формат часу (14:30)
                        });
                        TotalSum += reader.GetDecimal("amount"); 
                    }
                }
            }
        }
    }

    // --- ДОДАВАННЯ ---
    public void AddExpenseCommand()
    {
        if (string.IsNullOrWhiteSpace(NewTitle) || string.IsNullOrWhiteSpace(NewAmount)) return;
        try
        {
            decimal cost = decimal.Parse(NewAmount); 
            using (var conn = new MySqlConnection(ConnString))
            {
                conn.Open();
                // NOW() - це функція MySQL, яка бере поточний час твого комп'ютера (Україна)
                string sql = "INSERT INTO expenses (user_email, title, amount, date) VALUES (@uEmail, @uTitle, @uAmount, NOW())";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uEmail", Email);
                    cmd.Parameters.AddWithValue("@uTitle", NewTitle);
                    cmd.Parameters.AddWithValue("@uAmount", cost);
                    cmd.ExecuteNonQuery();
                }
            }
            
            // Якщо ми дивимось не на "Сьогодні", перемкнемо календар на сьогодні
            if (SelectedDate.Date != DateTime.Today) SelectedDate = DateTime.Now;
            else LoadExpenses(); 

            NewTitle = ""; NewAmount = ""; 
        }
        catch { Message = "Введіть число!"; }
    }

    public void DeleteExpenseCommand(int idToDelete)
    {
        try 
        {
            using (var conn = new MySqlConnection(ConnString))
            {
                conn.Open();
                string sql = "DELETE FROM expenses WHERE id = @uId";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uId", idToDelete);
                    cmd.ExecuteNonQuery();
                }
            }
            LoadExpenses(); 
        }
        catch (Exception ex) { Message = "Помилка видалення: " + ex.Message; }
    }

    public void LogoutCommand() { Email = ""; Password = ""; ShowLoginScreen(); }
    public void ShowRegisterScreen() { IsLoginVisible = false; IsRegisterVisible = true; IsAccountVisible = false; Message = ""; }
    public void ShowLoginScreen() { IsLoginVisible = true; IsRegisterVisible = false; IsAccountVisible = false; Message = ""; }
    public void ShowAccountScreen() { IsLoginVisible = false; IsRegisterVisible = false; IsAccountVisible = true; Message = ""; }
}