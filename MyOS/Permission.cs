using System;

namespace MyOS
{
    public struct Permission
    {
        [Flags]
        public enum Rights : byte
        {
            F = 0b1111, // Все права (Full Control = Modify + назначение прав другим пользователям).
            M = 0b0111, // Изменение (Modify = Read + Write).
            R = 0b0010, // Чтение (Read).
            W = 0b0001 // Запись (Write).
        }

        /// <summary>
        /// Администратор имеет полный доступ.
        /// </summary>
        public static Permission Default; // По умолчанию у администратора 

        /// <summary>
        /// Признак пользователя, показывающий смещение битов, отвечающих за его права.
        /// </summary>
        public enum UserSign
        {
            Administrator = 8, // Номер бита, начиная с которого записаны 4 бита прав администратора.
            Owner = 4, // Номер бита, начиная с которого записаны права админастратора.
            Other = 0 // В третьих - права всех остальных пользователей.
        }

        private ushort _permissions;

        public Permission(byte[] newPermissions)
        {
            if (newPermissions.Length != 2) throw new ArgumentException(nameof(newPermissions));

            ushort ushortPermissions = 0;
            // Поочередно записываем байты, содержащие права пользователей.
            ushortPermissions = (ushort) (ushortPermissions | newPermissions[0]);
            ushortPermissions = (ushort) (ushortPermissions << 8); // Сдвигаем влево на ещё один байт.
            ushortPermissions = (ushort) (ushortPermissions | newPermissions[1]);

            _permissions = ushortPermissions;
        }

        /// <summary>
        /// Администратор и владелец имеют полный доступ.
        /// </summary>
        /// <param name="userSign">Признак пользователя.</param>
        public Permission(UserSign userSign)
        {
            _permissions = Default._permissions;
            if (userSign == UserSign.Owner)
                SetPermission(userSign, Rights.F, true);
                
        }

        static Permission() // Для администратора и владельца файла по умолчанию определяются полные права.
        {
            ushort ushortPermissions = 0;
            // Записываем биты прав администратора.
            ushortPermissions = (ushort)(ushortPermissions | (ushort)Rights.F);
            // Сдвигаем влево на четыре бита для прав владельца.
            ushortPermissions = (ushort)(ushortPermissions << 4);
            // Записываем биты прав владельца файла.
            ushortPermissions = (ushort)(ushortPermissions | (ushort)Rights.F);

            ushortPermissions = (ushort)(ushortPermissions << 4);
            // Записываем права для всех остальных пользователей.
            //ushortPermissions = (ushort)(ushortPermissions | (ushort)Rights.);
            Default._permissions = ushortPermissions;
        }

        public bool CheckRights(UserSign userSign, Rights right)
        {
            return _permissions == (_permissions | ((ushort)right << (ushort)userSign));
        }

        public byte[] GetBytes()
        {
            byte[] permissionsBytes = new byte[2];
            permissionsBytes[0] = (byte) (_permissions >> 8);
            permissionsBytes[1] = (byte) _permissions;
            return permissionsBytes;
        }

        public void SetPermission(UserSign userSign, Rights right, bool state)
        {
            switch (userSign)
            {
                case UserSign.Administrator: _permissions &= 0b1111_0000_1111_1111; break;
                case UserSign.Owner: _permissions &= 0b1111_1111_0000_1111; break;
                case UserSign.Other: _permissions &= 0b1111_1111_1111_0000; break;
            }

            // Обновляем разрешения, добавляя к текущим новые права доступа.
            if (state) _permissions |= (ushort)((ushort)right << (ushort)userSign);
        }
    }
}
