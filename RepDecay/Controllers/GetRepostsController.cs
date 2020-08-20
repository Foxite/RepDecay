using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RepDecay.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class GetRepostsController : ControllerBase {
		private readonly ILogger m_Logger;

		public GetRepostsController(ILogger<GetRepostsController> logger) {
			m_Logger = logger;
		}

		[HttpGet]
		public IEnumerable<string> Get(string id) {
			string path = Path.Combine(Program.ImageStoragePath, id);

			/*
			if (!System.IO.File.Exists(path)) {
				using var client = new WebClient();
				client.Headers.Add("User-Agent", Program.UserAgent);
				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(client.DownloadString($"https://ruqqus.com/post/{id}"));
				HtmlAgilityPack.HtmlNode metaImage = doc.DocumentNode.SelectNodes("//html/head/meta[@property=\"og:image\"]").FirstOrDefault();
				client.DownloadFile(metaImage.GetAttributeValue("content", null), path);
			}//*/

			return DuplicateImages(path);
		}

		private IEnumerable<string> DuplicateImages(string duplicateOf) {
			var stw = new Stopwatch();
			stw.Start();
			var image = new Image<Gray, byte>(duplicateOf);

			var sift = new Emgu.CV.XFeatures2D.SIFT();
			var imagePoints = new VectorOfKeyPoint();
			var imageMat = new Mat();
			sift.DetectAndCompute(image, null, imagePoints, imageMat, false);

			ConcurrentBag<string> results = new ConcurrentBag<string>();
			//foreach (string filename in Directory.GetFiles("C:/temp/images")) {
			// Note: Parallel execution takes a LOT of RAM.
			// When processing 9 items concurrently, my memory usage rose to about 2GB momentarily.
			// I have 16 threads on my PC so it was able to do everything at once. If your PC has less threads than you have images to process, memory usage will cap out.
			ParallelEnumerable.ForAll(Directory.GetFiles(Program.ImageStoragePath).AsParallel(), filename => {
				if (filename != duplicateOf) {
					var otherImage = new Image<Gray, byte>(filename);

					var otherImageMat = new Mat();
					var otherImagePoints = new VectorOfKeyPoint();
					sift.DetectAndCompute(otherImage, null, otherImagePoints, otherImageMat, false);

					var matches = new VectorOfVectorOfDMatch();
					var matcher = new FlannBasedMatcher(new KdTreeIndexParams(5), new SearchParams(50));
					matcher.KnnMatch(imageMat, otherImageMat, matches, 2);

					// These constants have been stolen from https://github.com/magamig/duplicate_images_finder
					const float DistanceModifier = 0.3f;
					const int MinMatches = 50;

					int matchCount = 0;
					for (int i = 0; i < matches.Size; i++) {
						if (matches[i][0].Distance < DistanceModifier * matches[i][1].Distance) {
							matchCount++;
							if (matchCount >= MinMatches) {
								goto endLoop;
							}
						}
					}

					endLoop:
					if (matchCount >= MinMatches) {
						results.Add(filename);
					}
				}
			});
			stw.Stop();
			m_Logger.LogInformation((stw.ElapsedMilliseconds / 1000f).ToString("#.###"));
			return results;
		}
	}
}
