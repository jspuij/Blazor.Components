using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blazor.Components.ClientSample.Pages
{
    public partial class FetchData
    {
        private readonly HttpClient httpClient;

        public FetchData(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }
    }
}
