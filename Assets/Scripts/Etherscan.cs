using UnityEngine;
using UnityEngine.Networking;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine.Events;

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
		public GetBlockByNumberReturnResult result;
	}

	[System.Serializable]
	public class GetBlockByNumberReturnResult {
        public string hash;
		public string number;
		public GetBlockByNumberReturnResultTransaction[] transactions;
	}

	[System.Serializable]
	public class GetBlockByNumberReturnResultTransaction {
		public string from;
		public string to;
		public string value;
	}

    [System.Serializable]
    public class BlockDataEvent : UnityEvent<BlockData> { }

    //public string apiKey = "YourApiToken";

    public string apiKeyFileName = "apikey.txt";

    /// <summary>
    /// 起動時に読み込む数
    /// </summary>
    public int initialLoadingCount = 20;

    public BlockDataEvent OnRecieve;

	public GameObject prefab;

    string apiKey = null;

    // Use this for initialization
    IEnumerator Start () {

        // APIKey読み込み
        yield return LoadApiKey(apiKeyFileName);

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Fail Load Apikey");
            //yield break;
            apiKey = "YourApiToken";
        }

        apiKey = "YourApiToken";    // test

        UnityWebRequest request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=" + apiKey);
		yield return request.SendWebRequest();

		var blockNumberReturn = JsonUtility.FromJson<BlockNumberReturn>(request.downloadHandler.text);

		var currentBlockNumber = blockNumberReturn.result;
		ulong currentBlockNumberUInt64 = System.Convert.ToUInt64(currentBlockNumber.Substring(2), 16);

        // 起動時の読み込み処理
		for (ulong blockNumber = currentBlockNumberUInt64 - (ulong)initialLoadingCount + 1; blockNumber <= currentBlockNumberUInt64; blockNumber++) {
			request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_getBlockByNumber&apikey=" + apiKey + "&boolean=true&tag=0x" + blockNumber.ToString("X"));
			yield return request.SendWebRequest();

			var getBlockByNumberReturn = JsonUtility.FromJson<GetBlockByNumberReturn>(request.downloadHandler.text);
			GenerateBlock(getBlockByNumberReturn.result);
		}

		while (true) {
			request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=" + apiKey);
			yield return request.SendWebRequest();

			blockNumberReturn = JsonUtility.FromJson<BlockNumberReturn>(request.downloadHandler.text);
			if (!currentBlockNumber.Equals(blockNumberReturn.result)) {
				request = UnityWebRequest.Get("https://api.etherscan.io/api?module=proxy&action=eth_getBlockByNumber&apikey=" + apiKey + "&boolean=true&tag=" + blockNumberReturn.result);
				yield return request.SendWebRequest();

				var getBlockByNumberReturn = JsonUtility.FromJson<GetBlockByNumberReturn>(request.downloadHandler.text);
				GenerateBlock(getBlockByNumberReturn.result);
			}

			yield return new WaitForSeconds(5.0f);
		}
	}

	private void GenerateBlock(GetBlockByNumberReturnResult result) {

        BlockData block = new BlockData();
        block.Hash = result.hash;
        block.Number = System.Convert.ToUInt64(result.number.Substring(2), 16);
        List<BlockData.Transaction> transactions = new List<BlockData.Transaction>(result.transactions.Length);
        for (int i = 0; i < result.transactions.Length; i++)
        {
            var transaction = new BlockData.Transaction();

            transaction.From = result.transactions[i].from;
            transaction.To = result.transactions[i].to;

            string hex = result.transactions[i].value.Substring(2).PadLeft(32, '0');
            transaction.Value = ((double)System.Convert.ToUInt64(hex.Substring(0, 16), 16) * 1.844674407370955e19 + (double)System.Convert.ToUInt64(hex.Substring(16, 16), 16)) / 1000000000000000000.0;

            transactions.Add(transaction);
        }

        block.Transactions = transactions;
        OnRecieve.Invoke(block);

  //      GameObject obj = Instantiate(prefab);

		//Block block = obj.AddComponent<Block>();
		//block.Number = System.Convert.ToUInt64(result.number.Substring(2), 16);

		//List<Block.Transaction> transactions = new List<Block.Transaction>(result.transactions.Length);
		//for (int i = 0; i < result.transactions.Length; i++) {
		//	var transaction = new Block.Transaction();

		//	transaction.From = result.transactions[i].from;			
		//	transaction.To = result.transactions[i].to;	

		//	string hex = result.transactions[i].value.Substring(2).PadLeft(32, '0');
		//	transaction.Value = ((double)System.Convert.ToUInt64(hex.Substring(0, 16), 16) * 1.844674407370955e19 + (double)System.Convert.ToUInt64(hex.Substring(16, 16), 16)) / 1000000000000000000.0;

		//	transactions.Add(transaction);
		//}

		//block.Transactions = transactions;
	}

    IEnumerator LoadApiKey(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        WWW www = new WWW(path);
        yield return www;

        if (!string.IsNullOrEmpty(www.text))
        {
            apiKey = www.text;
            Debug.Log("Load Api key " + apiKey);
        }
    }

}
