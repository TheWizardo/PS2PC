using System;
using System.Runtime.InteropServices;
using SlimDX.DirectInput;
using System.Collections.Generic;

public class GamePad
{
    Joystick stick;
    int Ly;
    int Lx;
    int Rx;
    int Ry;
    int hat;
    bool[] buttons = new bool[12];
    string name;

    public GamePad(Joystick Stick)
    {
        this.stick = Stick;
        this.name = Stick.Properties.InstanceName;
        Refresh();
    }

    public void Refresh()
    {
        JoystickState state = stick.GetCurrentState();

        this.Ly = state.Y;
        this.Lx = state.X;
        this.Rx = state.Z;
        this.Ry = state.RotationZ;
        this.hat = state.GetPointOfViewControllers()[0] == -1 ? -1 : state.GetPointOfViewControllers()[0] / 100;
        bool[] button = state.GetButtons();
        for (int i = 0; i < this.buttons.Length; i++)
            this.buttons[i] = button[i];
    }

    public int[] Get_Left_Stick()
    {
        int[] left = { this.Lx, this.Ly };
        return left;
    }
    public int[] Get_Right_Stick()
    {
        int[] right = { this.Rx, this.Ry };
        return right;
    }
    public int Get_Hat() { return this.hat; }
    public bool[] Get_Buttons() { return this.buttons; }
    public int Get_Buttons_int()
    {
        return bool_to_flags(Get_Buttons());
    }
    public int bool_to_flags(bool[] b)
    {
        int f = 0;
        for (int i = 0; i < b.Length; i++)
            f += b[i] ? (int)Math.Pow(2, i) : 0;
        return f;
    }
    public string Get_Name() { return this.name; }
}
