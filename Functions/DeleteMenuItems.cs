using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    public class DeleteMenuItem
    {
        private readonly ILogger<DeleteMenuItem> _logger;

        public DeleteMenuItem(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteMenuItem>();
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
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

                try
                {
                    await tableClient.GetEntityAsync<Azure.Data.Tables.TableEntity>(
                        category,
                        id);
                }
                catch (Azure.RequestFailedException ex)
                    when (ex.Status == 404)
                {
                    var notFound = req.CreateResponse(
                        HttpStatusCode.NotFound);

                    await notFound.WriteStringAsync(
                        "Menu item not found.");

                    return notFound;
                }

                await tableClient.DeleteEntityAsync(
                    category,
                    id);

                var response = req.CreateResponse(
                    HttpStatusCode.NoContent);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu item.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    "An unexpected error occurred.");

                return response;
            }
        }
    }
}
