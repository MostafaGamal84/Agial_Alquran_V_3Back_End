using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Helpers.HttpClientHelper
{
    public class HttpClientService : IHttpClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpClientService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IFormFile> GetImageAsync(string imageUrl)
        {
            Response<string> output = new Response<string>();
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return null;

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var formFile = ConvertToFormFile(imageBytes, Path.GetFileName(imageUrl));

                return formFile;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private IFormFile ConvertToFormFile(byte[] fileBytes, string fileName, string contentType = "image/jpeg")
        {
            var stream = new MemoryStream(fileBytes);
            return new FormFile(stream, 0, fileBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
