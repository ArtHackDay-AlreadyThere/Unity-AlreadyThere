using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrefsGUI;

public class DebugMenu : MonoBehaviour {

    public BlockManager blockManager;
    public Etherscan etherscan;
    public Canvas canvas;

    bool isDebugMenu = false;

    bool isCalibrationpattern = false;

    // Use this for initialization
    void Start () {
        UpdateCanvas(isCalibrationpattern);
    }
	
	// Update is called once per frame
	void Update () {
        // デバッグメニュー
        if (Input.GetKeyDown(KeyCode.D))
        {
            isDebugMenu = !isDebugMenu;
            if (!isDebugMenu)
            {
                Prefs.Save();
            }
        }
    }

    void UpdateCanvas(bool isCalib)
    {
        isCalibrationpattern = isCalib;
        canvas.gameObject.SetActive(isCalib);
    }

    Rect _windowRect = new Rect(10, 10, 500, 500);
    private void OnGUI()
    {
        if (isDebugMenu)
        {
            _windowRect =
                GUILayout.Window(
                    GetHashCode(), _windowRect, (id) =>
                    {
                        using (var v = new GUILayout.VerticalScope(GUILayout.MinWidth(300f)))
                        {
                            blockManager.DebugMenu();
                            etherscan.DebugMenu();

                            bool isCalib = GUILayout.Toggle(isCalibrationpattern, "Disp CalibrationPattern");
                            if(isCalib != isCalibrationpattern)
                            {
                                UpdateCanvas(isCalib);
                            }
                            
                        }

                        GUI.DragWindow();
                    },
            "Settings");
        }
    }
}
