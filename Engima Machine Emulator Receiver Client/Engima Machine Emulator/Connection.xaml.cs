using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Forms;

namespace Engima_Machine_Emulator
{
    /// <summary>
    /// Interaction logic for Connection.xaml
    /// </summary>
    public partial class Connection : Window
    {
        public Connection()
        {
            InitializeComponent();
        }

        private void btn_connect_Copy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mform = new MainWindow();
            mform.ip = txt_IP.Text;
            mform.StartClient();
            if(mform.connected == false)
            {
                lbl_status2.Visibility = Visibility.Visible;
            }
            else
            {
                lbl_status.Visibility = Visibility.Visible;
            }

        }
    }
}
