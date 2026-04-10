using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoStudioMVC.Services;
using PhoStudioMVC.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PhoStudioMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public NotificationApiController(NotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        /// <summary>
        /// Lấy số thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _notificationService.GetUnreadCount(clientId);

            return Ok(new { count });
        }

        /// <summary>
        /// Lấy danh sách thông báo
        /// </summary>
        [HttpGet("list")]
        public IActionResult GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = _context.Notifications
                .Where(n => n.ClientId == clientId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(notifications);
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPost("mark-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(notificationId);

            if (notification == null || notification.ClientId != clientId)
                return NotFound();

            await _notificationService.MarkAsRead(notificationId);

            return Ok(new { message = "Đánh dấu đã đọc thành công" });
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.ClientId == clientId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                await _notificationService.MarkAsRead(notification.Id);
            }

            return Ok(new { message = "Đánh dấu tất cả đã đọc", count = notifications.Count });
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(notificationId);

            if (notification == null || notification.ClientId != clientId)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa thông báo thành công" });
        }
    }
}
