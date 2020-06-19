using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using SlimDX.DirectInput; //a library that needed to be downloaded
using System.Runtime.InteropServices;
using WindowsInput; //a library that needed to be downloaded

namespace PS2_Backend
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DefineDosDevice(int flags, string DeviceName, string path = "Z:");

        static void Mount(string iso)
        {
            if (!DefineDosDevice(0, iso))
            {
                throw new Win32Exception();
            }
        }

        static void Unmount(string iso)
        {
            if (!DefineDosDevice(2, iso))
            {
                throw new Win32Exception();
            }
        }

        Dictionary<string, string> key_maps = new Dictionary<string, string>();
        Dictionary<string, Dictionary<int, string>> joystick_bindings = new Dictionary<string, Dictionary<int, string>>(); //a dictionary of bindings according to the joystick
        bool keyboard_mode = false; //indicates whether to translated key presses to keyboard presses
        static GamePad[] joysticks; //an array of all the available gamepads
        int charge = 10;

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
            pictureBox1.BackgroundImage = Image.FromFile(@"C:\Users\sabba\Pictures\battery" + (((int)(SystemInformation.PowerStatus.BatteryLifePercent * 10)) * 10).ToString() + ".png");
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
            string Lily = Read_File(@"E:\Emulators\PCSX2\inis_1.4.0\LilyPad.ini");
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
            return Encoding.UTF8.GetString(raw_data, 0, raw_data.Length);
        }
        
        //retreving all available gamepads connected to the computer
        public static GamePad[] GetSticks()
        {
            List<GamePad> sticks = new List<GamePad>();
            DirectInput input = new DirectInput();
            foreach (DeviceInstance device in input.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    Joystick stick = new Joystick(input, device.InstanceGuid);
                    stick.Acquire();

                    foreach (DeviceObjectInstance deviceObject in stick.GetObjects())
                    {
                        if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                        {
                            stick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-10000, 10000);
                        }
                    }
                    sticks.Add(new GamePad(stick));
                }
                catch (DirectInputException){ }
            }
            return sticks.ToArray();
        }

        static int[] ActiveBits(int num) //gets an intager and returns a list that includes the places where a bit is active. 11 => [0,1,3]
        {
            if (num == 0)
            {
                return new int[0];
            }
            int max_pow = (int)(Math.Log(num) / Math.Log(2));
            List<int> bits = new List<int>();
            for (int i = 0; i <= max_pow; i++)
            {
                if ((num & (1 << i)) != 0)
                {
                    bits.Add(i);
                }
            }
            return bits.ToArray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*string Work_dir = Directory.GetCurrentDirectory();
            string data = Read_File(Work_dir + "\\GameIndex.txt");
            //A file which holds all the sony codes for each game, by which you can find the name of the game
            Dictionary<string, string> games = new Dictionary<string, string>();
            foreach (string d in data.Split('\n'))
            {
                //A dictionary that holds the name of the game by its code
                games.Add(key: d.Split(';')[0], value: d.Split(';')[1]);
            }*/

            foreach (DriveInfo curDrive in DriveInfo.GetDrives()) //Checking to see if a disk is inserted
            {
                if (curDrive.DriveType == DriveType.CDRom)
                {
                    if (curDrive.IsReady) //A disk is inserted
                    {
                        //related to knowing which game is inserted
                        /*string game_name = "";
                        foreach (FileInfo f in new DirectoryInfo(curDrive.Name).GetFiles())
                        {
                            string fname = f.Name.Replace(".", "");
                            if (games.ContainsKey(fname))
                            {
                                Console.WriteLine("found: " + games[f.Name.Replace(".", "")]);
                                game_name = games[f.Name.Replace(".", "")];
                                break;
                            }
                        }
                        if (game_name == "")
                        {
                        //TO DO
                        //Search for a PSX game
                        //if found, launch the PSX emulator
                        }
                        */

                        //TO DO!!!!!!!
                        //CHANGE PCSX2 FILE TO REFERENCE CD_DRIVE

                    }
                }
            }
        }

        private void HandleInput(GamePad pad)
        {
            if (keyboard_mode) //map buttons to keys
            {
                Thread t = null;
                foreach (int b in ActiveBits(pad.Get_Buttons_int())) //receiving the buttons pressed
                {
                    switch (key_maps[joystick_bindings[pad.Get_Name()][Translate_Buttons(b)]]) //accessing the joystick's binding, and from which finding out what is the wanted key
                    {
                        case "CROSS":
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                            });
                            break;
                        case "CIRCLE":
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.BACK);
                            });
                            break;
                    }
                }
                if (Translate_Hat(pad.Get_Hat()) > 0)
                {
                    switch (key_maps[joystick_bindings[pad.Get_Name()][Translate_Hat(pad.Get_Hat())]])
                    {
                        case "D_DOWN":
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.DOWN);
                            });
                            break;
                        case "D_UP":
                            t = new Thread(() =>
                            {
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.UP);
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
                //TO DO
                //FIND A CYPHER TO EXIT A GAME WHEN PRESSED.
                string[] cypher = { "TRIANGLE", "R1", "L2", "SELECT" }; //pressing all of those together will open the dialog box
                int[] bits = ActiveBits(pad.Get_Buttons_int());
                bool good = true;
                if (bits.Length == cypher.Length) //no point checking if not all the buttons are pressed or if any others are
                {
                    foreach (int b in bits) //checking if the right keys are pressed
                    {
                        if (!cypher.Contains(key_maps[joystick_bindings[pad.Get_Name()][Translate_Buttons(b)]]))
                        {
                            good = false;
                        }
                    }
                }
                else
                {
                    good = false;
                }
                if (good)
                {
                    keyboard_mode = true;
                    Thread t = new Thread(() => //this thread controlls the file dialog box
                    {
                        OpenFileDialog openFileDialog1;
                        openFileDialog1 = new OpenFileDialog();
                        //TO DO
                        //START THE FILEDIALOG IN THE ROMs LOCATION AND SET IT TO SHOW ISOs

                        //this thread is responsible for moving the highlighted area to the files section
                        //this is because the file dialog will not execute the following lines until exited
                        Thread tabs = new Thread(() =>
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                Thread.Sleep(250); //important for it to register every press
                                InputSimulator input = new InputSimulator();
                                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.TAB);
                            }
                        });

                        tabs.Start();
                        if (openFileDialog1.ShowDialog() == DialogResult.OK) //the user chose a file
                        {
                            string file_name = openFileDialog1.FileName;
                            MessageBox.Show(file_name);
                            keyboard_mode = false;
                            //mount the appropriate file
                            Mount(file_name);
                        }
                    });
                    t.SetApartmentState(ApartmentState.STA); //important for it to work
                    t.IsBackground = true;
                    t.Start();
                    this.Focus();
                }
            }
        }

        static int Translate_Buttons(int button) { return button + (int)Math.Pow(2, 18); }//gets the bit of a button and returns the corrosponding binding

        static int Translate_Hat(int direction) { return direction < 0 ? -1 : (int)((direction / 90) + 3) * 0x1000000 + 0x100000; } //gets the direction of the hat and returns the corrosponding binding

        private void Timer1_Tick(object sender, EventArgs e)
        {
            this.TopMost = true;
            if (joysticks != null)
            {
                for (int i = 0; i < joysticks.Length; i++)
                {
                    joysticks[i].Refresh();
                }
                HandleInput(joysticks[0]);
            }
            PowerStatus pwr = SystemInformation.PowerStatus;
            string file = @"C:\Users\sabba\Pictures\battery";
            if (pwr.PowerLineStatus == PowerLineStatus.Online) //if charging
            {
                file += "C";
            }
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
