using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Soldering_Mgmt
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private MainWindow? _mainWindow;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        public event Action<string>? LoginSuccess;
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            //_window = new MainWindow();     // 메인화면 직접 시작
            //_window.Activate();
            // 로그인 창             
            ShowLogin();
        }
        private void ShowLogin()
        {
            var login = new Login();
            login.LoginSuccess += OnLoginSuccess;
            login.Activate();
        }

        private void OnLoginSuccess(string userId)
        {
            UserSession.UserId = userId;

            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.OnIdleTimeoutTriggered += ShowLogin; // Idle되면 다시 로그인 띄움
                _mainWindow.Activate();
            }
            else
            {
                _mainWindow.RefreshUiWithNewUser();
            }
        }
    }
}
