using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Management;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using SharpDX.DirectInput; //a library that needed to be downloaded
using System.Runtime.InteropServices;
using WindowsInput; //a library that needed to be downloaded

namespace PS2_Backend
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DefineDosDevice(int flags, string DeviceName, string path = "Z:");

        private static string Work_dir = Directory.GetCurrentDirectory();
        private const string ISO_PATH = @"C:\Users\oz\Documents\PCSX2\isos";
        private const string BATTERY_PATH = @"C:\Users\oz\Pictures\battery\battery";
        private const string LILLYPAD_PATH = @"C:\Users\oz\Documents\PCSX2\inis\LilyPad.ini";
        Dictionary<string, string> key_maps = new Dictionary<string, string>();
        Dictionary<string, Dictionary<int, string>> joystick_bindings = new Dictionary<string, Dictionary<int, string>>(); //a dictionary of bindings according to the joystick
        bool keyboard_mode = false; //indicates whether to translated key presses to keyboard presses
        static GamePad[] joysticks; //an array of all the available gamepads
        int charge = 10;

        static void RunFile()
        {
            using (Process p = new Process())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                string args = "/C " + Work_dir + "\\run.bat";
                startInfo.Arguments = args;
                p.StartInfo = startInfo;
                p.Start();
            }
        }

        static int StringHex_to_Int(string hex) //gets a string that represent a number in Hexadecimal and returns its Decimal intager form
        {
            int intager = 0;
            for (int i = 2; i < hex.Length; i++)
            {
                char h = hex[i];
                if (h > 0x40) // A B C D E F
                {
                    switch (h)
                    {
                        case 'A':
                            intager += 10;
                            break;
                        case 'B':
                            intager += 11;
                            break;
                        case 'C':
                            intager += 12;
                            break;
                        case 'D':
                            intager += 13;
                            break;
                        case 'E':
                            intager += 14;
                            break;
                        case 'F':
                            intager += 15;
                            break;
                    }
                }
                else // 0 1 2 3 4 5 6 7 8 9
                {
                    intager += (int)Char.GetNumericValue(h);
                }
                intager *= 0x10;
            }
            return intager/0x10;
        }

        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - pictureBox1.Width - 15, Screen.PrimaryScreen.Bounds.Height - pictureBox1.Height - 50);
            this.Opacity = 0.35;
            //displaying the battery level
            pictureBox1.BackgroundImage = Image.FromFile(BATTERY_PATH + (((int)(SystemInformation.PowerStatus.BatteryLifePercent * 10)) * 10).ToString() + ".png");
            Application.EnableVisualStyles();
            this.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen; //setting the background transparent
            this.FormBorderStyle = FormBorderStyle.None; //not showing the borders. to blend with the game
            joysticks = GetSticks();
            //populating the dictionary with known LillyPad key bindings
            key_maps.Add("16", "SELECT");
            key_maps.Add("17", "L3");
            key_maps.Add("18", "R3");
            key_maps.Add("19", "START");
            key_maps.Add("20", "D_UP");
            key_maps.Add("21", "D_RIGHT");
            key_maps.Add("22", "D_DOWN");
            key_maps.Add("23", "D_LEFT");
            key_maps.Add("24", "L2");
            key_maps.Add("25", "R2");
            key_maps.Add("26", "L1");
            key_maps.Add("27", "R1");
            key_maps.Add("28", "TRIANGLE");
            key_maps.Add("29", "CIRCLE");
            key_maps.Add("30", "CROSS");
            key_maps.Add("31", "SQUARE");
            key_maps.Add("32", "L_UP");
            key_maps.Add("33", "L_RIGHT");
            key_maps.Add("34", "L_DOWN");
            key_maps.Add("35", "L_LEFT");
            key_maps.Add("36", "R_UP");
            key_maps.Add("37", "R_RIGHT");
            key_maps.Add("38", "R_DOWN");
            key_maps.Add("39", "R_LEFT");
            //getting the corrosponding keys from the LillyPad file
            string Lily = Read_File(LILLYPAD_PATH);
            string[] devices = Lily.Split('[');
            foreach (string d in devices)
            {
                foreach (GamePad j in joysticks)
                {
                    Dictionary<int, string> bindings = new Dictionary<int, string>();
                    if (d.Contains(j.Get_Name())) //found the device that is the used joystick
                    {
                        foreach (string p in d.Split('\n'))
                        {
                            if (p.Contains("Binding")) //got to the binding parts
                            {
                                string bind = p.Split('=')[1];
                                string[] b = bind.Split(',');
                                bindings.Add(StringHex_to_Int(b[0]), b[2].Substring(b[2].IndexOf(' ') + 1));
                            }
                        }
                        joystick_bindings.Add(j.Get_Name(), bindings);
                    }
                }
            }
        }

        //A function that handles all the file reading and provides the string
        static string Read_File(string path)
        {
            FileStream stream = File.Open(path, FileMode.Open);
            byte[] raw_data = new byte[stream.Length];
            stream.Read(raw_data, 0, (int)stream.Length);
            stream.Close();
            return Encoding.UTF8.GetString(raw_data, 0, raw_data.Length);
        }

        static void Write_file(string path, string text, int offset=0, int Len=-1)
        {
            FileStream stream = File.Open(path, FileMode.Open);
            if (Len < 0)
            {
                Len = Encoding.ASCII.GetBytes(text).Length;
            }
            stream.Write(Encoding.ASCII.GetBytes(text), offset, Len);
            stream.Close();
        }
        
        //retreving all available gamepads connected to the computer
        public static GamePad[] GetSticks()
        {
            List<GamePad> sticks = new List<GamePad>();
            DirectInput input = new DirectInput();
            Guid joystickGuid;

            //ittirating through all the DirectInput devices which are Joysticks
            foreach (var deviceInstance in input.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            {
                joystickGuid = deviceInstance.InstanceGuid; //obtaining the referance of this specific Joystick guid
                Joystick joystick = new Joystick(input, joystickGuid);
                joystick.Properties.AxisMode = DeviceAxisMode.Absolute; //a property that specifies that all movments are abolute and not reletieve to each other

                sticks.Add(new GamePad(joystick));
            }
            return sticks.ToArray();
        }

        static int[] ActiveBits(int num) //gets an intager and returns a list that includes the places where a bit is active. 11 => [0,1,3]
        {
            if (num == 0) { return new int[0]; }
            int max_pow = (int)(Math.Log(num) / Math.Log(2));
            List<int> bits = new List<int>();
            for (int i = 0; i <= max_pow; i++)
            {
                if ((num & (1 << i)) != 0) { bits.Add(i); }
            }
            return bits.ToArray();
        }

        static bool Is_Ps2_game(string code)
        {
            string data = Read_File(Work_dir + @"\GameIndexes.txt");
            string[] games = data.Split('\n'); //An array of all of the PS2 games indexes
            return games.Contains(code);
        }

        static bool Is_Music_Disk(string[] files)
        {
            if (files.Length == 0) { return false; }
            foreach(string f in files)
            {
                if (f.Substring(f.Length - 4, 4).ToLower() != ".cda") { return false; }
            }
            return true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(Work_dir + "\\run.bat"))
            {
                FileStream s = File.Create(Work_dir + "\\run.bat");
                s.Close();
            }
            else { File.WriteAllText(Work_dir + "\\run.bat", string.Empty); }
            string text = "@echo off\nset file=--usecd\n\"C:\\Program Files (x86)\\PCSX2\\pcsx2.exe\" %file% --fullscreen --nogui";
            Write_file(Work_dir + "\\run.bat", text);


            foreach (DriveInfo curDrive in DriveInfo.GetDrives()) //Checking to see if a disk is inserted
            {
                if (curDrive.DriveType == DriveType.CDRom)
                {
                    if (curDrive.IsReady) //A disk is inserted
                    {
                        bool exists = false;
                        string[] files = Directory.GetFiles(curDrive.RootDirectory.FullName);
                        foreach (string f in files) //checks if a disk is a PS2 game
                        {
                            string file = f.Split('\\')[f.Split('\\').Length - 1];
                            if (Is_Ps2_game(file))
                            {
                                exists = true;
                                RunFile();
                                break;
                            }
                        }
                        if (!exists)
                        {
                            if (!Is_Music_Disk(files))
                            {
                                AutoClosingMessageBox.Show("The disk inserted is not a PS2 game!", 5000, "MessageBox", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            RunFile();
                            //TODO
                            //check if is a PSX game and open emulator accordingly.
                        }
                    }
                    else { RunFile(); }
                }
                if (curDrive.DriveType == DriveType.Removable)
                {
                    //TODO
                    /*check if the drive has folders "PS2_ISOs" and "PSX_CUEs"
                     *transfer all games to the appropriate folders on the computer
                     */
                }
            }
        }

        private void HandleInput(GamePad pad)
        {
            if (keyboard_mode) //map buttons to keys
            {
                Thread t = null;
                foreach (int b in ActiveBits(pad.Get_Buttons())) //receiving the buttons pressed
                {
                    switch (key_maps[joystick_bindings[pad.Get_Name()][Translate_Buttons(b)]]) //accessing the joystick's binding, and from which finding out what is the wanted key
                    {
                        case "CROSS": //ENTER
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                            });
                            break;
                        case "CIRCLE": //BACKSPACE
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.BACK);
                            });
                            break;
                        case "TRIANGLE": //ESC
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.ESCAPE);
                            });
                            break;
                    }
                }
                if (Translate_Hat(pad.Get_Hat()) > 0) //if hat is untouched, hat = -1
                {
                    switch (key_maps[joystick_bindings[pad.Get_Name()][Translate_Hat(pad.Get_Hat())]])
                    {
                        case "D_DOWN": //DOWN ARROW
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.DOWN);
                            });
                            break;
                        case "D_UP": //UP ARROW
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.UP);
                            });
                            break;
                        case "D_LEFT": //SHIFT TAB
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.SHIFT);
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.TAB);
                                input.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.SHIFT);
                            });
                            break;
                        case "D_RIGHT": //TAB
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.TAB);
                            });
                            break;
                    }

                }
                if (null != t)
                {
                    t.Start();
                    t.Join();
                }
            }
            else //look for cypher
            {
                string[] f_select_cy = { "TRIANGLE", "R1", "L2", "SELECT" }; //pressing all of those together will open the dialog box
                string[] exit_game_cy = { "L1", "L3", "CROSS", "SQUARE" }; //pressing all of these together will close the game
                string[][] cyphers = new string[2][];
                cyphers[0] = f_select_cy;
                cyphers[1] = exit_game_cy;
                int[] bits = ActiveBits(pad.Get_Buttons());
                foreach (string[] cypher in cyphers)
                {
                    bool good = true;
                    if (bits.Length == cypher.Length) //no point checking if not all the buttons are pressed or if more are
                    {
                        foreach (int b in bits) //checking if the right keys are pressed
                        {
                            if (!cypher.Contains(key_maps[joystick_bindings[pad.Get_Name()][Translate_Buttons(b)]]))
                            {
                                good = false;
                                break;
                            }
                        }
                    }
                    else { good = false; }
                    if (good) //a cypher is found
                    {
                        if (cypher == f_select_cy) { File_Cypher(); }
                        else { if (cypher == exit_game_cy) { Exit_Game(); } }
                    }
                }
            }
        }
        private void Exit_Game()
        {
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle) && process.ProcessName.ToLower() == "pcsx2")
                {
                    if (!process.MainWindowTitle.Contains("| DVD "))
                    {
                        try { process.Kill(); }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                    }
                }
            }
        }
        private void File_Cypher() //a func that handles the opperations from when the cypher is pressed untill a file is loaded or canceled
        {
            keyboard_mode = true;
            string file_name = "";
            AutoClosingMessageBox.Show("Please wait untill next message", 1000);
            Thread t = new Thread(() => //this thread controlls the file dialog box
            {
                OpenFileDialog openFileDialog1;
                openFileDialog1 = new OpenFileDialog();
                openFileDialog1.InitialDirectory = ISO_PATH;
                openFileDialog1.Title = "Open File";

                //this thread is responsible for moving the highlighted area to the files section
                //this is because the file dialog will not execute the following lines until exited
                Thread tabs = new Thread(() =>
                {
                    IntPtr mbWnd = AutoClosingMessageBox.FindWindow("#32770", "Open File");
                    while (mbWnd == IntPtr.Zero) { mbWnd = AutoClosingMessageBox.FindWindow("#32770", "Open File"); }
                    Thread.Sleep(300);
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine("tab");
                        Thread.Sleep(250); //important for it to register every press
                        InputSimulator input = new InputSimulator();
                        input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.TAB);
                    }
                });
                tabs.Name = "tabs"; 
                tabs.Start();
                DialogResult res = openFileDialog1.ShowDialog();
                if (res == DialogResult.OK) //the user chose a file
                {
                    file_name = openFileDialog1.FileName;
                    Console.WriteLine(file_name);
                    string message = "Are you sure you want to load " + Simplify_Name(file_name) + "?";
                    DialogResult yORn = MessageBox.Show(message, "YES/NO", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    if (DialogResult.No == yORn)
                    {
                        file_name = "";
                        File_Cypher(); //prompts the user to choose a different file
                    }
                    else
                    { //mount and run the appropriate file
                        File.WriteAllText(Work_dir + "\\run.bat", string.Empty);
                        string text = "@echo off\nset file=\"" + file_name + "\"\n\"C:\\Program Files (x86)\\PCSX2\\pcsx2.exe\" %file% --fullscreen --nogui";
                        Write_file(Work_dir + "\\run.bat", text);
                        Exit_Game();
                        RunFile();
                    }
                }
                keyboard_mode = false;
            });
            t.SetApartmentState(ApartmentState.STA); //important for it to work
            t.IsBackground = true;
            t.Start();
        }

        static string Simplify_Name(string path)
        {
            string[] parts = path.Split('\\');
            return parts[parts.Length - 1];
        }

        static int Translate_Buttons(int button) { return button + (int)Math.Pow(2, 18); }//gets the bit of a button and returns the corrosponding binding

        static int Translate_Hat(int direction) { return direction < 0 ? -1 : (int)((direction / 90) + 3) * 0x1000000 + 0x100000; } //gets the direction of the hat and returns the corrosponding binding

        private void Timer1_Tick(object sender, EventArgs e)
        {
            this.TopMost = true;
            if (joysticks != null)
            {
                for (int i = 0; i < joysticks.Length; i++) { joysticks[i].Refresh(); }
                HandleInput(joysticks[0]);
            }
            PowerStatus pwr = SystemInformation.PowerStatus;
            string file = BATTERY_PATH;
            if (pwr.PowerLineStatus == PowerLineStatus.Online) { file += "C"; } //if charging
            if (charge != ((int)(pwr.BatteryLifePercent * 10)) * 10)
            {
                charge = ((int)(pwr.BatteryLifePercent * 10)) * 10;
                file += charge.ToString();
                file += ".png";
                pictureBox1.BackgroundImage = Image.FromFile(file);
            }
        }
    }
}
