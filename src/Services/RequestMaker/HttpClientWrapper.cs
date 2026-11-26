namespace Services.RequestMaker;

public class HttpClientWrapper : IHttpClient
{
    private static readonly HttpClient Client = new HttpClient();
    public virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
    {
        return await Client.SendAsync(message);
    }
}
