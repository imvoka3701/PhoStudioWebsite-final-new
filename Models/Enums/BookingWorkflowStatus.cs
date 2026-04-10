namespace PhoStudioMVC.Models.Enums
{
    // Higher-level workflow for business tracking (separate from payment/booking status).
    public enum BookingWorkflowStatus
    {
        PendingReview = 0,   // Customer submitted request; waiting for admin to review/assign
        Assigned = 1,        // Admin assigned photographer
        ShootCompleted = 2,  // Photographer marked the shoot completed
        Delivered = 3,       // Album delivered (CloudAlbum created)
        Closed = 4           // Final state (paid + delivered / business completed)
    }
}

