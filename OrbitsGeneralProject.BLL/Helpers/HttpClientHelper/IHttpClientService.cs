using Microsoft.AspNetCore.Http;
using Orbits.GeneralProject.BLL.BaseReponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.Helpers.HttpClientHelper
{
    public interface IHttpClientService
    {
        Task<IFormFile> GetImageAsync(string imageUrl);
    }
}
