using Enigma_Emulator;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Tools;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.TCP;
using System.Runtime.InteropServices;
using System.Collections;
using System.Timers;

namespace Engima_Machine_Emulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        string message;
        static Dictionary<char, string> translator;
        static Dictionary<string, char> invrsetranslator;
        int j = 1;
        int i = 1;
        int k = 1;
        int plugno = 0;
        EnigmaMachine machine = new EnigmaMachine();
        EnigmaSettings eSettings = new EnigmaSettings();
        static Dictionary<string, string> rotortranslate;
        // The port number for the remote device.  
        private const int port = 44;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;
        public class ClientStateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
        }
public static ManualResetEvent allDone = new ManualResetEvent(false);
        public MainWindow()
        {
            InitializeComponent();
            InitialiseDictionary2();
            InitialiseDictionary();
            InitialiseRotorDictionary();
            WindowState = WindowState.Maximized;
            StartListening();
        }
        public class ServerStateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }
        
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                ServerStateObject state = (ServerStateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public class enigmamachineinfo
        {
            public string morse;
            public string rotorinitial;
            public string rotor;
            public string order;
            public string ring;
        }
        public void StartListening()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            byte[] bytes = new Byte[1024];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(localIP), 43);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error" + ex);
            }
            Thread.Sleep(1000);
        }

        public static void AcceptCallback(IAsyncResult AR)
        {
            allDone.Set();


            Socket listener = (Socket)AR.AsyncState;
            Socket handler = listener.EndAccept(AR);
            ClientStateObject state = new ClientStateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);

        }
        static enigmamachineinfo x = new enigmamachineinfo();
        public static void ReadCallback(IAsyncResult AR)
        {
            String content = String.Empty;

            ClientStateObject state = (ClientStateObject)AR.AsyncState;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(AR);

            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                content = state.sb.ToString();

                if (content.IndexOf("<EOF>") > -1)
                {
                    string[] input = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    x.morse = input[0];
                    x.rotorinitial = input[1];
                    x.rotor = input[2];
                    x.order = input[3];
                    x.ring = input[4];
                }
                else
                {
                    handler.BeginReceive(state.buffer, 0, ClientStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }
        private void btn_press_y(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "y";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "y";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_x(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "x";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "x";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_z(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "z";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "z";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }

        }

        private void btn_press_a(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "a";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "a";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_q(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "q";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "q";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_w(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "w";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "w";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_s(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "s";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "s";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_c(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "c";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "c";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_d(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "d";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "d";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_e(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "e";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "e";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_r(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "r";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "r";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_f(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "f";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "f";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_v(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "v";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "v";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_b(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "b";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "b";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_g(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "g";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "g";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_t(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "t";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "t";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_h(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "h";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "h";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_n(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "n";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "n";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_u(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "u";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "u";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_j(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "j";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "j";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_m(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "m";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "m";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_k(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "k";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "k";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_i(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "i";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "i";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_o(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "o";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "o";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_l(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "l";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "l";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_press_p(object sender, RoutedEventArgs e)
        {
            if (plugno % 2 == 0)
            {
                txt_message.Text = message + "p";
                message = txt_message.Text;
                btn_rtr3_up(sender, e);
                string letter = "p";
                encrytfun(letter);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Please select two plugs");
            }
        }

        private void btn_rtr1_up(object sender, RoutedEventArgs e)
        {
            j++;
            if (j == 27)
            {
                j = 1;
            }
            switch (j)
            {
                case 1:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "A";
                    break;
                case 2:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "B";
                    break;
                case 3:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "C";
                    break;
                case 4:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "D";
                    break;
                case 5:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "E";
                    break;
                case 6:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "F";
                    break;
                case 7:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "G";
                    break;
                case 8:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "H";
                    break;
                case 9:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "I";
                    break;
                case 10:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "J";
                    break;
                case 11:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "K";
                    break;
                case 12:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "L";
                    break;
                case 13:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "M";
                    break;
                case 14:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "N";
                    break;
                case 15:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "O";
                    break;
                case 16:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "P";
                    break;
                case 17:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Q";
                    break;
                case 18:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "R";
                    break;
                case 19:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "S";
                    break;
                case 20:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "T";
                    break;
                case 21:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "U";
                    break;
                case 22:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "V";
                    break;
                case 23:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "W";
                    break;
                case 24:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "X";
                    break;
                case 25:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Y";
                    break;
                case 26:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Z";
                    break;
            }
        }

        private void btn_rtr1_down(object sender, RoutedEventArgs e)
        {
            j--;
            if (j == 0)
            {
                j = 26;
            }
            switch (j)
            {
                case 1:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "A";
                    break;
                case 2:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "B";
                    break;
                case 3:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "C";
                    break;
                case 4:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "D";
                    break;
                case 5:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "E";
                    break;
                case 6:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "F";
                    break;
                case 7:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "G";
                    break;
                case 8:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "H";
                    break;
                case 9:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "I";
                    break;
                case 10:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "J";
                    break;
                case 11:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "K";
                    break;
                case 12:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "L";
                    break;
                case 13:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "M";
                    break;
                case 14:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "N";
                    break;
                case 15:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "O";
                    break;
                case 16:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "P";
                    break;
                case 17:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Q";
                    break;
                case 18:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "R";
                    break;
                case 19:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "S";
                    break;
                case 20:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "T";
                    break;
                case 21:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "U";
                    break;
                case 22:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "V";
                    break;
                case 23:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "W";
                    break;
                case 24:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "X";
                    break;
                case 25:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Y";
                    break;
                case 26:
                    txt_rotor1.Clear();
                    txt_rotor1.Text = "Z";
                    break;
            }
        }

        private void btn_rtr2_up(object sender, RoutedEventArgs e)
        {
            k++;
            if (k == 27)
            {
                k = 1;
                btn_rtr1_up(sender, e);
            }
            switch (k)
            {
                case 1:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "A";
                    break;
                case 2:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "B";
                    break;
                case 3:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "C";
                    break;
                case 4:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "D";
                    break;
                case 5:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "E";
                    break;
                case 6:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "F";
                    break;
                case 7:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "G";
                    break;
                case 8:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "H";
                    break;
                case 9:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "I";
                    break;
                case 10:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "J";
                    break;
                case 11:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "K";
                    break;
                case 12:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "L";
                    break;
                case 13:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "M";
                    break;
                case 14:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "N";
                    break;
                case 15:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "O";
                    break;
                case 16:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "P";
                    break;
                case 17:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Q";
                    break;
                case 18:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "R";
                    break;
                case 19:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "S";
                    break;
                case 20:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "T";
                    break;
                case 21:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "U";
                    break;
                case 22:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "V";
                    break;
                case 23:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "W";
                    break;
                case 24:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "X";
                    break;
                case 25:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Y";
                    break;
                case 26:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Z";
                    break;
            }
        }

        private void btn_rtr2_down(object sender, RoutedEventArgs e)
        {
            k--;
            if (k == 0)
            {

                k = 26;
                if (j >= 1)
                {
                    btn_rtr1_down(sender, e);
                }
            }
            switch (k)
            {
                case 1:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "A";
                    break;
                case 2:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "B";
                    break;
                case 3:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "C";
                    break;
                case 4:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "D";
                    break;
                case 5:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "E";
                    break;
                case 6:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "F";
                    break;
                case 7:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "G";
                    break;
                case 8:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "H";
                    break;
                case 9:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "I";
                    break;
                case 10:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "J";
                    break;
                case 11:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "K";
                    break;
                case 12:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "L";
                    break;
                case 13:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "M";
                    break;
                case 14:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "N";
                    break;
                case 15:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "O";
                    break;
                case 16:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "P";
                    break;
                case 17:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Q";
                    break;
                case 18:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "R";
                    break;
                case 19:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "S";
                    break;
                case 20:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "T";
                    break;
                case 21:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "U";
                    break;
                case 22:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "V";
                    break;
                case 23:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "W";
                    break;
                case 24:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "X";
                    break;
                case 25:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Y";
                    break;
                case 26:
                    txt_rotor2.Clear();
                    txt_rotor2.Text = "Z";
                    break;
            }
        }

        private void btn_rtr3_up(object sender, RoutedEventArgs e)
        {
            i++;
            if (i == 27)
            {
                btn_rtr2_up(sender, e);
                i = 1;
            }
            switch (i)
            {
                case 1:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "A";
                    break;
                case 2:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "B";
                    break;
                case 3:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "C";
                    break;
                case 4:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "D";
                    break;
                case 5:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "E";
                    break;
                case 6:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "F";
                    break;
                case 7:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "G";
                    break;
                case 8:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "H";
                    break;
                case 9:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "I";
                    break;
                case 10:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "J";
                    break;
                case 11:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "K";
                    break;
                case 12:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "L";
                    break;
                case 13:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "M";
                    break;
                case 14:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "N";
                    break;
                case 15:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "O";
                    break;
                case 16:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "P";
                    break;
                case 17:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Q";
                    break;
                case 18:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "R";
                    break;
                case 19:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "S";
                    break;
                case 20:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "T";
                    break;
                case 21:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "U";
                    break;
                case 22:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "V";
                    break;
                case 23:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "W";
                    break;
                case 24:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "X";
                    break;
                case 25:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Y";
                    break;
                case 26:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Z";
                    break;
            }

        }
        private void btn_rtr3_down(object sender, RoutedEventArgs e)
        {
            i--;
            if (i == 0)
            {
                i = 26;
                if (k >= 1)
                {
                    btn_rtr2_down(sender, e);
                }
            }
            switch (i)
            {
                case 1:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "A";
                    break;
                case 2:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "B";
                    break;
                case 3:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "C";
                    break;
                case 4:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "D";
                    break;
                case 5:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "E";
                    break;
                case 6:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "F";
                    break;
                case 7:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "G";
                    break;
                case 8:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "H";
                    break;
                case 9:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "I";
                    break;
                case 10:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "J";
                    break;
                case 11:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "K";
                    break;
                case 12:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "L";
                    break;
                case 13:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "M";
                    break;
                case 14:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "N";
                    break;
                case 15:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "O";
                    break;
                case 16:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "P";
                    break;
                case 17:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Q";
                    break;
                case 18:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "R";
                    break;
                case 19:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "S";
                    break;
                case 20:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "T";
                    break;
                case 21:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "U";
                    break;
                case 22:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "V";
                    break;
                case 23:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "W";
                    break;
                case 24:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "X";
                    break;
                case 25:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Y";
                    break;
                case 26:
                    txt_rotor3.Clear();
                    txt_rotor3.Text = "Z";
                    break;
            }
        }
        private static string translate(string input)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char character in input)
            {
                if (translator.ContainsKey(character))
                {
                    sb.Append(translator[character] + " ");
                }
                else if (character == ' ')
                {
                    sb.Append("/ ");
                }
                else
                {
                    sb.Append(character + " ");
                }
            }
            return sb.ToString();
        }
        public string DecodeMorse(string morse)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (x.morse == null)
            {
                MessageBox.Show("No New Messages");
            }
            else
            {
                string[] words = x.morse.Split(' ');
                foreach (string a in words)
                {
                    if (invrsetranslator.ContainsKey(a))
                    {
                        sb.Append(invrsetranslator[a] + " ");
                    }
                    else if (a == " ")
                    {
                        sb.Append("/ ");
                    }
                    else
                    {
                        sb.Append(a + " ");
                    }
                }
            }
                return sb.ToString();
        }
        private static void InitialiseDictionary()
        {
            translator = new Dictionary<char, string>()
            {
                {'a', ".-"},
                {'b', "-..."},
                {'c', "-.-."},
                {'d', "-.."},
                {'e', "."},
                {'f', "..-."},
                {'g', "--."},
                {'h', "...."},
                {'i', ".."},
                {'j', ".---"},
                {'k', "-.-"},
                {'l', ".-.."},
                {'m', "--"},
                {'n', "-."},
                {'o', "---"},
                {'p', ".--."},
                {'q', "--.-"},
                {'r', ".-."},
                {'s', "..."},
                {'t', "-"},
                {'u', "..-"},
                {'v', "...-"},
                {'w', ".--"},
                {'x', "-..-"},
                {'y', "-.--"},
                {'z', "--.."},
                {'0', "-----"},
                {'1', ".----"},
                {'2', "..---"},
                {'3', "...--"},
                {'4', "....-"},
                {'5', "....."},
                {'6', "-...."},
                {'7', "--..."},
                {'8', "---.."},
                {'9', "----."}
            };
        }
        private static void InitialiseDictionary2()
        {
            invrsetranslator = new Dictionary<string,char>()
            {
                {".-",'a'},
                {"-...",'b'},
                {"-.-.",'c'},
                {"-..",'d'},
                {".",'e'},
                {"..-.",'f'},
                {"--.",'g'},
                {"....",'h'},
                {"..",'i'},
                {".---",'j'},
                {"-.-",'k'},
                {".-..",'l'},
                {"--",'m'},
                {"-.",'n'},
                {"---",'o'},
                {".--.",'p'},
                {"--.-",'q'},
                {".-.",'r'},
                {"...",'s'},
                {"-",'t'},
                {"..-",'u'},
                {"...-",'v'},
                {".--",'w'},
                {"-..-",'x'},
                {"-.--",'y'},
                {"--..",'z'},
            };
        }
        private static string Rtrtranslate(string input)
        {
            string vb;
            if (rotortranslate.ContainsKey(input))
            {
                vb = rotortranslate[input];
                return vb;
            }
            else
            {

            }
            return null;
        }
        private static void InitialiseRotorDictionary()
        {
            rotortranslate = new Dictionary<string, string>()
            {
                {"1", "A"},
                {"2", "B"},
                {"3", "C"},
                {"4", "D"},
                {"5", "E"},
                {"6", "F"},
                {"7", "G"},
                {"8", "H"},
                {"9", "I"},
                {"10", "J"},
                {"11", "K"},
                {"12", "L"},
                {"13", "M"},
                {"14", "N"},
                {"15", "O"},
                {"16", "P"},
                {"17", "Q"},
                {"18", "R"},
                {"19", "S"},
                {"20", "T"},
                {"21", "U"},
                {"22", "V"},
                {"23", "W"},
                {"24", "X"},
                {"25", "Y"},
                {"26", "Z"}
            };
        }

        private class EnigmaSettings
        {
            public char[] rings { get; set; }
            public char[] grund { get; set; }
            public string order { get; set; }
            public char reflector { get; set; }
            public List<string> plugs = new List<string>();
        }
        public void btn_send_Click(object sender, RoutedEventArgs e)
        {
            machine.setSettings(eSettings.rings, eSettings.grund, eSettings.order, eSettings.reflector);
        }
        private string encrytfun(string input)
        {
            string enc = machine.runEnigma(input);
            string morse = translate(enc.ToLower());
            txt_morse.Text += morse;
            txt_Encryption.Text += enc;
            encryptlight(enc);
            return null;
        }

        private void clearlights()
        {
                    btn_lighta.Background = Brushes.Gray;
                    btn_lightb.Background = Brushes.Gray;
                    btn_lightc.Background = Brushes.Gray;
                    btn_lightd.Background = Brushes.Gray;
                    btn_lighte.Background = Brushes.Gray;
                    btn_lightf.Background = Brushes.Gray;
                    btn_lightg.Background = Brushes.Gray;
                    btn_lighth.Background = Brushes.Gray;
                    btn_lighti.Background = Brushes.Gray;
                    btn_lightj.Background = Brushes.Gray;
                    btn_lightk.Background = Brushes.Gray;
                    btn_lightl.Background = Brushes.Gray;
                    btn_lightm.Background = Brushes.Gray;
                    btn_lightn.Background = Brushes.Gray;
                    btn_lighto.Background = Brushes.Gray;
                    btn_lightp.Background = Brushes.Gray;
                    btn_lightq.Background = Brushes.Gray;
                    btn_lightr.Background = Brushes.Gray;
                    btn_lights.Background = Brushes.Gray;
                    btn_lightt.Background = Brushes.Gray;
                    btn_lightu.Background = Brushes.Gray;
                    btn_lightv.Background = Brushes.Gray;
                    btn_lightw.Background = Brushes.Gray;
                    btn_lightx.Background = Brushes.Gray;
                    btn_lighty.Background = Brushes.Gray;
                    btn_lightz.Background = Brushes.Gray;
        }
        private void encryptlight(string lightboard)
        {
            switch(lightboard)
            {
                case "A":
                    clearlights();
                    btn_lighta.Background = Brushes.LightYellow;
                    break;
                case "B":
                    clearlights();
                    btn_lightb.Background = Brushes.LightYellow;
                    break;
                case "C":
                    clearlights();
                    btn_lightc.Background = Brushes.LightYellow;
                    break;
                case "D":
                    clearlights();
                    btn_lightd.Background = Brushes.LightYellow;
                    break;
                case "E":
                    clearlights();
                    btn_lighte.Background = Brushes.LightYellow;
                    break;
                case "F":
                    clearlights();
                    btn_lightf.Background = Brushes.LightYellow;
                    break;
                case "G":
                    clearlights();
                    btn_lightg.Background = Brushes.LightYellow;
                    break;
                case "H":
                    clearlights();
                    btn_lighth.Background = Brushes.LightYellow;
                    break;
                case "I":
                    clearlights();
                    btn_lighti.Background = Brushes.LightYellow;
                    break;
                case "J":
                    clearlights();
                    btn_lightj.Background = Brushes.LightYellow;
                    break;
                case "K":
                    clearlights();
                    btn_lightk.Background = Brushes.LightYellow;
                    break;
                case "L":
                    clearlights();
                    btn_lightl.Background = Brushes.LightYellow;
                    break;
                case "M":
                    clearlights();
                    btn_lightm.Background = Brushes.LightYellow;
                    break;
                case "N":
                    clearlights();
                    btn_lightn.Background = Brushes.LightYellow;
                    break;
                case "O":
                    clearlights();
                    btn_lighto.Background = Brushes.LightYellow;
                    break;
                case "P":
                    clearlights();
                    btn_lightp.Background = Brushes.LightYellow;
                    break;
                case "Q":
                    clearlights();
                    btn_lightq.Background = Brushes.LightYellow;
                    break;
                case "R":
                    clearlights();
                    btn_lightr.Background = Brushes.LightYellow;
                    break;
                case "S":
                    clearlights();
                    btn_lights.Background = Brushes.LightYellow;
                    break;
                case "T":
                    clearlights();
                    btn_lightt.Background = Brushes.LightYellow;
                    break;
                case "U":
                    clearlights();
                    btn_lightu.Background = Brushes.LightYellow;
                    break;
                case "V":
                    clearlights();
                    btn_lightv.Background = Brushes.LightYellow;
                    break;
                case "W":
                    clearlights();
                    btn_lightw.Background = Brushes.LightYellow;
                    break;
                case "X":
                    clearlights();
                    btn_lightx.Background = Brushes.LightYellow;
                    break;
                case "Y":
                    clearlights();
                    btn_lighty.Background = Brushes.LightYellow;
                    break;
                case "Z":
                    clearlights();
                    btn_lightz.Background = Brushes.LightYellow;
                    break;
            }
        }
        private void txt_message_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txt_message.Text.Length == 1)
            {
                    int p = i;
                    string rotor1 = p.ToString();
                    string rotor2 = j.ToString();
                    string rotor3 = k.ToString();
                    string rtr1 = Rtrtranslate(rotor1);
                    string rtr2 = Rtrtranslate(rotor2);
                    string rtr3 = Rtrtranslate(rotor3);
                    string rotororder3 = ddb_rotor3.Text.ToString();
                    string rotororder2 = ddb_rotor2.Text.ToString();
                    string rotororder1 = ddb_rotor1.Text.ToString();
                    string rotororder = rotororder1 + "-" + rotororder2 + "-" + rotororder3;
                    char[] reflector = cmb_reflector.Text.ToCharArray();
                    string rotor = rtr1 + rtr3 + rtr2;
                    char[] finalrotor = rotor.ToCharArray();
                    btn_rotor2_down.Visibility = Visibility.Hidden;
                    btn_rotor3_down.Visibility = Visibility.Hidden;
                    btn_rotor_down.Visibility = Visibility.Hidden;
                    btn_rotor2_up.Visibility = Visibility.Hidden;
                    btn_rotor_up.Visibility = Visibility.Hidden;
                    btn_rotor3_up.Visibility = Visibility.Hidden;
                    rdo_Pluga.IsEnabled = false;
                rdo_Plugb.IsEnabled = false;
                rdo_PlugC.IsEnabled = false;
                rdo_PlugD.IsEnabled = false;
                rdo_PlugE.IsEnabled = false;
                rdo_PlugF.IsEnabled = false;
                rdo_PlugG.IsEnabled = false;
                rdo_PlugH.IsEnabled = false;
                rdo_PlugI.IsEnabled = false;
                rdo_PlugJ.IsEnabled = false;
                rdo_PlugK.IsEnabled = false;
                rdo_PlugL.IsEnabled = false;
                rdo_PlugM.IsEnabled = false;
                rdo_PlugN.IsEnabled = false;
                rdo_PlugO.IsEnabled = false;
                rdo_PlugP.IsEnabled = false;
                rdo_PlugQ.IsEnabled = false;
                rdo_PlugR.IsEnabled = false;
                rdo_PlugS.IsEnabled = false;
                rdo_PlugT.IsEnabled = false;
                rdo_PlugU.IsEnabled = false;
                rdo_PlugV.IsEnabled = false;
                rdo_PlugW.IsEnabled = false;
                rdo_PlugX.IsEnabled = false;
                rdo_PlugY.IsEnabled = false;
                rdo_PlugZ.IsEnabled = false;
                btn_rotor2_up.IsEnabled = false;
                    btn_rotor_up.IsEnabled = false;
                    btn_rotor3_up.IsEnabled = false;
                    btn_rotor2_down.IsEnabled = false;
                    btn_rotor_down.IsEnabled = false;
                    btn_rotor3_down.IsEnabled = false;
                    ddb_rotor1.IsEnabled = false;
                    ddb_rotor2.IsEnabled = false;
                    ddb_rotor3.IsEnabled = false;
                    cmb_reflector.IsEnabled = false;
                    eSettings.reflector = reflector[4];
                    eSettings.order = rotororder;
                    eSettings.rings = finalrotor;
                    eSettings.grund = finalrotor;
                    machine.setSettings(eSettings.rings, eSettings.grund, eSettings.order, eSettings.reflector);
                    string plugb = txt_plugboard.Text;
                    List<string> plugboard = plugb.Split(new char[] { ' ' }).ToList();
                    eSettings.plugs = plugboard;
                foreach (string plug in eSettings.plugs)
                {
                    if (plug == "None" || plug == "")
                    {

                    }
                    else
                    {
                        char[] pl = plug.ToCharArray();
                        machine.addPlug(pl[0], pl[1]);
                    }
                }
            }
            }
        private void btn_lighta_Click(object sender, RoutedEventArgs e)
        {

        }
       
        private char[] plugboard(char plugs)
        {
            if (plugno % 2 == 0)
            {
                plugno++;
                string secondplug = plugs.ToString();
                txt_plugboard.Text += secondplug;
                return null;
            }
            else
            {
                plugno++;
                string firstplug = plugs.ToString();
                txt_plugboard.Text += firstplug + " ";
                return null;
            }
        }
        private void rdo_Pluga_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('a');
        }

        private void rdo_Plugb_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('b');
        }

        private void rdo_PlugC_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('c');
        }

        private void rdo_PlugD_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('d');
        }

        private void rdo_PlugE_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('e');
        }

        private void rdo_PlugF_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('f');
        }

        private void rdo_PlugG_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('g');
        }

        private void rdo_PlugH_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('h');
        }

        private void rdo_PlugI_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('i');
        }

        private void rdo_PlugJ_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('j');
        }

        private void rdo_PlugK_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('k');
        }

        private void rdo_PlugL_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('l');
        }

        private void rdo_PlugM_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('m');
        }

        private void rdo_PlugN_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('n');
        }

        private void rdo_PlugO_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('o');
        }

        private void rdo_PlugP_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('p');
        }

        private void rdo_PlugQ_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('q');
        }

        private void rdo_PlugR_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('r');
        }

        private void rdo_PlugS_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('s');
        }

        private void rdo_PlugT_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('t');
        }

        private void rdo_PlugU_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('u');
        }

        private void rdo_PlugV_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('v');
        }

        private void rdo_PlugW_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('w');
        }

        private void rdo_PlugX_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('x');
        }

        private void rdo_PlugY_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('y');
        }

        private void rdo_PlugZ_Checked(object sender, RoutedEventArgs e)
        {
            plugboard('z');
        }

        private void Menu_Connection_Click_1(object sender, RoutedEventArgs e)
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            MessageBox.Show("Your IP address is " + addr[3] + " Put this in the other client");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (x.morse == null)
            {
                MessageBox.Show("No New Message");
            }
            else
            {
                txt_morse_Copy.Text = x.morse;
                string decodemorse = DecodeMorse(x.morse);
                txt_Encryption_Copy.Text = decodemorse.Replace(" ", "");
                eSettings.order = x.order;
                char[] ring = x.rotor.ToCharArray();
                char[] rotor = x.rotorinitial.ToCharArray();
                char[] reflectorarray = x.ring.ToCharArray();
                char reflector = reflectorarray[0];
                eSettings.rings = ring;
                eSettings.grund = rotor;
                eSettings.reflector = reflector;
                machine.setSettings(eSettings.rings, eSettings.grund, eSettings.order, eSettings.reflector);
                string dec = machine.runEnigma(txt_Encryption_Copy.Text);
                txt_decryption_Copy.Text = dec;
            }
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            i = 1;
            j = 1;
            k = 1;
            message = "";
            txt_message.Text = "";
            txt_morse.Text = "";
            txt_Encryption.Text = "";
            txt_plugboard.Text = "";
            btn_rotor2_down.Visibility = Visibility.Visible;
            btn_rotor3_down.Visibility = Visibility.Visible;
            btn_rotor_down.Visibility = Visibility.Visible;
            btn_rotor2_up.Visibility = Visibility.Visible;
            btn_rotor_up.Visibility = Visibility.Visible;
            btn_rotor3_up.Visibility = Visibility.Visible;
            rdo_Pluga.IsEnabled = true;
            rdo_Plugb.IsEnabled = true;
            rdo_PlugC.IsEnabled = true;
            rdo_PlugD.IsEnabled = true;
            rdo_PlugE.IsEnabled = true;
            rdo_PlugF.IsEnabled = true;
            rdo_PlugG.IsEnabled = true;
            rdo_PlugH.IsEnabled = true;
            rdo_PlugI.IsEnabled = true;
            rdo_PlugJ.IsEnabled = true;
            rdo_PlugK.IsEnabled = true;
            rdo_PlugL.IsEnabled = true;
            rdo_PlugM.IsEnabled = true;
            rdo_PlugN.IsEnabled = true;
            rdo_PlugO.IsEnabled = true;
            rdo_PlugP.IsEnabled = true;
            rdo_PlugQ.IsEnabled = true;
            rdo_PlugR.IsEnabled = true;
            rdo_PlugS.IsEnabled = true;
            rdo_PlugT.IsEnabled = true;
            rdo_PlugU.IsEnabled = true;
            rdo_PlugV.IsEnabled = true;
            rdo_PlugW.IsEnabled = true;
            rdo_PlugX.IsEnabled = true;
            rdo_PlugY.IsEnabled = true;
            rdo_PlugZ.IsEnabled = true;
            rdo_Pluga.IsChecked = false;
            rdo_Plugb.IsChecked = false;
            rdo_PlugC.IsChecked = false;
            rdo_PlugD.IsChecked = false;
            rdo_PlugE.IsChecked = false;
            rdo_PlugF.IsChecked = false;
            rdo_PlugG.IsChecked = false;
            rdo_PlugH.IsChecked = false;
            rdo_PlugI.IsChecked = false;
            rdo_PlugJ.IsChecked = false;
            rdo_PlugK.IsChecked = false;
            rdo_PlugL.IsChecked = false;
            rdo_PlugM.IsChecked = false;
            rdo_PlugN.IsChecked = false;
            rdo_PlugO.IsChecked = false;
            rdo_PlugP.IsChecked = false;
            rdo_PlugQ.IsChecked = false;
            rdo_PlugR.IsChecked = false;
            rdo_PlugS.IsChecked = false;
            rdo_PlugT.IsChecked = false;
            rdo_PlugU.IsChecked = false;
            rdo_PlugV.IsChecked = false;
            rdo_PlugW.IsChecked = false;
            rdo_PlugX.IsChecked = false;
            rdo_PlugY.IsChecked = false;
            rdo_PlugZ.IsChecked = false;
            btn_rotor2_up.IsEnabled = true;
            btn_rotor_up.IsEnabled = true;
            btn_rotor3_up.IsEnabled = true;
            btn_rotor2_down.IsEnabled = true;
            btn_rotor_down.IsEnabled = true;
            btn_rotor3_down.IsEnabled = true;
            ddb_rotor1.IsEnabled = true;
            ddb_rotor2.IsEnabled = true;
            ddb_rotor3.IsEnabled = true;
            cmb_reflector.IsEnabled = true;
            clearlights();
            txt_rotor3.Text = "A";
            txt_rotor2.Text = "A";
            txt_rotor1.Text = "A";
 
        }

        private void Menu_help_Click(object sender, RoutedEventArgs e)
        {
            UserGuide ug = new UserGuide();
            ug.Show();
        }
    }
    }
