using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOS
{
    /// <summary>
    /// Запись MFT.
    /// </summary>
    class MFT_Record
    {
        // Размер каждой записи - фиксированный, равен 1024 байт.

        // Перечисление признаков записи.
        public enum Signature : byte
        {
            NOT_USED = 0,
            IN_USE = 1,
            IS_ACCESS_CONTROL_LIST = 2,
            IS_DATA = 3
        }

        //Signature signature; // Признак записи.
        //private byte sign;
        private byte attributes; // Атрибуты.
        private string extension; // Расширение файла.
        private int size; // Размер.
        private SystemStructures.MyDateTime creatDate; // Дата создания файла.
        private SystemStructures.MyDateTime modifDate; // Дата последней модификации файла.
        private byte userID; // Уникальный идентификатор владельца файла.
        private string fileName; // Имя файла.
        private int securityDescriptor; // Дескриптор безопасности.

        public Signature Sign { get; set; }
        public byte Attributes { get => attributes; set => attributes = value; }
        public string Extension { get => extension; set => extension = value; }
        public int Size { get => size; set => size = value; }
        public SystemStructures.MyDateTime CreatDate { get => creatDate; set => creatDate = value; }
        public SystemStructures.MyDateTime ModifDate { get => modifDate; set => modifDate = value; }
        public byte UserID { get => userID; set => userID = value; }
        public string FileName { get => fileName; set => fileName = value; }
        public int SecurityDescriptor { get => securityDescriptor; set => securityDescriptor = value; }

        // Размер информации о файле - 51 байт

        // Под данные файла (под Data) отводится 973 байт (1024 - 51).
    }
}
