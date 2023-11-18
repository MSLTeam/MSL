using HandyControl.Controls;
using MSL.controls;
using MSL.pages;
using MSL.pages.frpProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MSL
{
    /// <summary>
    /// SetFrpc.xaml 的交互逻辑
    /// </summary>
    public partial class SetFrpc : HandyControl.Controls.Window
    {
        public SetFrpc()
        {
            InitializeComponent();
        }
    }
}
