using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Utils;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PhoStudioMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        public ReviewApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gửi review/rating cho booking
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra booking tồn tại
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null || booking.ClientId != clientId)
                return NotFound(new { message = "Booking không tồn tại" });

            // Kiểm tra rating hợp lệ
            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating phải từ 1-5 sao" });

            // Kiểm tra review đã tồn tại chưa
            var existingReview = await _context.BookingReviews
                .FirstOrDefaultAsync(r => r.BookingId == request.BookingId);

            if (existingReview != null)
                return BadRequest(new { message = "Booking này đã được review" });

            // Tạo review
            var review = new BookingReview
            {
                BookingId = request.BookingId,
                ClientId = clientId,
                Rating = request.Rating,
                Comment = request.Comment,
                Photographer = request.Photographer,
                CreatedAt = VnNow
            };

            _context.BookingReviews.Add(review);

            // Cập nhật booking
            booking.IsReviewed = true;
            booking.ReviewDeadline = null;

            // Cập nhật photographer rating (nếu có photographer)
            if (!string.IsNullOrEmpty(booking.PhotographerId))
            {
                await UpdatePhotographerRating(booking.PhotographerId);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review gửi thành công", reviewId = review.Id });
        }

        /// <summary>
        /// Lấy reviews của booking
        /// </summary>
        [HttpGet("booking/{bookingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookingReview(string bookingId)
        {
            var review = await _context.BookingReviews
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            if (review == null)
                return NotFound();

            return Ok(review);
        }

        /// <summary>
        /// Lấy rating của photographer
        /// </summary>
        [HttpGet("photographer/{photographerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPhotographerRating(string photographerId)
        {
            var rating = await _context.PhotographerRatings.FindAsync(photographerId);

            if (rating == null)
                return NotFound();

            return Ok(rating);
        }

        /// <summary>
        /// Cập nhật average rating của photographer
        /// </summary>
        private async Task UpdatePhotographerRating(string photographerId)
        {
            var reviews = await _context.BookingReviews
                .Include(r => r.Booking)
                .Where(r => r.Booking.PhotographerId == photographerId)
                .ToListAsync();

            if (reviews.Count == 0)
                return;

            var averageRating = reviews.Average(r => r.Rating);
            var existingRating = await _context.PhotographerRatings.FindAsync(photographerId);

            if (existingRating == null)
            {
                existingRating = new PhotographerRating
                {
                    PhotographerId = photographerId,
                    AverageRating = averageRating,
                    TotalReviews = reviews.Count,
                    LastUpdated = VnNow
                };
                _context.PhotographerRatings.Add(existingRating);
            }
            else
            {
                existingRating.AverageRating = averageRating;
                existingRating.TotalReviews = reviews.Count;
                existingRating.LastUpdated = VnNow;
            }

            await _context.SaveChangesAsync();
        }

        public class SubmitReviewRequest
        {
            public string BookingId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public string Photographer { get; set; }
        }
    }
}
