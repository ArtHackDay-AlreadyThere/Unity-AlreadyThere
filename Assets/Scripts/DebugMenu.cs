using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrefsGUI;

public class DebugMenu : MonoBehaviour {

    public BlockManager blockManager;
    public Etherscan etherscan;

    bool isDebugMenu = false;

    // Use this for initialization
    void Start () {
		
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
                        }

                        GUI.DragWindow();
                    },
            "Settings");
        }
    }
}
