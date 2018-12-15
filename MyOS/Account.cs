using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyOS
{
    static class Account
    {
        public static User User;

        private static User GetUser(string name, string password)
        {
            DataAttributes usersData = SystemCalls.ReadDataAttributes(SystemConstants.UserListRecNumber);
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                int offset = 0;
                if (usersData.Blocks.Count == 0)
                {
                    br.BaseStream.Seek(SystemConstants.UserListRecNumber * SystemConstants.MftRecordSize + MftHeader.Length, SeekOrigin.Begin);
                    while (offset < usersData.Size)
                    {
                        byte[] userBytes = br.ReadBytes(User.AccountLength);
                        if (!userBytes.SequenceEqual(new byte[User.AccountLength]))
                        {
                            User user = new User(userBytes);
                            if (name == user.Name && password.ComputeHash().SequenceEqual(user.PasswordHash))
                                return user;
                        }

                        offset += User.AccountLength;
                    }
                }
                else
                {
                    int blockIndex = 0;
                    while (offset < usersData.Size)
                    {
                        List<byte> userBytes = new List<byte>();
                        if (SystemConstants.MftRecordSize - 1 - offset % (SystemConstants.MftRecordSize - 1) >=
                            User.AccountLength)
                        {
                            br.BaseStream.Seek(
                                usersData.Blocks[blockIndex] * (SystemConstants.MftRecordSize - 1) +
                                offset % SystemConstants.MftRecordSize,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(User.AccountLength));
                        }
                        else
                        {
                            userBytes.AddRange(
                                br.ReadBytes(SystemConstants.MftRecordSize - 1 - offset % (SystemConstants.MftRecordSize - 1)));
                            br.BaseStream.Seek(usersData.Blocks[++blockIndex] * SystemConstants.MftRecordSize - 1,
                                SeekOrigin.Begin);
                            userBytes.AddRange(br.ReadBytes(User.AccountLength - userBytes.Count));
                        }

                        if (!userBytes.SequenceEqual(new byte[User.AccountLength]))
                        {
                            User user = new User(userBytes.ToArray());
                            if (name == user.Name && password.ComputeHash().SequenceEqual(user.PasswordHash))
                                return user;
                        }

                        offset += User.AccountLength;
                    }
                }
            }

            return null;
        }

        public static bool SignIn(string name, string password)
        {
            User = GetUser(name, password);
            return User != null;
        }

        public static bool SignUp(string name, string password, bool isAdministrator)
        {
            if(GetUser(name, password) != null) return false;
            SystemCalls.RegisterUser(new User(name, password, isAdministrator));
            MftHeader mftHeader = SystemCalls.GetMftHeader(SystemConstants.UserListRecNumber);
            mftHeader.Size += User.AccountLength;
            SystemCalls.UpdateMftEntry(mftHeader, SystemConstants.UserListRecNumber);
            return true;
        }
    }
}
