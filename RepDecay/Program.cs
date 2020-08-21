using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RepDecay {
	public sealed class Program {
		public const string UserAgent = "RepDecay v0.1 (by @foxite)";
		public static readonly string ImageStoragePath = Path.Combine("C:", "temp", "images");
		public static readonly string MatStoragePath = Path.Combine("C:", "temp", "mats");

		public static void Main(string[] args) {
			/* // Prepend a / to this line to regenerate mats before starting
			using var sift = new Emgu.CV.XFeatures2D.SIFT();
			foreach (string filename in Directory.GetFiles(ImageStoragePath)) {
				using var imagePoints = new VectorOfKeyPoint();
				using var imageMat = new Mat();
				using var image = new Image<Gray, byte>(filename);
				sift.DetectAndCompute(image, null, imagePoints, imageMat, false);

				using FileStorage fs = new FileStorage(Path.Combine(MatStoragePath, Path.GetFileName(filename) + ".xml"), FileStorage.Mode.Write);
				fs.Write(imageMat, "mat");
			}//*/

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
