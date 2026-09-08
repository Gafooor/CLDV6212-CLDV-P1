using System.Net;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class GetMenuItemsByCategory
    {
        private readonly ILogger<GetMenuItemsByCategory> _logger;

        public GetMenuItemsByCategory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetMenuItemsByCategory>();
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu/category/{category}")]
            HttpRequestData req,
            string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    var badResponse = req.CreateResponse(
                        HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Category is required.");

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

                var menuItems = new List<MenuItem>();

                await foreach (var item in tableClient.QueryAsync<MenuItem>(
                    x => x.PartitionKey == category))
                {
                    menuItems.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(menuItems);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving menu items by category.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An unexpected error occurred.");

                return response;
            }
        }
    }
}
