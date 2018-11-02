using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AsyncReplicaOperations;

namespace Configurator
{
    /// <summary>
    /// Логика взаимодействия для SelectDirection.xaml
    /// </summary>
    public partial class SelectDirection : Window
    {
        public SelectDirection()
        {
            InitializeComponent();
            DirectionBox.ItemsSource = Enum.GetValues(typeof(DirectionsEnum)).Cast<DirectionsEnum>();
        }

        public DirectionsEnum Direction
        {
            get
            {
                return (DirectionsEnum)DirectionBox.SelectedItem;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
