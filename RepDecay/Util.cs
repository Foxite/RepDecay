using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace RepDecay {
	public static class Util {
		public static async Task SaveImageData(string name, Stream stream) {
			// Emgu.CV does not support loading images from streams, unfortunately.
			string path = Path.Combine("C:", "temp", "images", name);
			using (var fs = File.OpenWrite(path)) {
				await stream.CopyToAsync(fs);
			}

			await SaveImageData(name, path);
		}

		public static Task SaveImageData(string name, string path) {
			using var image = new Image<Bgr, byte>(path);
			return SaveImageData(name, image);
		}

		public static async Task SaveImageData(string name, Image<Bgr, byte> image) {
			using var imageMat = new Mat();
			var rHist = new Mat();
			var gHist = new Mat();
			var bHist = new Mat();

			await Task.WhenAll(Task.Run(() => {
				using var sift = new Emgu.CV.XFeatures2D.SIFT();
				using var imagePoints = new VectorOfKeyPoint();

				sift.DetectAndCompute(image, null, imagePoints, imageMat, false);
			}), Task.Run(() => {
				void getChannelHistogram(int channel, Mat mat) {
					CvInvoke.CalcHist(image[channel], new[] { channel }, null, mat, new int[] { 256 }, new float[] { 0, 256 }, false);
				}

				getChannelHistogram(0, rHist);
				getChannelHistogram(1, gHist);
				getChannelHistogram(2, bHist);
			}));
			
			using (var fs = new FileStorage(Path.Combine(Program.ImageStoragePath, name + ".xml"), FileStorage.Mode.Write)) {
				fs.Write(imageMat, "features");
				fs.Write(rHist, "rHist");
				fs.Write(gHist, "gHist");
				fs.Write(bHist, "bHist");
			}
		}

		public static Mat GetMat(this FileStorage fs, string key) {
			var ret = new Mat();
			fs[key].ReadMat(ret);
			return ret;
		}
	}
}
