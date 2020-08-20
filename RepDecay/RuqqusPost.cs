using System;

namespace RepDecay {
	public class RuqqusPost {
		public DateTime Date { get; set; }
		public string Id { get; set; }
		public string Guild { get; set; }

		public int Upvotes { get; set; }
		public int Downvotes { get; set; }
		public int Score { get; set; }
	}
}
