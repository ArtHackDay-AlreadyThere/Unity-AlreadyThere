using UnityEngine;
using UnityEngine.Networking;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

public class Etherscan : MonoBehaviour {
	[System.Serializable]
	public class BlockNumberReturn {
		public string jsonrpc;
		public string id;
		public string result;
	}

	[System.Serializable]
	public class GetBlockByNumberReturn {
		public string jsonrpc;
		public string id;
		public Block result;
	}

	[System.Serializable]
	public class Block {
		public string number;
		public Transaction[] transactions;
	}

	[System.Serializable]
	public class Transaction {
		public string blockHash;
		public string from;
		public string to;
		public string value;
	}

	public string apiKey = "YourApiToken";

	public List<Block> blocks;

	// Use this for initialization
	IEnumerator Start () {
		UnityWebRequest request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=" + apiKey);
		yield return request.SendWebRequest();

		var blockNumberResult = JsonUtility.FromJson<BlockNumberReturn>(request.downloadHandler.text);

		request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_getBlockByNumber&apikey=" + apiKey + "&boolean=true&tag=" + blockNumberResult.result);
		yield return request.SendWebRequest();

		var getBlockByNumberReturn = JsonUtility.FromJson<GetBlockByNumberReturn>(request.downloadHandler.text);
		blocks.Add(getBlockByNumberReturn.result);
	}
	
}
