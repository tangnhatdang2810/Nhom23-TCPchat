using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LMCB_TestForm
{
    public partial class Form1 : Form
    {

        private TcpClient tcpClient;
        private StreamWriter sWriter;
        private Thread clientThread;
        private bool stopTcpClient = true;
        private System.Windows.Forms.Label label;
        public Form1()
        {
            InitializeComponent();
            InitializeDynamicHeightTextBox();
        }
        private void ClientRecv()
        {
            StreamReader sr = new StreamReader(tcpClient.GetStream());
            try
            {
                while (!stopTcpClient && tcpClient.Connected)
                {
                    Application.DoEvents();
                    string data = sr.ReadLine();
                    UpdateChatHistoryThreadSafe($"{data}");
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
            if (label.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateChatHistoryThreadSafe);
                label.Invoke(d, new object[] {text});
            }
            else
            {
                InitializeDynamicHeightTextBox();
                var messagePost = JsonConvert.DeserializeObject<MessagePost>(text);
                string formattedMsg = $"[{DateTime.Now:MM/dd/yyyy h:mm tt}]\n {messagePost.Message}";
                label.Text = formattedMsg;
                var panel = new FlowLayoutPanel
                {
                    Width = this.flowLayoutPanel1.Width - 5,
                    Height = label.Height + 2,
                    FlowDirection = FlowDirection.LeftToRight,
                };
                if (messagePost.From_Username == textBox1.Text)
                {
                    panel.FlowDirection = FlowDirection.RightToLeft;
                }
                else
                {
                    panel.FlowDirection = FlowDirection.LeftToRight;
                }
                

                panel.Controls.Add(label);

                flowLayoutPanel1.Controls.Add(panel);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var messagePost = new MessagePost
                {
                    From_Username = textBox1.Text,
                    To_Username = textBox3.Text,
                };
                foreach (string line in sendMsgTextBox.Lines)
                {
                    messagePost.Message += line + "\n";
                }
                string messagePostStr = JsonConvert.SerializeObject(messagePost);
                sWriter.WriteLine($"{messagePostStr}");
                sendMsgTextBox.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Close(object sender, FormClosingEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                stopTcpClient = false;
                this.tcpClient = new TcpClient();
                this.tcpClient.Connect(new IPEndPoint(IPAddress.Parse(textBox2.Text), 9999));
                this.sWriter = new StreamWriter(tcpClient.GetStream());
                this.sWriter.AutoFlush = true;
                sWriter.WriteLine(this.textBox1.Text);
                clientThread = new Thread(this.ClientRecv);
                clientThread.Start();
                MessageBox.Show("Connected");
            }
            catch (SocketException sockEx)
            {
                MessageBox.Show(sockEx.Message, "Network error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void InitializeDynamicHeightTextBox()
        {
            label = new System.Windows.Forms.Label
            {
                AutoSize = true, // Cho phép label tự động điều chỉnh kích thước
                MaximumSize = new System.Drawing.Size(flowLayoutPanel1.Width*2/3, 0), // Chiều rộng tối đa là 200px, chiều cao không giới hạn
                Height = 30, // Chiều cao ban đầu của label
                BackColor = System.Drawing.Color.White,
            };
            label.TextChanged += TextBox_TextChanged; 
            this.Controls.Add(label);
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            //var numberOfLines = label.Lines.Length;
            //label.Height = numberOfLines * 30;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
