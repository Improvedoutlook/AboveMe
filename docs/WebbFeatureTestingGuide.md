# Webb Space Telescope Feature - Unit Testing Guide

## Overview
This guide provides instructions for creating unit tests for the Webb Space Telescope feature in the AboveMe Blazor application.

## Test Framework Setup
Use xUnit with bUnit for Blazor component testing and Moq for mocking HttpClient.

### Required NuGet Packages
```xml
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="bUnit" Version="1.26.64" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

## Test Cases

### Test 1: Successful Webb Data Fetch
**Purpose**: Verify that FetchWebbDataAsync() correctly parses a valid NASA Images API response.

```csharp
[Fact]
public async Task FetchWebbDataAsync_ValidResponse_PopulatesWebbData()
{
    // Arrange
    var mockResponse = @"{
        ""collection"": {
            ""items"": [
                {
                    ""links"": [
                        {
                            ""href"": ""https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e000393/GSFC_20171208_Archive_e000393~thumb.jpg""
                        }
                    ],
                    ""data"": [
                        {
                            ""title"": ""James Webb Space Telescope Mirror Seen in Full Bloom"",
                            ""description"": ""In the clean room at NASA's Goddard Space Flight Center in Greenbelt, Maryland, the James Webb Space Telescope team used a robot arm to install the last of the telescope's 18 mirrors onto the telescope structure."",
                            ""date_created"": ""2016-02-03T00:00:00Z""
                        }
                    ]
                }
            ]
        }
    }";

    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
    
    // Note: You'll need to modify Home.razor to make it testable by injecting HttpClient
    // or by making FetchWebbDataAsync testable through dependency injection
    
    // Act
    // Call the method under test
    
    // Assert
    Assert.NotNull(webbData);
    Assert.Equal("https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e000393/GSFC_20171208_Archive_e000393~thumb.jpg", webbData.ImageUrl);
    Assert.Equal("James Webb Space Telescope Mirror Seen in Full Bloom", webbData.Title);
    Assert.Contains("clean room", webbData.Description);
    Assert.Equal("February 03, 2016", webbData.DateCreated);
    Assert.Empty(WebbError);
}
```

### Test 2: Empty Items Array
**Purpose**: Verify that the error is set when NASA API returns no items.

```csharp
[Fact]
public async Task FetchWebbDataAsync_EmptyItems_SetsError()
{
    // Arrange
    var mockResponse = @"{
        ""collection"": {
            ""items"": []
        }
    }";

    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
    
    // Act
    // Call the method under test
    
    // Assert
    Assert.Null(webbData);
    Assert.Equal("No Webb images found.", WebbError);
}
```

### Test 3: Network Error / HTTP Failure
**Purpose**: Verify error handling when the API returns a non-success status code.

```csharp
[Fact]
public async Task FetchWebbDataAsync_HttpError_SetsError()
{
    // Arrange
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        });

    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
    
    // Act
    // Call the method under test
    
    // Assert
    Assert.Null(webbData);
    Assert.Contains("Failed to fetch Webb data", WebbError);
    Assert.Contains("500", WebbError);
}
```

### Test 4: Exception Handling
**Purpose**: Verify that exceptions are caught and converted to user-friendly error messages.

```csharp
[Fact]
public async Task FetchWebbDataAsync_ThrowsException_SetsError()
{
    // Arrange
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ThrowsAsync(new HttpRequestException("Network error"));

    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
    
    // Act
    // Call the method under test
    
    // Assert
    Assert.Null(webbData);
    Assert.Contains("Error:", WebbError);
    Assert.Contains("Network error", WebbError);
}
```

### Test 5: Caching Behavior
**Purpose**: Verify that cached data is reused and no network call is made.

```csharp
[Fact]
public async Task FetchWebbDataAsync_CachedData_ReusesCache()
{
    // Arrange
    var callCount = 0;
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(() =>
        {
            callCount++;
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(validMockResponse, Encoding.UTF8, "application/json")
            };
        });

    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
    
    // Act
    await FetchWebbDataAsync(); // First call
    await FetchWebbDataAsync(); // Second call should use cache
    
    // Assert
    Assert.Equal(1, callCount); // HTTP call should only happen once
    Assert.NotNull(webbData);
    Assert.NotNull(cachedWebbData);
}
```

### Test 6: Toggle Button Behavior
**Purpose**: Verify that ToggleWebb() correctly toggles the display and triggers data fetch.

```csharp
[Fact]
public async Task ToggleWebb_ShowsAndFetchesData()
{
    // Arrange
    using var ctx = new TestContext();
    var component = ctx.RenderComponent<Home>();
    
    // Mock HttpClient for the component
    // ... setup mock
    
    // Act
    var button = component.Find("button:contains('Webb Space Telescope')");
    await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
    
    // Assert
    Assert.True(showWebb);
    // Verify loading state is shown initially
    Assert.Contains("Loading Webb image...", component.Markup);
}
```

### Test 7: Missing Image URL
**Purpose**: Verify error handling when image URL is not found in response.

```csharp
[Fact]
public async Task FetchWebbDataAsync_MissingImageUrl_SetsError()
{
    // Arrange
    var mockResponse = @"{
        ""collection"": {
            ""items"": [
                {
                    ""data"": [
                        {
                            ""title"": ""Test Title"",
                            ""description"": ""Test Description""
                        }
                    ]
                }
            ]
        }
    }";

    // ... setup mock HTTP handler
    
    // Act
    // Call the method under test
    
    // Assert
    Assert.Null(webbData);
    Assert.Equal("No image URL found in Webb data.", WebbError);
}
```

## Integration Testing Recommendations

1. **End-to-End Test**: Create a test that verifies the entire flow from button click to image display using the actual NASA Images API (in a separate integration test suite).

2. **Accessibility Testing**: Verify that:
   - Alt text is properly set on images
   - Color contrast meets WCAG standards
   - Keyboard navigation works correctly

3. **Responsive Design Testing**: Test the Webb display on various screen sizes to ensure proper rendering.

## Running Tests

```powershell
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~FetchWebbDataAsync_ValidResponse"
```

## Code Coverage

To generate code coverage reports:

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Notes

- The current implementation in Home.razor uses private methods and fields, which may require refactoring for better testability
- Consider creating a separate service class (e.g., `WebbDataService`) to handle API calls, making it easier to mock in tests
- Use dependency injection to inject HttpClient into components for better testability

## Recommended Refactoring for Testability

Consider extracting the Webb functionality into a separate service:

```csharp
public interface IWebbDataService
{
    Task<WebbData?> FetchWebbDataAsync();
}

public class WebbDataService : IWebbDataService
{
    private readonly HttpClient _httpClient;
    private WebbData? _cachedData;
    
    public WebbDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<WebbData?> FetchWebbDataAsync()
    {
        // Implementation from Home.razor
    }
}
```

Then inject this service into Home.razor for easier testing.
