using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;

namespace MyOS.ViewModels
{
    class UserAccount
    {
        public string Name { get; set; } // Имя пользователя.
        public byte Id { get; set; } // Уникальный идентификатор.
        public string Type { get; } // Тип пользователя.
        public string HomeDirectory { get; set; } // Имя домашней директории.

        public UserAccount(UserRecord user)
        {
            Name = user.Name;
            Id = user.Id;
            Type = user.IsAdministrator ? "Администратор" : "Обычный";
            HomeDirectory = SystemCalls.GetMftHeader(user.HomeDirectoryMftEntry).FileName;
        }
    }
}
