using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Zachet;

namespace Zachet.Services
{
    public class AuthenticationService
    {
        private readonly FleetManagementDBEntities1 _context;

        public AuthenticationService()
        {
            _context = DB.Context;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public bool RegisterUser(string fullName, string email, string password, int roleId)
        {
            if (_context.Users.Any(u => u.Email == email)) return false;

            var user = new Users
            {
                FullName = fullName,
                Email = email,
                Password = HashPassword(password),
                RoleId = roleId
            };

            _context.Users.Add(user);
            _context.SaveChanges();  
            return true;
        }


        public Users LoginUser(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null && user.Password == HashPassword(password))
            {
                return user;
            }
            return null;
        }

    }
}
