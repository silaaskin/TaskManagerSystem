using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public TimeSpan DueTime { get; set; } // Kullanıcı girecek


        public int UserId { get; set; } // Hangi kullanıcıya ait


        [ForeignKey("UserId")]
        public User User { get; set; }


        // Bir görevin birden fazla dosyası olabilir (8.1 gereksinimi)
        public List<TaskAttachment> Attachments { get; set; }
    }
}
