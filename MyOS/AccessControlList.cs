using System.Collections.Generic;
using System.Linq;

namespace MyOS
{
    // Cписок управления досупом.
    public class AccessControlList
    {
        public enum Rights : byte
        {
            F = 0b1111,
            M = 0b111,
            R = 0b10,
            W = 0b1
        }

        private readonly Dictionary<byte, byte> _list;

        public AccessControlList()
        {
            _list = new Dictionary<byte, byte>();
        }

        public bool Add(byte uid, Rights[] rights)
        {
            if (_list.ContainsKey(uid)) return false;
            byte byteRights = 0;
            if (rights.Contains(Rights.F) || rights.Contains(Rights.M))
            {
                byteRights = rights.Contains(Rights.F) ? (byte) Rights.F : (byte) Rights.M;
                _list.Add(uid, byteRights);
                return true;
            }

            if (rights.Contains(Rights.R))
                byteRights = (byte)(byteRights | (int)Rights.R);
            if (rights.Contains(Rights.W))
                byteRights = (byte)(byteRights | (int)Rights.W);

            _list.Add(uid, byteRights);
            return true;
        }
        
        public byte[] ToBytes()
        {
            byte[] listBytes = new byte[_list.Count * 2]; // 2 байта на запись в списке управления доступом.
            int i = 0;
            foreach (var entry in _list)
            {
                listBytes[i] = entry.Key;
                listBytes[i + 1] = entry.Value;
                i += 2;
            }

            return listBytes;
        }
    }
}
