using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_Direction { Up, Down, Left, Right }

public enum E_DockPosition
{
    Left,
    Top,
    Right,
    Bottom
}
public enum E_BrushOption
{
    Type,
    Setting,
    Color,
    Effect
}
public enum E_BrushType
{
    One, Box , Capsule
}

public enum E_CreateLayer
{
    New,
    Clone,
}

public enum E_KeyInput
{
    None,
    Ctrl,
    Shift,
}

public enum E_DRAWING
{
    Select,
    Draw,
    Erase,
}
