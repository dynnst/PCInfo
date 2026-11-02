using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Data.Entities
{
    [Table("Devices")]
    public class DeviceEntity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(64)]
        [Display(Name = "Demirbaş No")]
        public string? AssetTag { get; set; }

        [Required]
        [Display(Name = "Cihaz Kategorisi")]
        public AssetCategory Category { get; set; } = AssetCategory.DesktopPc;

        [MaxLength(128)]
        [Display(Name = "Marka / Üretici")]
        public string Brand { get; set; } = string.Empty;

        [MaxLength(128)]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;

        [MaxLength(128)]
        [Display(Name = "Seri Numarası")]
        public string SerialNumber { get; set; } = string.Empty;

        [MaxLength(256)]
        [Display(Name = "Mevki / Kurulduğu Yer")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(64)]
        [Display(Name = "Oda / Birim No")]
        public string? RoomNumber { get; set; }

        [MaxLength(128)]
        [Display(Name = "Zimmetli Personel")]
        public string? AssignedPerson { get; set; }

        [Display(Name = "Tedarik Tarihi")]
        public DateTime? ProcurementDate { get; set; }

        [Display(Name = "Teslim Tarihi")]
        public DateTime? DeliveryDate { get; set; }

        [Display(Name = "Garanti Bitiş Tarihi")]
        public DateTime? WarrantyExpiryDate { get; set; }

        [Required]
        [Display(Name = "Durum")]
        public AssetStatus Status { get; set; } = AssetStatus.Active;

        [MaxLength(64)]
        [Display(Name = "IP Adresi")]
        public string? IpAddress { get; set; }

        [MaxLength(64)]
        [Display(Name = "MAC Adresi")]
        public string? MacAddress { get; set; }

        [MaxLength(500)]
        [Display(Name = "Cihaz / Alan Fotoğrafı")]
        public string? ImagePath { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Açıklama & Notlar")]
        public string? Notes { get; set; }

        public string? TechnicalDetailsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
