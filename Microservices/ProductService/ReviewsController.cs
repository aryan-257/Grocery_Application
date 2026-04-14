using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Controllers;

/// <summary>
/// Manages customer reviews for products.
/// Reviews are gated behind purchase verification — only customers who have
/// a non-cancelled order containing the product may submit a review,
/// and each customer may only review a product once.
/// </summary>
[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ReviewsController(ProductDbContext db) : ControllerBase
{
    /// <summary>Extracts the authenticated user's ID from the JWT <c>sub</c> claim.</summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    /// <summary>
    /// Returns all reviews for a given product, ordered by most recent first.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid productId)
    {
        var reviews = await db.Reviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id.ToString(), r.ProductId.ToString(), r.CustomerId.ToString(),
                r.CustomerName, r.Rating, r.Comment, r.CreatedAt.ToString("o")))
            .ToListAsync();
        return Ok(reviews);
    }

    /// <summary>
    /// Checks whether the authenticated user is eligible to review a specific product.
    /// Returns <c>canReview: true</c> only if the user has a non-cancelled order containing
    /// the product AND has not already submitted a review for it.
    /// Used by the frontend to show or hide the review submission form.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet("can-review")]
    [Authorize]
    public async Task<IActionResult> CanReview(Guid productId)
    {
        var hasPurchased = await db.Orders
            .AnyAsync(o => o.CustomerId == UserId &&
                           o.Status != "Cancelled" &&
                           o.Items.Any(i => i.ProductId == productId));
        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.CustomerId == UserId);
        return Ok(new { canReview = hasPurchased && !alreadyReviewed, alreadyReviewed });
    }

    /// <summary>
    /// Submits a new review for a product.
    /// Enforces purchase verification and one-review-per-customer rules.
    /// On success, recalculates and persists the product's average rating.
    /// Returns 400 if the user has not purchased the product or the rating is out of range.
    /// Returns 409 Conflict if the user has already reviewed this product.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid productId, CreateReviewRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
            return BadRequest(new { error = "Rating must be between 1 and 5" });

        var hasPurchased = await db.Orders
            .AnyAsync(o => o.CustomerId == UserId &&
                           o.Status != "Cancelled" &&
                           o.Items.Any(i => i.ProductId == productId));
        if (!hasPurchased)
            return BadRequest(new { error = "You can only review products you have ordered" });

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.CustomerId == UserId);
        if (alreadyReviewed)
            return Conflict(new { error = "You have already reviewed this product" });

        var user = await db.Users.FindAsync(UserId);
        var review = new Review
        {
            ProductId = productId,
            CustomerId = UserId,
            CustomerName = $"{user!.FirstName} {user.LastName}",
            Rating = req.Rating,
            Comment = req.Comment
        };
        db.Reviews.Add(review);

        // Update product average rating
        var product = await db.Products.FindAsync(productId);
        if (product != null)
        {
            var allRatings = await db.Reviews.Where(r => r.ProductId == productId).Select(r => r.Rating).ToListAsync();
            allRatings.Add(req.Rating);
            product.AverageRating = allRatings.Average();
        }

        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReviews), new { productId },
            new ReviewDto(review.Id.ToString(), review.ProductId.ToString(), review.CustomerId.ToString(),
                review.CustomerName, review.Rating, review.Comment, review.CreatedAt.ToString("o")));
    }
}
