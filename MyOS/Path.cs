﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MyOS
{
    public class Path :INotifyPropertyChanged
    {
        public static readonly char VolumeName;
        public static readonly string Root;
        public const char Separator = '/';
        public List<string> DirectoriesList;
        private string _fullPath;
        public string CurrentPath
        {
            get => _fullPath;
            set {
                if (value == _fullPath)
                    return;

                _fullPath = value;
                OnPropertyChanged(CurrentPath);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        static Path()
        {
            using (BinaryReader br = new BinaryReader(File.Open(SystemConstants.SystemFile, FileMode.Open)))
            {
                br.BaseStream.Seek(SystemConstants.VolumeRecNumber * SystemConstants.MftRecordSize + SystemConstants.MftHeaderLength, SeekOrigin.Begin);
                if (br.ReadByte() == 0) // Данные записи помещаютс в поле данных одной Mft-записи.
                    VolumeName = br.ReadChar();
                br.BaseStream.Seek(SystemConstants.RootDirectoryRecNumber * SystemConstants.MftRecordSize, SeekOrigin.Begin);
                MftRecord root = SystemCalls.ReadMftRecord(br);
                Root = root.FileName.Remove(0, 1); // Удаляем символ $, с которого начинаются все системные файлы.
            }
        }

        public Path()
        {
            DirectoriesList = new List<string>();
            CurrentPath = VolumeName + ":" + Root + string.Join(Separator.ToString(), DirectoriesList);
        }

        public void Add(string directory)
        {
            DirectoriesList.Add(directory);
            CurrentPath = VolumeName + ":" + Root + string.Join(Separator.ToString(), DirectoriesList);
        }

        public void GetPreviosFolder()
        {
            if (DirectoriesList.Count > 0)
            {
                DirectoriesList.RemoveAt(DirectoriesList.Count - 1);
                CurrentPath = VolumeName + ":" + Root + string.Join(Separator.ToString(), DirectoriesList);
            }
        }
    }
}
