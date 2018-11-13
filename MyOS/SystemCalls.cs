using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using static MyOS.SystemStructures;

namespace MyOS
{
    class SystemCalls
    {
        public static void Formatting()
        {
            using (var fileStream = new FileStream("MyOS.txt", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileStream.SetLength(419430400); // Устанавливаем размер файла в 400 Мб.
            }

            BinaryWriter bw = new BinaryWriter(File.Open("MyOS.txt", FileMode.Open));

            #region Форматирование MFT-пространства.

            // Создание файла $MFT, представляющего централизованный каталог
            // всех остальных файлов диска и себя самого.

            // 1 файл - запись о самом MFT.
            MFT_Record mft = new MFT_Record
            { Sign = MFT_Record.Signature.IN_USE, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 1024, // Размер записи.
                CreatDate = new MyDateTime(DateTime.Now),
                ModifDate = new MyDateTime(DateTime.Now),
                UserID = Constants.AdminUid,
                FileName = "MFT",
                SecurityDescriptor = 45 //!!!!!!!!!!!
            };
            // Записываем в файл данные о записи $MFT.
            bw.BaseStream.Seek(0 * Constants.FixFileLength, SeekOrigin.Begin);
            WriteBinaryDataToFile(bw, mft, Constants.MftRecNumber);
            
            // 2 файл - $Volume.
            MFT_Record volume = new MFT_Record
            {
                Sign = MFT_Record.Signature.IN_USE, // Признак - запись используется.
                Attributes = 0b11, // Системный файл, только для чтения.
                Extension = "",
                Size = 1024, 
                CreatDate = new MyDateTime(DateTime.Now),
                ModifDate = new MyDateTime(DateTime.Now),
                UserID = Constants.AdminUid,
                FileName = "volume",
                SecurityDescriptor = 46 //!!!!!!!!!!!
            };
            // Записываем в файл данные о записи $Volume.
            WriteBinaryDataToFile(bw, volume, Constants.VolumeRecNumber);

            // 3 файл - .$ (корневой каталог).
            MFT_Record rootDirectory = new MFT_Record
            {
                Sign = MFT_Record.Signature.IN_USE, // Признак - запись используется.
                Attributes = 0b1000, // Директория.
                Extension = "",
                Size = Constants.RootSize,
                CreatDate = new MyDateTime(DateTime.Now),
                ModifDate = new MyDateTime(DateTime.Now),
                UserID = Constants.AdminUid,
                FileName = ".",
                SecurityDescriptor = 47 //!!!!!!!!!!!
            };
            // Записываем в файл данные о записи $Volume.
            WriteBinaryDataToFile(bw, rootDirectory, Constants.RootDirectoryRecNumber);

            // Создание файла $Volume.
            string name = "H"; // Метка тома.
            string version = Constants.FsVersion;
            byte state = 0;
            
            // Записываем в файл данные файла $Volume.
            bw.BaseStream.Seek(Constants.MftRecSize + Constants.VolumeRecNumber, SeekOrigin.Begin);
            bw.Write(Encoding.GetEncoding(1251).GetBytes(name));
            bw.Write(Encoding.GetEncoding(1251).GetBytes(version));
            bw.Write(state);
            
            // Создание корневого каталога $.
            bw.BaseStream.Seek(Constants.MftRecSize + Constants.RootDirectoryRecNumber * Constants.FixFileLength, SeekOrigin.Begin);
            byte b = 1;
            for (int i = 0; i < Constants.RootSize; i++)
                bw.Write(b);

            // Создание битовой карты.
            bw.BaseStream.Seek(Constants.MftRecSize + Constants.BitmapRecNumber * Constants.FixFileLength, SeekOrigin.Begin);
            b = 0b10; //Служебный кластер.
            for (int i = 0; i < Constants.MftRecSize; i++)
                bw.Write(b);
            b = 0b00; // Свободный кластер.
            for (int i = 0; i < Constants.BitmapSize - Constants.MftRecSize; i++)
                bw.Write(b);

            string salt = HashEncryptor.GenerateSalt();
            // Создание списка пользователей.
            User admin = new User()
            {
                Login = "admin",
                Name = "Administrator",
                UID = Constants.AdminUid,
                Password = HashEncryptor.EncodePassword("admin", salt),
                Salt = salt,
                HomeDirectory = "Admin"
            };

            
            bw.BaseStream.Seek(Constants.MftSize + Constants.UserListRecNumber * Constants.FixFileLength, SeekOrigin.Begin);
            byte[] loginbytes20 = new byte[20];
            byte[] loginBytes = Encoding.GetEncoding(1251).GetBytes(admin.Login);
            loginBytes.CopyTo(loginbytes20, 0);
            bw.Write(loginbytes20);

            byte[] namebytes30 = new byte[30];
            byte[] nameBytes = Encoding.GetEncoding(1251).GetBytes(admin.Name);
            nameBytes.CopyTo(namebytes30, 0);
            bw.Write(namebytes30);

            bw.Write(BitConverter.GetBytes(admin.UID));
            bw.Write(Encoding.GetEncoding(1251).GetBytes(admin.Salt));
            bw.Write(Encoding.GetEncoding(1251).GetBytes(admin.Password));

            byte[] directorybytes30 = new byte[30];
            byte[] directoryBytes = Encoding.GetEncoding(1251).GetBytes(admin.HomeDirectory);
            directoryBytes.CopyTo(directorybytes30, 0);
            bw.Write(directorybytes30);

            #endregion


            bw.Close();

            MessageBox.Show("Диск отформатирован!");
        }

        static void WriteBinaryDataToFile(BinaryWriter bw, MFT_Record record, int recordNumber)
        {
            bw.BaseStream.Seek(recordNumber * Constants.MftRecLength, SeekOrigin.Begin);
            bw.Write((byte)record.Sign);
            bw.Write(record.Attributes);

            byte[] extbytes5 = new byte[5];
            byte[] extensionBytes = Encoding.GetEncoding(1251).GetBytes(record.Extension);
            extensionBytes.CopyTo(extbytes5, 0);
            bw.Write(extbytes5);

            bw.Write(BitConverter.GetBytes(record.Size));
            foreach (var t in record.CreatDate.DateTimeBytes)
                bw.Write(t);
            foreach (var t in record.ModifDate.DateTimeBytes)
                bw.Write(t);

            bw.Write(BitConverter.GetBytes(record.UserID));

            byte[] namebytes25 = new byte[25];
            byte[] fileNameBytes = Encoding.GetEncoding(1251).GetBytes(record.FileName);
            namebytes25.CopyTo(namebytes25, 0);
            bw.Write(fileNameBytes);
            
            bw.Write(BitConverter.GetBytes(record.SecurityDescriptor));
        }
    }

}
