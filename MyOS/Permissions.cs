namespace MyOS
{
    public struct Permissions
    {
        public enum Rights : byte
        {
            F = 0b1111,
            M = 0b111,
            R = 0b10,
            W = 0b1
        }

        public byte[] PermissionBytes;

        public Permissions(byte[] newPermissions)
        {
            PermissionBytes = new byte[2];
            if (PermissionBytes.Length == 2)
                PermissionBytes = newPermissions;
        }
    }
}
