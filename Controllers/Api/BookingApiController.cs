using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Helpers;
using PhoStudioMVC.Models.Enums;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers.Api;

[Route("api/bookings")]
[ApiController]
[Authorize(AuthenticationSchemes = "ApiBearer", Roles = "Client,Customer")]
public class BookingApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private static DateTime VnNow => AppTime.VnNow;

    public BookingApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/bookings/my
    [HttpGet("my")]
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bookings = await _db.Bookings
            .Include(b => b.ServicePackage)
            .Where(b => b.ClientId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .Select(b => new
            {
                b.Id,
                b.Status,
                StatusLabel = b.Status.ToString(),
                ServiceName = b.ServicePackage!.Name,
                ShootDate = b.BookingDate,
                StartTime = b.StartTime.ToString(@"hh\:mm"),
                b.TotalAmount,
                b.DepositAmount,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // GET /api/bookings/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var booking = await _db.Bookings
            .Include(b => b.ServicePackage)
            .Include(b => b.Photographer)
            .Include(b => b.CloudAlbum)
            .Include(b => b.RetouchRequests)
            .Where(b => b.Id == id && b.ClientId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (booking == null)
            return NotFound(new { error = "Không tìm thấy đặt lịch." });

        var latestAlbum = booking.CloudAlbum;

        return Ok(new
        {
            booking.Id,
            booking.Status,
            StatusLabel = booking.Status.ToString(),
            ServiceName = booking.ServicePackage!.Name,
            ServicePrice = booking.ServicePackage!.Price,
            ShootDate = booking.BookingDate,
            StartTime = booking.StartTime.ToString(@"hh\:mm"),
            EndTime = booking.EndTime.ToString(@"hh\:mm"),
            booking.TotalAmount,
            booking.DepositAmount,
            booking.ConceptNote,
            PhotographerName = booking.Photographer?.FullName,
            HasAlbum = latestAlbum != null,
            AlbumExpired = latestAlbum?.ExpiryDate < VnNow,
            RetouchCount = booking.RetouchRequests.Count,
            booking.CreatedAt
        });
    }

    // GET /api/packages
    [HttpGet("/api/packages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPackages()
    {
        var packages = await _db.ServicePackages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.DurationMinutes,
                p.ThumbnailUrl
            })
            .ToListAsync();

        return Ok(packages);
    }

    // GET /api/bookings/booked-slots?date=2026-04-15
    [HttpGet("booked-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookedSlots([FromQuery] string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest(new { error = "Ngày không hợp lệ." });

        var bookedSlots = await _db.Bookings
            .Where(b =>
                b.BookingDate == parsedDate.Date &&
                b.Status != BookingStatus.Cancelled &&
                !(b.Status == BookingStatus.TimeSlotLocked && b.LockExpirationTime <= VnNow))
            .Select(b => b.StartTime.ToString(@"hh\:mm"))
            .Distinct()
            .ToListAsync();

        return Ok(new { date = parsedDate.ToString("yyyy-MM-dd"), bookedSlots });
    }
}

