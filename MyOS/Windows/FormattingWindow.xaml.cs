using System;
using System.ComponentModel;
using System.Windows.Input;
using MyOS.FileSystem;
using MyOS.FileSystem.SpecialDataTypes;

namespace MyOS
{
    /// <summary>
    /// Логика взаимодействия для FrmattingWindow.xaml
    /// </summary>
    public partial class FormattingWindow
    {
        private readonly BackgroundWorker _backgroundWorker;
        private readonly FormattingOptions _formattingOptions;
        public FormattingWindow( FormattingOptions formattingOptions)
        {
            InitializeComponent();

            _formattingOptions = formattingOptions;
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SystemCalls.Formatting((FormattingOptions)e.Argument);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }
        
        private void FormattingWindow_OnActivated(object sender, EventArgs e)
        {
            string[] animation = { ".    ", ". .  ", ". . ." };
            int second = 0;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1),
                IsEnabled = true
            };
            timer.Tick += (o, t) =>
            {
                Animation.Text = animation[second];
                if (second == 2) second = 0;
                else second++;
            };
            timer.Start();

            if(!_backgroundWorker.IsBusy)
            _backgroundWorker.RunWorkerAsync(_formattingOptions);
        }
    }
}
