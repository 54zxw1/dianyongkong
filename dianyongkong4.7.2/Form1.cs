using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.VisualBasic;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.ObjectModel;
using HgCommunicate;
using System.Xml;
using System.Text.RegularExpressions;
using System.Timers;
using ANCA_HMI.SystemInfo;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using System.Configuration;



namespace dianyongkong4._7._2
{
    public partial class Form1 : Form
    {
        public event EventHandler TimeToTakePicture;//拍照事件触发
        public event EventHandler ConfirmRecive;//接受PLC反馈信号
        public event EventHandler CarbonSteel;//碳钢管
        public event EventHandler Aluminum;//铝管
        private bool startsetcamparam = false;

        string cncOldStr = ""; //Cnc收到的旧消息
        string plcOldStr = "";//Plc收到的旧消息
        string cndOldLoadFinish = "";
        public const string CMD_PLC = "plc";  //PLC指令类型

        Thread sendCmdThread = null;    //客户端，发送命令
        Thread recieveCmdThread = null; //接收服务端信息
        Thread recPlcThread = null; //接收服务端信息

        private System.Windows.Forms.Timer timer;

        

        private TcpClient client;
        private ModbusIpMaster master;
        public Thread myThread;
        bool Connected;
        public bool test = false;
        System.Timers.Timer timer_SendModbus;

        public Form1()
        {
            InitializeComponent();
        }

       
        private void Form1_Load(object sender, EventArgs e)
        {
            IniAutoProcessDelegateEvent();
            CreatBasePath();
            InitializeButtons();
            DeviceListAcq();
            //bnOpen_Click(null, null);
            //bnStartGrab_Click(null, null);
            Control.CheckForIllegalCrossThreadCalls = false;


            tb_houghrho.Text = ConfigurationManager.AppSettings["HoughRho_Carboon"];
            tb_houghtheta.Text = ConfigurationManager.AppSettings["HoughTheta_Carboon"];
            tb_houghthreshold.Text = ConfigurationManager.AppSettings["HoughThreshold_Carboon"];
            tb_cannyparm.Text = ConfigurationManager.AppSettings["CannyParm_Carboon"];
            tb_cannyparm2.Text = ConfigurationManager.AppSettings["CannyParm2_Carboon"];
            tb_erzhihuaparm.Text = ConfigurationManager.AppSettings["ErZhiHuaParm_Carboon"];
            tb_line_mid.Text= ConfigurationManager.AppSettings["Line_mid"];


            timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000; // 10000毫秒 = 10秒
            timer.Tick += Timer_Tick; // 订阅Tick事件
            timer.Start(); // 启动Timer

            label7.Text = "等待结果";

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
            catch
            {
                MessageBox.Show("连接服务器失败  ");
                label7.Text = "连接服务器失败";

            }

            //int maxRetries = 2; // 最大尝试次数 
            //int retryCount = 0;
            //bool connected = false;
            //client = new TcpClient();
            //while (retryCount < maxRetries && !connected)
            //{

            //    try
            //    {

            //        client.Connect(IPAddress.Parse(Program.ModbusDianBiaoIP.Trim()), Convert.ToInt32(Program.ModbusDianBiaoPort));
            //        master = ModbusIpMaster.CreateIp(client);
            //        connected = true;

            //        timer_SendModbus = new System.Timers.Timer(100);
            //        timer_SendModbus.Elapsed += new System.Timers.ElapsedEventHandler(timer_SendModbus_Elapsed);
            //        timer_SendModbus.AutoReset = false;
            //        timer_SendModbus.Start();
            //        label7.Text = "连接服务器成功";
            //        break; // 连接成功，跳出循环
            //    }
            //    catch
            //    {
            //        retryCount++;
            //        Thread.Sleep(5000); // 每次尝试之间等待5秒
            //    }
            //}

            //if (!connected)
            //{

            //    this.Invoke(new Action(() => label7.Text = "连接服务器失败"));
            //    // 要启动的.exe文件的路径
            //    string exePath = @"D:\Debug2\modbus_startup.exe";

            //    // 创建进程启动信息
            //    ProcessStartInfo startInfo = new ProcessStartInfo(exePath);

            //    try
            //    {
            //        Process.Start(startInfo);
            //        Environment.Exit(0);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("启动.exe应用时出错: " + ex.Message);
            //        // 这里可以选择不退出程序，或者根据错误情况决定是否退出
            //        // Environment.Exit(1); // 如果需要立即退出，可以使用这行代码，但通常不建议在catch块中直接退出
            //    }
            //}

        }

        private void modbus_connect()
        {
            TcpClient client = null; // 将client声明在循环外部，但初始化为null
            ModbusIpMaster master = null; // 同样，将master声明在外部
            bool isConnected = false;
            int retryCount = 0;
            const int maxRetries = 1; // 最大重试次数
            const int retryDelay = 4000; // 每次重试之间的延迟（毫秒）
            System.Timers.Timer timer_SendModbus = null; // 将定时器也声明在外部

            while (!isConnected && retryCount < maxRetries)
            {
                try
                {
                    // 如果client不是null，说明之前有创建过实例，需要释放资源
                    if (client != null)
                    {
                        client.Close(); // 或者使用 client.Dispose();
                        client = null;

                        // 如果master和timer_SendModbus也被初始化了，同样需要释放或停止它们
                        if (master != null)
                        {
                            // 假设ModbusIpMaster也有某种释放资源的方法，这里可能需要根据实际API来调整
                            // 例如：master.Dispose();（如果ModbusIpMaster实现了IDisposable）
                            // 或者其他适当的清理逻辑
                            master = null;
                        }

                        if (timer_SendModbus != null)
                        {
                            timer_SendModbus.Stop();
                            timer_SendModbus.Dispose();
                            timer_SendModbus = null;
                        }
                    }

                    // 尝试建立新的连接
                    client = new TcpClient();
                    client.Connect(IPAddress.Parse(Program.ModbusDianBiaoIP.Trim()), Convert.ToInt32(Program.ModbusDianBiaoPort));
                    master = ModbusIpMaster.CreateIp(client); // 假设这个方法返回的是一个新的ModbusIpMaster实例
                    Connected = true; // 这个变量应该是一个类的成员变量，用于跟踪连接状态
                    isConnected = true; // 设置循环中的连接成功标志

                    // 设置定时器和其他初始化操作
                    timer_SendModbus = new System.Timers.Timer(100);
                    timer_SendModbus.Elapsed += new System.Timers.ElapsedEventHandler(timer_SendModbus_Elapsed);
                    timer_SendModbus.AutoReset = false;
                    timer_SendModbus.Start();
                }
                catch (Exception ex)
                {
                    // 记录异常信息，可能有助于调试
                    // Console.WriteLine(ex.Message);

                    MessageBox.Show("连接服务器失败，正在重试...");
                    retryCount++;
                    Thread.Sleep(retryDelay); // 等待一段时间后再重试 
                }
            }

            // 检查是否成功连接
            if (!isConnected)
            {
                MessageBox.Show("无法连接到服务器，请检查服务器状态。");
            }



        }



        private void timer_SendModbus_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_SendModbus.Stop();
            if (Connected)
            {

                ushort[] holding_register = master.ReadHoldingRegisters(0, 2, 1);
                double PowerUsed = holding_register[0];

                holding_register = master.ReadHoldingRegisters(0, 6, 1);
                double jianpower = holding_register[0];

                // 检查PowerUsed的值
                if (PowerUsed == 1.0)
                {
                    
                    if (is_take_picture==false)
                    {
                        is_take_picture = true;
                        take_pic_solve_resulit(null, null); // 当PowerUsed等于1时调用TakePic函数
                        label11.Text = "拍照";
                    }
                    
                }
                else 
                {
                    label11.Text = "等待拍照";
                
                }

                if (jianpower == 1)
                {
                    label10.Text = "执行碳钢黑参数";
                    is_carbon_steel = true;
                    is_aluminum = false;
                    is_carbon_steel_white = false;
                    Cam_Material();
                }
                else if (jianpower == 2)
                {
                    label10.Text = "执行铝管参数";
                    is_aluminum = true;
                    is_carbon_steel = false;
                    is_carbon_steel_white = false;
                    Cam_Material();
                }
                else if (jianpower == 3)
                {
                    label10.Text = "执行碳钢白参数";
                    is_aluminum = false;
                    is_carbon_steel = false;
                    is_carbon_steel_white = true;
                    Cam_Material();
                }
                else
                {
                    label10.Text = "执行输入参数";
                    is_carbon_steel = false;
                    is_aluminum = false;
                    is_carbon_steel_white = false;
                    Cam_Material();

                }

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        textBox1_pic.Text = (PowerUsed).ToString("0.000");
                        
                        textBox2_met.Text = (jianpower).ToString("0.000");
                        
                    }));
                }
                else
                {
                    textBox1_pic.Text = (PowerUsed).ToString("0.000");
                    textBox2_met.Text = (jianpower).ToString("0.000");

                }
            }
            Thread.Sleep(100);
            timer_SendModbus.Start();
        }

       
        private async Task SendCommandsWithDelayCorrected()
        {
            await Task.Delay(1500); // 等待服务器连接稳定

            // 发送命令，每个命令之间间隔一秒
            bsComObj.PushCommand("plc", $"<get2><var>{SysConstant.TAKE_PICTURE}</var><auto>yes</auto></get2>");
            await Task.Delay(1000);

            bsComObj.PushCommand("plc", $"<get2><var>{SysConstant.CONFIRM_RECIVE}</var><auto>yes</auto></get2>");
            await Task.Delay(1000);

            bsComObj.PushCommand("plc", $"<get2><var>{SysConstant.HAS_HOLE}</var><auto>yes</auto></get2>");
            await Task.Delay(1000);

            bsComObj.PushCommand("plc", $"<get2><var>{SysConstant.MATERIAL}</var><auto>yes</auto></get2>");
            
            // 最后一个命令后不需要再延迟
        }
        string pathname;//定义图片打开路径
        Mat Img;//用 Mat类定义原始图片
        Mat ImgCvt;//用 Mat类定义灰度图片
        Mat bianyuan;
        Mat huofu;
        Mat zhixian;
        Mat yanmo;
        Mat erzhihua;
        Mat lunkuo;
        Mat jieguo;
        private bool HasHole = false;
        private bool Errow_signal = false;
        private bool is_take_picture = false;
        private bool is_confirm_recive = false;
        private bool is_carbon_steel = false;
        private bool is_carbon_steel_white = false;
        private bool is_aluminum = false;
        private string basepath_PIC;
        private int pic_num = 0;

        Bitmap bitmap;//Bitmap类定义picturebox读取的图片
        private bool isEngineerMode = false;

        //plc通讯socket
        public static Form1 mw;
        public BaseCommunicate bsComObj = new BaseCommunicate();

        //camera
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private MyCamera m_MyCamera = new MyCamera();
        bool m_bGrabbing = false;
        Thread m_hReceiveThread = null;
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        // ch:用于从驱动获取图像的缓存 | en:Buffer for getting image from driver
        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver = IntPtr.Zero;
        private static Object BufForDriverLock = new Object();


        public static class SysConstant
        {
            public const string TAKE_PICTURE = ".TAKE_PICTURE";//开始拍照
            public const string HAS_HOLE = ".HAS_HOLE";//是否有孔
            public const string CONFIRM_RECIVE = ".CONFIRM_RECIVE";//确认收到

            public const string MATERIAL = ".MATERIAL";//

        }

        private void StartProcessServerInfo()
        {
            try
            {
                //recieveCmdThread = new Thread(new ThreadStart(ReceiveInfoThread));
                //recieveCmdThread.IsBackground = true;
                ////启动线程
                //recieveCmdThread.Start();

                recPlcThread = new Thread(new ThreadStart(RecPlcThread));
                recPlcThread.IsBackground = true;
                recPlcThread.Start();
                
                Console.WriteLine("PLC链接成功");
            }
            catch (Exception ex)
            {

                //LogHelper.WriteLog(DateTime.Now.ToString() + "接收服务器消息时错误:" + ex.Message);
                Console.WriteLine("PLC链接失败" + ex);
            }
        }

        //public void ReceiveInfoThread()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            Thread.Sleep(3);
        //            string cncStr = Form1.mw.bsComObj.Cnc_Socket.Msg;
        //            // string plcStr = BaseForm.mw.bsComObj.Plc_Socket.Msg;
        //            string[] subCncStrs = cncStr.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        //            //string[] suPlcStrs = plcStr.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        //            if (cncOldStr != cncStr)
        //            {
        //                LogHelper.WriteLog(DateTime.Now.ToString() + "中控接收CNC服务器消息:" + cncStr);
        //                cncOldStr = cncStr;
        //            }
        //            //if (plcOldStr != plcStr)
        //            //{
        //            //    LogHelper.WriteLog(DateTime.Now.ToString() + "主界面接收PLC服务器消息:" + plcStr);
        //            //    plcOldStr = plcStr;
        //            //}        
        //            foreach (string substr in subCncStrs)
        //            {
        //                //if (!string.IsNullOrEmpty(substr) && !Regex.IsMatch(substr, @"^[\u4e00-\u9fff]+$"))
        //                if (!string.IsNullOrEmpty(substr) && IsXml(substr))
        //                {
        //                    if (cndOldLoadFinish != substr)
        //                    {
        //                        cndOldLoadFinish = substr;
        //                        ExecuteOperByCncInfo(substr);
        //                    }
        //                    //UpdateListBoxItems("接收CNC服务器消息:" + cncStr);
        //                }
        //            }
        //            //foreach (string substr in suPlcStrs)
        //            //{
        //            //    //if (!string.IsNullOrEmpty(substr) && !Regex.IsMatch(substr, @"^[\u4e00-\u9fff]+$"))
        //            //    if (!string.IsNullOrEmpty(substr) && IsXml(substr))
        //            //    {
        //            //        ExecuteOperByPlcInfo(substr);
        //            //        //UpdateListBoxItems("接收CNC服务器消息:" + plcStr);
        //            //    }
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //        LogHelper.WriteLog(DateTime.Now.ToString() + "线程内接收服务器消息时错误:" + ex.Message);
        //    }
        //}

        public void RecPlcThread()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(3);
                    //string cncStr = BaseForm.mw.bsComObj.Cnc_Socket.Msg;
                    string plcStr = Form1.mw.bsComObj.Plc_Socket.Msg;
                    Console.WriteLine($"{plcStr}");
                    label6.Text = plcStr;
                    //string[] subCncStrs = cncStr.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] suPlcStrs = plcStr.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    //if (cncOldStr != cncStr)
                    //{
                    //    LogHelper.WriteLog(DateTime.Now.ToString() + "中控接收CNC服务器消息:" + cncStr);
                    //    cncOldStr = cncStr;
                    //}
                    if (plcOldStr != plcStr)
                    {
                        //LogHelper.WriteLog(DateTime.Now.ToString() + "主界面接收PLC服务器消息:" + plcStr);
                        plcOldStr = plcStr;
                    }
                    //foreach (string substr in subCncStrs)
                    //{
                    //    //if (!string.IsNullOrEmpty(substr) && !Regex.IsMatch(substr, @"^[\u4e00-\u9fff]+$"))
                    //    if (!string.IsNullOrEmpty(substr) && IsXml(substr))
                    //    {
                    //        if (cndOldLoadFinish != substr)
                    //        {









                    //            cndOldLoadFinish = substr;
                    //            ExecuteOperByCncInfo(substr);
                    //        }
                    //        //UpdateListBoxItems("接收CNC服务器消息:" + cncStr);
                    //    }
                    //}
                    foreach (string substr in suPlcStrs)
                    {
                        //if (!string.IsNullOrEmpty(substr) && !Regex.IsMatch(substr, @"^[\u4e00-\u9fff]+$"))
                        if (!string.IsNullOrEmpty(substr) && IsXml(substr))
                        {
                            ExecuteOperByPlcInfo(substr);
                            

                            //UpdateListBoxItems("接收CNC服务器消息:" + plcStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //LogHelper.WriteLog(DateTime.Now.ToString() + "线程内接收服务器消息时错误:" + ex.Message);
            }
        }

        private bool IsXml(string text)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(text);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ExecuteOperByPlcInfo(string plcInfo/*, Newtonsoft.Json.Linq.JObject jo*/)
        {
            if (string.IsNullOrEmpty(plcInfo)) return;
            //先获取订阅的参数名
            //先获取Para的str参数
            string pattern = @"<var>(.*?)</var>";   //订阅获取程序名                    
            string varName = GetAttributeItem(plcInfo, pattern);

            switch (varName)
            {
                case SysConstant.TAKE_PICTURE:  //是否拍照
                    {
                        string varPattern = @"<val>(.*?)</val>";   //订阅获取程序名                    
                        string varStr = GetAttributeItem(plcInfo, varPattern);
                        if (!string.IsNullOrEmpty(varStr))
                        {
                            BTakePicture = Convert.ToInt16(varStr) != 0;
                        }
                    }
                    break;
                case SysConstant.CONFIRM_RECIVE:  //PLC是否接受到结果
                    {
                        string varPattern = @"<val>(.*?)</val>";   //订阅获取程序名                    
                        string varStr = GetAttributeItem(plcInfo, varPattern);
                        if (!string.IsNullOrEmpty(varStr))
                        {
                            BConfirmRecive = Convert.ToInt16(varStr) != 0;
                        }
                    }
                    break;
                case SysConstant.MATERIAL:  //是否为碳钢管
                    {
                        string varPattern = @"<val>(.*?)</val>";   //订阅获取程序名                    
                        string varStr = GetAttributeItem(plcInfo, varPattern);
                        if (!string.IsNullOrEmpty(varStr))
                        {

                            int material = Convert.ToInt16(varStr);
                            switch (material)
                            { 
                                case 0:
                                    {
                                        //MessageBox.Show("请选择材料");
                                    }
                                    break ;
                                case 1:
                                    {
                                        //MessageBox.Show("铝材");
                                        
                                        BIsAluminum = true;
                                    }
                                    break;
                                case 2:
                                    {
                                        //MessageBox.Show("碳钢");  
                                        BIsCarbonSteel = true;
                                    }
                                    break;


                            }
                        }
                    }
                    break;
                default:
                    break;

            }
        }

       

        public bool BTakePicture
        {
            get => is_take_picture;
            set
            {
                if (is_take_picture != value)
                {
                    is_take_picture = value;
                    if (is_take_picture)
                    {
                        
                        this.TimeToTakePicture?.Invoke(null, null);
                    }
                }
            }
        }

        public bool BConfirmRecive
        {
            get => is_confirm_recive;
            set
            {
                if (is_confirm_recive != value)
                {
                    is_confirm_recive = value;
                    if (is_confirm_recive)
                    {
                        this.ConfirmRecive?.Invoke(null, null);
                    }
                }
            }

        }

        public bool BIsCarbonSteel
        {
            get => is_carbon_steel;
            set
            {
                if (is_carbon_steel != value)
                {
                    is_carbon_steel = value;
                    if (is_carbon_steel)
                    {
                        this.CarbonSteel?.Invoke(null, null);
                    }
                }
            }


        }

        public bool BIsAluminum
        {
            get => is_aluminum;
            set
            {
                if (is_aluminum != value)
                {
                    is_aluminum = value;
                    if (is_aluminum)
                    {
                        this.Aluminum?.Invoke(null, null);
                    }
                }
            }


        }


        private void IniAutoProcessDelegateEvent()
        {
            //循环开始，开始 拍照 + 处理
            TimeToTakePicture += new EventHandler(take_pic_solve_resulit);
            //确认PLC收到有无孔信息
            ConfirmRecive += new EventHandler(AfterConfirmRecive);

            CarbonSteel += new EventHandler(IsCarbonSteel);//执行碳钢管参数

            Aluminum += new EventHandler(IsAluminum);//执行铝管参数
            
        }

        //出发后执行的代码
        //整个流程自动
        private void take_pic_solve_resulit(object sender, EventArgs e)
        {
            //MessageBox.Show("开始执行拍照");

            //拍照
            take_picture();
            //自动打开图片
            open_pic_auto();
            //处理
            btn_processpic_Click(null, null);
            //返回结果
            show_result();
            //等待确认结果接收
            //confirm_recive();
            AfterConfirmRecive(null,null);
            //pic_num = pic_num + 1;

        }

        private void AfterConfirmRecive(object sender, EventArgs e)
        {

            HasHole = false;
            is_confirm_recive = false;
            

            pic_num = pic_num + 1;
            label7.Text = "结果重置";
            is_take_picture = false;
        }

        private void IsCarbonSteel(object sender, EventArgs e)
        {
            MessageBox.Show("执行碳钢管参数");
            is_carbon_steel = true;
            is_aluminum = false;
            Cam_Material();

        }


        private void IsAluminum(object sender, EventArgs e)
        {
            MessageBox.Show("执行铝管参数");
            
            is_aluminum = true;
            is_carbon_steel = false;
            Cam_Material();

        }

        private void Cam_Material()
        {
            if (is_carbon_steel && !is_aluminum && !is_carbon_steel_white) // 碳钢
            {
                int exposureValue = Convert.ToInt32(ConfigurationManager.AppSettings["CarboonBlackExposure"]);
                tbExposure.Text = exposureValue.ToString();
                if (startsetcamparam)
                { 
                    bnSetParam_Click(null,null);
                
                }
                

            }
            else if (!is_carbon_steel && is_aluminum && !is_carbon_steel_white) // 铝管
            {
                int exposureValue = Convert.ToInt32(ConfigurationManager.AppSettings["AlumExposure"]);
                tbExposure.Text = exposureValue.ToString();
                if (startsetcamparam)
                {
                    bnSetParam_Click(null, null);

                }
            }
            else if (!is_carbon_steel && !is_aluminum && is_carbon_steel_white) //碳钢管白
            {
                int exposureValue = Convert.ToInt32(ConfigurationManager.AppSettings["CarboonWhiteExposure"]);
                tbExposure.Text = exposureValue.ToString();
                if (startsetcamparam)
                {
                    bnSetParam_Click(null, null);

                }
            }
            else
            {
                int exposureValue = 30000;
                tbExposure.Text = exposureValue.ToString();
                if (startsetcamparam)
                {
                    bnSetParam_Click(null, null);

                }
            }


        }

        private string GetAttributeItem(string scrStr, string mathStr)
        {
            //string plcinfo = "<get2><auto>yes</auto><val>0</val><var>PLC_PRG.SBCD1</var></get2>";
            //string pattern = @"<var>(.*?)</var>";
            //string varName = GetAttributeItem(plcInfo, pattern);
            string attributeStr = "";
            if (string.IsNullOrEmpty(scrStr) || string.IsNullOrEmpty(mathStr)) return null;
            Match match = Regex.Match(scrStr, mathStr);
            if (match.Success)
            {
                attributeStr = match.Groups[1].Value;

            }
            else
            {
                attributeStr = "";
            }
            return attributeStr;
        }
        
        
        

        private async void confirm_recive()
        {

            while (true) // 无限循环来检查条件
            {
                await Task.Delay(100); // 等待100毫秒以避免阻塞UI线程
                if (is_confirm_recive)
                {
                    HasHole = false;
                    is_confirm_recive = false;

                }

            }
        }

        private void CreatBasePath()
        {
            //Directory.CreateDirectory(@"D:\RTCPtest");

            Directory.CreateDirectory(@"C:\\RTCPtest\\");
            //Directory.CreateDirectory(@"D:\\RTCPtest\\file");
            DateTime now = DateTime.Now;
            string Timenow = now.ToString("yyyy-MM-dd-HH-mm");
            Directory.CreateDirectory(@"C:\\RTCPtest\\file" + Timenow);
            Directory.CreateDirectory(@"C:\\RTCPtest\\file" + Timenow + "\\PIC");
            //Directory.CreateDirectory(@"D:\\RTCPtest\\file" + Timenow + "\\TEMP");
            //Directory.CreateDirectory(@"D:\\RTCPtest\\PARAM");
            string currentdirectory = "C:\\";

            basepath_PIC = currentdirectory + "\\RTCPtest\\file" + Timenow + "\\PIC";

            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\");
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\file");
            //DateTime now = DateTime.Now;
            //string Timenow = now.ToString("yyyy-MM-dd-HH-mm");
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\file" + Timenow);
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\file" + Timenow + "\\PIC");
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\file" + Timenow + "\\TEMP");
            //Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\RTCPtest\\PARAM");
            //string currentdirectory = Directory.GetCurrentDirectory();

            //basepath_PIC = currentdirectory + "\\RTCPtest\\file" + Timenow + "\\PIC";
        }

        private void take_picture()
        {


            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Bmp Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.iMethodValue = 2;
                //stSaveFileParam.pImagePath = "Image_w" + stSaveFileParam.nWidth.ToString() + "_h" + stSaveFileParam.nHeight.ToString() + "_fn" + m_stFrameInfo.nFrameNum.ToString() + ".bmp";
                string picnum = pic_num.ToString();
                stSaveFileParam.pImagePath = basepath_PIC + "\\" + picnum + ".bmp";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Bmp Fail!", nRet);
                    return;
                }



            }
        }

        private void open_pic_auto()
        {
            string picnum1 = pic_num.ToString();
            string pathname= basepath_PIC + "\\" + picnum1 + ".bmp";
            //string pathnamerotate = Rotate_pic(pathname);
            Img = Cv2.ImRead(pathname);//读取路径下的图片
            pictureBox2.Load(pathname); //pictureBox1直接加载

            label1.Text = "图片路径：" + pathname; //显示图片路径

        }

        private void show_result()
        {
            
            if (HasHole)
            {
                //MessageBox.Show("有孔");
                try
                {
                    int test = 2;
                    ushort ushort_test = (ushort)test;
                    master.WriteSingleRegister(0, 4, ushort_test);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            else
            {
                //MessageBox.Show("无孔");
                try
                {
                    int test = 1;
                    ushort ushort_test = (ushort)test;
                    master.WriteSingleRegister(0, 4, ushort_test);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            

        }

        private void InitializeButtons()
        {
            
            button1.Visible = true;
            button3.Visible = true;
            button4.Visible = true;
            button5.Visible = true;
            button6.Visible = true;
            button7.Visible = true;
            button9.Visible = true;
            label1.Text = "请打开图片";
            label2.Text = "客户";
            Auto_claer_data();
            Check_disk_used();
        }

            private void testopencv_Click(object sender, EventArgs e)
        {
            Mat srcImage = new Mat(new OpenCvSharp.Size(200, 200), MatType.CV_8UC3, Scalar.All(0));
            Cv2.Circle(srcImage, 100, 100, 80, new Scalar(255, 0, 0), 20);
            Cv2.Circle(srcImage, 100, 100, 60, new Scalar(0, 255, 0), 20);
            Cv2.Circle(srcImage, 100, 100, 20, new Scalar(0, 0, 255), 20);
            Bitmap bitmap = BitmapConverter.ToBitmap(srcImage);
            pictureBox2.Image = bitmap;



        }

        private void btn_openpic_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();//OpenFileDialog是一个类，实例化此类可以设置弹出一个文件对话框
            file.Filter = "JPG(*.JPG;*.JPEG);PNG文件(*.PNG);bmp文件(*.BMP);gif文件(*.GIF)|*.jpg;*.jpeg;*.png;*.bmp;*.gif";//文件类型过滤，只可选择图片的类型

            file.ShowDialog();//显示通用对话框
            if (file.FileName != string.Empty)
            {
                try
                {
                    pathname = file.FileName;
                    //string pathnamerotate = Rotate_pic(pathname);



                    Img = Cv2.ImRead(pathname);//读取路径下的图片
                    //  Cv2.ImShow("1", Img); 
                    
                    pictureBox2.Load(pathname); //pictureBox1直接加载

                    label1.Text = "图片路径：" + pathname; //显示图片路径

                    //Img = Cv2.ImRead(pathname);//读取路径下的图片
                    //                           //  Cv2.ImShow("1", Img); 

                    //pictureBox2.Load(pathname); //pictureBox1直接加载

                    //label1.Text = "图片路径：" + pathname; //显示图片路径
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }

        private string Rotate_pic(string pic_path)
        {
            // 加载图片
            Image image = Image.FromFile(pic_path);

            // 旋转图片 90 度
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);

            // 保存旋转后的图片
            string picnum2 = pic_num.ToString();
            string picpath_rotate= basepath_PIC + "\\" + picnum2 + "_rotate.bmp";
            image.Save(picpath_rotate);

            // 释放资源
            image.Dispose();

            Console.WriteLine("图片已旋转并保存。");
            
            return picpath_rotate;
        
        
        }



        private void btn_processpic_Click(object sender, EventArgs e)
        {
            DateTime now1 = DateTime.Now;
            string Timenow1 = now1.ToString("yyyy-MM-dd-HH-mm-ss-fff");
            label8.Text = Timenow1;
            //根据材料选择参数
            double houghrho;
            double houghtheta;
            int houghthreshold;
            double cannyparm;
            double erzhihuaparm;
            double cannyparm2;
            double line_mid;

            if (is_carbon_steel && !is_aluminum && !is_carbon_steel_white) // 当A为true且B为false时
            {
                houghrho = Convert.ToDouble(ConfigurationManager.AppSettings["HoughRho_Carboon"]);
                houghtheta = Convert.ToDouble(ConfigurationManager.AppSettings["HoughTheta_Carboon"]);
                houghthreshold = Convert.ToInt32(ConfigurationManager.AppSettings["HoughThreshold_Carboon"]);
                cannyparm = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm_Carboon"]);
                erzhihuaparm = Convert.ToDouble(ConfigurationManager.AppSettings["ErZhiHuaParm_Carboon"]);
                cannyparm2 = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm2_Carboon"]);
                line_mid = Convert.ToDouble(ConfigurationManager.AppSettings["Line_mid"]);
            }
            else if (!is_carbon_steel && is_aluminum && !is_carbon_steel_white) // 当A为false且B为true时
            {
                houghrho = Convert.ToDouble(ConfigurationManager.AppSettings["HoughRho_Alum"]);
                houghtheta = Convert.ToDouble(ConfigurationManager.AppSettings["HoughTheta_Alum"]);
                houghthreshold = Convert.ToInt32(ConfigurationManager.AppSettings["HoughThreshold_Alum"]);
                cannyparm = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm_Alum"]);
                erzhihuaparm = Convert.ToDouble(ConfigurationManager.AppSettings["ErZhiHuaParm_Alum"]);
                cannyparm2 = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm2_Alum"]);
                line_mid = Convert.ToDouble(ConfigurationManager.AppSettings["Line_mid"]);
            }
            else if (!is_carbon_steel && !is_aluminum && is_carbon_steel_white) // 当A为false且B为true时
            {
                houghrho = Convert.ToDouble(ConfigurationManager.AppSettings["HoughRho_CarboonWhite"]);
                houghtheta = Convert.ToDouble(ConfigurationManager.AppSettings["HoughTheta_CarboonWhite"]);
                houghthreshold = Convert.ToInt32(ConfigurationManager.AppSettings["HoughThreshold_CarboonWhite"]);
                cannyparm = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm_CarboonWhite"]);
                erzhihuaparm = Convert.ToDouble(ConfigurationManager.AppSettings["ErZhiHuaParm_CarboonWhite"]);
                cannyparm2 = Convert.ToDouble(ConfigurationManager.AppSettings["CannyParm2_CarboonWhite"]);
                line_mid = Convert.ToDouble(ConfigurationManager.AppSettings["Line_mid"]);
            }
            else
            {
                // 如果A和B的组合不是上述两种情况，可以设置一个默认值或抛出异常

                houghrho = Convert.ToDouble(tb_houghrho.Text);
                cannyparm = Convert.ToDouble(tb_cannyparm.Text);
                cannyparm2 = Convert.ToDouble(tb_cannyparm2.Text);
                houghtheta = Convert.ToDouble(tb_houghtheta.Text);
                erzhihuaparm = Convert.ToDouble(tb_erzhihuaparm.Text);
                houghthreshold = Convert.ToInt32(tb_houghthreshold.Text);
                line_mid = Convert.ToDouble(tb_line_mid.Text);
            }


            // 步骤 1: 读取图像并转换为灰度图
            Mat img = Img;
            int channelsImg = img.Channels();
            Console.WriteLine($"图像通道数: {channelsImg}");

            Mat resizeGrayImg = new Mat();
            Cv2.Resize(img, resizeGrayImg, new OpenCvSharp.Size(0, 0), 0.25, 0.25, InterpolationFlags.Linear);

            Mat origin_resize_gray_img = new Mat();
            Cv2.Resize(img, origin_resize_gray_img, new OpenCvSharp.Size(0, 0), 0.25, 0.25, InterpolationFlags.Linear);
            //Cv2.ImShow("origin_resize_gray_img", resizeGrayImg);

            int channels = resizeGrayImg.Channels();
            Console.WriteLine($"图像通道数: {channels}");

            // 转换为灰度图（如果尚未转换）
            Mat grayImg = new Mat();
            if (channels == 3)
            {
                Cv2.CvtColor(resizeGrayImg, grayImg, ColorConversionCodes.BGR2GRAY);
            }
            else
            { 
                grayImg = resizeGrayImg.Clone();
            }

            // 或者加入中值滤波
            Mat medianBlurredImg = new Mat();
            Cv2.MedianBlur(grayImg, medianBlurredImg, 5);

            // 加入高斯滤波
            Mat blurredImg = new Mat();
            Cv2.GaussianBlur(grayImg, blurredImg, new OpenCvSharp.Size(5, 5), 0);

            // 使用Canny边缘检测
            Mat edges = new Mat();
            Cv2.Canny(blurredImg, edges, cannyparm, cannyparm2);
            //Cv2.ImShow("bianyuan", edges);
            Mat bgrMat = new Mat();
            Cv2.CvtColor(edges, bgrMat, ColorConversionCodes.GRAY2BGR);
            bianyuan = bgrMat;




            

            // 使用霍夫变换检测直线
            OpenCvSharp.LineSegmentPolar[] lineSegments = Cv2.HoughLines(edges, houghrho, Math.PI / houghtheta, houghthreshold); // 注意：这里我将Math.PI / 360改为Math.PI / 180，因为通常角度的步长会大一些

            // 初始化一个空列表来存储符合条件的(rho, theta)元组
            List<(double rho, double theta)> linesTheta = new List<(double, double)>();
            if (lineSegments == null)
            {
                Errow_signal = true;//盖子没打开时
            }

            // 检查lineSegments是否为空
            if (lineSegments != null)
            {
                // 定义theta的阈值（弧度制）
                double thetaMin = 80 * Math.PI / 180;
                double thetaMax = 100 * Math.PI / 180;

                // 筛选符合条件的(rho, theta)对
                foreach (var lineSegment in lineSegments)
                {
                    double rho = lineSegment.Rho;
                    double theta = lineSegment.Theta;
                    if (thetaMin <= theta && theta <= thetaMax)
                    {
                        linesTheta.Add((rho, theta));
                    }
                }
            }



            // 打印所有符合条件的(rho, theta)对
            foreach (var (rho, theta) in linesTheta)
            {
                Console.WriteLine($"rho: {rho}, theta: {theta:.2f} radians");
            }

            // 绘制直线
            Mat hough = resizeGrayImg.Clone();

            foreach (var (rho, theta) in linesTheta)
            {
                double a = Math.Cos(theta);
                double b = Math.Sin(theta);
                double x0 = a * rho;
                double y0 = b * rho;
                int x1 = (int)(x0 + 2000 * (-b));  // 可以根据需要调整这个距离
                int y1 = (int)(y0 + 2000 * a);
                int x2 = (int)(x0 - 2000 * (-b));
                int y2 = (int)(y0 - 2000 * a);

                // 绘制直线
                Cv2.Line(hough, new OpenCvSharp.Point(x1, y1), new OpenCvSharp.Point(x2, y2), new Scalar(0, 255, 0), 2);
            }
            //Cv2.ImShow("houghsuoyou", hough);

            huofu = hough;

            // 获取图像尺寸
            OpenCvSharp.Size size = resizeGrayImg.Size();
            int height = size.Height;
            int width = size.Width;
            int channel = resizeGrayImg.Channels();
            Console.WriteLine($"{height},{width},{channel}");

            // 初始化两个列表来存储正负差值
            List<(double diff, (double rho, double theta) tuple)> positiveDifferences = new List<(double, (double, double))>();
            List<(double diff, (double rho, double theta) tuple)> negativeDifferences = new List<(double, (double, double))>();

            // 计算每个rho与height/2的差值，并根据正负分别存储在positiveDifferences和negativeDifferences中
            foreach (var (rho, theta) in linesTheta)
            {
                double diff = rho - height / line_mid; // 2.125
                if (diff > 0)
                {
                    positiveDifferences.Add((diff, (rho, theta)));
                }
                else if (diff < 0)
                {
                    negativeDifferences.Add((diff, (rho, theta)));
                }
            }

            // 对正差值列表进行排序，以便找到最接近0的正差值
            positiveDifferences.Sort((a, b) => a.diff.CompareTo(b.diff));

            // 对负差值列表进行排序，以便找到最接近0的负差值
            negativeDifferences.Sort((a, b) => b.diff.CompareTo(a.diff));

            // 提取最接近0的正差值元组
            var closestPositive = positiveDifferences.FirstOrDefault();
            var closestNegative = negativeDifferences.FirstOrDefault();

            // 提取最接近0的正负差值元组
            List<(double rho, double theta)> linesRho_double = new List<(double rho, double theta)>();
            if (closestPositive != default)
            {
                linesRho_double.Add(closestPositive.tuple);
                Console.WriteLine($"最接近0的正差值: {closestPositive.diff}");
                Console.WriteLine($"对应的(rho, theta): ({closestPositive.tuple.rho}, {closestPositive.tuple.theta})");
            }

            if (closestNegative != default)
            {
                linesRho_double.Add(closestNegative.tuple);
                Console.WriteLine($"最接近0的负差值: {closestNegative.diff}");
                Console.WriteLine($"对应的(rho, theta): ({closestNegative.tuple.rho}, {closestNegative.tuple.theta})");
            }

            // 检查两个直线的rho距离是否过近
            if (linesRho_double.Count == 2)
            {
                double rho_distance = Math.Abs(linesRho_double[0].rho - linesRho_double[1].rho);
                if (rho_distance < 10)
                {
                    Errow_signal = true; // 两直线过近
                }
            }

            //// 初始化一个列表来存储差值的绝对值
            //List<(double diff, (double rho, double theta) tuple)> differences = new List<(double, (double, double))>();

            //// 计算每个rho与height/2的差的绝对值，并存储在differences中
            //foreach (var (rho, theta) in linesTheta)
            //{
            //    double diff = Math.Abs(rho - height / line_mid);//2.125
            //    differences.Add((diff, (rho, theta)));
            //}

            //// 对差值列表进行排序，以便找到最小的两个差值
            //differences.Sort((a, b) => a.diff.CompareTo(b.diff));

            //// 提取最接近的1个(rho, theta)元组
            //List<(double rho, double theta)> linesRho = differences.Take(1).Select(x => x.tuple).ToList();

            //// 提取最小的一个(rho, theta)元组
            //var minDifferenceTuple = differences.First();
            //double minDiff = minDifferenceTuple.diff;
            //(double rho, double theta) closest1 = minDifferenceTuple.tuple;
            //Console.WriteLine($"1最小差值: {minDiff}");
            //Console.WriteLine($"1对应的(rho, theta): ({closest1.rho}, {closest1.theta})");



            ////找第二个
            //double rho_close2 = (2 * height / line_mid) - closest1.rho;
            //List<(double diff, (double rho, double theta) tuple)> differences2 = new List<(double, (double, double))>();
            //foreach (var (rho, theta) in linesTheta)
            //{
            //    double diff = Math.Abs(rho - rho_close2);
            //    differences2.Add((diff, (rho, theta)));
            //}
            //differences2.Sort((a, b) => a.diff.CompareTo(b.diff));
            //List<(double rho, double theta)> linesRho2 = differences2.Take(1).Select(x => x.tuple).ToList();
            //var minDifferenceTuple2 = differences2.First();
            //double minDiff2 = minDifferenceTuple2.diff;
            //(double rho, double theta) closest2 = minDifferenceTuple2.tuple;
            //Console.WriteLine($"最小差值: {minDiff2}");
            //Console.WriteLine($"对应的(rho, theta): ({closest2.rho}, {closest2.theta})");

            //double rho_diatance = Math.Abs(closest1.rho - closest2.rho);
            //if (rho_diatance<10)
            //{
            //    Errow_signal = true;//两直线过近

            //}

            //List<(double rho, double theta)> linesRho_double = new List<(double rho, double theta)>
            //{
            //    closest1,
            //    closest2
            //};

            // 绘制直线
            Mat outputImg = resizeGrayImg.Clone();

            foreach (var (rho, theta) in linesRho_double)
            {
                double a = Math.Cos(theta);
                double b = Math.Sin(theta);
                double x0 = a * rho;
                double y0 = b * rho;
                int x1 = (int)(x0 + 2000 * (-b));  // 可以根据需要调整这个距离
                int y1 = (int)(y0 + 2000 * a);
                int x2 = (int)(x0 - 2000 * (-b));
                int y2 = (int)(y0 - 2000 * a);

                // 绘制直线
                Cv2.Line(outputImg, new OpenCvSharp.Point(x1, y1), new OpenCvSharp.Point(x2, y2), new Scalar(0, 255, 0), 2);
            }

            //Cv2.ImShow("zhixian", outputImg);
            //Bitmap bitmap2 = BitmapConverter.ToBitmap(outputImg);
            //pictureBox2.Image = bitmap2;
            zhixian = outputImg;


            double rho1 = linesRho_double[0].rho, theta1 = linesRho_double[0].theta;
            double rho2 = linesRho_double[1].rho, theta2 = linesRho_double[1].theta;

            double rho_up, theta_up, rho_down, theta_down;
            if (rho1 > rho2)
            {
                rho_up = rho2;
                theta_up = theta2;
                rho_down = rho1;
                theta_down = theta1;
            }
            else
            {
                rho_up = rho1;
                theta_up = theta1;
                rho_down = rho2;
                theta_down = theta2;
            }

            //Cv2.ImShow("beforeImage with Masked Area", origin_resize_gray_img);

            for (int x = 0; x < origin_resize_gray_img.Cols; x++)
            {
                for (int y = 0; y < origin_resize_gray_img.Rows; y++)
                {
                    // 计算上直线的 y 截距
                    double y_intersect_up = (rho_up - x * Math.Cos(theta_up)) / Math.Sin(theta_up);
                    if (y < y_intersect_up)
                    {
                        // 直接使用索引器设置像素值为 255（白色）
                        origin_resize_gray_img.Set<Vec3b>(y, x, new Vec3b(255, 255, 255)); // 这里假设 origin_resize_gray_img 是 CV_8UC1 类型
                    }

                    // 计算下直线的 y 截距（同样注意处理无穷大和 NaN 的情况）
                    double y_intersect_down = (rho_down - x * Math.Cos(theta_down)) / Math.Sin(theta_down);
                    if (y > y_intersect_down)
                    {
                        // 同样使用索引器设置像素值为 255
                        origin_resize_gray_img.Set<Vec3b>(y, x, new Vec3b(255, 255, 255));
                    }
                }
            }
            //Cv2.ImShow("Image with Masked Area", origin_resize_gray_img);
            yanmo = origin_resize_gray_img;


            // 显示结果
            //二值化
            Mat binary_lines=new Mat();
            double thresh = Cv2.Threshold(origin_resize_gray_img, binary_lines, erzhihuaparm, 255, ThresholdTypes.Binary);//参数改这儿

            //Cv2.ImShow("binary_lines", binary_lines);
            erzhihua = binary_lines;

            Mat gray=new Mat();
            Cv2.CvtColor(binary_lines, gray, ColorConversionCodes.BGR2GRAY);
            Mat anti_gray = new Mat();
            Cv2.Subtract(new Mat(gray.Size(), gray.Type(), Scalar.All(255)), gray, anti_gray);

            // 定义一个结构元素（这里使用半径为2的圆形，但在OpenCvSharp中通常使用椭圆形近似）
            int radius = 2;
            Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(2 * radius + 1, 2 * radius + 1), new OpenCvSharp.Point(radius, radius));

            // 对二值图像进行闭运算（这通常用于填充小空洞或连接邻近的物体）
            // 注意：这不是remove_small_objects的等价操作，但可以作为后处理步骤
            Mat closed_image = new Mat();
            Cv2.MorphologyEx(anti_gray, closed_image, MorphTypes.Close, element);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(closed_image, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);

            contours = contours.Where(contour => Cv2.ContourArea(contour) >= 20).ToArray();//移除小于数值的连通域

            int h = closed_image.Rows;
            int w = closed_image.Cols;
            Mat objImage = new Mat(h, w, MatType.CV_8UC3, new Scalar(0, 0, 0));

            Scalar[] colorList = new Scalar[]
            {
            new Scalar(94, 17, 43), new Scalar(128, 31, 113), new Scalar(121, 54, 180), new Scalar(118, 173, 253),
            new Scalar(190, 251, 250), new Scalar(93, 96, 139), new Scalar(94, 77, 143), new Scalar(128, 31, 113),
            new Scalar(121, 54, 180), new Scalar(118, 173, 253), new Scalar(190, 251, 250), new Scalar(93, 96, 239),
            new Scalar(122, 54, 180), new Scalar(119, 173, 253), new Scalar(191, 251, 250), new Scalar(94, 96, 239),
            new Scalar(123, 54, 180), new Scalar(120, 173, 253), new Scalar(192, 251, 250), new Scalar(95, 96, 239),
            new Scalar(124, 54, 180), new Scalar(121, 173, 253), new Scalar(193, 251, 250), new Scalar(96, 96, 239),
            new Scalar(125, 54, 180), new Scalar(122, 173, 253), new Scalar(194, 251, 250), new Scalar(97, 96, 239)
            };

            for (int ind = 0; ind < contours.Length; ind++)
            {
                Cv2.DrawContours(objImage, new OpenCvSharp.Point[][] { contours[ind] }, -1, colorList[ind], -1);
            }

            //Cv2.ImShow("Contours", objImage);
            lunkuo = objImage;

            double roundThresh = 0.75;
            for (int ind = 0; ind < contours.Length; ind++)
            {
                double perimeter = Cv2.ArcLength(contours[ind], true);
                double area = Cv2.ContourArea(contours[ind]);
                double alpha = 4 * Math.PI * area / (perimeter * perimeter);
                Console.WriteLine($"{ind,6} {area,9:N0} {perimeter,9:N0} {alpha,9:F3}");

                Moments moments = Cv2.Moments(contours[ind]);
                int cx = (int)(moments.M10 / moments.M00);
                int cy = (int)(moments.M01 / moments.M00);


                if (alpha > roundThresh)
                {
                    HasHole = true;
                    // Cv2.PutText(resizeGrayImg, ind.ToString(), new Point(cx, cy), HersheyFonts.HersheySimplex, 1.5, new Scalar(0, 255, 0), 4);
                    Cv2.Circle(objImage, new OpenCvSharp.Point(cx, cy), 2, new Scalar(0, 0, 255), 2);
                    Cv2.PutText(objImage, ind.ToString(), new OpenCvSharp.Point(cx, cy), HersheyFonts.HersheySimplex, 2.5, new Scalar(0, 128, 0), 2);
                    // Cv2.Circle(resizeGrayImg, new Point(cx, cy), 2, new Scalar(0, 0, 255), 2); // 取消注释如果你已经有了resizeGrayImg
                }
            }

            //Cv2.ImShow("Final", objImage);

            Bitmap objImage1 = BitmapConverter.ToBitmap(objImage);
            pictureBox2.Image = objImage1;
            jieguo = objImage;
            
            if (HasHole == true)
            {
                label7.Text = "有孔";//返回值2
                
            }
            else
            {
                label7.Text = "无孔";//返回值1
                
            }
            
            DateTime now2 = DateTime.Now;
            string Timenow2 = now2.ToString("yyyy-MM-dd-HH-mm-ss-fff");
            label9.Text = Timenow2;


        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (PasswordInputForm passwordForm = new PasswordInputForm())
            {
                passwordForm.ShowDialog(); // 显示模态对话框并等待用户输入

                if (!string.IsNullOrEmpty(passwordForm.Password))
                {
                    // 处理密码
                    const string correctPassword = "123";
                    if (passwordForm.Password == correctPassword)
                    {
                        isEngineerMode = true;
                        button1.Visible = true;
                        button3.Visible = true;
                        button4.Visible = true;
                        button5.Visible = true;
                        button6.Visible = true;
                        button7.Visible = true;
                        button9.Visible = true;
                        
                        label2.Text = "当前权限: 工程师";
                        MessageBox.Show("权限已切换到工程师模式");
                        button2.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show("密码错误，无法切换到工程师模式");
                    }

                }
                else
                {
                    MessageBox.Show("密码不能为空！");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            InitializeButtons();
            button2.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (bianyuan != null)
            {
                
                Bitmap binayuan1 = BitmapConverter.ToBitmap(bianyuan);
                pictureBox2.Image = binayuan1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }


        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (huofu != null)
            {

                Bitmap huofu1 = BitmapConverter.ToBitmap(huofu);
                pictureBox2.Image = huofu1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (zhixian != null)
            {

                Bitmap zhixian1 = BitmapConverter.ToBitmap(zhixian);
                pictureBox2.Image = zhixian1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (yanmo != null)
            {

                Bitmap yanmo1 = BitmapConverter.ToBitmap(yanmo);
                pictureBox2.Image = yanmo1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }

        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (erzhihua != null)
            {

                Bitmap erzhihua1 = BitmapConverter.ToBitmap(erzhihua);
                pictureBox2.Image = erzhihua1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }

        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (lunkuo != null)
            {

                Bitmap lunkuo1 = BitmapConverter.ToBitmap(lunkuo);
                pictureBox2.Image = lunkuo1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (jieguo != null)
            {

                Bitmap jieguo1 = BitmapConverter.ToBitmap(jieguo);
                pictureBox2.Image = jieguo1;
            }
            else
            {
                MessageBox.Show("图像为空！");
            }

        }

        #region 相机单元
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            MessageBox.Show(errorMsg, "PROMPT");
        }

        private void bnEnum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }

        private void DeviceListAcq()
        {
            // ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            m_stDeviceList.nDeviceNum = 0;
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("Enumerate devices fail!", 0);
                return;
            }

            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                    if (gigeInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }
                }
            }

            // ch:选择第一项 | en:Select the first item
            if (m_stDeviceList.nDeviceNum != 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }

        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;

            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnContinuesMode.Enabled = true;
            bnContinuesMode.Checked = true;
            bnTriggerMode.Enabled = true;
            cbSoftTrigger.Enabled = false;
            bnTriggerExec.Enabled = false;

            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            tbFrameRate.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
        }

        private void bnOpen_Click(object sender, EventArgs e)
        {
            if (m_stDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
            {
                ShowErrorMsg("No device, please select", 0);
                return;
            }

            // ch:获取选择的设备信息 | en:Get selected device information
            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));

            // ch:打开设备 | en:Open device
            if (null == m_MyCamera)
            {
                m_MyCamera = new MyCamera();
                if (null == m_MyCamera)
                {
                    return;
                }
            }

            int nRet = m_MyCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }

            nRet = m_MyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_MyCamera.MV_CC_DestroyDevice_NET();
                ShowErrorMsg("Device open fail!", nRet);
                return;
            }

            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = m_MyCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = m_MyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        ShowErrorMsg("Set Packet Size failed!", nRet);
                    }
                }
                else
                {
                    ShowErrorMsg("Get Packet Size failed!", nPacketSize);
                }
            }

            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            m_MyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            bnGetParam_Click(null, null);// ch:获取参数 | en:Get parameters

            // ch:控件操作 | en:Control operation
            SetCtrlWhenOpen();
        }

        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;

            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            bnContinuesMode.Enabled = false;
            bnTriggerMode.Enabled = false;
            cbSoftTrigger.Enabled = false;
            bnTriggerExec.Enabled = false;

            bnSaveBmp.Enabled = false;
            bnSaveJpg.Enabled = false;
            bnSaveTiff.Enabled = false;
            bnSavePng.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            tbFrameRate.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
        }

        private void bnClose_Click(object sender, EventArgs e)
        {
            // ch:取流标志位清零 | en:Reset flow flag bit
            if (m_bGrabbing == true)
            {
                m_bGrabbing = false;
                m_hReceiveThread.Join();
            }

            if (m_BufForDriver != IntPtr.Zero)
            {
                Marshal.Release(m_BufForDriver);
            }

            // ch:关闭设备 | en:Close Device
            m_MyCamera.MV_CC_CloseDevice_NET();
            m_MyCamera.MV_CC_DestroyDevice_NET();

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();
        }

        private void bnContinuesMode_CheckedChanged(object sender, EventArgs e)
        {
            if (bnContinuesMode.Checked)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                cbSoftTrigger.Enabled = false;
                bnTriggerExec.Enabled = false;
            }
        }

        private void bnTriggerMode_CheckedChanged(object sender, EventArgs e)
        {
            // ch:打开触发模式 | en:Open Trigger Mode
            if (bnTriggerMode.Checked)
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                //           1 - Line1;
                //           2 - Line2;
                //           3 - Line3;
                //           4 - Counter;
                //           7 - Software;
                if (cbSoftTrigger.Checked)
                {
                    m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    if (m_bGrabbing)
                    {
                        bnTriggerExec.Enabled = true;
                    }
                }
                else
                {
                    m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                }
                cbSoftTrigger.Enabled = true;
            }
        }

        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            
            bnStopGrab.Enabled = true;
            startsetcamparam = true;

            if (bnTriggerMode.Checked && cbSoftTrigger.Checked)
            {
                bnTriggerExec.Enabled = true;
            }

            bnSaveBmp.Enabled = true;
            bnSaveJpg.Enabled = true;
            bnSaveTiff.Enabled = true;
            bnSavePng.Enabled = true;
        }

        public void ReceiveThreadProcess()
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            int nRet = MyCamera.MV_OK;

            while (m_bGrabbing)
            {
                nRet = m_MyCamera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    lock (BufForDriverLock)
                    {
                        if (m_BufForDriver == IntPtr.Zero || stFrameInfo.stFrameInfo.nFrameLen > m_nBufSizeForDriver)
                        {
                            if (m_BufForDriver != IntPtr.Zero)
                            {
                                Marshal.Release(m_BufForDriver);
                                m_BufForDriver = IntPtr.Zero;
                            }

                            m_BufForDriver = Marshal.AllocHGlobal((Int32)stFrameInfo.stFrameInfo.nFrameLen);
                            if (m_BufForDriver == IntPtr.Zero)
                            {
                                return;
                            }
                            m_nBufSizeForDriver = stFrameInfo.stFrameInfo.nFrameLen;
                        }

                        m_stFrameInfo = stFrameInfo.stFrameInfo;
                        CopyMemory(m_BufForDriver, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);
                    }

                    if (RemoveCustomPixelFormats(stFrameInfo.stFrameInfo.enPixelType))
                    {
                        m_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                        continue;
                    }
                    stDisplayInfo.hWnd = pictureBox1.Handle;
                    stDisplayInfo.pData = stFrameInfo.pBufAddr;
                    stDisplayInfo.nDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                    stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                    stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                    stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                    m_MyCamera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);

                    m_MyCamera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
                else
                {
                    if (bnTriggerMode.Checked)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
        }

        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            // ch:标志位置位true | en:Set position bit true
            m_bGrabbing = true;

            m_hReceiveThread = new Thread(ReceiveThreadProcess);
            m_hReceiveThread.Start();

            m_stFrameInfo.nFrameLen = 0;//取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            // ch:开始采集 | en:Start Grabbing
            int nRet = m_MyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_bGrabbing = false;
                m_hReceiveThread.Join();
                ShowErrorMsg("Start Grabbing Fail!", nRet);
                return;
            }

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();
        }

        private void cbSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSoftTrigger.Checked)
            {
                // ch:触发源设为软触发 | en:Set trigger source as Software
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                if (m_bGrabbing)
                {
                    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
                m_MyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                bnTriggerExec.Enabled = false;
            }
        }

        private void bnTriggerExec_Click(object sender, EventArgs e)
        {
            // ch:触发命令 | en:Trigger command
            int nRet = m_MyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Trigger Software Fail!", nRet);
            }
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;

            bnTriggerExec.Enabled = false;


            bnSaveBmp.Enabled = false;
            bnSaveJpg.Enabled = false;
            bnSaveTiff.Enabled = false;
            bnSavePng.Enabled = false;
        }

        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            // ch:标志位设为false | en:Set flag bit false
            m_bGrabbing = false;
            m_hReceiveThread.Join();

            // ch:停止采集 | en:Stop Grabbing
            int nRet = m_MyCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Stop Grabbing Fail!", nRet);
            }

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();
        }

        private void bnSaveBmp_Click(object sender, EventArgs e)
        {
            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Bmp Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.iMethodValue = 2;
                stSaveFileParam.pImagePath = "Image_w" + stSaveFileParam.nWidth.ToString() + "_h" + stSaveFileParam.nHeight.ToString() + "_fn" + m_stFrameInfo.nFrameNum.ToString() + ".bmp";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Bmp Fail!", nRet);
                    return;
                }
            }

            ShowErrorMsg("Save Succeed!", 0);
        }

        private void bnSaveJpg_Click(object sender, EventArgs e)
        {
            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Jpeg Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.nQuality = 80;
                stSaveFileParam.iMethodValue = 2;
                stSaveFileParam.pImagePath = "Image_w" + stSaveFileParam.nWidth.ToString() + "_h" + stSaveFileParam.nHeight.ToString() + "_fn" + m_stFrameInfo.nFrameNum.ToString() + ".jpg";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Jpeg Fail!", nRet);
                    return;
                }
            }

            ShowErrorMsg("Save Succeed!", 0);
        }

        private void bnSavePng_Click(object sender, EventArgs e)
        {
            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Png Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Png;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.nQuality = 8;
                stSaveFileParam.iMethodValue = 2;
                stSaveFileParam.pImagePath = "Image_w" + stSaveFileParam.nWidth.ToString() + "_h" + stSaveFileParam.nHeight.ToString() + "_fn" + m_stFrameInfo.nFrameNum.ToString() + ".png";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Png Fail!", nRet);
                    return;
                }
            }

            ShowErrorMsg("Save Succeed!", 0);
        }

        private void bnSaveTiff_Click(object sender, EventArgs e)
        {
            if (false == m_bGrabbing)
            {
                ShowErrorMsg("Not Start Grabbing", 0);
                return;
            }

            if (RemoveCustomPixelFormats(m_stFrameInfo.enPixelType))
            {
                ShowErrorMsg("Not Support!", 0);
                return;
            }

            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();

            lock (BufForDriverLock)
            {
                if (m_stFrameInfo.nFrameLen == 0)
                {
                    ShowErrorMsg("Save Tiff Fail!", 0);
                    return;
                }
                stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Tif;
                stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                stSaveFileParam.pData = m_BufForDriver;
                stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                stSaveFileParam.iMethodValue = 2;
                stSaveFileParam.pImagePath = "Image_w" + stSaveFileParam.nWidth.ToString() + "_h" + stSaveFileParam.nHeight.ToString() + "_fn" + m_stFrameInfo.nFrameNum.ToString() + ".tif";
                int nRet = m_MyCamera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Save Tiff Fail!", nRet);
                    return;
                }
            }
            ShowErrorMsg("Save Succeed!", 0);
        }

        private void bnGetParam_Click(object sender, EventArgs e)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = m_MyCamera.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbExposure.Text = stParam.fCurValue.ToString("F1");
            }

            nRet = m_MyCamera.MV_CC_GetFloatValue_NET("Gain", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbGain.Text = stParam.fCurValue.ToString("F1");
            }

            nRet = m_MyCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                tbFrameRate.Text = stParam.fCurValue.ToString("F1");
            }
        }

        private void bnSetParam_Click(object sender, EventArgs e)
        {
            try
            {
                float.Parse(tbExposure.Text);
                float.Parse(tbGain.Text);
                float.Parse(tbFrameRate.Text);
            }
            catch
            {
                ShowErrorMsg("Please enter correct type!", 0);
                return;
            }

            m_MyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            int nRet = m_MyCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(tbExposure.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Exposure Time Fail!", nRet);
            }

            m_MyCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
            nRet = m_MyCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Gain Fail!", nRet);
            }

            nRet = m_MyCamera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(tbFrameRate.Text));
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Frame Rate Fail!", nRet);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            bnClose_Click(sender, e);
        }

        // ch:去除自定义的像素格式 | en:Remove custom pixel formats
        private bool RemoveCustomPixelFormats(MyCamera.MvGvspPixelType enPixelFormat)
        {
            Int32 nResult = ((int)enPixelFormat) & (unchecked((Int32)0x80000000));
            if (0x80000000 == nResult)
            {
                return true;
            }
            else
            {
                return false;
            }
        }






        #endregion

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 停止Timer
            timer.Stop();

            // 调用button8的Click事件
            button8.PerformClick();
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            bnOpen_Click(null, null);
            bnStartGrab_Click(null, null);


        }

        private void button10_Click(object sender, EventArgs e)
        {
            //string test = "1.";
            //Form1.mw.bsComObj.Plc_Socket.WriteCommand(CMD_PLC, $"<set><var>{SysConstant.HAS_HOLE}</var><val>{test}</val></set>");
            take_pic_solve_resulit(null,null);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            AfterConfirmRecive(null, null);
            
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Form_Dianbiao form_Dianbiao = new Form_Dianbiao();
            // 实例化新窗口  


            form_Dianbiao.Owner = this;
            //Screen screen = Screen.FromControl(this);
            //form_Dianbiao.Location = new System.Drawing.Point(screen.Bounds.Left, screen.Bounds.Top + 1388 - form_Dianbiao.Height);

            //form_Dianbiao.StartPosition = FormStartPosition.Manual;

            // 显示新窗口  
            form_Dianbiao.Show();
        }

        public static void Between()
        {
            MessageBox.Show("空间之间");

        }


        private void Check_disk_used()
        {
            try {
                // 获取D盘信息
                DriveInfo driveInfo = new DriveInfo("D");
                if (driveInfo.IsReady)
                {
                    // 计算占用百分比
                    double totalSize = driveInfo.TotalSize;
                    double freeSpace = driveInfo.TotalFreeSpace;
                    double usedSpace = totalSize - freeSpace;
                    double usagePercentage = (usedSpace / totalSize) * 100;

                    // 显示百分比
                    label25.Text = $"{usagePercentage:F2}%";
                    progressBar1.Value = ((int)usagePercentage);
                }

            }
            catch {

                label25.Text = "磁盘空间读取失败";
            
            
            }
        
        
        
        }

        private void btn_ClearData_Click(object sender, EventArgs e)
        {
            // 指定要清理的文件夹路径
            string folderPath = @"D:\RTCPtest"; // 替换为你要清理的文件夹路径

            try
            {
                if (Directory.Exists(folderPath))
                {
                    // 删除文件夹中的所有文件
                    foreach (string file in Directory.GetFiles(folderPath))
                    {
                        File.Delete(file);
                    }

                    // 删除文件夹中的所有子文件夹
                    foreach (string dir in Directory.GetDirectories(folderPath))
                    {
                        Directory.Delete(dir, true);
                    }
                    CreatBasePath();

                   
                }
                else
                {
                    
                    label25.Text = "文件夹不存在！";
                }
            }
            catch (Exception ex)
            {
               
                label25.Text = "清理文件夹时出错！";
            }
        }

        private void Auto_claer_data() {

            try
            {
                // 获取D盘信息
                DriveInfo driveInfo = new DriveInfo("D");
                if (driveInfo.IsReady)
                {
                    // 计算占用百分比
                    double totalSize = driveInfo.TotalSize;
                    double freeSpace = driveInfo.TotalFreeSpace;
                    double usedSpace = totalSize - freeSpace;
                    double usagePercentage = (usedSpace / totalSize) * 100;

                    double deadline = 70;
                    if (usagePercentage>deadline) {
                        btn_ClearData_Click(null,null);
                    }

                }

            }
            catch
            {

                label25.Text = "磁盘空间读取失败";


            }


        }

    }
}
