using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour {
	[System.Serializable]
	public struct Transaction {
		public string From;
		public string To;
		public double Value;
	}

	public ulong Number;
	public List<Transaction> Transactions;
}
