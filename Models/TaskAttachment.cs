using Newtonsoft.Json;

namespace TaskManagerSystem.Models
{
    public class TaskAttachment
    {
        public int Id { get; set; }

        // İstenen Veriler:
        public string OriginalFileName { get; set; } // Orijinal Dosya Adı
        public string StoragePath { get; set; }      // Depolama Yolu / URL
        public long FileSize { get; set; }           // Dosya boyutu (byte)
        public string ContentType { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now; // Yükleme tarihi

        // İlişkiler (Foreign Keys)
        public int TaskId { get; set; }
    

        [JsonIgnore]  // BU SATIR ÇOK ÖNEMLİ!
        public UserTask Task { get; set; }

        public int UploadedByUserId { get; set; } // Kim yükledi?
    }
}