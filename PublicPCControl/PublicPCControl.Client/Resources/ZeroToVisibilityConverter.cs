// File: PublicPCControl.Client/Resources/ZeroToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PublicPCControl.Client.Resources
{
    /// <summary>
    /// 숫자 값이 0일 때 Visible, 그 외에는 Collapsed를 반환한다.
    /// null이나 숫자가 아닐 때도 안전하게 Collapsed를 반환한다.
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number)
            {
                return number == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    var asInt = convertible.ToInt32(culture);
                    return asInt == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch
                {
                    // 변환 실패 시에는 Collapsed를 반환한다.
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
