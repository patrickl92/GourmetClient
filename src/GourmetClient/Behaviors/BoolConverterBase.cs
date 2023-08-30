namespace GourmetClient.Behaviors
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public abstract class BoolConverterBase<T> : IValueConverter
    {
        protected BoolConverterBase(T trueValue, T falseValue)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        public T TrueValue { get; set; }

        public T FalseValue { get; set; }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return TrueValue;
            }

            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, TrueValue);
        }
    }
}
