using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.ComponentModel;
using System.IO.Ports;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using DMSS;
using DEBUG;
using System.Collections.Specialized;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using System.Windows.Data;


namespace TEST
{

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private BatteryTesterViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            Debug.Open("syslog.txt");
            viewModel = new BatteryTesterViewModel(Dispatcher);
            DataContext = viewModel;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BatteryTesterSettings");
            if (key != null)
            {
                try
                {
                    viewModel.port.SetPort((string)key.GetValue("port", "COM1"));
                    if ((int)key.GetValue("audio", 0) == 0)
                        viewModel.AudioEnable = false;
                    else
                        viewModel.AudioEnable = true;
                    viewModel.UpdatePortsCommand.Execute(null);
                    comPortCombo.SelectedItem = viewModel.port.GetPortName();
                }
                catch (Exception e)
                { }
                key.Close();
            }
            Debug.Send("Открытие программы");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BatteryTesterSettings");
            //storing the values  
            key.SetValue("port", viewModel.port.GetPortName());
            if (viewModel.AudioEnable)
                key.SetValue("audio", 1);
            else
                key.SetValue("audio", 0);
            key.Close();
            Debug.Send("Закрытие программы");
            Debug.Close();
        }

        private void comPortCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (viewModel.UpdatePortsCommand != null)
            {
                viewModel.UpdatePortsCommand.Execute(null);
            }
        }

        private void comPortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (viewModel.ChangePortCommand != null)
            {
                if(e.AddedItems.Count>0)
                    viewModel.ChangePortCommand.Execute(((System.Collections.DictionaryEntry)(e.AddedItems[0])).Key);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    public class PwmConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            // Возвращаем строку в формате 
            if ((int)value != 0)
            {
                return 255 + 40 - (int)value;
            }
            else
                return 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public class PwmHintConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            // Возвращаем строку в формате 
            if ((int)value != 0)
            {
                return "Ток " + ((int)value / 2.55 * 18.5).ToString("##,#") + "mA";// $"+ ((int)value ).ToString("###");
            }
            else
                return "Ток максимальный";
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public class CapacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return (float)value*1.15;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    public enum Channel_ModeState
    {
        STANDBY = 0, AUTO_STANDBY, AUTO_CHARGE, AUTO_DISCHARGE, AUTO_RECHARGE, AUTO_READY, CHARGE, DISCHARGE, READY, FULL_AUTO_STANDBY, FULL_AUTO_DISCHARGE, FAST_AUTO_STANDBY, FAST_AUTO_CHARGE
    }

    public class BatteryData : INotifyPropertyChanged
    {
        private float voltage = 0;
        private Channel_ModeState mode = 0;
        private int batteryType = 0;
        private float capacity = 0;
        private TimeSpan time;
        private int batNumber = 0;
        private int pwm = 0;
        private string  batName = "";
        public int PwmProp
        {
            get
            {
                 return pwm;
            }
            set
            {
                if (pwm == value)
                {
                    return;
                }
                pwm = value;
                OnPropertyChanged("PwmProp");
            }
        }
        public float voltageProp
        {
            get { return voltage; }
            set
            {
                if (voltage == value)
                {
                    return;
                }
                voltage = value;
                OnPropertyChanged("voltageProp");
            }
        }
        public Channel_ModeState modeProp
        {
            get { return mode; }
            set
            {
                if (mode == value)
                {
                    return;
                }
                mode = value;
                OnPropertyChanged("modeProp");
                OnPropertyChanged("modePropText");
                Debug.Send("Смена состояния " + batName + " на " + modeToText(mode));
                Debug.Send(typePropText+" Напряжение=" + voltageProp+" Ёмкость="+ capacityProp+" Время-"+timeProp);
            }
        }
        public static string modeToText(Channel_ModeState m)
        {
            switch (m)
            {
                case Channel_ModeState.STANDBY:
                    return "ЖДУ АКБ";
                case Channel_ModeState.AUTO_STANDBY:
                    return "AUTO ЖДУ АКБ";
                case Channel_ModeState.AUTO_CHARGE:
                    return "AUTO ЗАРЯД";
                case Channel_ModeState.AUTO_DISCHARGE:
                    return "AUTO РАЗРЯД";
                case Channel_ModeState.AUTO_RECHARGE:
                    return "AUTO ДОЗАРЯД";
                case Channel_ModeState.AUTO_READY:
                    return "AUTO ГОТОВО";
                case Channel_ModeState.CHARGE:
                    return "ЗАРЯД";
                case Channel_ModeState.DISCHARGE:
                    return "РАЗРЯД";
                case Channel_ModeState.READY:
                    return "ГОТОВО";
                case Channel_ModeState.FULL_AUTO_STANDBY:
                    return "FULL ЖДУ АКБ";
                case Channel_ModeState.FULL_AUTO_DISCHARGE:
                    return "предв. РАЗРЯД";
                case Channel_ModeState.FAST_AUTO_STANDBY:
                    return "FAST ЖДУ АКБ";
                case Channel_ModeState.FAST_AUTO_CHARGE:
                    return "быстр ЗАРЯД";
            }
            return "";
        }
        public string modePropText
        {
            get
            {
                //STANDBY = 0U, AUTO_STANDBY , AUTO_CHARGE, AUTO_DISCHARGE, AUTO_RECHARGE, AUTO_READY, CHARGE, DISCHARGE, READY
                return modeToText(mode);
            }
        }
        public int typeProp
        {
            get { return batteryType; }
            set
            {
                if (batteryType == value)
                {
                    return;
                }
                batteryType = value;
                OnPropertyChanged("typeProp");
                OnPropertyChanged("typePropText");
            }
        }
        public string typePropText
        {
            get
            {
                switch (batteryType)
                {
                    case 0:
                        return "6V";
                    case 1:
                        return "12V";
                }
                return "";
            }
        }

        public float capacityProp
        {
            get
            {
                //return (float)(capacity * 1.35);
                return (float)(capacity);
            }
            set
            {
                if (capacity == value)
                {
                    return;
                }
                capacity = value;
                OnPropertyChanged("capacityProp");
            }
        }
        public TimeSpan timeProp
        {
            get { return time; }
            set
            {
                if (time == value)
                {
                    return;
                }
                time = value;
                OnPropertyChanged("timeProp");
            }
        }
        public string  BatNameProp
        {
            get { return batName; }
            set
            {
                if (batName == value)
                {
                    return;
                }
                batName = value;
                OnPropertyChanged("BatNameProp");
            }
        }
        public int numProp
        {
            get { return batNumber; }
            set
            {
                if (batNumber == value)
                {
                    return;
                }
                batNumber = value;
                OnPropertyChanged("numProp");
            }
        }
        public BatteryData(int n)
        { numProp = n; }

        public BatteryData()
        {
            // TODO: Complete member initialization
            time = new TimeSpan(0);
        }
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class AutoCanExecuteCommandWrapper : ICommand
    {
        public ICommand WrappedCommand { get; private set; }

        public AutoCanExecuteCommandWrapper(ICommand wrappedCommand)
        {
            if (wrappedCommand == null)
            {
                throw new ArgumentNullException("wrappedCommand");
            }

            WrappedCommand = wrappedCommand;
        }

        public void Execute(object parameter)
        {
            WrappedCommand.Execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return WrappedCommand.CanExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    class BatteryTesterViewModel : INotifyPropertyChanged
    {
        private MediaPlayer player = new MediaPlayer();
        private bool audioEnable;

        private Dispatcher UIDispatcher;
        volatile bool isListening = false;
        public DmssPort port;
        private StringDictionary portNames = new StringDictionary();
        private Dictionary<Channel_ModeState, string> availableModes = new Dictionary<Channel_ModeState, string>();

        private Byte[] receiveBytes = new Byte[0];
        private Byte[] tmpBytes = new Byte[0];
        private Byte[] sendBytes = new Byte[0];

        private Byte[] responce = { };

         object locker = new object();
        EventWaitHandle go = new AutoResetEvent(false);
        private Thread listenThread;
        private Thread kollektorThread;
        private Thread liveThread;

        public class BatteryTesterTask
        {
            public enum Operations
            {
                ReadData = 1, SetData
            }
            public enum Results
            {
                Error = 0, OK
            }
            protected Operations _operation;
            protected byte[] data;
            public BatteryTesterTask(Operations op, byte[] data)
            {
                _operation = op;
                this.data = data;
            }
            public BatteryTesterTask()
            {

            }
            public virtual Object check(byte[] responce)
            {
                return null;
            }
            public byte[] Data { get { return data; } }
            public Operations Operation { get { return _operation; } }
        }
        public class SetTask : BatteryTesterTask
        {
            public SetTask(int chanel, Channel_ModeState st)
            {
                _operation = Operations.SetData;
                data = new byte[] { (byte)Operations.SetData, (byte)chanel, (byte)st };
            }
            public override Object check(byte[] responce)
            {
                if (responce[0] != (byte)Operations.SetData)
                {
                    throw new Exception("Error. Responce type " + responce[0] + " error");
                }
                if (responce.Length != 2)
                {
                    throw new Exception("Error. Set responce length=" + responce.Length);
                }
                if (responce[1] != (byte)BatteryTesterTask.Results.OK)
                {
                    throw new Exception("Error setting data");
                }
                return null;
            }

        }
        public void EnqueueTask(BatteryTesterTask task)
        {
            lock (locker)
                tasks.Enqueue(task);
            wh.Set();
        }

        public StringDictionary PortNames
        {
            get
            {
                return portNames;
                //=getPortNames(); 
            }
            set
            {
                portNames = value;
                OnPropertyChanged("PortNames");
            }
        }

        public bool AudioEnable {
            get { return audioEnable; }
            set { audioEnable = value;
                if(audioEnable==false)
                {
                    player.Stop();
                }
                OnPropertyChanged("AudioEnable");
            }
        }

        public Dictionary<Channel_ModeState, string> AvailableModes
        {
            get { return availableModes; }
        }
        public ObservableCollection<string> Log { get; set; }

        private BatteryData _selectedBattery;
        public ObservableCollection<BatteryData> BatteryCannels { get; set; }
        private float refValue;
        public float RefValue
        {
            get
            {
                return refValue;
            }
            set
            {
                refValue = value;
                OnPropertyChanged("RefValue");
            }
        }
        private float tempValue;
        public float TempValue
        {
            get
            {
                return tempValue;
            }
            set
            {
                tempValue = (float)((1.43 - (value * 0.00080566)) / 0.0043) + 25;
                OnPropertyChanged("TempValue");
            }
        }
        private bool _linkState;
        public bool LinkState
        {
            get { return _linkState; }
            set
            {
                _linkState = value;
                OnPropertyChanged("LinkState");

            }
        }
        public BatteryData SelectedBattery
        {
            get { return _selectedBattery; }
            set
            {
                _selectedBattery = value;
                OnPropertyChanged("SelectedBattery");
            }
        }

        public BatteryTesterViewModel(Dispatcher disp)
        {
            UIDispatcher = disp;
            Log = new ObservableCollection<string> { "" };
            port = new DmssPort();
            port.ChangeBaudRate(9600);

            player.Open(new Uri(@"Sounds\buzzer.wav", UriKind.Relative));
            player.MediaFailed += (o, args) =>
            {
                MessageBox.Show("Media load Failed!!");
            };

            BatteryCannels = new ObservableCollection<BatteryData>
            {
            new BatteryData {numProp=0,BatNameProp="Батарея 1", voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=1,BatNameProp="Батарея 2",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=2,BatNameProp="Батарея 3",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=3,BatNameProp="Батарея 4",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=4,BatNameProp="Батарея 5",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=5,BatNameProp="Батарея 6",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=6,BatNameProp="Батарея 7",voltageProp=0,capacityProp=0, modeProp=0},
            new BatteryData {numProp=7,BatNameProp="Батарея 8",voltageProp=0,capacityProp=0, modeProp=0}
            };

             availableModes = new Dictionary<Channel_ModeState, string>
            {
                { Channel_ModeState.STANDBY,"СТОП"},
                { Channel_ModeState.CHARGE,"ЗАРЯД"},
                { Channel_ModeState.DISCHARGE,"РАЗРЯД"},
                { Channel_ModeState.AUTO_STANDBY,"АВТО"},
                { Channel_ModeState.FAST_AUTO_STANDBY,"АВТО БЫСТРО"}
 //               { Channel_ModeState.FULL_AUTO_STANDBY,"АВТО FULL"}
            };
            //Комманды
            RestartCollectorCommand = new DelegateCommand(restartCollector);
            ChangeModeCommand = new DelegateCommand(changeMode, param => LinkState);
            UpdatePortsCommand = new DelegateCommand(updatePorts);
            ChangePortCommand = new DelegateCommand(changePort);
            //========
            listenThread = new Thread(listen);
            listenThread.IsBackground = true; // <-- Set your thread to background
            kollektorThread = new Thread(kollector);
            kollektorThread.IsBackground = true; // <-- Set your thread to background
            ListenStart();
            kollektorThread.Start();
            liveThread = new Thread(live);
            liveThread.IsBackground = true; // <-- Set your thread to background
            liveThread.Start();
        }
        ~BatteryTesterViewModel()
        {
            liveThread.Abort();
            EnqueueTask(null);      // Сигнал Потребителю на завершение
            kollektorThread.Join();          // Ожидание завершения Потребителя
            wh.Close();             // Освобождение ресурсов
            addLog("Закрытие программы");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }


        EventWaitHandle wh = new AutoResetEvent(false);
        Queue<BatteryTesterTask> tasks = new Queue<BatteryTesterTask>();

        public void addLog(string str)
        {
            if (UIDispatcher.CheckAccess())
            {
                // This thread has access so it can update the UI thread.
                Log.Add(DateTime.Now.ToLongTimeString() + "  " + str + Environment.NewLine);
                Debug.Send(str);
            }
            else
            {
                // This thread does not have access to the UI thread.
                // Place the update method on the Dispatcher of the UI thread.
                UIDispatcher.BeginInvoke((Action)(() =>
                {
                    addLog(str);
                }));

            }
        }
        public void playSound()
        {
            if (UIDispatcher.CheckAccess())
            {
                // This thread has access so it can update the UI thread.
                try
                {
                    if (AudioEnable)
                    {
                        player.Position = TimeSpan.Zero;
                        player.Play();
                    }
                }
                catch (Exception e)
                {
                }
            }
            else
            {
                // This thread does not have access to the UI thread.
                // Place the update method on the Dispatcher of the UI thread.
                UIDispatcher.BeginInvoke((Action)(() =>
                {
                    playSound();
                }));

            }
        }

        private StringDictionary getPortNames()
        {
            StringDictionary portNames = new StringDictionary();
            portNames.Clear();
            foreach (string str in SerialPort.GetPortNames())
            {
                if (portNames.ContainsKey(str) == false)
                    portNames.Add(str, str);
            }
            //ModifyRegistry mr = new ModifyRegistry();
            RegistryKey ServRk = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services");
            string[] servKeysL0 = ServRk.GetSubKeyNames();
            foreach (string skL0 in servKeysL0)
            {
                RegistryKey rk1 = ServRk.OpenSubKey(skL0);
                string[] servKeysL1 = rk1.GetSubKeyNames();
                foreach (string skL1 in servKeysL1)
                {
                    if (skL1.Contains("Enum"))
                    {
                        RegistryKey rk2 = rk1.OpenSubKey(skL1);
                        string[] vnames2 = rk2.GetValueNames();
                        foreach (string vn2 in vnames2)
                        {
                            if (rk2.GetValue(vn2).ToString().Contains("USB\\V") || rk2.GetValue(vn2).ToString().Contains("SWMUXBUS\\"))
                            {
                                try
                                {
                                    RegistryKey rk4 = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\" + rk2.GetValue(vn2).ToString() + "\\Device Parameters", RegistryKeyPermissionCheck.ReadSubTree);
                                    string[] vnames3 = rk4.GetValueNames();
                                    foreach (string vn3 in vnames3)
                                    {
                                        if (vn3.Contains("PortName"))
                                        {
                                            RegistryKey rk3 = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\" + rk2.GetValue(vn2).ToString(), RegistryKeyPermissionCheck.ReadSubTree);
                                            string fname = rk3.GetValue("FriendlyName").ToString();
                                            int i = fname.ToLower().IndexOf("(com");
                                            if (i > 0) fname = fname.Substring(0, i);
                                            portNames[rk4.GetValue(vn3).ToString()] = rk4.GetValue(vn3).ToString() + " - " + fname;
                                        }
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
            }
            int maxwidth = 1;
            foreach (string s in portNames.Values)
            {
                if (maxwidth < s.Length) maxwidth = s.Length;
            }
            return portNames;
        }

        //запуск в своем потоке
        //ожидает приема данных и выставляет EventWaitHandle
        private void listen()
        {
            try
            {
                port.PortOpen();
                while (isListening && port.isOpen)
                {
                    //receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    receiveBytes = port.GetResponse(300, false).ToArray();
                    if (receiveBytes.Length == 0) continue;
                    lock (locker)
                    {
                        responce = receiveBytes.ToArray();
                        go.Set();
                    }

                }
            }
            catch (Exception e)
            {
                port.PortClose();
            }
            isListening = false;
        }

        //запуск в своем потоке
        //перезапускает коллектор
        private void live()
        {
            while (true)
            {
                Thread.Sleep(2000);
                lock (locker)
                {
                    if (!kollektorThread.IsAlive)
                    {
                        addLog("Trying to restart kollektor");
                        if (RestartCollectorCommand != null)
                        {
                            RestartCollectorCommand.Execute(null);
                        }
                    }
                }
            }
        }

        //запуск в своем потоке
        //постоянный опрос параметоров
        private void kollector()
        {
            const int MaxLink = 5;
            int link = MaxLink;
            int chanN = 0;
            LinkState = false;
            addLog("Коллектор открыт ");
            try
            {
                while (isListening)
                {
                    try
                    {
                        getChannelData(chanN);
                        chanN++;
                        chanN %= 8;
                        if (chanN == 0)
                        {
                            //getTemp();
                            //getRef();
                            wh.WaitOne(1000);
                        }
                        if (LinkState==false)
                        {
                            LinkState = true;
                            addLog("Link enabled");
                        }
                        link = MaxLink;
                    }
                    catch (Exception)
                    {
                        if (link != 0)
                        {
                            if (--link == 0)
                            {
                                LinkState = false;
                                addLog("NoLink");
                                break;
                                //linkLabel.Dispatcher.BeginInvoke((Action)(() => { linkLabel.Foreground = Brushes.Red; }));
                            }
                        }
                    }
                    BatteryTesterTask task = null;

                    lock (locker)
                    {
                        if (tasks.Count > 0)
                        {
                            task = tasks.Dequeue();
                            if (task == null)
                                break;
                        }
                    }
                    try
                    {
                        if (task != null)
                        {
                            doTask(task);
                            //string resp = getCotekData(task);
                            //if (resp != null) addLog(task + resp + " OK");
                            //else addLog(task + "ERROR!!!");
                        }
                        else
                        {
//                            wh.WaitOne(1000);
                        }
                    }
                    catch (Exception e)
                    {
                        addLog(e.Message);
                    }
                }
                addLog("Коллектор закрыт ");
            }
            catch (Exception e)
            {
                // receivingUdpClient.Close(); 
                //linkLabel.Dispatcher.BeginInvoke((Action)(() => { linkLabel.Foreground = Brushes.Black; }));
                addLog(e.Message + "Коллектор закрыт ");
            }
        }

        //Запрос состояния канала
        private void getChannelData(int n)
        {

            lock (locker)
                port.SendCommand(new byte[] { 1, (byte)n });
            if (go.WaitOne(500)) // Ожидаем сигнала начать...
            {
                lock (responce)
                {
                    //test
                    if (responce.Length != 22)
                    {
                        throw new Exception("Error. Responce " + n + " length=" + responce.Length);
                    }
                    if (responce[0] != 1)
                    {
                        throw new Exception("Error. Responce type " + responce[0] + " error");
                    }
                    if (responce[1] != n)
                    {
                        throw new Exception("Responce chanel №" + responce[1] + " error");
                    }
                    BatteryCannels[n].voltageProp = System.BitConverter.ToSingle(responce, 2);
                    BatteryCannels[n].capacityProp = System.BitConverter.ToSingle(responce, 6);
                    if(BatteryCannels[n].modeProp!= (Channel_ModeState)responce[10]&&((Channel_ModeState)responce[10]==Channel_ModeState.READY|| (Channel_ModeState)responce[10] == Channel_ModeState.AUTO_READY))
                    {
                        playSound();
                    }
                    BatteryCannels[n].modeProp = (Channel_ModeState)responce[10];
                    BatteryCannels[n].typeProp = responce[11];

                    BatteryCannels[n].timeProp = TimeSpan.FromSeconds(System.BitConverter.ToInt32(responce, 12));
                    BatteryCannels[n].PwmProp = System.BitConverter.ToInt16(responce, 20);
                }
                return;
            }
            else
            {
                throw new Exception("Request " + n + " error");
            }
        }
        //Запрос температуры
        private void getTemp()
        {

            lock (locker)
                port.SendCommand(new byte[] { 3 });
            if (go.WaitOne(500)) // Ожидаем сигнала начать...
            {
                lock (responce)
                {
                    //test
                    if (responce.Length != 3)
                    {
                        throw new Exception("Error. Responce temp length=" + responce.Length);
                    }
                    if (responce[0] != 3)
                    {
                        throw new Exception("Error. Responce temp type error");
                    }
                    TempValue = System.BitConverter.ToInt16(responce, 1);
                }
                return;
            }
            else
            {
                throw new Exception("Request temp error");
            }
        }
        private void getRef()
        {

            lock (locker)
                port.SendCommand(new byte[] { 4 });
            if (go.WaitOne(500)) // Ожидаем сигнала начать...
            {
                lock (responce)
                {
                    //test
                    if (responce.Length != 3)
                    {
                        throw new Exception("Error. Responce temp length=" + responce.Length);
                    }
                    if (responce[0] != 4)
                    {
                        throw new Exception("Error. Responce temp type error");
                    }
                    RefValue = System.BitConverter.ToInt16(responce, 1);
                }
                return;
            }
            else
            {
                throw new Exception("Request temp error");
            }
        }
        //выполнить таск
        void doTask(BatteryTesterTask task)
        {
            if (task.Operation == BatteryTesterTask.Operations.SetData)
            {
                lock (locker)
                    port.SendCommand(task.Data);
                if (go.WaitOne(500)) // Ожидаем сигнала начать...
                {
                    lock (responce)
                    {
                        //test
                        task.check(responce);
                    }
                    return;
                }
                else
                {
                    throw new Exception("Set Request error");
                }
            }
        }
        private void ListenStop()
        {
            isListening = false;
            listenThread.Abort();
            port.PortClose();
        }
        private void ListenStart()
        {
            if (listenThread == null || (!listenThread.IsAlive))
            {
                listenThread = new Thread(listen);
                listenThread.IsBackground = true; // <-- Set your thread to background
                isListening = true;
                listenThread.Start();
            }
        }

        //Комманды
        public ICommand ChangeModeCommand { get; private set; }
        private void changeMode(object o)
        {
            if (o == null) return;
            try
            {
                if (o.GetType().Equals(typeof(Channel_ModeState)))
                {
                    EnqueueTask(new SetTask(SelectedBattery.numProp, (Channel_ModeState)o));
                    addLog("Try to change mode to " + BatteryData.modeToText((Channel_ModeState)o));
                }
                if (o.GetType().Equals(typeof(Channel_ModeState[])))
                {
                    EnqueueTask(new SetTask(SelectedBattery.numProp, ((Channel_ModeState[])o)[0]));
                    addLog("Try to change mode to "+ BatteryData.modeToText( ((Channel_ModeState[])o)[0]));
                }
            }
            catch (Exception)
            { }
        }
        public ICommand RestartCollectorCommand { get; private set; }
        private void restartCollector(object o)
        {
            if (kollektorThread == null || (!kollektorThread.IsAlive))
            {
                    ListenStop();
                    ListenStart();
                    Thread.Sleep(300);
                kollektorThread = new Thread(kollector);
                kollektorThread.IsBackground = true; // <-- Set your thread to background
                kollektorThread.Start();
            }
            else
            {
                if (isListening == true)
                {
                    ListenStop();
                }
                kollektorThread.Join();
                port.PortClose();
            }
        }
         public ICommand UpdatePortsCommand { get; private set; }
        private void updatePorts(object o)
        {
            PortNames = getPortNames();
        }
        public ICommand ChangePortCommand { get; private set; }
        private void changePort(object o)
        {
            string portName = o as string;
            if (portName != null)
            {
                try
                {
                    if (portName != port.GetPortName() || port.isOpen == false)
                    {
                        ListenStop();
                        port.SetPort((string)o);
                    }
                    addLog("Port changed to"+ (string)o);
                }
                catch (Exception) { }
            }
        }

    }

}
