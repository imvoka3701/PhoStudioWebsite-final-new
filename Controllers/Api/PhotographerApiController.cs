using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Helpers;
using PhoStudioMVC.Models.Enums;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers.Api;

[Route("api/photographer")]
[ApiController]
[Authorize(AuthenticationSchemes = "ApiBearer", Roles = "Photographer")]
public class PhotographerApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private static DateTime VnNow => AppTime.VnNow;

    public PhotographerApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/photographer/schedule
    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bookings = await _db.Bookings
            .Include(b => b.Client)
            .Include(b => b.ServicePackage)
            .Where(b => b.PhotographerId == userId
                     && b.Status >= BookingStatus.Deposited
                     && b.Status != BookingStatus.Cancelled)
            .AsNoTracking()
            .ToListAsync();

        // SQLite does not support ORDER BY on TimeSpan values.
        // Sort on the client side after materializing.
        var sorted = bookings
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                ClientName = b.Client!.FullName,
                ClientPhone = b.Client!.Phone,
                ServiceName = b.ServicePackage!.Name,
                ShootDate = b.BookingDate,
                StartTime = b.StartTime.ToString(@"hh\:mm"),
                EndTime = b.EndTime.ToString(@"hh\:mm"),
                b.Status,
                StatusLabel = b.Status.ToString(),
                b.ConceptNote
            })
            .ToList();

        return Ok(sorted);
    }

    // POST /api/photographer/complete/{bookingId}
    [HttpPost("complete/{bookingId}")]
    public async Task<IActionResult> MarkComplete(string bookingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId
              && b.PhotographerId == userId
              && b.Status == BookingStatus.Deposited);

        if (booking == null)
            return NotFound(new { error = "Không tìm thấy lịch hoặc trạng thái không hợp lệ." });

        var shootDateTime = booking.BookingDate.Date + booking.StartTime;
        if (shootDateTime > VnNow)
            return BadRequest(new { error = "Chưa đến giờ chụp." });

        booking.Status = BookingStatus.ShootCompleted;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật trạng thái: Chụp xong." });
    }
}

