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

namespace Configurator
{
    /// <summary>
    /// Interaction logic for SelectCountRestrictions.xaml
    /// </summary>
    public partial class SelectCountRestrictions : Window
    {
        private int count;

        public int Count
        {
            get
            {
                return count;
            }

            set
            {
                count = value;
            }
        }

        public SelectCountRestrictions(int count)
        {
            InitializeComponent();
            RestrictionCountBox.Text = count.ToString();
        }

        private void RestrictionCountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Count = Convert.ToInt32(RestrictionCountBox.Text);
            this.Close();
        }
    }
}
