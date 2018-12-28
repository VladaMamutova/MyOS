using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyOS.FileSystem.SpecialDataTypes
{
    public class Path :INotifyPropertyChanged
    {
        private const char Separator = '/';
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

        public static readonly string RootPath;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        static Path()
        {
            RootPath = FileSystem.VolumeName + ":" + FileSystem.Root;
        }

        public Path()
        {
            DirectoriesList = new List<string>();
            CurrentPath = RootPath;
        }

        public Path(List<string> directories)
        {
            DirectoriesList = new List<string>(directories);
            CurrentPath = RootPath + string.Join(Separator.ToString(), DirectoriesList);
        }

        public Path(Path path)
        {
            DirectoriesList = new List<string>(path.DirectoriesList);
            CurrentPath = path.CurrentPath;
        }

        public void Add(string directory)
        {
            DirectoriesList.Add(directory);
            CurrentPath = RootPath + string.Join(Separator.ToString(), DirectoriesList);
        }

        public void GetPreviosFolder()
        {
            if (DirectoriesList.Count > 0)
            {
                DirectoriesList.RemoveAt(DirectoriesList.Count - 1);
                CurrentPath = FileSystem.VolumeName + ":" + FileSystem.Root + string.Join(Separator.ToString(), DirectoriesList);
            }
        }

        public string GetAbsolutePath(string fileName)
        {
            if (DirectoriesList.Count > 0)
                return CurrentPath + Separator + fileName;
            return CurrentPath + fileName;
        }
    }
}
