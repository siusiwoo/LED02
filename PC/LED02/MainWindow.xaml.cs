using LED02.Communication;
using LED02.Models;
using LED02.Packet;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace LED02
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        int id = 0;
        int logCount = 0;
        string logPath = "";
        string jsonPath = "settings.json";


        List<byte> data = new List<byte>();

        DateTime lastBlink;



        SerialLink serialLink = new SerialLink();
        CancellationTokenSource? cts;

        TestSettings testSettings = new TestSettings();



        List<string> logLines = new List<string>();
        enum State : byte
        {
            wait = 1, work = 2, end = 3
        }
        State state = State.wait;
        public MainWindow()
        {
            InitializeComponent();
            LoadSettingsToTextBoxes();

            Directory.CreateDirectory("logs");
            serialLink.PacketReceived += HandlePacket;
            if (serialLink.Connect("COM3", 115200))
            {
                serialLink.StartThreads();
            }
            else
            {
                MessageBox.Show($"연결 중 오류가 발생했습니다: {serialLink.lastError}");
            }

        }
        void LoadSettingsToTextBoxes()
        {
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    testSettings = JsonSerializer.Deserialize<TestSettings>(json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"설정 파일을 읽는 중 오류가 발생했습니다: {ex.Message}");

                }
                aMinTextBox.Text = testSettings?.AMin.ToString();
                aMaxTextBox.Text = testSettings?.AMax.ToString();
                bMinTextBox.Text = testSettings?.BMin.ToString();
                bMaxTextBox.Text = testSettings?.BMax.ToString();
                loopCountTextBox.Text = testSettings?.LoopCount.ToString();
                loopIntervalTextBox.Text = testSettings?.Interval.ToString();
            }

        }
        string Operator(int op)
        {
            string oper = "";
            if (op == 1)
            {
                oper = "+";
            }
            else if (op == 2)
            {
                oper = "-";
            }
            else if (op == 3)
            {
                oper = "*";
            }
            else if (op == 4)
            {
                oper = "/";
            }
            else if (op == 5)
            {
                oper = "%";
            }
            return oper;
        }
        public void HandlePacket(byte[] data)
        {



            byte len = data[2];
            switch (data[1])
            {
                case Protocol.ACK:
                    break;
                case Protocol.CMD_CALC:

                    try
                    {

                        int start = 3;
                        long a = PacketCodec.ReadValue(data, ref start);
                        byte op = data[start++];

                        long b = PacketCodec.ReadValue(data, ref start);
                        long result = PacketCodec.ReadValue(data, ref start); // result

                        string oper = Operator(op);
                        var row = new { A = a, B = b, Operation = oper, Result = result };
                        logCount++;
                        logLines.Add($"{id},{logCount},{a},{oper},{b},{result}");

                        Dispatcher.BeginInvoke(() =>
                        {
                            CalculationHistory.Items.Add(row);//여기에 한번에 다 추가하는게 아니라 row로 묶어서 사용해서 리팩터링 함
                            CalculationHistory.ScrollIntoView(row);//ScrollIntoView 최신줄 보이게하는 기능

                        });

                        File.AppendAllLines(logPath, logLines, Encoding.UTF8);
                        logLines.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일을 닫아주세요.: {ex.Message}");
                    }

                    break;
                case Protocol.Status_READ:
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (data[3] == (byte)State.end)
                        {
                            Yellow.Fill = Brushes.White;
                        }
                        else if (data[3] == (byte)State.work)
                        {
                            Yellow.Fill = Brushes.Yellow;
                        }
                        else if (DateTime.Now - lastBlink >= TimeSpan.FromMilliseconds(250) && data[3] == (byte)State.wait)
                        {
                            if (Yellow.Fill == Brushes.Yellow)
                            {
                                Yellow.Fill = Brushes.White;
                            }
                            else
                            {
                                Yellow.Fill = Brushes.Yellow;
                            }


                            lastBlink = DateTime.Now;
                        }
                    });
                    if (data[3] == (byte)State.end)
                    {

                        Dispatcher.BeginInvoke(() =>
                        {
                            startButton.IsChecked = false;
                            serialLink.SendPacket(new byte[0], Protocol.CMD_DONE, 0);
                            startButton.Content = "시작";



                        });

                    }
                    break;
                case Protocol.NAK:
                    break;

            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {

            string aMax = aMaxTextBox.Text;
            string aMin = aMinTextBox.Text;
            string bMax = bMaxTextBox.Text;
            string bMin = bMinTextBox.Text;
            string interval = loopIntervalTextBox.Text;
            string loopcount = loopCountTextBox.Text;
            CalculationHistory.Items.Clear();

            if (!string.IsNullOrEmpty(aMin) && !string.IsNullOrEmpty(aMax) && !string.IsNullOrEmpty(bMin) && !string.IsNullOrEmpty(bMax) && !string.IsNullOrEmpty(interval) && !string.IsNullOrEmpty(loopcount))
            {
                int aMinValue = 0;
                int aMaxValue = 0;
                int bMinValue = 0;
                int bMaxValue = 0;
                int intervalValue = 0;
                int loopcountValue = 0;
                try
                {
                    aMinValue = int.Parse(aMin);
                    aMaxValue = int.Parse(aMax);
                    bMinValue = int.Parse(bMin);
                    bMaxValue = int.Parse(bMax);
                    intervalValue = int.Parse(interval);
                    loopcountValue = int.Parse(loopcount);
                }
                catch (OverflowException ex)
                {
                    MessageBox.Show($"int 범위를 넘었습니다.: {ex.Message}");
                    startButton.IsChecked = false;
                    return;
                }
                catch (FormatException ex)
                {
                    MessageBox.Show($"숫자를 바르게 입력해 주세요.: {ex.Message}");
                    startButton.IsChecked = false;
                    return;
                }


                if (loopcountValue > 3000 || intervalValue > 30000 || loopcountValue < 1 || intervalValue < 1)
                {
                    MessageBox.Show("횟수는 3000이하로 간격은 30초 이하로 진행해주세요");
                    startButton.IsChecked = false;
                    return;
                }
                if (bMaxValue < bMinValue)
                {
                    MessageBox.Show("bMaxValue는 bMinValue보다 커야 합니다.");
                    startButton.IsChecked = false;
                    return;
                }
                if (aMaxValue < aMinValue)
                {
                    MessageBox.Show("aMaxValue는 aMinValue보다 커야 합니다.");
                    startButton.IsChecked = false;
                    return;
                }
                id++;
                logCount = 0;
                logPath = Path.Combine("logs", $"{DateTime.Now:yyyy-MM-dd_HH_mm_ss}.csv");
                File.WriteAllText(logPath, "ID,LogCount,A,Operation,B,Result" + Environment.NewLine, Encoding.UTF8);
                startButton.Content = "정지";





                data.Clear();
                PacketCodec.AddInt(data, aMinValue);
                PacketCodec.AddInt(data, bMinValue);
                PacketCodec.AddInt(data, aMaxValue);
                PacketCodec.AddInt(data, bMaxValue);
                PacketCodec.AddInt(data, loopcountValue);
                PacketCodec.AddInt(data, intervalValue);
                serialLink.SendPacket(data.ToArray(), Protocol.CMD_START, data.Count);

                return;

            }
            else
            {

                MessageBox.Show("모든 값을 입력해주세요.");
                startButton.IsChecked = false;
            }

        }



        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingsToTextBoxes();
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string aMax = aMaxTextBox.Text;
            string aMin = aMinTextBox.Text;
            string bMax = bMaxTextBox.Text;
            string bMin = bMinTextBox.Text;
            string interval = loopIntervalTextBox.Text;
            string loopcount = loopCountTextBox.Text;
            int aMinValue = int.TryParse(aMin, out aMinValue) ? aMinValue : 0;
            int aMaxValue = int.TryParse(aMax, out aMaxValue) ? aMaxValue : 0;
            int bMinValue = int.TryParse(bMin, out bMinValue) ? bMinValue : 0;
            int bMaxValue = int.TryParse(bMax, out bMaxValue) ? bMaxValue : 0;
            int intervalValue = int.TryParse(interval, out intervalValue) ? intervalValue : 0;
            int loopcountValue = int.TryParse(loopcount, out loopcountValue) ? loopcountValue : 0;
            testSettings.AMin = aMinValue;
            testSettings.AMax = aMaxValue;
            testSettings.BMin = bMinValue;
            testSettings.BMax = bMaxValue;
            testSettings.LoopCount = loopcountValue;
            testSettings.Interval = intervalValue;
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(testSettings), Encoding.UTF8);
        }

        private void ToggleButton_UnChecked(object sender, RoutedEventArgs e)
        {
            startButton.Content = "시작";
            serialLink.SendPacket(new byte[0], Protocol.CMD_STOP, 0);
        }
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
        void SignedNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var senderTextBox = sender as TextBox;
            if (e.Text == "-")
            {
                if (senderTextBox.CaretIndex != 0 || senderTextBox.Text.Contains('-'))
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = !e.Text.All(char.IsDigit);
            }



        }
    }
}