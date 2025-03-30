// Power by ChatGPT o3-mini-high

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

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

        // 用于语言切换
        private string currentLanguage = "中文"; // 默认中文
        private ComboBox comboBoxLanguage;

        private Label labelSelectLanguage;   // 新增：语言选择提示标签
        private Label labelControllerStatus;
        private Label labelVibrationStatus;
        private Timer timer;
        private Button btnVibrate;
        private TrackBar trackBarIntensity;  // 用于控制震动力度的滑动条
        private bool isVibrating = false;     // 当前震动状态

        public XBOX_TiaoDan()
        {
            InitializeComponent();

            // 设置窗体启动居中，并设定默认大小
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(400, 350);

            // 创建 TableLayoutPanel，所有控件剧中排列
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.AutoSize = true;
            tableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanel.Padding = new Padding(0, 20, 0, 20);
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            // 定义6行布局：标签、下拉框、按钮、连接状态、震动状态、滑动条
            tableLayoutPanel.RowCount = 6;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 选择语言提示
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 语言选择 ComboBox
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 震动按钮
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 手柄连接状态
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 震动状态及程度
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 震动力度滑动条

            // 新增：创建语言选择提示 Label
            labelSelectLanguage = new Label();
            labelSelectLanguage.Text = "选择语言 / Select Language";
            labelSelectLanguage.AutoSize = true;
            labelSelectLanguage.TextAlign = ContentAlignment.MiddleCenter;
            labelSelectLanguage.Anchor = AnchorStyles.None;
            tableLayoutPanel.Controls.Add(labelSelectLanguage, 0, 0);

            // 创建语言选择 ComboBox
            comboBoxLanguage = new ComboBox();
            comboBoxLanguage.Items.Add("中文");
            comboBoxLanguage.Items.Add("English");
            comboBoxLanguage.SelectedIndex = 0; // 默认中文
            comboBoxLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLanguage.SelectedIndexChanged += ComboBoxLanguage_SelectedIndexChanged;
            comboBoxLanguage.Anchor = AnchorStyles.None;
            tableLayoutPanel.Controls.Add(comboBoxLanguage, 0, 1);

            // 创建开关震动按钮
            btnVibrate = new Button();
            btnVibrate.Text = "开启震动";
            btnVibrate.Width = 120;
            btnVibrate.Height = 40;
            btnVibrate.Click += BtnVibrate_Click;
            btnVibrate.Anchor = AnchorStyles.None;
            tableLayoutPanel.Controls.Add(btnVibrate, 0, 2);

            // 创建用于显示手柄连接状态的 Label
            labelControllerStatus = new Label();
            labelControllerStatus.Text = "检测中...";
            labelControllerStatus.AutoSize = true;
            labelControllerStatus.Anchor = AnchorStyles.None;
            tableLayoutPanel.Controls.Add(labelControllerStatus, 0, 3);

            // 创建用于显示震动状态和震动程度的 Label
            labelVibrationStatus = new Label();
            labelVibrationStatus.Text = "震动状态: 关闭, 震动程度: 0%";
            labelVibrationStatus.AutoSize = true;
            labelVibrationStatus.Anchor = AnchorStyles.None;
            tableLayoutPanel.Controls.Add(labelVibrationStatus, 0, 4);

            // 创建 TrackBar 控件，用于控制震动力度（百分比）
            trackBarIntensity = new TrackBar();
            trackBarIntensity.Minimum = 0;
            trackBarIntensity.Maximum = 100;
            trackBarIntensity.Value = 100; // 默认震动100%
            trackBarIntensity.TickFrequency = 10;
            trackBarIntensity.SmallChange = 1;
            trackBarIntensity.LargeChange = 10;
            trackBarIntensity.Width = 200;
            trackBarIntensity.Anchor = AnchorStyles.None;
            trackBarIntensity.Scroll += TrackBarIntensity_Scroll;
            tableLayoutPanel.Controls.Add(trackBarIntensity, 0, 5);

            // 将 TableLayoutPanel 添加到窗体
            this.Controls.Add(tableLayoutPanel);

            // 创建 Timer 定时器，每秒检测一次手柄连接状态
            timer = new Timer();
            timer.Interval = 1000; // 1秒
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // ComboBox 语言切换事件
        private void ComboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentLanguage = comboBoxLanguage.SelectedItem.ToString();
            UpdateUILanguage();
        }

        // 根据 currentLanguage 更新界面所有文本
        private void UpdateUILanguage()
        {
            if (currentLanguage == "English")
            {
                btnVibrate.Text = isVibrating ? "Disable Vibration" : "Enable Vibration";
                labelVibrationStatus.Text = isVibrating ?
                    $"Vibration: On, Intensity: {trackBarIntensity.Value}%" :
                    $"Vibration: Off, Intensity: {trackBarIntensity.Value}%";
            }
            else
            {
                btnVibrate.Text = isVibrating ? "关闭震动" : "开启震动";
                labelVibrationStatus.Text = isVibrating ?
                    $"震动状态: 开启, 震动程度: {trackBarIntensity.Value}%" :
                    $"震动状态: 关闭, 震动程度: {trackBarIntensity.Value}%";
            }
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
            }
            else
            {
                vibration.wLeftMotorSpeed = 0;
                vibration.wRightMotorSpeed = 0;
                XInputSetState(0, ref vibration);
                isVibrating = false;
            }
            UpdateUILanguage();
        }

        // TrackBar 滑动事件：实时调整震动力度
        private void TrackBarIntensity_Scroll(object sender, EventArgs e)
        {
            int intensityPercentage = trackBarIntensity.Value;
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
            UpdateUILanguage();
        }

        // Timer Tick 事件：检测手柄是否连接
        private void Timer_Tick(object sender, EventArgs e)
        {
            XInputState state;
            uint result = XInputGetState(0, out state);
            if (currentLanguage == "English")
                labelControllerStatus.Text = result == 0 ? "Controller connected" : "Controller not connected";
            else
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
