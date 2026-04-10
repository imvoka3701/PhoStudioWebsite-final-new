namespace PhoStudioMVC.Models.Enums
{
    public enum BookingStatus
    {
        TimeSlotLocked  = 0, // Vừa chọn giờ, đang giữ slot 15 phút
        Deposited       = 1, // Đã thanh toán cọc ≥ 30%
        ShootCompleted  = 2, // Thợ ảnh xác nhận đã chụp xong
        FullyPaid       = 3, // Đã thanh toán đủ 100%
        Cancelled       = 4  // Hủy
    }
}
