using SharpDX.DirectInput;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Linq.Expressions;
using System.Drawing;

namespace Joystick_Reaction_Timer
{
    public partial class Form1 : Form
    {
        String connectController = "Please connect a controller...";
        String ready = "Ready.";
        bool joystickConnected = false;

        Joystick joystick = null;


        Thread backgroundThread;
        Thread backgroundThread1;

        State state = State.Init;

        int buttonRequestTime = 120000;//60000; //MS



        public Form1()
        {
            InitializeComponent();
            backgroundThread = new Thread(new ThreadStart(this.joystickHandler));
            backgroundThread.IsBackground = true;
            backgroundThread.Start();

            backgroundThread1 = new Thread(new ThreadStart(this.programHandler));
            backgroundThread1.IsBackground = true;
            backgroundThread1.Start();

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundThread.Interrupt();
            backgroundThread1.Interrupt();
        }

        private void joystickHandler()
        {
            try
            {
                // Initialize DirectInput
                var directInput = new DirectInput();
                // Find a Joystick Guid
                var joystickGuid = Guid.Empty;

                while (true)
                {
                    if (joystick == null)
                    {
                        foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                            joystickGuid = deviceInstance.InstanceGuid;
                        if (joystickGuid != Guid.Empty)
                        {
                            joystick = new Joystick(directInput, joystickGuid);
                            // Set BufferSize in order to use buffered data.
                            joystick.Properties.BufferSize = 128;
                            // Acquire the joystick
                            joystick.Acquire();
                            joystickConnected = true;
                        }
                    }
                    else
                    {
                        if (!directInput.IsDeviceAttached(joystickGuid))
                        {
                            joystickGuid = Guid.Empty;
                            joystick = null;
                            joystickConnected = false;
                            state = State.Init;
                        }
                        else
                        {
                            joystick.Poll();
                        }
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }
        Stopwatch sw;
        Random randomButton = new Random();

        List<ButtonPress> buttonRequests = new List<ButtonPress>();
        List<ButtonPress> buttonPresses = new List<ButtonPress>();
        List<ButtonPress> correctPresses = new List<ButtonPress>();
        List<ButtonPress> incorrectPresses = new List<ButtonPress>();
        Stopwatch timer;

        private void programHandler()
        {
            try
            {
                while (true)
                {
                    switch (state)
                    {
                        case State.Init:
                            if (joystickConnected)
                            {
                                this.label1.Invoke((MethodInvoker)delegate
                                {
                                    this.label1.Text = ready;
                                    this.button1.Enabled = true;
                                });
                            }
                            else
                            {
                                this.label1.Invoke((MethodInvoker)delegate
                                {
                                    this.label1.Text = connectController;
                                    this.button1.Enabled = false;
                                });
                            }
                            break;
                        case State.step0:
                            timer = Stopwatch.StartNew(); // Start a timer
                            sw = Stopwatch.StartNew(); // Start a timer
                            state = State.step1;
                            break;
                        case State.step1:
                            int delay = 5000;
                            
                            if (sw.ElapsedMilliseconds>delay) // Allow time for the user to pickup the contoller after picking start.
                            {                          
                                state = State.step2;
                                break;
                            }
                            this.label1.Invoke((MethodInvoker)delegate
                            {
                                this.label1.Text = String.Format("Starting in {0} seconds",  ((delay - sw.ElapsedMilliseconds) / 1000.0));
                                this.button1.Enabled = false;
                            });
                            break;
                        case State.step2:
                            this.label1.Invoke((MethodInvoker)delegate
                            {
                                this.label1.Text = String.Format("Press Button: {0}", "--");
                                this.button1.Enabled = false;
                            });
                            state = State.step3;
                            sw = Stopwatch.StartNew(); // Start a timer

                            break;
                        case State.step3:
                            if(sw.ElapsedMilliseconds>400)
                            {
                                state = State.step4;
                            }
                            break;
                        case State.step4:
                            int button = randomButton.Next(0, 13);
                            String buttonName = Enum.GetName(typeof(LogitechF310Buttons), button).Replace("_"," ");
                            this.label1.Invoke((MethodInvoker)delegate
                            {
                                this.label1.Text = String.Format("Press Button: {0}",buttonName);
                                this.button1.Enabled = false;
                            });
                            state = State.step5;
                            buttonRequests.Add(new ButtonPress(DateTime.Now, buttonName));
                            sw = Stopwatch.StartNew(); // Start a timer

                            break;

                        case State.step5:
                            if (timer.ElapsedMilliseconds > buttonRequestTime)
                            {
                                state = State.step6;
                                break;
                            }
                            var datas = joystick.GetBufferedData();
                            foreach (var data in datas)
                            {
                                String offset = data.Offset.ToString();
                                if (offset.Contains("Buttons") && data.Value==128)
                                {
                                    offset = offset.Replace("Buttons","");
                                    String buttonName1 = Enum.GetName(typeof(LogitechF310Buttons), int.Parse(offset)).Replace("_", " ");
                                    buttonPresses.Add(new ButtonPress(DateTime.Now, buttonName1));

                                    if (buttonRequests.Last().buttonName.Equals(buttonName1))
                                    {
                                        correctPresses.Add(new ButtonPress(DateTime.Now, buttonName1));
                                        if (timer.ElapsedMilliseconds<buttonRequestTime)
                                        {
                                            state = State.step2;
                                        }
                                        break;
                                    }
                                    else
                                        incorrectPresses.Add(new ButtonPress(DateTime.Now, buttonName1));
                                }
                                else if(offset.Contains("PointOfViewControllers0"))
                                {
                                    if (data.Value == -1)
                                        break;
                                    String buttonName1 = Enum.GetName(typeof(LogitechF310POV), data.Value).Replace("_", " ");
                                    buttonPresses.Add(new ButtonPress(DateTime.Now, buttonName1));

                                    if (buttonRequests.Last().buttonName.Equals(buttonName1))
                                    {
                                        correctPresses.Add(new ButtonPress(DateTime.Now, buttonName1));
                                        if (timer.ElapsedMilliseconds < buttonRequestTime)
                                        {
                                            state = State.step2;
                                        }
                                        break;
                                    }
                                    else
                                        incorrectPresses.Add(new ButtonPress(DateTime.Now, buttonName1));
                                }
                            }
                            break;
                        case State.step6:
                            this.label1.Invoke((MethodInvoker)delegate
                            {
                                this.label1.Text = String.Format("Complete\n{0} buttons successfully identified\n with {1} incorrect button presses.", correctPresses.Count, incorrectPresses.Count);
                                //this.button1.Text = "Export"; // UNCOMMENT TO ENABLE EXPORTS
                                this.button1.Text = "Clear";    // COMMENT TO ENABLE EXPORTS
                                this.button1.Enabled = true;
                            });
                            state = State.step7;
                            break;

                        case State.step7:
                            break;
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if(button1.Text=="Start")
                state = State.step0;
            if(button1.Text == "Clear")
            {
                // Get the button presses from the actualButtonPress list between the two button presses from the requestedButton list
                buttonPresses.Clear();
                buttonRequests.Clear();
                correctPresses.Clear();
                incorrectPresses.Clear();
                state = State.Init;
                button1.Text = "Start";

            }
            if(button1.Text == "Export")
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 0;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;
                    using (StreamWriter writer = new StreamWriter(fileName))
                    {
                        for (int i = 0; i < buttonRequests.Count; i++)
                        {
                            List<ButtonPress> result = new List<ButtonPress>();
                            if (i + 1 > buttonRequests.Count - 1)
                            {
                                result = buttonPresses.Where(x => x.time > buttonRequests[i].time).ToList();
                            }
                            else
                            {
                                result = buttonPresses.Where(x => x.time > buttonRequests[i].time && x.time < buttonRequests[i + 1].time).ToList();
                            }
                            writer.WriteLine(String.Format("Requested Button: {0}", buttonRequests[i].buttonName));
                            foreach (ButtonPress press in result)
                            {
                                writer.WriteLine(String.Format("Button Press: {0}   After: {1}ms", press.buttonName, ((int)(press.time-buttonRequests[i].time).TotalMilliseconds)));
                            }
                        }
                    }
                    
                    buttonPresses.Clear();
                    buttonRequests.Clear();
                    correctPresses.Clear();
                    incorrectPresses.Clear();
                }
                
                // Get the button presses from the actualButtonPress list between the two button presses from the requestedButton list
                state = State.Init;
                button1.Text = "Start";
            }

        }
        public enum State
        {
            Init = 0,
            step0 = 1,
            step1 = 2,
            step2 = 3,
            step3 = 4,
            step4 = 5,
            step5 = 6,
            step6 = 7,
            step7 = 8
        }

        public enum LogitechF310POV
        {
            DPad_Up = 0,
            DPad_Right = 9000,
            DPad_Down = 18000,
            DPad_Left = 27000
            
        }
        public enum LogitechF310Buttons
        {
            A = 0,
            B = 1,
            X = 2,
            Y = 3,
            Left_Bumper = 4,
            Right_Bumper = 5,
            Back_Button = 6,
            Start_Button = 7,
            Left_Stick = 8,
            Right_Stick = 9,
            DPad_Up = 10,
            DPad_Right = 11,
            DPad_Down = 12,
            DPad_Left = 13
        }

    }
}