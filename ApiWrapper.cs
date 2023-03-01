using RestSharp;

namespace TestSample;

public class ApiWrapper
{
    private readonly IRestClient _client;
    internal const string FirstResource = "first";
    internal const string SecondResource = "second";

    public ApiWrapper(IRestClient client)
    {
        // will inject client only for testing purposes
        _client = client;
    }

    public async Task<RestResponse> FirstRequestAsync()
    {
        var request = new RestRequest(FirstResource, Method.Post);

        var result = await _client.ExecuteAsync(request);
        
        return result;
    }

    public async Task SecondRequestAsync(SomeNiceUsefulEntity usefulEntity)
    {
        var request = new RestRequest(SecondResource);

        var retryCount = 0;
        
        do
        {
            var result = await _client.ExecuteAsync(request);

            if (result.IsSuccessful)
            {
                break;
            }
            
            retryCount++;
            
        } while (retryCount < usefulEntity.MaxTimesToRetry);
    }
}