using System.Linq;
using Zachet;

namespace Zachet.Services
{
    public class AuthenticationService
    {
        private readonly FleetManagementDBEntities _context;

        public AuthenticationService()
        {
            _context = DB.Context; 
        }

        public bool RegisterUser(string fullName, string email, string password, int roleId)
        {
            //Проверка
            if (_context.Users.Any(u => u.Email == email))
            {
                return false;
            }

            var user = new Users 
            {
                FullName = fullName,
                Email = email,
                Password = password, 
                RoleId = roleId 
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return true; 
        }

        public Users LoginUser(string email, string password)
        {
            // Ищем пользователя с таким email и паролем
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                return user; 
            }

            return null; 
        }
    }
}
