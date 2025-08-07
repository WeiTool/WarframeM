using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WarframeM
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置Toast通知激活处理
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // 激活主窗口
                Current.MainWindow?.Activate();
                Current.MainWindow.WindowState = WindowState.Normal;
            };
        }
    }
}