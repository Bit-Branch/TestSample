using System.Net;
using Moq;
using RestSharp;

namespace TestSample;

public class GenericTestSample
{
    [Test]
    public async Task Test1()
    {
        // there we have some unique logic and setup for this particular test
        const int timesToRetry = 2;

        await Test((usefulEntity, mockedClient) =>
            {
                usefulEntity.MaxTimesToRetry = timesToRetry;
                
                mockedClient.Setup(c => c.ExecuteAsync(
                    It.Is<RestRequest>(r => r.Resource == ApiWrapper.FirstResource && r.Method == Method.Post),
                    default
                    ))
                    .ReturnsAsync(new RestResponse() { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, Content = "nice content"});

                mockedClient.SetupSequence(c => c.ExecuteAsync(
                        It.Is<RestRequest>(r => r.Resource == ApiWrapper.SecondResource && r.Method == Method.Get),
                        default))
                    .ReturnsAsync(new RestResponse() { IsSuccessStatusCode = false, StatusCode = HttpStatusCode.Forbidden})
                    .ReturnsAsync(new RestResponse() { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK});
            },
            (_, mockedClient) =>
            {
                mockedClient.Verify(x => x.ExecuteAsync(
                        It.Is<RestRequest>(r => r.Resource == ApiWrapper.SecondResource),
                        default
                    ),
                    Times.Exactly(2),
                    "Should be invoked twice");
            }
        );
    }
    
    [Test]
    public async Task Test2()
    {
        // some unique logic and setup here 
        
        
        await Test((_, mockedClient) =>
            {
                mockedClient.Setup(c => c.ExecuteAsync(
                        It.Is<RestRequest>(r => r.Resource == ApiWrapper.FirstResource && r.Method == Method.Post),
                        default
                    ))
                    .ReturnsAsync(new RestResponse() { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, Content = "nice content"});

                mockedClient.Setup(c => c.ExecuteAsync(
                    It.Is<RestRequest>(r => r.Resource == ApiWrapper.SecondResource && r.Method == Method.Get),
                    default))
                    .ReturnsAsync(new RestResponse());
            },
            (response, mockedClient) =>
            {
                Assert.IsTrue(response.Content.Contains("nice"));
                
                mockedClient.Verify(x => x.ExecuteAsync(
                        It.Is<RestRequest>(r => r.Resource == ApiWrapper.FirstResource),
                        default
                    ),
                    Times.Once,
                    "Should be invoked once");
            }
        );
    }

    // here we have the logic that will be executed for all tests
    // this logic is similar for them all and the only things that differs is the configuration of SomeNiceUsefulEntity
    // and Mocked RestClient

    // also if we will create different setup in configuration - most likely verification will also be different
    private static async Task Test(
        Action<SomeNiceUsefulEntity, Mock<IRestClient>> configuration,
        Action<RestResponse, Mock<IRestClient>> verify)
    {
        // this is the same for all tests
        var usefulEntity = new SomeNiceUsefulEntity();
        var mockedRestClient = new Mock<IRestClient>();

        var sut = new ApiWrapper(mockedRestClient.Object);

        // we are calling configuration to preform custom setup for mocked client and entity 
        configuration.Invoke(usefulEntity, mockedRestClient);

        var restResponse = await sut.FirstRequestAsync();
        await sut.SecondRequestAsync(usefulEntity);

        Assert.IsNotNull(restResponse, "sendResponse");

        // calling verify to perform our custom verification
        verify.Invoke(restResponse, mockedRestClient);
    }
    
    [TestCaseSource(nameof(Scenarios))]
    public async Task Test3(Action<SomeNiceUsefulEntity, Mock<IRestClient>> configuration, Action<RestResponse, Mock<IRestClient>> verify)
    {
        // if we want to perform some test multiple times but with different values/setups
        await Test(configuration, verify);
    }
    
    // you can use the same for the sequence of tests
    public static IEnumerable<TestCaseData> Scenarios
    {
        get
        {
            yield return new TestCaseData(
                (SomeNiceUsefulEntity _, Mock<IRestClient> client) =>
                {
                    client.Setup(c => c.ExecuteAsync(
                            It.Is<RestRequest>(r => r.Resource == ApiWrapper.FirstResource && r.Method == Method.Post),
                            default
                        ))
                        .ReturnsAsync(new RestResponse() { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, Content = "nice baked cake"});

                    client.Setup(c => c.ExecuteAsync(
                            It.Is<RestRequest>(r => r.Resource == ApiWrapper.SecondResource && r.Method == Method.Get),
                            default))
                        .ReturnsAsync(new RestResponse());
                },
                (RestResponse response, Mock<IRestClient> _) =>
                {
                    Assert.IsTrue(response.Content?.Contains("nice baked cake"));
                }
                ).SetName("Expecting some nice baked cake");
            
            yield return new TestCaseData(
                (SomeNiceUsefulEntity _, Mock<IRestClient> client) =>
                {
                    client.Setup(c => c.ExecuteAsync(
                            It.Is<RestRequest>(r => r.Resource == ApiWrapper.FirstResource && r.Method == Method.Post),
                            default
                        ))
                        .ReturnsAsync(new RestResponse());

                    
                    client.Setup(c => c.ExecuteAsync(
                            It.Is<RestRequest>(r => r.Resource == ApiWrapper.SecondResource && r.Method == Method.Get),
                            default))
                        .ReturnsAsync(new RestResponse());
                },
                (RestResponse _, Mock<IRestClient> client) =>
                {
                    client.Verify();
                }
                ).SetName("Expecting some other condition"); 
        }
    }
}