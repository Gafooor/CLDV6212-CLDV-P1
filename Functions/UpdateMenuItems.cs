using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class UpdateMenuItem
    {
        private readonly ILogger<UpdateMenuItem> _logger;

        public UpdateMenuItem(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateMenuItem>();
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "menu/{category}/{id}")]
            HttpRequestData req,
            string category,
            string id)
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

                MenuItem existingItem;

                try
                {
                    existingItem = await tableClient.GetEntityAsync<MenuItem>(
                        category,
                        id);
                }
                catch (RequestFailedException ex)
                    when (ex.Status == 404)
                {
                    var notFound = req.CreateResponse(
                        HttpStatusCode.NotFound);

                    await notFound.WriteStringAsync(
                        "Menu item not found.");

                    return notFound;
                }

                var request = await JsonSerializer.DeserializeAsync<MenuItemRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (request == null)
                {
                    var badResponse = req.CreateResponse(
                        HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Invalid request body.");

                    return badResponse;
                }

                if (request.Price < 0)
                {
                    var badResponse = req.CreateResponse(
                        HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Price cannot be negative.");

                    return badResponse;
                }

                existingItem.Price = request.Price;
                existingItem.IsAvailable = request.IsAvailable;

                await tableClient.UpdateEntityAsync(
                    existingItem,
                    existingItem.ETag,
                    TableUpdateMode.Replace);

                var response = req.CreateResponse(
                    HttpStatusCode.OK);

                await response.WriteAsJsonAsync(existingItem);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating menu item.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An unexpected error occurred.");

                return response;
            }
        }
    }
}
