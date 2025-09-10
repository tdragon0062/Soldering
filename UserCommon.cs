using Microsoft.UI;             // Colors
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;            // XamlRoot
using Microsoft.UI.Xaml.Controls;   // ContentDialog
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;  // SolidColorBrush

using System;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;


namespace Soldering_Mgmt;

public static class UserCheckMSG
{
    public static async Task ShowErrorMSG(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            XamlRoot = xamlRoot
        };

        var okButton = new Button
        {
            Content = "확인",
            Width = 100,
            Background = new SolidColorBrush(Colors.LightGray),  
            Foreground = new SolidColorBrush(Colors.Black),
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 20, 0, 0),          
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        okButton.Click += (_, __) => dialog.Hide();

        dialog.Content = new StackPanel
        {
            Children =
            {
                new TextBlock{ Text=message, Margin=new Thickness(0,0,0,10)},
                okButton
            }
        };

        await dialog.ShowAsync();
    }   
    public static async Task<bool> ShowConfirmMSG(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "확인",   // 확인 버튼
            CloseButtonText = "취소",     // 취소 버튼
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync().AsTask(); // await 정상 동작

        return result == ContentDialogResult.Primary;
        // true  → 확인 눌림
        // false → 취소 눌림
    }
}
public static class UserBtnColorSet
{
    public static void ButtonColorSet(Button btn, string currTagColor, string currBGColor, string currFGColor, string toBGColor, string toFGColor)
    {
        if (btn.Tag.ToString() == currBGColor )
        {
            // 실내등 켜는 Soldering API 삽입 필요
            // --------------------------------------
            // 배경색을 파란색으로 변경
            btn.Background = new SolidColorBrush(GetColorFromName(toBGColor));
            btn.Foreground = new SolidColorBrush(GetColorFromName(toFGColor));
            btn.Tag = toBGColor;
        }
        else
        {
            // 실내등 off Soldering API 삽입 필요
            //----------------------------------------
            // 배경색을 원래 색상으로 변경
            btn.Background = new SolidColorBrush(GetColorFromName(currBGColor));
            btn.Foreground = new SolidColorBrush(GetColorFromName(currFGColor));
            btn.Tag = currBGColor;
        }      
    }    
    public static Color GetColorFromName(string colorName)
    {
        var prop = typeof(Colors).GetProperty(colorName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (prop != null)
            return (Color)prop.GetValue(null)!;

        // 못 찾으면 기본 Black
        return Colors.Black;
    }


}
