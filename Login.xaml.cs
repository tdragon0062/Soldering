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
        // �ַ�� ��Ʈ ��� ���ϱ�
        string solutionDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\.."));

        string imagePath = Path.Combine(solutionDir, "img", "gaon-logo-black_ko.png");

        // �̹��� Source ����
        LogoImage.Source = new BitmapImage(new Uri(imagePath));

    }
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        string id = UserIdBox.Text?.Trim();
        string pw = PasswordBox.Password ?? "";
        string errorGubun = "�α���";
        string errorMessage = "";

        if (string.IsNullOrWhiteSpace(id))
        {
            errorMessage = "�α��� ���̵� �Է��ϼ���.";
           UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            return;
        }

        // ���� ������ Ȯ���� ��� ���� (��: %LocalAppData%\YourApp\data\users.csv)
        // string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YourApp");
        // string filePath = Path.Combine(baseDir, "data", "users.csv");

        // string filePath = Path.Combine(AppContext.BaseDirectory, "data", "users.csv");
        string filePath = Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\..", "data", "users.csv");

        if (!File.Exists(filePath))
        {
            //errorMessage = "����� ������ �������� �ʽ��ϴ�." + Environment.NewLine + $"{AppContext.BaseDirectory}";
            //errorMessage = "����� ������ �������� �ʽ��ϴ�." + Environment.NewLine + $"{filePath}";
            errorMessage = "����� ������ �������� �ʽ��ϴ�." + Environment.NewLine + "�����ڿ� ���� �ϼ���. ";
            UserCheckMSG.ShowErrorMSG(this.Content.XamlRoot, errorGubun, errorMessage);
            return;
        }

        // 1) ��ü �б�
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            errorMessage = "����� ������ �д� �� ������ �߻��ߴϴ�." + Environment.NewLine + $"{ex.Message}";
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
                newLines.Add(line); // ���� ���ε� �״�� ����(���ϸ� ��ŵ ����)
                continue;
            }

            string[] parts = line.Split(';');

            // �ּ� 2�ʵ�(id, passwd) ���� ���� �մ��� �ʰ� ����
            if (parts.Length < 2)
            {
                newLines.Add(line);
                continue;
            }

            string userId = parts[0].Trim();
            string passwd = parts[1].Trim();

            // 2) Base64 ���� ���ڵ�
            string decodedPw = passwd;
            try
            {
                // �н������ Base64�� ����Ǿ� �ִٴ� ����
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

                // 3) �ʵ� ���� ��Ȯ�� 7ĭ���� ����: [0]=ID, [1]=PW(Base64), [2..5]=��Ÿ, [6]=LastLogin
                if (parts.Length < 7)
                {
                    Array.Resize(ref parts, 7);
                }
                parts[6] = currentTime; // LastLogin ������Ʈ (������ ���� ä��)

                // 4) Ʈ���ϸ� �����ݷ� ���� ����ϰ� ����
                string updatedLine = string.Join(";", parts);
                newLines.Add(updatedLine);
            }
            else
            {
                // ��ġ �� �Ǹ� ���� ���� ����
                newLines.Add(line);
            }
        }

        if (found)
        {
            try
            {
                // 5) �� ���� ���� (�б� ���� ��)
                File.WriteAllLines(filePath, newLines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorMessage = "����� ������ ���� �� ������ �߻��ߴϴ�." + Environment.NewLine + $"{ex.Message}";
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
            errorMessage = "���̵� �Ǵ� ��й�ȣ�� �߸��Ǿ����ϴ�.";
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
