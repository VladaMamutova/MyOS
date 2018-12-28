using System;
using System.Windows.Forms;

namespace MyOS.FileSystem.SpecialDataTypes
{
    public sealed class FsException : Exception
    {
        public enum Command
        {
            Create,
            Rename,
            Copy,
            Delete,
            Save,
            Open,
            SignIn,
            SignUp
        }

        public enum Code
        {
            NotFound,
            NoReadPermission,
            ReadOnly,
            NoWritePermission,
            NoModifyPermission,
            NoFullControlPermission,
            NoFreeSpace,
            NoSystemSpace,
            MaxSize,
            FileExists,
            FolderExists,
            NameTooLong,
            ExtensionTooLong,
            MaxUserCount,
            UserNull,
            UserExists,
            IncompleteCopying,
            EndlessCopy,
            IncompleteDeleting
        }

        public enum Element
        {
            File,
            Folder,
            HomeDirectory,
            User
        }

        public override string Message { get; }
        public Code ErrorCode { get; }

        public static string GetCaption(Command command, Element element)
        {
            string caption = "";
            switch (command)
            {
                case Command.Create: caption = "Создание"; break;
                case Command.Rename: caption = "Переименование"; break;
                case Command.Copy: caption = "Копирование"; break;
                case Command.Delete: caption = "Удаление"; break;
                case Command.Save: caption = "Сохранение"; break;
                case Command.Open: caption = "Открытие"; break;
                case Command.SignIn: caption = "Авторизация"; break;
                case Command.SignUp: caption = "Регистрация"; break;
            }

            switch (element)
            {
                case Element.File: caption += " файла"; break;
                case Element.Folder: caption += " папки"; break;
                case Element.HomeDirectory: caption += " домашней директории"; break;
                case Element.User: caption += " пользователя"; break;
            }

            return caption;
        }

        public void ShowError(Command command, Element element)
        {
            MessageBox.Show(Message, GetCaption(command, element), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public FsException(Code code, string element)
        {
            ErrorCode = code;
            
            switch (code)
            {
                case Code.NotFound: Message = "Не удалось найти этот элемент: "; break;
                case Code.NoReadPermission: Message = "Отсутствует право на чтение данного элемента:"; break;
                case Code.NoWritePermission: Message = "Отсутствует право на запись данного элемента:"; break;
                case Code.NoModifyPermission: Message = "Отсутствует право на изменение данного элемента:"; break;
                case Code.NoFullControlPermission: Message = "Отсутствует полный доступ к данному элементу:"; break;
                case Code.ReadOnly: Message = "Данный файл доступен только для чтения."; break;
                case Code.NoFreeSpace: Message = $"Элемент: {element} {Environment.NewLine}" +
                                                 "Недостаточно места на диске для завершения текущей операции."; return;
                case Code.MaxSize: Message = $"Элемент: {element} {Environment.NewLine}" +
                                             "Превышен максимально допустимый размер "; return;
                case Code.FileExists: Message = "В данном расположении уже существует файл с таким именем:"; break;
                case Code.FolderExists: Message = "В данном расположении уже существует папка с таким именем:"; break;
                case Code.NameTooLong: Message = $"Имя файла слишком длинное: {element} {Environment.NewLine}" +
                                                 "Оно не должно превышать 26 символов."; return;
                case Code.ExtensionTooLong: Message = $"Расширение слишком длинное: {element} {Environment.NewLine}" +
                                                      "Оно не должно превышать 5 символов."; return;
                case Code.MaxUserCount: Message = "Достигнуто максимальное число пользователей в системе."; return;
                case Code.UserNull: Message = "Пользователь с данным именем и паролем не зарегистрирован в системе."; return;
                case Code.UserExists: Message = "Пользователь с таким именем уже существует в системе:"; return;
                case Code.IncompleteCopying:
                    Message = "Не все файлы из директории были скопированы:"; break;
                case Code.IncompleteDeleting: Message = "Данная папка полностью не удалена:"; break;
                case Code.EndlessCopy:
                    Message = "Конечная папка, в которую следует поместить файлы, является" +
                              " дочерней для папки, в которой они находятся:"; break;
                case Code.NoSystemSpace:
                    Message = "Недостаточно системного пространства для завершения текущей операции."; return;
            }

            Message += Environment.NewLine + element + ".";
        }
    }
}
