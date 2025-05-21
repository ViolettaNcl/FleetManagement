using System.Windows;
using System.Windows.Controls;
using Zachet.Services;

namespace Zachet.Views
{
    public partial class PageCreateAcc : Page
    {
        private readonly AuthenticationService _authService;

        public PageCreateAcc()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            int roleId = email.EndsWith("@admin") ? 1 : 2; // 1 - администратор, 2 - клиент

            bool registrationSuccessful = _authService.RegisterUser(fullName, email, password, roleId);

            if (registrationSuccessful)
            {
                MessageBox.Show("Учетная запись успешно создана!");

                if (roleId == 1) // Администратор
                {
                    NavigationService.Navigate(new PageMenuAdmin(fullName)); 
                }
                else // Клиент
                {
                    NavigationService.Navigate(new PageAccountUser(fullName));
                }
            }
            else
            {
                MessageBox.Show("Пользователь с таким email уже существует.");
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageLogin());
        }
    }
}
