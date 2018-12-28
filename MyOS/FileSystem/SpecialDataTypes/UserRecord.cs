using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyOS.FileSystem.SpecialDataTypes
{
    /// <summary>
    /// Запись в списке пользователей, представляющая полную информацию об одном пользователе системы.
    /// </summary>
    public class UserRecord : IRecord
    {
        public const byte Length = 96; // Размер заявки пользователя в списке пользователей.
       
        public string Name { get; set; } // Имя пользователя.
        public bool IsAdministrator { get; set; } // Признак администратора.
        public byte Id { get; set; } // Уникальный идентификатор.
        public byte[] PasswordHash { get; set; } // Пароль.
        public int HomeDirectoryMftEntry { get; set; } // Номер Mft-записи домашней директории.

        public static readonly UserRecord Empty;

        static UserRecord()
        {
            Empty = new UserRecord(new byte[Length]);
        }

        public UserRecord(string name, string password, int id, bool isAdministrator, int homeDirectoryMftEntry)
        {
            Id = (byte)id;
            IsAdministrator = isAdministrator;
            Name = name;
            PasswordHash = password.ComputeHash();
            HomeDirectoryMftEntry = homeDirectoryMftEntry;
        }

        public UserRecord(UserRecord user)
        {
            Id = user.Id;
            IsAdministrator = user.IsAdministrator;
            Name = user.Name;
            PasswordHash = user.PasswordHash;
            HomeDirectoryMftEntry = user.HomeDirectoryMftEntry;
        }
        public UserRecord(byte[] userBytes)
        {
            if(userBytes.Length != Length) return;
            Name = Encoding.UTF8.GetString(userBytes.GetRange(0, 26)).Trim('\0');
            Id = userBytes[26];
            IsAdministrator = userBytes[27] == 1;
            PasswordHash = userBytes.GetRange(28, 64);
            HomeDirectoryMftEntry = BitConverter.ToInt32(userBytes, 92);
        }

        public void SetRecord(byte[] recordBytes)
        {
            if (recordBytes.Length != Length) return;
            Name = Encoding.UTF8.GetString(recordBytes.GetRange(0, 26)).Trim('\0');
            Id = recordBytes[26];
            IsAdministrator = recordBytes[27] == 1;
            PasswordHash = recordBytes.GetRange(28, 64);
            HomeDirectoryMftEntry = BitConverter.ToInt32(recordBytes, 92);
        }
        public int GetLength() => Length;
        public bool Equals(byte[] recordBytes) => GetBytes().SequenceEqual(recordBytes);
        
        public bool EqualsByName(string name) { return Name == name; }

        public byte[] GetBytes()
        {
            List<byte> userBytes = new List<byte>();
            userBytes.AddRange(Name.GetFormatBytes(26));
            userBytes.Add(Id);
            userBytes.Add(IsAdministrator ? (byte)1 : (byte)0);
            userBytes.AddRange(PasswordHash);
            userBytes.AddRange(BitConverter.GetBytes(HomeDirectoryMftEntry));
            return userBytes.ToArray();
        }
    }
}
