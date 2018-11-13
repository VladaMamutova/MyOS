namespace MyOS
{
    /// <summary>
    /// Запись в списке пользователей, представляющая полную информацию об одном пользователе системы.
    /// </summary>
    class User
    {
        private string login; // Логин.
        private string name; // Имя пользователя.
        private byte uid; // Уникаотный идентификатор.
        private string password; // Пароль.
        private string salt; // Соль для пароля.
        private string homeDirectory; // Домашняя директория.

        public string Login { get => login; set => login = value; }
        public string Name { get => name; set => name = value; }
        public byte UID { get => uid; set => uid = value; }
        public string Password { get => password; set => password = value; }
        public string Salt { get => salt; set => salt = value; }
        public string HomeDirectory { get => homeDirectory; set => homeDirectory = value; }


        // Размер - 193 байта.
    }
}
