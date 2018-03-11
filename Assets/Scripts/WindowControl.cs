using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class WindowControl : MonoBehaviour {

    #region public
    public bool hideTitleBar = true;
    public int width = 1920;
    public int height = 1080;

    public bool isSavePosition = true;
    #endregion

    #region private
    private string windowName;
    private static IntPtr windowHandle = IntPtr.Zero;

    private int x = 0;
    private int y = 0;

    private int oldX;
    private int oldY;
    #endregion

    // assorted constants needed
    public static int GWL_STYLE = -16;
    public static int WS_CHILD = 0x40000000; //child window
    public static int WS_BORDER = 0x00800000; //window with border
    public static int WS_DLGFRAME = 0x00400000; //window with double border but no title
    public static int WS_CAPTION = WS_BORDER | WS_DLGFRAME; //window with a title bar

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public static implicit operator Vector2(POINT p)
        {
            return new Vector2(p.X, p.Y);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(System.String className, System.String windowName);

    // Sets window attributes
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Gets window attributes
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public static void SetPosition(int x, int y, int width = 0, int height = 0, bool isSave=true)
    {
        if (windowHandle != IntPtr.Zero)
        {
            SetWindowPos(windowHandle, 0, x, y, width, height, width * height == 0 ? 1 : 0);
            if (isSave)
            {
                PlayerPrefs.SetInt("WindowPosX", x);
                PlayerPrefs.SetInt("WindowPosY", y);
            }
        }
    }

    public static Vector2 GetWindowMousePosition()
    {
        POINT pos;

        Vector2 pos2 = Vector2.zero;

        if(GetCursorPos(out pos))
        {
            pos2.x = pos.X;
            pos2.y = pos.Y;
        }

        return pos2;
    }
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    private void Awake()
    {
        windowName = Application.productName;
        windowHandle = FindWindow(null, windowName);
        Debug.Log("windowName " + windowName);

        if (hideTitleBar)
        {
            int style = GetWindowLong(windowHandle, GWL_STYLE);
            SetWindowLong(windowHandle, GWL_STYLE, (style & ~WS_CAPTION));
        }
        if (isSavePosition)
        {
            x = PlayerPrefs.GetInt("WindowPosX");
            y = PlayerPrefs.GetInt("WindowPosY");
        }
        SetPosition(x, y, width, height);
    }

    void Update () {

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            y++;
            SetPosition(x, y, width, height);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            y--;
            SetPosition(x, y, width, height);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            x--;
            SetPosition(x, y, width, height);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            x++;
            SetPosition(x, y, width, height);
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            x = 0;
            y = 0;
            SetPosition(x, y, width, height);
        }

        // Drag Start
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = GetWindowMousePosition();
            oldX = (int)pos.x;
            oldY = (int)pos.y;
        }

        // Drag
        if (Input.GetMouseButton(0))
        {
            Vector2 pos = GetWindowMousePosition();
            int nowX = (int)pos.x;
            int nowY = (int)pos.y;
            if((nowX != oldX)||(nowY != oldY))
            {
                x += nowX - oldX;
                y += nowY - oldY;
                SetPosition(x, y, width, height);
                oldX = nowX;
                oldY = nowY;
            }
        }
    }
#endif

}
