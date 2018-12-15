using System;
using System.Collections.Generic;
using System.Text;

namespace MyOS
{
    /// <summary>
    /// Запись в списке пользователей, представляющая полную информацию об одном пользователе системы.
    /// </summary>
    public class User
    {
        public const byte AdministratorId = 0; // Идентификатор администратора.
        public const byte AccountLength = 91; // Размер заявки пользователя в списке пользователей.
        private const byte MaxUserId = Byte.MaxValue;
        private static int _newId = 1;

        public string Name { get; set; } // Имя пользователя.
        public byte Id { get; set; } // Уникальный идентификатор.
        public byte[] PasswordHash { get; set; } // Пароль.

        public User(string name, string password, bool isAdministrator)
        {
            if (isAdministrator) Id = AdministratorId;
            else
            {
                if (_newId > MaxUserId)
                    throw new ArgumentException(
                        "Превышено максимально допустимое количество пользоваетелей. Чтобы создать новую учётную запись, отформатируйте систему. Не забудьте сохранить на внешнем носителе всю информацию!");
                Id = (byte) _newId;
                _newId++;
            }

            Name = name;
            PasswordHash = password.ComputeHash();
        }

        public User(byte[] userBytes)
        {
            if(userBytes.Length != AccountLength) return;
            Name = Encoding.UTF8.GetString(userBytes.GetRange(0, 26)).Trim('\0');
            Id = userBytes[26];
            PasswordHash = userBytes.GetRange(27, 64);
        }

        public byte[] GetBytes()
        {
            List<byte> userBytes = new List<byte>();
            userBytes.AddRange(Name.GetFormatBytes(26));
            userBytes.Add(Id);
            userBytes.AddRange(PasswordHash);
            return userBytes.ToArray();
        }
    }
}
