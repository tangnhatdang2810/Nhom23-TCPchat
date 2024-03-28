using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerChatForm
{
    public partial class Form1 : Form
    {
        private Thread listenThread;
        private TcpListener tcpListener;
        private bool stopChatServer = true;
        private Dictionary<string,TcpClient> dict = new Dictionary<string,TcpClient>();
        public Form1()
        {
            InitializeComponent();
        }
        public void Listen()
        {
            try
            {
                tcpListener = new TcpListener(new IPEndPoint(IPAddress.Parse(textBox1.Text), 9999));
                tcpListener.Start();
                while (!stopChatServer)
                {
                    TcpClient _client = tcpListener.AcceptTcpClient();
                    StreamReader sr = new StreamReader(_client.GetStream());
                    StreamWriter sw = new StreamWriter(_client.GetStream());
                    sw.AutoFlush = true;
                    string username = sr.ReadLine();
                    if (string.IsNullOrEmpty(username))
                    {
                        sw.WriteLine("Please pick a username");
                        _client.Close();
                    }
                    else
                    {
                        if (!dict.ContainsKey(username))
                        {
                            Thread clientThread = new Thread(() => this.ClientRecv(username, _client));
                            dict.Add(username, _client);
                            clientThread.Start();
                        }
                        else
                        {
                            sw.WriteLine("Username already exist, pick another one");
                            _client.Close();
                        }
                    }

                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void ClientRecv(string username, TcpClient tcpClient)
        {
            StreamReader sr = new StreamReader(tcpClient.GetStream());
            try
            {
                while (!stopChatServer)
                {
                    System.Windows.Forms.Application.DoEvents();
                    string userNameAndMsg = sr.ReadLine();
                    var messagePost = JsonConvert.DeserializeObject<MessagePost>(userNameAndMsg);                   
                    TcpClient friendTcpClient;
                    if (dict.TryGetValue(messagePost.To_Username, out friendTcpClient))
                    {
                        StreamWriter sw = new StreamWriter(friendTcpClient.GetStream());
                        sw.WriteLine(userNameAndMsg);
                        sw.AutoFlush = true;
                    }
                    StreamWriter sw2 = new StreamWriter(tcpClient.GetStream());
                    sw2.WriteLine(userNameAndMsg);
                    sw2.AutoFlush = true;
                    UpdateChatHistoryThreadSafe(userNameAndMsg);
                }
            }
            catch (SocketException sockEx)
            {
                tcpClient.Close();
                sr.Close();

            }

        }
        private delegate void SafeCallDelegate(string text);

        private void UpdateChatHistoryThreadSafe(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateChatHistoryThreadSafe);
                richTextBox1.Invoke(d, new object[] { text });
            }
            else
            {
                var messagePost = JsonConvert.DeserializeObject<MessagePost>(text);
                string formattedMsg = $"[{DateTime.Now:MM/dd/yyyy h:mm tt}] {messagePost.From_Username}:\n {messagePost.Message}\n";
                richTextBox1.Text += formattedMsg;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (stopChatServer)
            {
                stopChatServer = false;
                listenThread = new Thread(this.Listen);
                listenThread.Start();
                MessageBox.Show(@"Start listening for incoming connections");
                button1.Text = @"Stop";
            }
            else
            {
                stopChatServer = true;
                button1.Text = @"Start listening";
                tcpListener.Stop();
                listenThread = null;
               
            }
        }
    }
}
