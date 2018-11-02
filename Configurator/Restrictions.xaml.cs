using AsyncReplicaOperations;
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
    /// Interaction logic for Restrictions.xaml
    /// </summary>
    public partial class Restrictions : Window
    {
        private List<Restriction> restrictions;
        private DirectionsEnum direction;
        public Restrictions(List<Restriction> restrictions,DirectionsEnum direction)
        {
            InitializeComponent();
            this.restrictions = restrictions;
            RestrictionBox.Text = String.Join(";", restrictions.Select<Restriction, string>(x => x.ReplicaId).ToArray());
            this.direction = direction;
        }

        public List<Restriction> RestrictionsList
        {
            get
            {
                return restrictions;
            }

            set
            {
                restrictions = value;
            }
        }

        private void RestrictionBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text[0]) && e.Text[0] != ';' )
            {
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            restrictions = new List<Restriction>();
            var splits = RestrictionBox.Text.Split(';');
            foreach(var s in splits)
            {
                if (s != string.Empty)
                {
                    var restrict = new Restriction();
                    restrict.ReplicaId = s;
                    if ((bool)checkBox.IsChecked)
                    {
                        restrict.WorkDirectory = workDirectory.Text + "\\" + restrict.ReplicaId + "\\" + ((direction == DirectionsEnum.Export) ? "Out" : "In");
                    }
                    restrictions.Add(restrict);
                }
            }
            this.Close();
        }
    }
}
