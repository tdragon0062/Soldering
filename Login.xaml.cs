using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.NetworkOperators;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Soldering_Mgmt;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Login : Window
{
    public Login()
    {
        this.InitializeComponent();
        // 솔루션 루트 경로 구하기
        string solutionDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\.."));

        string imagePath = Path.Combine(solutionDir, "img", "gaon-logo-black_ko.png");

        // 이미지 Source 설정
        LogoImage.Source = new BitmapImage(new Uri(imagePath));

    }
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        string id = UserIdBox.Text?.Trim();
        string pw = PasswordBox.Password ?? "";
        string errorGubun = "로그인";
        string errorMessage = "";

        if (string.IsNullOrWhiteSpace(id))
        {
            errorMessage = "로그인 아이디를 입력하세요.";
           UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            return;
        }

        // 쓰기 권한이 확실한 경로 권장 (예: %LocalAppData%\YourApp\data\users.csv)
        // string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YourApp");
        // string filePath = Path.Combine(baseDir, "data", "users.csv");

        // string filePath = Path.Combine(AppContext.BaseDirectory, "data", "users.csv");
        string filePath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\..", "data", "users.csv");

        if (!File.Exists(filePath))
        {
            //errorMessage = "사용자 파일이 존재하지 않습니다." + Environment.NewLine + $"{AppContext.BaseDirectory}";
            //errorMessage = "사용자 파일이 존재하지 않습니다." + Environment.NewLine + $"{filePath}";
            errorMessage = "사용자 파일이 존재하지 않습니다." + Environment.NewLine + "관리자에 연락 하세요. ";
            UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            return;
        }

        // 1) 전체 읽기
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            errorMessage = "사용자 파일을 읽는 중 오류가 발생했니다." + Environment.NewLine + $"{ex.Message}";
            UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            return;
        }

        var newLines = new List<string>(capacity: lines.Length);
        bool found = false;
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var raw in lines)
        {
            var line = raw;
            if (string.IsNullOrWhiteSpace(line))
            {
                newLines.Add(line); // 공백 라인도 그대로 보존(원하면 스킵 가능)
                continue;
            }

            string[] parts = line.Split(';');

            // 최소 2필드(id, passwd) 없는 줄은 손대지 않고 보존
            if (parts.Length < 2)
            {
                newLines.Add(line);
                continue;
            }

            string userId = parts[0].Trim();
            string passwd = parts[1].Trim();

            // 2) Base64 안전 디코딩
            string decodedPw = passwd;
            try
            {
                // 패스워드는 Base64로 저장되어 있다는 전제
                byte[] data = Convert.FromBase64String(passwd);
                decodedPw = Encoding.UTF8.GetString(data);
            }
            catch
            {
                continue; // Base64 
            }

            if (!found && userId.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                decodedPw.Equals(pw, StringComparison.OrdinalIgnoreCase))
            {
                found = true;

                // 3) 필드 수를 정확히 7칸으로 맞춤: [0]=ID, [1]=PW(Base64), [2..5]=기타, [6]=LastLogin
                if (parts.Length < 7)
                {
                    Array.Resize(ref parts, 7);
                }
                parts[6] = currentTime; // LastLogin 업데이트 (없으면 새로 채움)

                // 4) 트레일링 세미콜론 없이 깔끔하게 조인
                string updatedLine = string.Join(";", parts);
                newLines.Add(updatedLine);
            }
            else
            {
                // 매치 안 되면 원본 라인 유지
                newLines.Add(line);
            }
        }

        if (found)
        {
            try
            {
                // 5) 한 번만 쓰기 (읽기 종료 후)
                File.WriteAllLines(filePath, newLines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorMessage = "사용자 파일을 쓰는 중 오류가 발생했니다." + Environment.NewLine + $"{ex.Message}";
                UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
                return;
            }

            UserSession.UserId = id;

            var mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }
        else
        {
            errorMessage = "아이디 또는 비밀번호가 잘못되었습니다.";
            UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
        }
    }
    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            Login_Click(LoginBtn, null);
        }
    }

}
