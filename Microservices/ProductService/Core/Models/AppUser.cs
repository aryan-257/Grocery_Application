namespace ProductService.Core.Models;

/// <summary>
/// We store a small copy of user info here so we don't have to call
/// AuthService when showing who wrote a review.
/// This gets populated from the JWT claims when a review is submitted.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
