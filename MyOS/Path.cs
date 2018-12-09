using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MyOS
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        
        public Path()
        {
            DirectoriesList = new List<string>();
            CurrentPath = SystemData.VolumeName + ":" + SystemData.Root + string.Join(Separator.ToString(), DirectoriesList);
        }

        public Path(List<string> directories)
        {
            DirectoriesList = new List<string>(directories);
            CurrentPath = SystemData.VolumeName + ":" + SystemData.Root + string.Join(Separator.ToString(), DirectoriesList);
        }

        public Path(Path path)
        {
            DirectoriesList = new List<string>(path.DirectoriesList);
            CurrentPath = path.CurrentPath;
        }

        public void Add(string directory)
        {
            DirectoriesList.Add(directory);
            CurrentPath = SystemData.VolumeName + ":" + SystemData.Root + string.Join(Separator.ToString(), DirectoriesList);
        }

        public void GetPreviosFolder()
        {
            if (DirectoriesList.Count > 0)
            {
                DirectoriesList.RemoveAt(DirectoriesList.Count - 1);
                CurrentPath = SystemData.VolumeName + ":" + SystemData.Root + string.Join(Separator.ToString(), DirectoriesList);
            }
        }
    }
}
