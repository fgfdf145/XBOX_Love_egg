using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Xbox_TiaoDan
{
    public partial class XBOX_TiaoDan : Form
    {
        // 通过 P/Invoke 调用 XInput API

        // 用于设置手柄震动
        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern uint XInputSetState(uint dwUserIndex, ref XInputVibration pVibration);

        // 用于检测手柄状态
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState(uint dwUserIndex, out XInputState pState);

        // 定义震动状态数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct XInputVibration
        {
            public ushort wLeftMotorSpeed;   // 左侧马达震动强度
            public ushort wRightMotorSpeed;  // 右侧马达震动强度
        }

        // 定义手柄状态数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct XInputState
        {
            public uint dwPacketNumber;
            public XInputGamepad Gamepad;
        }

        // 定义手柄数据结构
        [StructLayout(LayoutKind.Sequential)]
        public struct XInputGamepad
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        private Label labelControllerStatus;
        private Label labelVibrationStatus;
        private Timer timer;
        private Button btnVibrate;
        private TrackBar trackBarIntensity; // 用于控制震动力度的滑动条
        private bool isVibrating = false;    // 当前震动状态

        public XBOX_TiaoDan()
        {
            InitializeComponent();

            // 创建开关震动按钮
            btnVibrate = new Button();
            btnVibrate.Text = "开启震动";
            btnVibrate.Width = 120;
            btnVibrate.Height = 40;
            btnVibrate.Top = 50;
            btnVibrate.Left = 50;
            btnVibrate.Click += BtnVibrate_Click;
            this.Controls.Add(btnVibrate);

            // 创建用于显示手柄连接状态的 Label
            labelControllerStatus = new Label();
            labelControllerStatus.Text = "检测中...";
            labelControllerStatus.AutoSize = true;
            labelControllerStatus.Top = btnVibrate.Bottom + 20;
            labelControllerStatus.Left = btnVibrate.Left;
            this.Controls.Add(labelControllerStatus);

            // 创建用于显示震动状态和震动程度的 Label
            labelVibrationStatus = new Label();
            labelVibrationStatus.Text = "震动状态: 关闭, 震动程度: 0%";
            labelVibrationStatus.AutoSize = true;
            labelVibrationStatus.Top = labelControllerStatus.Bottom + 20;
            labelVibrationStatus.Left = btnVibrate.Left;
            this.Controls.Add(labelVibrationStatus);

            // 创建 TrackBar 控件，用于控制震动力度（百分比）
            trackBarIntensity = new TrackBar();
            trackBarIntensity.Minimum = 0;
            trackBarIntensity.Maximum = 100;
            trackBarIntensity.Value = 100; // 默认震动100%
            trackBarIntensity.TickFrequency = 10;
            trackBarIntensity.SmallChange = 1;
            trackBarIntensity.LargeChange = 10;
            trackBarIntensity.Top = labelVibrationStatus.Bottom + 20;
            trackBarIntensity.Left = btnVibrate.Left;
            trackBarIntensity.Width = 200;
            trackBarIntensity.Scroll += TrackBarIntensity_Scroll;
            this.Controls.Add(trackBarIntensity);

            // 创建 Timer 定时器，每秒检测一次手柄连接状态
            timer = new Timer();
            timer.Interval = 1000; // 1秒
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // 按钮点击事件：切换震动状态
        private void BtnVibrate_Click(object sender, EventArgs e)
        {
            XInputVibration vibration = new XInputVibration();

            if (!isVibrating)
            {
                int intensityPercentage = trackBarIntensity.Value;
                // 根据百分比计算震动力度（0～65535）
                ushort intensity = (ushort)(intensityPercentage / 100.0 * 65535);
                vibration.wLeftMotorSpeed = intensity;
                vibration.wRightMotorSpeed = intensity;
                XInputSetState(0, ref vibration);
                isVibrating = true;
                btnVibrate.Text = "关闭震动";
                labelVibrationStatus.Text = $"震动状态: 开启, 震动程度: {intensityPercentage}%";
            }
            else
            {
                vibration.wLeftMotorSpeed = 0;
                vibration.wRightMotorSpeed = 0;
                XInputSetState(0, ref vibration);
                isVibrating = false;
                btnVibrate.Text = "开启震动";
                labelVibrationStatus.Text = $"震动状态: 关闭, 震动程度: {trackBarIntensity.Value}%";
            }
        }

        // TrackBar 滑动事件：实时调整震动力度
        private void TrackBarIntensity_Scroll(object sender, EventArgs e)
        {
            int intensityPercentage = trackBarIntensity.Value;
            labelVibrationStatus.Text = $"震动状态: {(isVibrating ? "开启" : "关闭")}, 震动程度: {intensityPercentage}%";
            if (isVibrating)
            {
                // 根据滑动条值计算震动力度并更新手柄状态
                ushort intensity = (ushort)(intensityPercentage / 100.0 * 65535);
                XInputVibration vibration = new XInputVibration
                {
                    wLeftMotorSpeed = intensity,
                    wRightMotorSpeed = intensity
                };
                XInputSetState(0, ref vibration);
            }
        }

        // Timer Tick 事件：检测手柄是否连接
        private void Timer_Tick(object sender, EventArgs e)
        {
            XInputState state;
            uint result = XInputGetState(0, out state);
            labelControllerStatus.Text = result == 0 ? "手柄已连接" : "手柄未连接";
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new XBOX_TiaoDan());
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
