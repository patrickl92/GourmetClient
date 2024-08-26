using System.Net.Http;

namespace GourmetClient.Utils
{
    public record HttpClientResult<T>(HttpClient Client, T ResponseResult);
}
