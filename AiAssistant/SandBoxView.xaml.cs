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

namespace AiAssistant
{
    /// <summary>
    /// Interaction logic for SandBoxView.xaml
    /// </summary>
    public partial class SandBoxView : Window
    {
        public SandBoxView()
        {
            InitializeComponent();
        }
        public void SetCommand(string Command)
        {
            CommandBox.Text = Command;
        }

        public bool? Pass = null;

        private void Allow(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to execute this command?", "Msgbox", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Pass = true;
                this.Close();
            }
        }

        private void Deny(object sender, MouseButtonEventArgs e)
        {
            Pass = false;
            this.Close();
        }
    }
}
