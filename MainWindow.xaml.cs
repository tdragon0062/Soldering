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
        // 카메라 스트리밍을 위한 전역변수 선언
        private MediaCapture _mediaCapture;
        private DispatcherTimer _frameTimer;
        // 타이머 핸들   
        private DispatcherTimer _uiTimer;
        public event Action? OnIdleTimeoutTriggered;
        private UserIdleCheck _idleCheck = new UserIdleCheck();

        public MainWindow()
        {
            this.InitializeComponent();
            // 창에서 최대화 버튼 제거
            DisableMaximizeButton();
            DisableCloseButton();

            // Full Screen을 위해 현재 윈도우 인스턴스를 가져옵니다
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

           if (appWindow != null)
            {
                // 윈도우를 최대화하여 Full Size 또는 특정 크기로 만듭니다.
                // appWindow.Resize(new SizeInt32(1920, 1080));
                // appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
               
                if (appWindow.Presenter is OverlappedPresenter p)
                {
                    p.Maximize(); // 창틀은 유지되고, 화면 전체로 확장됨
                    p.IsResizable = false; // 창틀에서 Resize 방지
                }     
            }
            // 유휴 시간 처리 설정 및 핸들러 전달
            UserSession.tmOutMin = 1;
            _idleCheck.TimerSet(this, UserSession.tmOutMin,OnIdleTimeout);
            // 2. UI 시계 갱신 타이머 (매 분)
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _uiTimer.Tick += (s, e) => UpdateCurrentTime();
            _uiTimer.Start();

            UpdateCurrentTime();

            // MainWindow 이미지 디스플레이
            ImageDisplay();


            // 조건에 따라 SEMEMA 통신 진입부 배출부 표기 컬러 지정
            // DeepPinkInterInBorderColor(), LightGrayInterInBorderColor(), DeepPinkInterOutBorderColor(), LightGrayInterOutBorderColor()
            // DeepPinkExInBorderColor(), LightGrayExInBorderColor(), DeepPinkExOutBorderColor(), LightGrayExOutBorderColor()
            //DeepPinkExInBorderColor();

            /* x:Name으로 지정한 Border 컨트롤을 참조하여 배경색을 변경합니다.
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

            

            ServiceActivatioMode.Text = "Bypass"; // 서비스 동작 모드
            LineInfoDisplay(); // data\line.csv 라인명 디스플레이 
           

            User.Text =  UserSession.UserId; // 사용자

            if (NumDevideRpm.Value != null)  //  납조 ->분류 초기 속도 표기
                DevideRpm.Text = (NumDevideRpm.Value.ToString());
            if (NumSuckBackRpm.Value != null)   //  납조 ->석백 초기 속도 표기
                SuckBackRpm.Text = (NumSuckBackRpm.Value.ToString());

            //  Main Pressure 도넛 상태표기
            // 예: 실시간 센서 값 6.0, 5.0  정상 : Green,  에러 : Red
            MainPressureOne.Text = "6";
            MainPressureTwo.Text = "5";
            MainDonutArc.Stroke = new SolidColorBrush(Colors.Green);
            UpdateDonut("Main", "green");
            // N2 Pressure 도넛 상태표기
            // 예: 실시간 센서 값 6.0, 5.0  정상 : Green,  에러 : Red
            NTwoPressureOne.Text = "4";
            NTwoPressureTwo.Text = "8";
            NTwoDonutArc.Stroke = new SolidColorBrush(Colors.Green);
            UpdateDonut("NTwo", "green");
            // Tank Pressure 도넛 상태표기
            // 예: 실시간 센서 값 6.0, 5.0  정상 : Green, 에러 : Red
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
            // 솔루션 루트 경로 구하고 이미지 파일 경로 생성
            //string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\.."));
            string solutionDir = AppContext.BaseDirectory;

            // Buzzer Stop 이미지 Source 설정
            string imagePath = System.IO.Path.Combine(solutionDir, "img", "gaon-logo-black_ko.png");
            GaonLogoImage.Source = new BitmapImage(new Uri(imagePath)); 

            // Buzzer Stop 이미지 Source 설정
            imagePath = System.IO.Path.Combine(solutionDir, "img", "buzzer-stop.png");
            BuzzerStopImage.Source = new BitmapImage(new Uri(imagePath));

            // AutoModeBtn  이미지 Source 설정
            imagePath = System.IO.Path.Combine(solutionDir, "img", "AutoModeBtn.png");
            AutoModeBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // ManualModeBtn  이미지 Source 설정
            imagePath = System.IO.Path.Combine(solutionDir, "img", "ManualModeBtn.png");
            ManualModeBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // StartBtn  이미지 Source 설정
            imagePath = System.IO.Path.Combine(solutionDir, "img", "StartBtn.png");
            StartBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // StopBtn  이미지 Source 설정
            imagePath = System.IO.Path.Combine(solutionDir, "img", "StopBtn.png");
            StopBtnImage.Source = new BitmapImage(new Uri(imagePath));

            // ResetBtn  이미지 Source 설정
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
            // 1) 전체 읽기
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorMessage = "사용자 파일을 읽는 중 오류가 발생했니다." + Environment.NewLine + $"{ex.Message}";
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

                // 최소 2필드(id, passwd) 없는 줄은 손대지 않고 보존
                if (parts.Length < 2)
                {
                    continue;
                }
                // LineName.Text = "가온글로벌 1 라인";  // data\line.csv
                //ModelName.Text = "가온글로벌 프로젝트";  
                LineName.Text = parts[0].Trim();
                ModelName.Text = parts[1].Trim();
            }
        }
        private void Timer_Tick(object sender, object e)
        {
            // 타이머가 1분마다 호출되면 실행
            UpdateCurrentTime();
        }
        public void UpdateCurrentTime()
        {
            // 현재 시간을 "YYYY-MM-DD HH:MM" 형식으로 포맷
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // TextBlock에 표시
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

            // 최대화 버튼 제거 (비트 마스킹)
            style &= ~NativeMethods.WS_MAXIMIZEBOX;

            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GWL_STYLE, style);
        }
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this); // WinRT.Interop 사용
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        // 재로그인 시 화면 리프레쉬
        public async void RefreshUiWithNewUser()
        {
            if (UserSession.UserId != null)
            {
                User.Text = UserSession.UserId;
                UpdateCurrentTime();
            }
            else
            {
                string errorGubun = "로그인";
                string errorMessage = "다시 로그인이 필요합니다.";
                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            }
        }
       
        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string confirmGubun = "로그인";
            string confirmMSG = "정말 현재 사용자를 변경하고 싶으십니까? ";
            bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
            if (ok)
            {
                UserSession.UserId = null;
                // 세션로그아웃 다시 설정

                _idleCheck.Restart(this, UserSession.tmOutMin, OnIdleTimeout);

                // App에게 알림 (직접 Login 띄우지 않고 App에 위임)
                OnIdleTimeoutTriggered?.Invoke();          
                // UserSession.UserId = null;
                // var newlogin = new Login();
                // newlogin.Activate();
                // this.Close();
            }
        }             
        private async Task OnIdleTimeout()
        {

            string errorGubun = "장시간 대기상태";
            string errorMessage = "다시 로그인이 필요합니다.";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);

            // UI 스레드에서 처리
            this.DispatcherQueue.TryEnqueue(() =>
            {
                //  세션 정리
                UserSession.UserId = null;
                // 세션로그아웃 다시 설정

                _idleCheck.Restart(this, UserSession.tmOutMin, OnIdleTimeout);

                // App에게 알림 (직접 Login 띄우지 않고 App에 위임)
                OnIdleTimeoutTriggered?.Invoke();               
            });
        }
        private async void OriginBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);           
        }
        private async void FileCallBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void BuzzerStopBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
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

                /* Debugging Code로 값 확인
               string errorGubun = "태그값";
               string errorMessage = $"버튼의 태그 값 : CurrTag = {currTagColor}, CurrBG = {currBGColor},  CurrFG = {currFGColor}" + Environment.NewLine + $"ToBG = {toBGColor}, ToFG = {toFGColor} ";
               UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
               */

                UserBtnColorSet.ButtonColorSet(button, currTagColor, currBGColor, currFGColor, toBGColor, toFGColor);
            }
        }
        private async void SetUpBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            if (UserSession.UserId != "root")
            {
                string errorGubun = "권한오류";
                string errorMessage = "슈퍼 관리자만 설정 변경이 가능합니다.";
                await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                return;
            }
            else
            {
                string errorGubun = "태그값";
                string errorMessage = $"버튼의 태그 값 : {tagValue}";
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
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void DivSpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;

            if (NumDevideRpm.Value != null) //  납조 ->분류 초기 속도 표기
            {
                if (clickedButton != null)
                {
                    // 2. Tag 속성 값 가져오기
                    string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                    // tagValue가 비어 있지 않을 경우에만 대화 상자를 표시합니다.
                    if (!string.IsNullOrEmpty(tagValue))
                    {
                        if (NumDevideRpm.Value.ToString() != DevideRpm.Text)
                        {
                            string confirmGubun = "납조 분류속도 값";
                            string confirmMSG = $"설정값( {NumDevideRpm.Value.ToString()} rpm )으로 변경하시겠습니까?.";
                            bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
                            if (ok)
                            {
                                if (NumDevideRpm.Value != null)  //  납조 ->분류 초기 속도 표기
                                    DevideRpm.Text = (NumDevideRpm.Value.ToString());
                            }
                            else
                            {
                                if (int.TryParse(DevideRpm.Text, out int rpm))
                                {
                                    // 변환 성공
                                    NumDevideRpm.Value = rpm;
                                }
                                else
                                {
                                    // 변환 실패 (예: 빈 문자열, 숫자가 아님)
                                    NumDevideRpm.Value = 0;   // 기본값 대입                                                                
                                }
                            }
                        }
                        else
                        {
                            string errorGubun = "납조 분류속도 값";
                            string errorMessage = $"설정값( {NumDevideRpm.Value.ToString()} rpm )이 동일합니다.";
                            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                        }
                    }
                }
            }
        }
        private async void SuckBackSpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                if (NumSuckBackRpm.Value != null)  //  납조 ->석백 초기 속도 표기
                {
                    if (clickedButton != null)
                    {
                        // 2. Tag 속성 값 가져오기
                        string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                        // tagValue가 비어 있지 않을 경우에만 대화 상자를 표시합니다.
                        if (!string.IsNullOrEmpty(tagValue))
                        {
                            if (NumSuckBackRpm.Value.ToString() != SuckBackRpm.Text)
                            {
                                string confirmGubun = "납조 석백속도 값";
                                string confirmMSG = $"설정값( {NumSuckBackRpm.Value.ToString()} rpm )으로 변경하시겠습니까?.";
                                bool ok = await UserCheckMSG.ShowConfirmMSG(this.Content.XamlRoot, confirmGubun, confirmMSG);
                                if (ok)
                                {
                                    if (NumSuckBackRpm.Value != null)  //  납조 -> 석백 속도 표기
                                        SuckBackRpm.Text = (NumSuckBackRpm.Value.ToString());
                                }
                                else
                                {
                                    if (int.TryParse(SuckBackRpm.Text, out int rpm))
                                    {
                                        // 변환 성공
                                        NumSuckBackRpm.Value = rpm;
                                    }
                                    else
                                    {
                                        // 변환 실패 (예: 빈 문자열, 숫자가 아님)
                                        NumSuckBackRpm.Value = 0;   // 기본값 대입
                                                                    // 또는 사용자에게 에러 메시지 표시
                                    }
                                }
                            } 
                            else
                            {
                                string errorGubun = "납조 석백속도 값";
                                string errorMessage = $"설정값( {NumSuckBackRpm.Value.ToString()} rpm )이 동일합니다.";
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
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
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
                Debug.WriteLine($"프레임 캡처 오류: {ex.Message}");
                // 필요시 사용자에게 ContentDialog로 알림 가능
            }
        }
        private async Task StopCaptureFrameLoopAsync()
        {
            try
            {
                // 프레임 수신 루프 타이머 정지
                _frameTimer?.Stop();
                _frameTimer = null;

                // 미리보기 정지
                if (_mediaCapture != null)
                {
                    await _mediaCapture.StopPreviewAsync();
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }

                // UI 초기화 (예: 카메라 이미지 제거)
                SolderNozzleCameraImage.Source = null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("카메라 권한 오류: " + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine("이미 해제된 MediaCapture: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("프레임 종료 중 오류 발생: " + ex.Message);
            }
        }
        private void StartFrameTimer()    // 카메라 프레임 타이버
        {
            _frameTimer = new DispatcherTimer();
            _frameTimer.Interval = TimeSpan.FromMilliseconds(100); //  100ms 마다 CapturefraneAsync 호출 10fps 정도
            _frameTimer.Tick += async (s, e) => await StartCaptureFrameLoopAsync();
            _frameTimer.Start();
        }
        private void UpdateDonut(string ArcName, string colorName)
        {
                       // 값에 따른 각도 비율 (0 ~ 270도)
            double angle = 270;

            // 반지름, 중심
            double radius = 20;
            double centerX = 30;
            double centerY = 30;

            // 시작 각도: 135도 (왼쪽 아래)
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

            // 아크 경로 설정
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
                // 색상 적용
                MainDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
            else if (ArcName == "NTwo")
            {
                NTwoDonutArc.Data = pathGeometry;
                // 색상 적용
                NTwoDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
            else
            {
                TankDonutArc.Data = pathGeometry;
                // 색상 적용
                TankDonutArc.Stroke = new SolidColorBrush(GetColorFromString(colorName));
            }
        }
        private async void AutoModeBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                // 2. Tag 속성 값 가져오기
                string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;

                // tagValue가 비어 있지 않을 경우에만 대화 상자를 표시합니다.
                if (!string.IsNullOrEmpty(tagValue))
                {
                    ContentDialog messageDialog = new ContentDialog
                    {
                        Title = "태그 값",
                        Content = $"버튼의 태그 값: {tagValue}",
                        CloseButtonText = "확인",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await messageDialog.ShowAsync();
                }
            }
        }
        private async void ManualModeBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        private async void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            // 1. sender를 Button 타입으로 캐스팅
            Button clickedButton = sender as Button;
            // 2. Tag가 null 이 아니면 ToString() 호출 null 이면 "" 
            string tagValue = clickedButton.Tag?.ToString() ?? string.Empty;
            string errorGubun = "태그값";
            string errorMessage = $"버튼의 태그 값 : {tagValue}";
            await UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
        // 문자열 → Color 매핑 함수
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
            // 현재 날짜와 시간을 가져옵니다.
            DateTime currentTime = DateTime.Now;

            return currentTime;
        }
    }
}
