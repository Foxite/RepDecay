using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RepDecay.Controllers {
	[Route("[controller]")]
	[ApiController]
	public class IndexController : ControllerBase {
		private readonly ILogger m_Logger;

		public IndexController(ILogger<IndexController> logger) {
			m_Logger = logger;
		}

		[HttpGet]
		public IActionResult Get(string guild) {
			bool threadStarted = false;
			HttpClient client = null;
			try {
				client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", Program.UserAgent);
				string requestUri = $"https://ruqqus.com/+{guild}?sort=new";
				HttpResponseMessage result = client.GetAsync(requestUri).Result;
				if ((int) result.StatusCode == 200) {
					threadStarted = true;
					Task.Run(async () => {
						try {
							int pageCount = 0;
							bool downloadNextPage = true;
							while (downloadNextPage) {
								pageCount++;
								var document = new HtmlDocument();
								document.LoadHtml(await result.Content.ReadAsStringAsync());
								IEnumerable<HtmlNode> postDivs = document.GetElementbyId("posts").ChildNodes.Where(node => node.HasClass("card"));
								int postCount = 0;
								foreach (HtmlNode postDiv in postDivs) {
									postCount++;
									m_Logger.LogInformation($"Page {pageCount}, post {postCount}");
									HtmlNodeCollection htmlNodeCollections = postDiv.SelectNodes("div[1]/div[2]/div[1]/a[1]");
									HtmlNode anchor = htmlNodeCollections.FirstOrDefault();
									string imageUrl = anchor.GetAttributeValue("href", null);

									if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) {
										string postId = postDiv.Id.Substring("post-".Length);
										if (System.IO.File.Exists(Path.Combine("C:", "temp", "images", postId))) {
											downloadNextPage = false;
											break;
										} else {
											result = await client.GetAsync(imageUrl);
											if (result.IsSuccessStatusCode && (
												result.Content.Headers.ContentType.MediaType == "image/jpeg" ||
												result.Content.Headers.ContentType.MediaType == "image/webp" ||
												result.Content.Headers.ContentType.MediaType == "image/png")) {
												await ImageStore.Instance.GetDataAsync(postId, result.Content.ReadAsStreamAsync);
											}
										}
									}
								}

								if (postCount == 0) {
									// No more posts
									downloadNextPage = false;
								}

								if (downloadNextPage) {
									result = await client.GetAsync(requestUri + "&page=" + pageCount);
									downloadNextPage = result.IsSuccessStatusCode;
								}
							}
						} finally {
							client.Dispose();
						}
					});
					return Accepted();
				} else {
					return StatusCode((int) result.StatusCode);
				}
			} finally {
				if (client != null && !threadStarted) {
					client.Dispose();
				}
			}
		}
	}
}
