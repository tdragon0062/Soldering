using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.PointOfService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Networking.NetworkOperators;
using Windows.UI;
using WinRT.Interop;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Soldering_Mgmt
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // ī�޶� ��Ʈ������ ���� �������� ����
        private MediaCapture _mediaCapture;
        private DispatcherTimer _frameTimer;
        // Ÿ�̸� �ڵ�   
        private DispatcherTimer _uiTimer;
        public event Action? OnIdleTimeoutTriggered;
        private UserIdleCheck _idleCheck = new UserIdleCheck();

        public MainWindow()
        {
            this.InitializeComponent();
            // â���� �ִ�ȭ ��ư ����
            DisableMaximizeButton();
            DisableCloseButton();

            // Full Screen�� ���� ���� ������ �ν��Ͻ��� �����ɴϴ�
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

           if (appWindow != null)
            {
                // �����츦 �ִ�ȭ�Ͽ� Full Size �Ǵ� Ư�� ũ��� ����ϴ�.
                // appWindow.Resize(new SizeInt32(1920, 1080));
                // appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
               
                if (appWindow.Presenter is OverlappedPresenter p)
                {
                    p.Maximize(); // âƲ�� �����ǰ�, ȭ�� ��ü�� Ȯ���
                    p.IsResizable = false; // âƲ���� Resize ����
                }     
            }
            // ���� �ð� ó�� ���� �� �ڵ鷯 ����
            UserSession.tmOutMin = 1;
            _idleCheck.TimerSet(this, UserSession.tmOutMin,OnIdleTimeout);
            // 2. UI �ð� ���� Ÿ�̸� (�� ��)
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _uiTimer.Tick += (s, e) => UpdateCurrentTime();
            _uiTimer.Start();

            UpdateCurrentTime();

            // MainWindow �̹��� ���÷���
            ImageDisplay();


            // ���ǿ� ���� SEMEMA ��� ���Ժ� ����� ǥ�� �÷� ����
            // DeepPinkInterInBorderColor(), LightGrayInterInBorderColor(), DeepPinkInterOutBorderColor(), LightGrayInterOutBorderColor()
            // DeepPinkExInBorderColor(), LightGrayExInBorderColor(), DeepPinkExOutBorderColor(), LightGrayExOutBorderColor()
            //DeepPinkExInBorderColor();

            /* x:Name���� ������ Border ��Ʈ���� �����Ͽ� ������ �����մϴ�.
               InterInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);
               InterInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
               InterOutBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);
               InterOutBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);       
               ExInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);      
               ExInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);        
               ExOutBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);
               ExOutBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            */
            InterInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);
            ExInBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DeepPink);

            

            ServiceActivatioMode.Text = "Bypass"; // ���� ���� ���
            LineInfoDisplay(); // data\line.csv ���θ� ���÷��� 
           

            User.Text =  UserSession.UserId; // �����

            if (NumDevideRpm.Value != null)  //  ���� ->�з� �ʱ� �ӵ� ǥ��
                DevideRpm.Text = (NumDevideRpm.Value.ToString());
            if (NumSuckBackRpm.Value != null)   //  ���� ->���� �ʱ� �ӵ� ǥ��
                SuckBackRpm.Text = (NumSuckBackRpm.Value.ToString());

            //  Main Pressure ���� ����ǥ��
            // ��: �ǽð� ���� �� 6.0, 5.0  ���� : Green,  ���� : Red
            MainPressureOne.Text = "6";
            MainPressureTwo.Text = "5";
            MainDonutArc.Stroke = new SolidColorBrush(Colors.Green);
            UpdateDonut("Main", "green");
            // N2 Pressure ���� ����ǥ��
            // ��: �ǽð� ���� �� 6.0, 5.0  ���� : Green,  ���� : Red
            NTwoPressureOne.Text = "4";
            NTwoPressureTwo.Text = "8";
            NTwoDonutArc.Stroke = new SolidColorBrush(Colors.Green);
            UpdateDonut("NTwo", "green");
            // Tank Pressure ���� ����ǥ��
            // ��: �ǽð� ���� �� 6.0, 5.0  ���� : Green, ���� : Red
            TankPressureOne.Text = "7";
            TankPressureOne.Foreground = new SolidColorBrush(Colors.Red);
            TankPressureTwo.Text = "2";
            TankPressureDot.Foreground = new SolidColorBrush(Colors.Red);
            TankPressureTwo.Foreground = new SolidColorBrush(Colors.Red);
            TankPressureMpa.Foreground = new SolidColorBrush(Colors.Red);
            TankDonutArc.Stroke = new SolidColorBrush(Colors.Green);
            UpdateDonut("Tank", "red");
        }
        // Disable Close Button
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const uint SC_CLOSE = 0xF060;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_BYCOMMAND = 0x00000000;

        private void DisableCloseButton()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            IntPtr hMenu = GetSystemMenu(hWnd, false);
            if (hMenu != IntPtr.Zero)
            {
                EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
            }
        }
        private void ImageDisplay()
        {
            // �ַ�� ��Ʈ ��� ���ϰ� �̹��� ���� ��� ����
            //string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\.."));
            string solutionDir = AppContext.BaseDirectory;

            // Buzzer Stop �̹��� Source ����
            string imagePath = System.IO.Path.Combine(solutionDir, "img", "gaon-logo-black_ko.png");
            GaonLogoImage.Source = new BitmapImage(new Uri(imagePath)); 

            // Buzzer Stop �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "buzzer-stop.png");
            BuzzerStopImage.Source = new BitmapImage(new Uri(imagePath));

            // AutoModeBtn  �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "AutoModeBtn.png");
            AutoModeBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // ManualModeBtn  �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "ManualModeBtn.png");
            ManualModeBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // StartBtn  �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "StartBtn.png");
            StartBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // StopBtn  �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "StopBtn.png");
            StopBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // ResetBtn  �̹��� Source ����
            imagePath = System.IO.Path.Combine(solutionDir, "img", "ResetBtn.png");
            ResetBtnImage.Source = new BitmapImage(new Uri(imagePath));            
        }
        private void LineInfoDisplay()
        {
            string filePath = System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\..", "data", "line.csv");
            string errorGubun = "MainWindow";
            string errorMessage = "";


            if (!System.IO.File.Exists(filePath))
            {
                LineName.Text = "";
                ModelName.Text = "";
                return;
            }
            // 1) ��ü �б�
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorMessage = "����� ������ �д� �� ������ �߻��ߴϴ�." + Environment.NewLine + $"{ex.Message}";
                UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                return;
            }


            foreach (var raw in lines)
            {
                var line = raw;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(';');

                // �ּ� 2�ʵ�(id, passwd) ���� ���� �մ��� �ʰ� ����
                if (parts.Length < 2)
                {
                    continue;
                }
                // LineName.Text = "���±۷ι� 1 ����";  // data\line.csv
                //ModelName.Text = "���±۷ι� ������Ʈ";  
                LineName.Text = parts[0].Trim();
                ModelName.Text = parts[1].Trim();
            }
        }
        private void Timer_Tick(object sender, object e)
        {
            // Ÿ�̸Ӱ� 1�и��� ȣ��Ǹ� ����
            UpdateCurrentTime();
        }
        public void UpdateCurrentTime()
        {
            // ���� �ð��� "YYYY-MM-DD HH:MM" �������� ����
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // TextBlock�� ǥ��
            CurrentTime.Text = formattedTime;
        }
        public static class NativeMethods
        {
            public const int GWL_STYLE = -16;
            public const uint WS_MAXIMIZEBOX = 0x00010000;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, uint dwNewLong);
        }
        private void DisableMaximizeButton()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            uint style = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_STYLE);

            // �ִ�ȭ ��ư ���� (��Ʈ ����ŷ)
            style &= ~NativeMethods.WS_MAXIMIZEBOX;

            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GWL_STYLE, style);
        }
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this); // WinRT.Interop ���
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        // ��α��� �� ȭ�� ��������
        public async void RefreshUiWithNewUser()
        {
            if (UserSession.UserId != null)
            {
                User.Text = UserSession.UserId;
                UpdateCurrentTime();
            }
            else
            {
                string errorGubun = "�α���";
                string errorMessage = "�ٽ� �α����� �ʿ��մϴ�.";
                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            }
        }
       
        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string confirmGubun = "�α���";
            string confirmMSG = "���� ���� ����ڸ� �����ϰ� �����ʴϱ�? ";
            bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
            if (ok)
            {
                UserSession.UserId = null;
                // ���Ƿα׾ƿ� �ٽ� ����

                _idleCheck.Restart(this, UserSession.tmOutMin, OnIdleTimeout);

                // App���� �˸� (���� Login ����� �ʰ� App�� ����)
                OnIdleTimeoutTriggered?.Invoke();          
                // UserSession.UserId = null;
                // var newlogin = new Login();
                // newlogin.Activate();
                // this.Close();
            }
        }             
        private async Task OnIdleTimeout()
        {

            string errorGubun = "��ð� ������";
            string errorMessage = "�ٽ� �α����� �ʿ��մϴ�.";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);

            // UI �����忡�� ó��
            this.DispatcherQueue.TryEnqueue(() =>
            {
                //  ���� ����
                UserSession.UserId = null;
                // ���Ƿα׾ƿ� �ٽ� ����

                _idleCheck.Restart(this, UserSession.tmOutMin, OnIdleTimeout);

                // App���� �˸� (���� Login ����� �ʰ� App�� ����)
                OnIdleTimeoutTriggered?.Invoke();               
            });
        }
        private async void OriginBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);           
        }
        private async void FileCallBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void BuzzerStopBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private void RoomLightBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                /* Debugging Code�� �� Ȯ��
               string errorGubun = "�±װ�";
               string errorMessage = $"��ư�� �±� �� : CurrTag = {currTagColor}, CurrBG = {currBGColor},  CurrFG = {currFGColor}" + Environment.NewLine + $"ToBG = {toBGColor}, ToFG = {toFGColor} ";
               UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
               */

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private async void SetUpBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            if (UserSession.UserId != "root")
            {
                string errorGubun = "���ѿ���";
                string errorMessage = "���� �����ڸ� ���� ������ �����մϴ�.";
                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                return;
            }
            else
            {
                string errorGubun = "�±װ�";
                string errorMessage = $"��ư�� �±� �� : {tagValue}";
                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            }
        }
        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "DeepPink";
                string currFGColor = "White";
                string toBGColor = "Gray";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void RecipeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void PassiveBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void IOBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void AlramBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void ChartBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void ByPassBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void WeeklyTimmerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void HeaterPowerBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Gray";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private void NTwoValveBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "Black";
                string currFGColor = "White";
                string toBGColor = "DeepPink";
                string toFGColor = "White";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private async void HeaterSetStoreBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void DivSpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;

            if (NumDevideRpm.Value != null) //  ���� ->�з� �ʱ� �ӵ� ǥ��
            {
                if (clickedButton != null)
                {
                    // 2. Tag �Ӽ� �� ��������
                    string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                    // tagValue�� ��� ���� ���� ��쿡�� ��ȭ ���ڸ� ǥ���մϴ�.
                    if (!string.IsNullOrEmpty(tagValue))
                    {
                        if (NumDevideRpm.Value.ToString() != DevideRpm.Text)
                        {
                            string confirmGubun = "���� �з��ӵ� ��";
                            string confirmMSG = $"������( {NumDevideRpm.Value.ToString()} rpm )���� �����Ͻðڽ��ϱ�?.";
                            bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
                            if (ok)
                            {
                                if (NumDevideRpm.Value != null)  //  ���� ->�з� �ʱ� �ӵ� ǥ��
                                    DevideRpm.Text = (NumDevideRpm.Value.ToString());
                            }
                            else
                            {
                                if (int.TryParse(DevideRpm.Text, out int rpm))
                                {
                                    // ��ȯ ����
                                    NumDevideRpm.Value = rpm;
                                }
                                else
                                {
                                    // ��ȯ ���� (��: �� ���ڿ�, ���ڰ� �ƴ�)
                                    NumDevideRpm.Value = 0;   // �⺻�� ����                                                                
                                }
                            }
                        }
                        else
                        {
                            string errorGubun = "���� �з��ӵ� ��";
                            string errorMessage = $"������( {NumDevideRpm.Value.ToString()} rpm )�� �����մϴ�.";
                            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                        }
                    }
                }
            }
        }
        private async void SuckBackSpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                if (NumSuckBackRpm.Value != null)  //  ���� ->���� �ʱ� �ӵ� ǥ��
                {
                    if (clickedButton != null)
                    {
                        // 2. Tag �Ӽ� �� ��������
                        string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                        // tagValue�� ��� ���� ���� ��쿡�� ��ȭ ���ڸ� ǥ���մϴ�.
                        if (!string.IsNullOrEmpty(tagValue))
                        {
                            if (NumSuckBackRpm.Value.ToString() != SuckBackRpm.Text)
                            {
                                string confirmGubun = "���� ����ӵ� ��";
                                string confirmMSG = $"������( {NumSuckBackRpm.Value.ToString()} rpm )���� �����Ͻðڽ��ϱ�?.";
                                bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
                                if (ok)
                                {
                                    if (NumSuckBackRpm.Value != null)  //  ���� -> ���� �ӵ� ǥ��
                                        SuckBackRpm.Text = (NumSuckBackRpm.Value.ToString());
                                }
                                else
                                {
                                    if (int.TryParse(SuckBackRpm.Text, out int rpm))
                                    {
                                        // ��ȯ ����
                                        NumSuckBackRpm.Value = rpm;
                                    }
                                    else
                                    {
                                        // ��ȯ ���� (��: �� ���ڿ�, ���ڰ� �ƴ�)
                                        NumSuckBackRpm.Value = 0;   // �⺻�� ����
                                                                    // �Ǵ� ����ڿ��� ���� �޽��� ǥ��
                                    }
                                }
                            } 
                            else
                            {
                                string errorGubun = "���� ����ӵ� ��";
                                string errorMessage = $"������( {NumSuckBackRpm.Value.ToString()} rpm )�� �����մϴ�.";
                                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                            }
                        }
                    }
                }
            }
        }
        private void NabjoStopBtn_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string currTagColor = button.Tag.ToString();
                string currBGColor = "LightGray";
                string currFGColor = "Black";
                string toBGColor = "DeepPink";
                string toFGColor = "Black";

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }

        }
        private async void NabjoSetStoreBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);

        }       
        private async Task StartCaptureFrameLoopAsync()
        {
            try
            {
                using var frame = new VideoFrame(BitmapPixelFormat.Bgra8, 640, 480);
                await _mediaCapture.GetPreviewFrameAsync(frame);

                SoftwareBitmap bitmap = frame.SoftwareBitmap;

                var bitmapSource = new SoftwareBitmapSource();
                await bitmapSource.SetBitmapAsync(bitmap);

                SolderNozzleCameraImage.Source = bitmapSource;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"������ ĸó ����: {ex.Message}");
                // �ʿ�� ����ڿ��� ContentDialog�� �˸� ����
            }
        }
        private async Task StopCaptureFrameLoopAsync()
        {
            try
            {
                // ������ ���� ���� Ÿ�̸� ����
                _frameTimer?.Stop();
                _frameTimer = null;

                // �̸����� ����
                if (_mediaCapture != null)
                {
                    await _mediaCapture.StopPreviewAsync();
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }

                // UI �ʱ�ȭ (��: ī�޶� �̹��� ����)
                SolderNozzleCameraImage.Source = null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("ī�޶� ���� ����: " + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine("�̹� ������ MediaCapture: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("������ ���� �� ���� �߻�: " + ex.Message);
            }
        }
        private void StartFrameTimer()    // ī�޶� ������ Ÿ�̹�
        {
            _frameTimer = new DispatcherTimer();
            _frameTimer.Interval = TimeSpan.FromMilliseconds(100); //  100ms ���� CapturefraneAsync ȣ�� 10fps ����
            _frameTimer.Tick += async (s, e) => await StartCaptureFrameLoopAsync();
            _frameTimer.Start();
        }
        private void UpdateDonut(string ArcName, string colorName)
        {
                       // ���� ���� ���� ���� (0 ~ 270��)
            double angle = 270;

            // ������, �߽�
            double radius = 20;
            double centerX = 30;
            double centerY = 30;

            // ���� ����: 135�� (���� �Ʒ�)
            double startAngle = 135;
            double endAngle = startAngle + angle;

            Point startPoint = new Point(
                centerX + radius * Math.Cos(startAngle * Math.PI / 180),
                centerY + radius * Math.Sin(startAngle * Math.PI / 180)
            );

            Point endPoint = new Point(
                centerX + radius * Math.Cos(endAngle * Math.PI / 180),
                centerY + radius * Math.Sin(endAngle * Math.PI / 180)
            );

            // ��ũ ��� ����
            bool isLargeArc = angle > 180;
            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            };

            var pathFigure = new PathFigure
            {
                StartPoint = startPoint,
                Segments = { arcSegment },
                IsClosed = false
            };

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            if (ArcName == "Main")
            {
                MainDonutArc.Data = pathGeometry;
                // ���� ����
                MainDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
            else if (ArcName == "NTwo")
            {
                NTwoDonutArc.Data = pathGeometry;
                // ���� ����
                NTwoDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
            else
            {
                TankDonutArc.Data = pathGeometry;
                // ���� ����
                TankDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
        }
        private async void AutoModeBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                // 2. Tag �Ӽ� �� ��������
                string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                // tagValue�� ��� ���� ���� ��쿡�� ��ȭ ���ڸ� ǥ���մϴ�.
                if (!string.IsNullOrEmpty(tagValue))
                {
                    ContentDialog messageDialog = new ContentDialog
                    {
                        Title = "�±� ��",
                        Content = $"��ư�� �±� ��: {tagValue}",
                        CloseButtonText = "Ȯ��",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await messageDialog.ShowAsync();
                }
            }
        }
        private async void ManualModeBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender�� Button Ÿ������ ĳ����
            Button clickedButton = sender as Button;
            // 2. Tag�� null �� �ƴϸ� ToString() ȣ�� null �̸� "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "�±װ�";
            string errorMessage = $"��ư�� �±� �� : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        // ���ڿ� �� Color ���� �Լ�
        private Color GetColorFromString(string colorName)
    {
        return colorName.ToLower() switch
        {
            "green" => Colors.Green,
            "yellow" => Colors.Yellow,
            "red" => Colors.Red,
            _ => Colors.Gray
        };
    }    
    private void StopFrameTimer()
        {
            _frameTimer?.Stop();
            _frameTimer = null;
        }
        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
        private DateTime GetCurrentTime()
        {
            // ���� ��¥�� �ð��� �����ɴϴ�.
            DateTime currentTime = DateTime.Now;

            return currentTime;
        }
    }
}
