using System;
using System.Collections.Generic;

namespace PhoStudioMVC.Models.ViewModels
{
    public class SelectedPhotoItem
    {
        public int AssetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public DateTime? FavoritedAt { get; set; }
    }

    public class SelectedPhotosBookingGroup
    {
        public string BookingId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? ClientPhone { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string? PhotographerName { get; set; }
        public string? LatestRequestNote { get; set; }
        public DateTime? LatestRequestAt { get; set; }
        public int FavoriteCount { get; set; }
        public List<SelectedPhotoItem> Photos { get; set; } = new();
    }
}

