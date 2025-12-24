using System;
using System.Collections.Generic; // List için gerekli
using System.ComponentModel.DataAnnotations; // Required için gerekli
using System.ComponentModel.DataAnnotations.Schema; // ForeignKey için gerekli

namespace TaskManagerSystem.Models
{
    public class UserTask
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public int Category { get; set; } // 1-Work, 2-Personal, 3-Other

        [Required]
        public int Status { get; set; } // 0-Not Started, 1-In Progress, 2-Completed

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public TimeSpan DueTime { get; set; }

        // Görevin kime atandığı (Sahibi)
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }


        public int? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User? Creator { get; set; }

        // Bir görevin birden fazla dosyası olabilir
        public List<TaskAttachment> Attachments { get; set; }
    }
}