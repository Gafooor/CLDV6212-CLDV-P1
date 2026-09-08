using System.Net;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class GetAllMenuItems
    {
        private readonly ILogger<GetAllMenuItems> _logger;

        public GetAllMenuItems(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetAllMenuItems>();
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                string connectionString =
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                    ?? "UseDevelopmentStorage=true";

                var tableServiceClient =
                    new TableServiceClient(connectionString);

                var tableClient =
                    tableServiceClient.GetTableClient("MenuItems");

                await tableClient.CreateIfNotExistsAsync();

                var menuItems = new List<MenuItem>();

                await foreach (var item in tableClient.QueryAsync<MenuItem>())
                {
                    menuItems.Add(item);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(menuItems);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu items.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An unexpected error occurred.");

                return response;
            }
        }
    }
}