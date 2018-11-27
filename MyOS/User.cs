//using System.Collections.Generic;

namespace MyOS
{
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
            Password = HashEncryptor.EncodePassword(password, salt);
            Salt = salt;
            HomeDirectory = homeDirectory;
        }

        // Размер - 193 байта.
    }
}
