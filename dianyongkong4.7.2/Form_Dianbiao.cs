using ANCA_HMI;
using Modbus.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using dianyongkong4._7._2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ANCA_HMI.SystemInfo
{
    public partial class Form_Dianbiao : Form
    {
        private TcpClient client;
        private ModbusIpMaster master;
        public Thread myThread;
        bool Connected;
        public bool test=false;
        System.Timers.Timer timer_SendModbus;
        public event EventHandler TextBox1Changed;
        public event EventHandler TextBox2Changed;
        public double a;
        public double b;
        
        public Form_Dianbiao()
        {
            InitializeComponent();
            // 订阅TextBox1的TextChanged事件
            textBox1_pic.TextChanged += TextBox1_TextChanged;
            // 订阅TextBox2的TextChanged事件
            textBox2_met.TextChanged += TextBox2_TextChanged;

            try
            {
               
                client = new TcpClient();
                client.Connect(IPAddress.Parse(Program.ModbusDianBiaoIP.Trim()), Convert.ToInt32(Program.ModbusDianBiaoPort));
                master = ModbusIpMaster.CreateIp(client);
                Connected = true;
                timer_SendModbus = new System.Timers.Timer(100); ;
                timer_SendModbus.Elapsed += new System.Timers.ElapsedEventHandler(timer_SendModbus_Elapsed);
                timer_SendModbus.AutoReset = false;
                timer_SendModbus.Start();
            }
            catch (SocketException e)
            {
                MessageBox.Show("连接服务器失败  " + e.Message);
            }

        }

        private void timer_SendModbus_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_SendModbus.Stop();
            if (Connected)
            {
                
                ushort[] holding_register = master.ReadHoldingRegisters(0,2,1);
                double PowerUsed = holding_register[0];

                holding_register = master.ReadHoldingRegisters(0,6,1);
                double jianpower = holding_register[0];
                


                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        textBox1_pic.Text = (PowerUsed).ToString("0.000") + "kwh";
                        a = PowerUsed;
                        textBox2_met.Text = (jianpower).ToString("0.000") + "kwh";
                        b = jianpower;
                    }));
                }
                else
                {
                    textBox1_pic.Text = (PowerUsed).ToString("0.000") + "kwh";
                    textBox2_met.Text = (jianpower).ToString("0.000") + "kwh";
                                      
                }
            }
            Thread.Sleep(100);
            timer_SendModbus.Start();
        }

        private void btn_Modbus_quit_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int a;
            if (test)
            {
                a = 1;
            }
            else
            {
                a = 2;            
            }
            ushort b = (ushort)a;
            master.WriteSingleRegister(0,4,b);
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            test = true;
            Form1.Between();
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            test = false;
        }

        //------------------------------------------------------------
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            // 触发TextBox1Changed事件
            OnTextBox1Changed(EventArgs.Empty);
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            // 触发TextBox2Changed事件
            OnTextBox2Changed(EventArgs.Empty);
        }

        // 触发TextBox1Changed事件的方法
        protected virtual void OnTextBox1Changed(EventArgs e)
        {
            TextBox1Changed?.Invoke(this, e);
        }

        // 触发TextBox2Changed事件的方法
        protected virtual void OnTextBox2Changed(EventArgs e)
        {
            TextBox2Changed?.Invoke(this, e);
        }

        
    }
}
