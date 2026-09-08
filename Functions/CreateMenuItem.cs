using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class CreateMenuItem
    {
        private readonly ILogger<CreateMenuItem> _logger;

        public CreateMenuItem(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateMenuItem>();
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<MenuItemRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid request body.");
                    return badResponse;
                }

                if (string.IsNullOrWhiteSpace(request.Category) ||
                    string.IsNullOrWhiteSpace(request.Id) ||
                    string.IsNullOrWhiteSpace(request.Name))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync(
                        "Category, Id and Name are required.");
                    return badResponse;
                }

                if (request.Price < 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync(
                        "Price cannot be negative.");
                    return badResponse;
                }

                string connectionString =
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                    ?? "UseDevelopmentStorage=true";

                var tableServiceClient =
                    new TableServiceClient(connectionString);

                var tableClient =
                    tableServiceClient.GetTableClient("MenuItems");

                await tableClient.CreateIfNotExistsAsync();

                var menuItem = new MenuItem
                {
                    PartitionKey = request.Category,
                    RowKey = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    IsAvailable = request.IsAvailable
                };

                await tableClient.AddEntityAsync(menuItem);

                var response = req.CreateResponse(HttpStatusCode.Created);

                await response.WriteAsJsonAsync(menuItem);

                return response;
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Error creating menu item.");

                var response = req.CreateResponse(
                    HttpStatusCode.Conflict);

                await response.WriteStringAsync(
                    "Menu item already exists or could not be created.");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating menu item.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An unexpected error occurred.");

                return response;
            }
        }
    }
}