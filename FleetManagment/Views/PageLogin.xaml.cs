using System.Windows;
using System.Windows.Controls;
using Zachet.Services;
using System.Windows.Media;

namespace Zachet.Views
{
    public partial class PageLogin : Page
    {
        private readonly AuthenticationService _authService;

        public PageLogin()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            // Проверка
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            var user = _authService.LoginUser(email, password);

            if (user != null)
            {
                MessageBox.Show($"Вход выполнен успешно! Добро пожаловать, {user.FullName}.");

                PasswordBorder.BorderBrush = Brushes.Green;

                if (user.RoleId == 1) // Администратор
                {
                    NavigationService.Navigate(new PageMenuAdmin(user.FullName)); 
                }
                else if (user.RoleId == 2) // Клиент
                {
                    NavigationService.Navigate(new PageAccountUser(user.FullName));
                }
            }
            else
            {
                MessageBox.Show("Неверный email или пароль.");

                PasswordBorder.BorderBrush = Brushes.Red;
            }
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageCreateAcc());
        }
    }
}
