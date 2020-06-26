using System;
using SharpDX.DirectInput;

public class GamePad
{
    Joystick joy;
    float Ly;
    float Lx;
    float Rx;
    float Ry;
    int hat;
    bool[] bool_buttons = new bool[12];
    string name;
    enum Stick
    {
        R,
        L
    }

    public GamePad(Joystick JoyStick)
    {
        this.joy = JoyStick;
        this.name = JoyStick.Properties.InstanceName;
        Refresh();
    }

    public void Refresh()
    {
        JoystickState state = new JoystickState();
        joy.Acquire(); //acquiring information about the joystick
        joy.Poll(); //pulling it
        joy.GetCurrentState(ref state); //storing it in 'state'

         this.Ly = state.Y > 0 ? ((state.Y + 1) / 65536f) - 0.5f : -0.5f;
        this.Lx = state.X > 0 ? ((state.X + 1) / 65536f) - 0.5f : -0.5f;
        this.Rx = state.Z > 0 ? ((state.Z + 1) / 65536f) - 0.5f : -0.5f;
        this.Ry = state.RotationZ > 0 ? ((state.RotationZ + 1) / 65536f) - 0.5f : -0.5f;
        this.hat = state.PointOfViewControllers[0] == -1 ? -1 : state.PointOfViewControllers[0] / 100;
        bool[] button = state.Buttons;
        for (int i = 0; i < this.bool_buttons.Length; i++)
        {
            this.bool_buttons[i] = button[i];
        }
    }

    public float[] Get_Left_Stick() { return Get_Stick(Stick.L); }//[X cordinations, Y cordinations]
    public float[] Get_Right_Stick() { return Get_Stick(Stick.R); }//[X cordinations, Y cordinations]
    private float[] Get_Stick(Stick which)
    {
        float[] cord = { this.Rx, this.Ry };
        if (which == Stick.L)
        {
            cord[0] = this.Lx;
            cord[1] = this.Ly;
        }
        return cord;
    }
    public int Get_Hat() { return this.hat; } //the number indicates the clock-wise azimuth of the vector
    public int Get_Buttons() //returns a 12 bit int that represent the buttons. each bit is a button
    {
        int f = 0;
        for (int i = 0; i < this.bool_buttons.Length; i++)
            f += this.bool_buttons[i] ? (int)Math.Pow(2, i) : 0;
        return f;
    }
    public string Get_Name() { return this.name; }
}
