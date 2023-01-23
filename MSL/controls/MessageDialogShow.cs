using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSL.controls
{
    public class MessageDialogShow
    {
        public static void Show(string dialogText,string dialogTitle,bool primaryBtn,string primaryText,string closeText)
        {
            MessageDialog._dialogText = dialogText;
            MessageDialog._dialogTitle = dialogTitle;
            MessageDialog._dialogPrimaryBtn = primaryBtn;
            MessageDialog._dialogPrimaryText = primaryText;
            MessageDialog._dialogCloseText = closeText;
        }
    }
}
