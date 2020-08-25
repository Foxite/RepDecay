using System.Collections.Generic;
using OpenCvSharp;

namespace RepDecay {
	public class ImageData {
		public Mat Features { get; }
		public List<float> DominantHues { get; }
		public List<RuqqusPost> Posts { get; }

		public ImageData(Mat features, List<float> dominantHues) {
			Features = features;
			DominantHues = dominantHues;
			Posts = new List<RuqqusPost>();
		}
	}
}
