using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HtmlAgilityPack;
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
		public async Task<IEnumerable<string>> Get(string id) {
			string path = Path.Combine(Program.ImageStoragePath, id);

			if (!System.IO.File.Exists(path)) {
				using var client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", Program.UserAgent);
				var doc = new HtmlDocument();
				HttpResponseMessage result = await client.GetAsync($"https://ruqqus.com/post/{id}");
				doc.LoadHtml(await result.Content.ReadAsStringAsync());
				HtmlNode metaImage = doc.DocumentNode.SelectNodes("//html/head/meta[@property=\"og:image\"]").FirstOrDefault();
				HttpResponseMessage downloadImage = await client.GetAsync(metaImage.GetAttributeValue("content", null));
				if (downloadImage.Content.Headers.ContentType.MediaType == "image/jpeg" ||
					downloadImage.Content.Headers.ContentType.MediaType == "image/png") {
					using Stream downloadStream = await downloadImage.Content.ReadAsStreamAsync();
					await DownloadImages.ConvertImage(id, downloadStream);
				}
			}

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

			// This is really slow. Like, REALLY slow.
			// Processing 176 images on a 16-core machine took 26 seconds and peaked at 11 GB of RAM.
			// We need to find ways of making this more efficient.
			// Also, is there a way to run FLANN on a GPU?
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
								break;
							}
						}
					}

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
