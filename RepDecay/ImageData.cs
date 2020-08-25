using Emgu.CV;

namespace RepDecay {
	public class ImageData {
		public Mat Features { get; set; }
		public Mat RHist { get; set; }
		public Mat GHist { get; set; }
		public Mat BHist { get; set; }
	}
}
