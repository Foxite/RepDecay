using System;
using System.Collections.Generic;
using System.IO;
using Emgu.CV;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace RepDecay {
	public sealed class Program {
		public const string UserAgent = "RepDecay v0.1 (by @foxite)";
		public static readonly string MatStoragePath = Path.Combine("C:", "temp", "mats");
		public static Dictionary<string, Mat> MatStore { get; private set; }

		public static void Main(string[] args) {
			string[] files = Directory.GetFiles(MatStoragePath);
			MatStore = new Dictionary<string, Mat>(files.Length);
			foreach (string fileName in files) {
				using var fs = new FileStorage(fileName, FileStorage.Mode.Read);
				var mat = new Mat();
				fs["mat"].ReadMat(mat);
				MatStore[Path.GetFileNameWithoutExtension(fileName)] = mat;
			}

			GC.Collect();

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
