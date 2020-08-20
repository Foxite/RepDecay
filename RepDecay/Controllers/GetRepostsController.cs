using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
		private readonly ILogger<GetRepostsController> m_Logger;

		public GetRepostsController(ILogger<GetRepostsController> logger) {
			// Ctor is called every time the page is loaded
			m_Logger = logger;
			m_Logger.LogDebug("ctor");
		}

		[HttpGet]
		public IEnumerable<string> Get(string id) {
			m_Logger.LogDebug("get");
			Image<Gray, byte> image = null;
			string filePath = Path.Combine("C:/temp/images", id);

			if (System.IO.File.Exists(filePath)) {
				image = new Image<Gray, byte>(filePath);
			} else {
				using var client = new WebClient();
				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.Load(client.DownloadString($"https://ruqqus.com/post/{id}"));
				HtmlAgilityPack.HtmlNode metaImage = doc.DocumentNode.SelectNodes("//html/head/meta[@property=\"og:image\"]").FirstOrDefault();
				client.DownloadFile(metaImage.GetAttributeValue("content", null), filePath);
				image = new Image<Gray, byte>(filePath);
			}

			var sift = new Emgu.CV.XFeatures2D.SIFT();

			var imagePoints = new VectorOfKeyPoint();
			var imageMat = new Mat();
			sift.DetectAndCompute(image, null, imagePoints, imageMat, false);

			foreach (string filename in Directory.GetFiles("C:/temp/images")) {
				if (Path.GetFileName(filename) != id) {
					var otherImage = new Image<Gray, byte>(filename);

					var otherImageMat = new Mat();
					var otherImagePoints = new VectorOfKeyPoint();
					sift.DetectAndCompute(otherImage, null, otherImagePoints, otherImageMat, false);

					var matches = new VectorOfVectorOfDMatch();
					var matcher = new FlannBasedMatcher(new KdTreeIndexParams(5), new SearchParams(50));
					matcher.KnnMatch(imageMat, otherImageMat, matches, 2);

					int matchCount = 0;
					for (int i = 0; i < matches.Size; i++) {
						if (matches[i][0].Distance < 0.3f * matches[i][1].Distance) {
							matchCount++;

							if (matchCount >= 50) {
								break;
							}
						}

						if (matchCount >= 50) {
							break;
						}
					}

					if (matchCount >= 50) {
						m_Logger.LogDebug("yield");
						yield return filename;
					}
				}
			}
		}
	}
}
