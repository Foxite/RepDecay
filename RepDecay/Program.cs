using System.IO;
using Emgu.CV;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RepDecay {
	public sealed class Program {
		public const string UserAgent = "RepDecay v0.1 (by @foxite)";
		public static readonly string MatStoragePath = Path.Combine("C:", "temp", "mats");
		public static FileStorage FileStore { get; private set; }

		public static void Main(string[] args) {
			FileStore = new FileStorage("C:/temp/mats.xml", FileStorage.Mode.Memory | FileStorage.Mode.Write);
			foreach (string item in Directory.GetFiles(MatStoragePath)) {
				using var fs = new FileStorage(item, FileStorage.Mode.Read);
				var mat = new Mat();
				fs["mat"].ReadMat(mat);
				FileStore.Write(mat, "mat_" + Path.GetFileNameWithoutExtension(item));
			}

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
