using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MSL.controls
{
    public class WindowViewModel
    {
        public RelayCommand MinimizeCommand { get; }
        public RelayCommand MaximizeCommand { get; }
        public RelayCommand CloseCommand { get; }
        public WindowViewModel()
        {
            MinimizeCommand = new RelayCommand(MinimizeWindow);
            MaximizeCommand = new RelayCommand(MaximizeWindow);
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private void MinimizeWindow(object obj)
        {
            SystemCommands.MinimizeWindow(obj as Window);
        }

        private void MaximizeWindow(object obj)
        {
            var window = obj as Window;
            switch(window.WindowState)
            {
                case WindowState.Normal:
                    SystemCommands.MaximizeWindow(window);
                    break;
                case WindowState.Maximized:
                    SystemCommands.RestoreWindow(window);
                    break;
                case WindowState.Minimized:
                    break;
            }
        }

        private void CloseWindow(object obj)
        {
            SystemCommands.CloseWindow(obj as Window);
        }
    }
}
