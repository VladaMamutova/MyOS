using System.Windows.Input;

namespace MyOS.ViewModels
{
    public class Commands
    {
        static Commands()
        {
            // Создаём и инициализируем команды.
            InputGestureCollection inputs =
                new InputGestureCollection {new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + C")};
            Copy = new RoutedUICommand("Copy", "Copy", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + V") };
            Paste = new RoutedUICommand("Paste", "Paste", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.F2, 0, "F2") };
            Rename = new RoutedUICommand("Rename", "Rename", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + D") };
            Delete = new RoutedUICommand("Delete", "Delete", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl + Shift + S") };
            Save = new RoutedUICommand("Save", "Save", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.Apps, 0, "Apps") };
            ShowProperties = new RoutedUICommand("ShowProperties", "ShowProperties", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control, "Ctrl + F") };
            CreateFile = new RoutedUICommand("CreateFile", "CreateFile", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl + D") };
            CreateFolder = new RoutedUICommand("CreateFolder", "CreateFolder", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + F") };
            Create100Files = new RoutedUICommand("Create100Files", "Create100Files", typeof(Commands), inputs);
            inputs =
                new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + D") };
            Create100Folders = new RoutedUICommand("Create100Folders", "Create100Folders", typeof(Commands), inputs);
        }

        public static RoutedUICommand Copy { get; }
        public static RoutedUICommand Paste { get; }
        public static RoutedUICommand Rename { get; }
        public static RoutedUICommand Delete { get; }
        public static RoutedUICommand Save { get; }
        public static RoutedUICommand CreateFile { get; }
        public static RoutedUICommand CreateFolder { get; }
        public static RoutedUICommand Create100Files { get; }
        public static RoutedUICommand Create100Folders { get; }
        public static RoutedUICommand ShowProperties { get; }
    }
}
