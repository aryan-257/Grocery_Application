using System.Net.Http.Json;
using OrderService.Core.Models;

namespace OrderService.Infrastructure.Services;

/// <summary>
/// Used to fetch product info from ProductService when a user adds something to their cart.
/// If the call fails for any reason, we return null and let the caller handle it.
/// </summary>
public class ProductServiceClient(HttpClient http)
{
    public async Task<Product?> GetProductAsync(Guid productId)
    {
        try
        {
            return await http.GetFromJsonAsync<Product>($"/api/v1/products/{productId}");
        }
        catch
        {
            // if ProductService is down or returns error, just return null
            return null;
        }
    }
}
