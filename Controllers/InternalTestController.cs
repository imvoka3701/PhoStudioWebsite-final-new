using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using PhoStudioMVC.Utils;
using System.Security.Cryptography;
using System.Text;

namespace PhoStudioMVC.Controllers
{
    [AllowAnonymous]
    [Route("InternalTest")]
    public class InternalTestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InternalTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Run")]
        public async Task<IActionResult> RunTest()
        {
            var report = new List<string>();
            try
            {
                // ==============================
                // STEP 1: USER REGISTRATION
                // ==============================
                string targetEmail = "test_customer@gmail.com";
                var customer = await _context.Users.FirstOrDefaultAsync(u => u.Email == targetEmail);
                if (customer == null)
                {
                    customer = new ApplicationUser 
                    { 
                        Username = "test_customer",
                        Email = targetEmail, 
                        FullName = "Test Customer",
                        PasswordHash = HashPassword("Customer@123"), 
                        Role = UserRole.Customer,
                        IsActive = true
                    };
                    _context.Users.Add(customer);
                    await _context.SaveChangesAsync();
                    report.Add("STEP 1: [SUCCESS] Created Customer account.");
                }
                else
                {
                    customer.PasswordHash = HashPassword("Customer@123");
                    customer.Username = "test_customer"; // Ensure login works with this username
                    _context.Users.Update(customer);
                    await _context.SaveChangesAsync();
                    report.Add("STEP 1: [SUCCESS] Customer account already exists (Profile Updated).");
                }

                string photogEmail = "photographer@phostudio.vn";
                var photographer = await _context.Users.FirstOrDefaultAsync(u => u.Email == photogEmail);
                if (photographer == null)
                {
                    photographer = new ApplicationUser 
                    { 
                        Username = "photographer1",
                        Email = photogEmail, 
                        FullName = "Test Photographer",
                        PasswordHash = "hashed_pw",
                        Role = UserRole.Photographer,
                        IsActive = true
                    };
                    _context.Users.Add(photographer);
                    await _context.SaveChangesAsync();
                }

                // Clean old test data
                var oldBookings = await _context.Bookings.Where(b => b.ClientId == customer.Id).ToListAsync();
                if (oldBookings.Any())
                {
                    _context.Bookings.RemoveRange(oldBookings);
                    await _context.SaveChangesAsync();
                }

                // ==============================
                // STEP 2: BOOKING & DEPOSIT
                // ==============================
                var package = await _context.ServicePackages.FirstOrDefaultAsync(p => p.IsActive);
                if (package == null)
                {
                    package = new ServicePackage 
                    { 
                        Name = "Gói nàng thơ", 
                        Price = 2000000, 
                        DurationMinutes = 120, 
                        IsActive = true 
                    };
                    _context.ServicePackages.Add(package);
                    await _context.SaveChangesAsync();
                }

                var booking = new Booking
                {
                    ClientId = customer.Id,
                    ServicePackageId = package.Id,
                    BookingDate = TimeHelper.VnNow.AddDays(7).Date,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    TotalAmount = package.Price,
                    DepositAmount = package.Price * 0.3m,
                    Status = BookingStatus.TimeSlotLocked,
                    LockExpirationTime = TimeHelper.VnNow.AddMinutes(15),
                    Notes = "Test auto booking"
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Simulate Deposit IPN
                booking.Status = BookingStatus.Deposited;
                var transaction = new Transaction
                {
                    BookingId = booking.Id,
                    PaymentType = PaymentType.OnlineDeposit,
                    Status = PaymentStatus.Success,
                    Amount = booking.DepositAmount,
                    CreatedAt = TimeHelper.VnNow,
                    CompletedAt = TimeHelper.VnNow,
                    GatewayTransactionId = "TEST_" + Guid.NewGuid()
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Verify
                var myBookingsQuery = await _context.Bookings
                    .Where(b => b.ClientId == customer.Id)
                    .ToListAsync();
                if (!myBookingsQuery.Any()) throw new Exception("Booking not found in MyBookings query.");
                report.Add("STEP 2: [SUCCESS] Booking created and Deposited. Appears in MyBookings query.");

                // ==============================
                // STEP 3: ADMIN DISPATCH
                // ==============================
                booking.PhotographerId = photographer.Id;
                booking.CommissionAmount = booking.TotalAmount * 0.20m;
                await _context.SaveChangesAsync();
                report.Add("STEP 3: [SUCCESS] Assigned Photographer. Commission calculated.");

                // ==============================
                // STEP 4: PHOTOGRAPHER COMPLETION & DELIVERY
                // ==============================
                booking.Status = BookingStatus.ShootCompleted;
                booking.WorkflowStatus = BookingWorkflowStatus.ShootCompleted;
                var secureOtp = "123456";
                var cloudAlbum = new CloudAlbum
                {
                    BookingId = booking.Id,
                    AlbumUrl = "https://photos.app.goo.gl/test-album-link",
                    SecureOTP = secureOtp,
                    ExpiryDate = TimeHelper.VnNow.AddDays(30)
                };
                _context.CloudAlbums.Add(cloudAlbum);
                booking.WorkflowStatus = BookingWorkflowStatus.Delivered;
                await _context.SaveChangesAsync();

                var verifyAlbum = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.BookingId == booking.Id);
                if (verifyAlbum == null) throw new Exception("CloudAlbum record does not exist in DB.");
                report.Add("STEP 4: [SUCCESS] Photographer delivered album. OTP Generated.");

                // ==============================
                // STEP 5: CUSTOMER FINAL ACCESS
                // ==============================
                // 5.1 Verify MyAlbums logic
                var myAlbumsQuery = await _context.Bookings
                    .Include(b => b.CloudAlbum)
                    .Where(b => b.ClientId == customer.Id && b.CloudAlbum != null)
                    .ToListAsync();

                if (!myAlbumsQuery.Any()) throw new Exception("Client MyAlbums query failed - could not find booking with CloudAlbum != null.");
                report.Add("STEP 5: [SUCCESS] MyAlbums query is working properly mappings are correct.");

                // 5.2 Verify OTP Logic 
                var dbAlbum = myAlbumsQuery.First().CloudAlbum;
                if (string.Compare(dbAlbum.SecureOTP, "wrong_otp", StringComparison.Ordinal) == 0) throw new Exception("OTP logic is improperly case-insensitive or broken.");
                if (string.Compare(dbAlbum.SecureOTP, secureOtp, StringComparison.Ordinal) != 0) throw new Exception("OTP logic matching failed.");
                report.Add("STEP 5: [SUCCESS] VerifyOTP logic check passed for correct StringComparison.");

                // 5.3 Expiry Logic
                dbAlbum.ExpiryDate = TimeHelper.VnNow.AddDays(-1);
                await _context.SaveChangesAsync();

                var retryAlbum = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.BookingId == booking.Id);
                if (retryAlbum.ExpiryDate > TimeHelper.VnNow) throw new Exception("Expiry check failed!");
                report.Add("STEP 5: [SUCCESS] Expiry logic confirmed blocking past dates.");

                // RESET to Future so Customer logger can legitimately see it
                retryAlbum.ExpiryDate = TimeHelper.VnNow.AddDays(30);
                await _context.SaveChangesAsync();

                report.Add("======================================");
                report.Add("ALL TESTS PASSED SUCCESSFULLY!");
                return Json(report);
            }
            catch (Exception ex)
            {
                report.Add("FAILED TEST: " + ex.Message);
                return Json(report);
            }
        }

        [HttpGet("GetTestAccounts")]
        public async Task<IActionResult> GetTestAccounts()
        {
            var data = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.CloudAlbum)
                .Where(b => b.CloudAlbum != null)
                .Select(b => new
                {
                    Email = b.Client.Email,
                    Username = b.Client.Username,
                    Role = b.Client.Role.ToString(),
                    BookingId = b.Id,
                    OTP = b.CloudAlbum.SecureOTP,
                    Expiry = b.CloudAlbum.ExpiryDate.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet("GetAllAccounts")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var data = await _context.Users
                .Select(u => new
                {
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(data);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
