using System.Text;

namespace PhoStudioMVC.Services
{
    public class QRCodeService
    {
        /// <summary>
        /// T?o d? li?u QR code cho chuy?n kho?n
        /// Format: Mã booking + s? ti?n + ngân hàng
        /// </summary>
        public static string GenerateQRCodeData(string bookingId, decimal amount)
        {
            // Format: BOOKING_ID|AMOUNT|VND
            return $"{bookingId}|{amount:F0}|VND";
        }

        /// <summary>
        /// T?o URL Google Charts ?? hi?n th? QR code
        /// </summary>
        public static string GenerateQRCodeUrl(string bookingId, decimal amount)
        {
            string data = GenerateQRCodeData(bookingId, amount);
            string encoded = Uri.EscapeDataString(data);

            // S? d?ng Google Charts API ?? t?o QR code
            return $"https://chart.googleapis.com/chart?chs=300x300&chld=L|0&cht=qr&chl={encoded}";
        }

        /// <summary>
        /// T?o text thông tin thanh toán
        /// </summary>
        public static string GeneratePaymentInfo(string bookingId, decimal amount)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Mã ??n: {bookingId}");
            sb.AppendLine($"S? ti?n: {amount:N0}?");
            sb.AppendLine("N?i dung: Thanh toán c?c ch?p hình");
            return sb.ToString();
        }
    }
}
