using UnityEngine;

using System.Collections.Generic;

using MiniJSON;
using WebSocketSharp;

public class BlockInfo : MonoBehaviour {
	private WebSocket webSocket;

	// Use this for initialization
	void Start () {
		webSocket = new WebSocket("wss://ws.blockchain.info/inv");

        webSocket.OnMessage += (sender, e) => {
			var data = Json.Deserialize(e.Data) as Dictionary<string, object>;

            Debug.Log(data["op"]);
		};

		webSocket.Connect();

		webSocket.Send("{\"op\":\"unconfirmed_sub\"}");
		webSocket.Send("{\"op\":\"blocks_sub\"}");
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnApplicationQuit() {
		webSocket.Close();
	}
}
