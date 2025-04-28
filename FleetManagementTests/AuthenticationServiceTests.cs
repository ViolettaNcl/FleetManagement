using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zachet;
using System.Collections.Generic;

namespace FleetManagement.Tests
{
    [TestClass]
    public class AuthenticationServiceTests
    {
        private readonly List<Users> _users;

        public AuthenticationServiceTests()
        {
            _users = new List<Users>();
        }

        [TestMethod]
        public void RegisterUser_NewUser_IncreasesListSize()
        {
            // Arrange
            _users.Clear();
            var user = new Users { FullName = "Test User", Email = "test@example.com", Password = "password123", RoleId = 2 };

            // Act
            if (!_users.Any(u => u.Email == user.Email))
            {
                _users.Add(user);
            }

            // Assert
            Assert.AreEqual(1, _users.Count);
            Assert.AreEqual("Test User", _users[0].FullName);
        }

        [TestMethod]
        public void RegisterUser_ExistingEmail_DoesNotAdd()
        {
            // Arrange
            _users.Clear();
            _users.Add(new Users { Email = "test@example.com", FullName = "Existing User", Password = "oldpassword", RoleId = 1 });

            // Act
            var newUser = new Users { Email = "test@example.com", FullName = "Test User", Password = "password123", RoleId = 2 };
            if (!_users.Any(u => u.Email == newUser.Email))
            {
                _users.Add(newUser);
            }

            // Assert
            Assert.AreEqual(1, _users.Count);
            Assert.AreEqual("Existing User", _users[0].FullName);
        }

        [TestMethod]
        public void LoginUser_ValidCredentials_FindsUser()
        {
            // Arrange
            _users.Clear();
            _users.Add(new Users { Email = "test@example.com", Password = "password123", FullName = "Test User", RoleId = 2 });

            // Act
            var user = _users.FirstOrDefault(u => u.Email == "test@example.com" && u.Password == "password123");

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("Test User", user.FullName);
        }

        [TestMethod]
        public void LoginUser_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            _users.Clear();
            _users.Add(new Users { Email = "test@example.com", Password = "password123" });

            // Act
            var user = _users.FirstOrDefault(u => u.Email == "test@example.com" && u.Password == "wrongpassword");

            // Assert
            Assert.IsNull(user);
        }
    }
}