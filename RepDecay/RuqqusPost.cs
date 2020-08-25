using System;
using System.Collections.Generic;

namespace RepDecay {
	public class RuqqusPost : IEquatable<RuqqusPost> {
		public string Id { get; set; }
		public DateTime Date { get; }
		public string Title { get; }

		public RuqqusPost(string id) {
			Id = id;
		}

		public override bool Equals(object obj) => Equals(obj as RuqqusPost);
		public bool Equals(RuqqusPost other) => other != null && Id == other.Id;
		public override int GetHashCode() => HashCode.Combine(Id);

		public static bool operator ==(RuqqusPost left, RuqqusPost right) => EqualityComparer<RuqqusPost>.Default.Equals(left, right);
		public static bool operator !=(RuqqusPost left, RuqqusPost right) => !(left == right);
	}
}
