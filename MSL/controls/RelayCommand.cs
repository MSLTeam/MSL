using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSL.controls
{
    public class RelayCommand : ICommand
    {
        public Action<object> ExecuteAction { get; }
        public RelayCommand(Action<object> executeAction)
        {
            ExecuteAction= executeAction;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                return;
            }

            remove
            {
                return;
            }
        }

        //public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            ExecuteAction?.Invoke(parameter);
        }
    }
}
