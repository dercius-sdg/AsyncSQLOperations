using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AsyncReplicaOperations;

namespace AsyncReplicaTool
{
    class StatusImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(!(value is TaskRunningStatus))
            {
                return null;
            }
            TaskRunningStatus b = (TaskRunningStatus)value;
            var source = new BitmapImage();
            source.BeginInit();
            switch(b)
            {
                case TaskRunningStatus.Running:
                    {
                        source.UriSource = new Uri(@"/AsyncReplicaTool;component/Icons/hourglass.png", UriKind.Relative);
                        break;
                    }
                case TaskRunningStatus.Success:
                    {
                        source.UriSource = new Uri(@"/AsyncReplicaTool;component/Icons/checked.png", UriKind.Relative);
                        break;
                    }
                case TaskRunningStatus.Failure:
                    {
                        source.UriSource = new Uri(@"/AsyncReplicaTool;component/Icons/error.png", UriKind.Relative);
                        break;
                    }
                default:
                    break;

                    
            }
            source.EndInit();
            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
