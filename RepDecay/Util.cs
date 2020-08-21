using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace RepDecay {
	public static class Util {
		public static async Task SaveMatsForImage(string name, Stream stream) {
			// Emgu.CV does not support loading images from streams, unfortunately.
			string path = Path.GetTempFileName();
			using (var fs = File.OpenWrite(path)) {
				await stream.CopyToAsync(fs);
			}

			using var cvImage = new Image<Gray, byte>(path);
			using var imageMat = new Mat();

			await Task.Run(() => {
				using var sift = new Emgu.CV.XFeatures2D.SIFT();
				using var imagePoints = new VectorOfKeyPoint();

				sift.DetectAndCompute(cvImage, null, imagePoints, imageMat, false);
			});

			using (var fs = new FileStorage(Path.Combine(Program.MatStoragePath, name + ".xml"), FileStorage.Mode.Write)) {
				fs.Write(imageMat, "mat");
			}
		}
	}
}
