// File: PublicPCControl.Client/Resources/InverseBooleanConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PublicPCControl.Client.Resources
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolean = value is bool b && b;
            if (targetType == typeof(Visibility))
            {
                return boolean ? Visibility.Collapsed : Visibility.Visible;
            }
            return !boolean;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}