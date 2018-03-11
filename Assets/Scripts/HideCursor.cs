using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideCursor : MonoBehaviour {

    public float disableWaitTime = 3;
    public float moveEnableDistance = 3f;

    protected float duration = 0;
    protected Vector2 oldMousePosition;
    
	// Use this for initialization
	void Start () {
        duration = disableWaitTime;
    }

    // Update is called once per frame
    void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0)
        {
            Cursor.visible = false;
        }

        Vector2 mousePosition = Input.mousePosition;
        float distance = (oldMousePosition - mousePosition).magnitude;
        if(distance >= moveEnableDistance)
        {
            Cursor.visible = true;
            duration = disableWaitTime;
        }
        oldMousePosition = mousePosition;
    }
}
