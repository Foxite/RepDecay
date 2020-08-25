using System.Collections.Generic;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RepDecay {
	public sealed class Program {
		public const string UserAgent = "RepDecay v0.1 (by @foxite)";
		public static readonly string ImageStoragePath = Path.Combine("C:", "temp", "images");
		public static Dictionary<string, ImageData> ImageStore { get; private set; }

		public static void Main(string[] args) {
			string[] files = Directory.GetFiles(ImageStoragePath);
			ImageStore = new Dictionary<string, ImageData>(files.Length);
			foreach (string path in files) {
				using var image = new Image<Bgr, byte>(path);

				var hist = new Mat(image.Rows, image.Cols, Emgu.CV.CvEnum.DepthType.Cv8U, 3);

				using var fs = new FileStorage(Path.Combine("C:", "temp", "data", Path.GetFileNameWithoutExtension(path) + ".xml"), FileStorage.Mode.Read);

				CvInvoke.CalcHist(image, new[] { 0, 1, 2 }, new Mat(), hist, new int[] { 256, 256, 256 }, new float[] { 0, 256, 0, 256, 0, 256 }, false);

				ImageStore[Path.GetFileNameWithoutExtension(path)] = new ImageData() {
					Features = fs.GetMat("features"),
					RHist = fs.GetMat("rHist"),
					GHist = fs.GetMat("gHist"),
					BHist = fs.GetMat("bHist"),
				};
			}

			//CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
