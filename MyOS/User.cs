using System.Collections.Generic;

namespace MyOS
{
    /// <summary>
    /// Список пользователей системы.
    /// </summary>
    class Users
    {
        public readonly List<User> List;
        public static int MaxUserCount { get; }

        /// <summary>
        /// Запись в списке пользователей, представляющая полную информацию об одном пользователе системы.
        /// </summary>
        public class User
        {
            public string Login { get; set; } // Логин.
            public string Name { get; set; } // Имя пользователя.
            public byte Uid { get; set; } // Уникальный идентификатор.
            public string Password { get; set; } // Пароль.
            public string Salt { get; set; } // Уникальная соль для пароли.
            public string HomeDirectory { get; set; } // Домашняя директория.

            public User(string login, string name, byte uid, string password, string salt, string homeDirectory)
            {
                Login = login;
                Name = name;
                Uid = uid;
                Password = password;
                Salt = salt;
                HomeDirectory = homeDirectory;
            }

            // Размер - 193 байта.
        }

       
        static Users()
        {
            MaxUserCount = 256;
        }

        public Users()
        {
            var salt = HashEncryptor.GenerateSalt();
            List = new List<User>
            {
                new User("admin", "Administrator", SystemStructures.Constants.AdminUid,
                    HashEncryptor.EncodePassword("admin", salt), salt, ".")
            };
        }

        public bool CanCreateNewUser() => List.Count < MaxUserCount;

        public void Add(string login, string name, string password, string salt, string homeDirectory)
            => List.Add(new User(login, name, (byte) List.Count, password, salt, homeDirectory));
    }
}
