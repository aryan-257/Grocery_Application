using System.Net.Http.Json;
using OrderService.Models;

namespace OrderService.Services;

public class ProductServiceClient(HttpClient http)
{
    public async Task<Product?> GetProductAsync(Guid productId)
    {
        try
        {
            return await http.GetFromJsonAsync<Product>($"/api/v1/products/{productId}");
        }
        catch { return null; }
    }
}
