namespace MiRestaurante.Api.FunctionalTests;

internal static class PreflightRequest
{
    public static HttpRequestMessage Create(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.TryAddWithoutValidation("Origin", "http://otro-origen.test");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "x-foo");
        return request;
    }
}
