using System;

namespace MyOS.FileSystem.SpecialDataTypes
{
    public class Permission
    {
        [Flags]
        public enum Rights : byte
        {
            FullControl = 0b1111, // Все права (Full Control = Modify + назначение прав другим пользователям).
            Modify = 0b0111, // Изменение (Modify = Read + Write).
            Read = 0b0010, // Чтение (Read).
            Write = 0b0001 // Запись (Write).
        }

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

            // Поочередно записываем байты, содержащие права пользователей.
            ushort ushortPermissions = newPermissions[0];
            ushortPermissions = (ushort) (ushortPermissions << 8); // Сдвигаем влево на ещё один байт.
            ushortPermissions = (ushort) (ushortPermissions | newPermissions[1]);

            _permissions = ushortPermissions;
        }

        /// <summary>
        /// По умолчанию для администраторов и владельца определяются полные права.
        /// </summary>
        public Permission()
        {
            // Записываем права администратора и сдвигаем на 4 бита влево,
            // освобождая место под права владельца.
            _permissions = (ushort)Rights.FullControl << 4;
            _permissions |= (byte)Rights.FullControl; // Записываем права владельца в младшие четыре бита.
            _permissions <<= 4; // Снова сдвигаем на 4 бита для поля прав остальных пользователей.
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
            byte allUserRights = (byte)((_permissions >> (ushort)userSign) & 0b0000_1111);
            if (state) allUserRights |= (byte)right;
            else
            {
                switch (right)
                {
                    case Rights.FullControl: allUserRights &= 0b0111; break;
                    case Rights.Modify: allUserRights &= 0b0011; break;
                    case Rights.Read: allUserRights &= 0b0001; break;
                    case Rights.Write: allUserRights &= 0b0010; break;
                }
            }

            switch (userSign)
            {
                case UserSign.Administrator: _permissions &= 0b1111_0000_1111_1111; break;
                case UserSign.Owner: _permissions &= 0b1111_1111_0000_1111; break;
                case UserSign.Other: _permissions &= 0b1111_1111_1111_0000; break;
            }

            // Обновляем разрешения, добавляя к текущим новые права доступа.
            _permissions |= (ushort)(allUserRights << (ushort)userSign);
        }

        public override string ToString()
        {
            string permission = "";
            for (int i = 0; i < 12; i++)
            {
                permission = permission.Insert(0, ((_permissions >> i) | 1) == _permissions >> i++ ? "R" : "-");
                permission = permission.Insert(0, ((_permissions >> i) | 1) == _permissions >> i++ ? "W" : "-");
                permission = permission.Insert(0, ((_permissions >> i) | 1) == _permissions >> i++ ? "M" : "-");
                permission = permission.Insert(0, ((_permissions >> i) | 1) == _permissions >> i ? " F" : " -");
            }

            return permission;
        }
    }
}
