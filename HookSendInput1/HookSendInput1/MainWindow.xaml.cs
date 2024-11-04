using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HookSendInput1
{
    public partial class MainWindow : Window
    {
        

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.hookId = ViewModel.SetHook(ViewModel.mouseProc);
        }

        public void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.UnhookWindowsHookEx(ViewModel.hookId);
        }

    }
}
