using TaskManagerSystem.Data;
using TaskManagerSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace TaskManagerSystem.Controllers
{
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TasksController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Ana Sayfa (Admin herkesi görür, User sadece kendini)
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            List<UserTask> tasks;

            if (userRole == "Admin")
            {
                // Admin: tüm görevler + kullanıcı + ekler
                tasks = _context.Tasks
                    .Include(t => t.User)          // 👈 EKLENDİ
                    .Include(t => t.Attachments)
                    .ToList();

                ViewBag.IsAdmin = true;
            }
            else
            {
                // User: sadece kendi görevleri + kullanıcı + ekler
                tasks = _context.Tasks
                    .Include(t => t.Attachments)
                    .Where(t => t.UserId == userId)
                    .ToList();

                ViewBag.IsAdmin = false;
            }

            return View(tasks);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userRole == "Admin")
            {
                ViewBag.Users = _context.Users.ToList();
            }

            return View();
        }

        [HttpPost]
        public IActionResult Create(string title, string description, int category, DateTime dueDate, TimeSpan dueTime, List<IFormFile> attachments, int? assignedUserId)
        {
            // Uç Durum Kontrolü: Başlık boş olamaz
            if (string.IsNullOrWhiteSpace(title))
            {
                return View();
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (currentUserId == null) return RedirectToAction("Login", "Account");

            int targetUserId = currentUserId.Value;

            // Gereksinim 8.2: Admin başkasına görev atayabilir
            if (userRole == "Admin" && assignedUserId.HasValue)
            {
                targetUserId = assignedUserId.Value;
            }

            var newTask = new UserTask
            {
                Title = title,
                Description = string.IsNullOrEmpty(description) ? "" : description,
                Category = category,
                Status = 0,
                DueDate = dueDate,
                DueTime = dueTime,
                UserId = targetUserId
            };

            _context.Tasks.Add(newTask);
            _context.SaveChanges();

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    UploadFileInternal(file, newTask.Id, currentUserId.Value);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login", "Account");

            // Görevi ekleriyle birlikte getir
            var task = _context.Tasks
                .Include(t => t.Attachments)
                .FirstOrDefault(t => t.Id == id);

            if (task == null) return NotFound();

            // GEREKSİNİM 8.2: Normal kullanıcı başkasının görevini göremez/düzenleyemez
            if (userRole != "Admin" && task.UserId != userId)
            {
                return Unauthorized();
            }

            // GEREKSİNİM 8.2: Eğer düzenleyen kişi Admin ise, reassign (yeniden atama) 
            // yapabilmesi için tüm kullanıcı listesini gönderiyoruz.
            if (userRole == "Admin")
            {
                ViewBag.Users = _context.Users.ToList();
            }

            return View(task);
        }

        // POST: Edit İşlemi
        [HttpPost]
        public IActionResult Edit(int id, string title, string description, int category, int status, DateTime dueDate, TimeSpan dueTime, List<IFormFile> attachments, int? assignedUserId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login", "Account");

            var existingTask = _context.Tasks.FirstOrDefault(t => t.Id == id);
            if (existingTask == null) return NotFound();

            // GEREKSİNİM 8.2: Yetki kontrolü (Back-end enforcement)
            if (userRole != "Admin" && existingTask.UserId != userId) return Unauthorized();

            // Temel alanları güncelle
            existingTask.Title = title;
            existingTask.Description = description ?? "";
            existingTask.Category = category;
            existingTask.Status = status;
            existingTask.DueDate = dueDate;
            existingTask.DueTime = dueTime;

            // GEREKSİNİM 8.2: Admin başka bir kullanıcıya görev atayabilir
            if (userRole == "Admin" && assignedUserId.HasValue)
            {
                existingTask.UserId = assignedUserId.Value;
            }

            _context.Tasks.Update(existingTask);
            _context.SaveChanges();

            // GEREKSİNİM 8.1: Düzenleme sırasında yeni dosya yükleme desteği
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    UploadFileInternal(file, existingTask.Id, userId.Value);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UploadAttachment(int taskId, IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum açın." });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Dosya seçilmedi." });

            var result = UploadFileInternal(file, taskId, userId.Value);

            if (result)
                return Json(new { success = true, message = "Dosya yüklendi." });
            else
                return Json(new { success = false, message = "Hata: Dosya boyutu (Max 10MB) veya formatı hatalı." });
        }

        private bool UploadFileInternal(IFormFile file, int taskId, int userId)
        {
            // Gereksinim 8.1: Maksimum 10MB sınırı
            if (file.Length > 10 * 1024 * 1024) return false;

            // Gereksinim 8.1: Desteklenen formatlar
            var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext)) return false;

            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                UploadedByUserId = userId,
                OriginalFileName = file.FileName,
                StoragePath = uniqueFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadDate = DateTime.Now
            };

            _context.TaskAttachments.Add(attachment);
            _context.SaveChanges();

            return true;
        }

        [HttpGet]
        public IActionResult DownloadAttachment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login", "Account");

            // Gereksinim 8.1: Dosyalar sadece yetkili kullanıcılara açık olmalı
            var attachment = _context.TaskAttachments.Include(a => a.Task).FirstOrDefault(a => a.Id == id);
            if (attachment == null) return NotFound();

            if (userRole != "Admin" && attachment.Task.UserId != userId)
            {
                return Unauthorized("Bu dosyaya erişim yetkiniz yok.");
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", attachment.StoragePath);

            if (!System.IO.File.Exists(filePath)) return NotFound("Dosya sunucuda bulunamadı.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, attachment.ContentType, attachment.OriginalFileName);
        }

        [HttpPost]
        public IActionResult DeleteAttachment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var attachment = _context.TaskAttachments.Include(a => a.Task).FirstOrDefault(a => a.Id == id);
            if (attachment == null) return Json(new { success = false });

            if (userRole != "Admin" && attachment.Task.UserId != userId)
            {
                return Json(new { success = false, message = "Yetkisiz işlem." });
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", attachment.StoragePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.TaskAttachments.Remove(attachment);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult StatusUpdate(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var task = _context.Tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();

            if (userRole != "Admin" && task.UserId != userId) return Unauthorized();

            if (task.Status == 2) task.Status = 0;
            else task.Status++;

            _context.Tasks.Update(task);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login", "Account");

            var task = _context.Tasks
                .Include(t => t.Attachments)
                .FirstOrDefault(t => t.Id == id);

            if (task == null) return NotFound();

            if (userRole != "Admin" && task.UserId != userId) return Unauthorized();

            // Gereksinim 8.1: Görev silindiğinde tüm ekler de diskten temizlenir
            if (task.Attachments != null && task.Attachments.Any())
            {
                foreach (var file in task.Attachments)
                {
                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoragePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            _context.Tasks.Remove(task);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // API Endpoints
        [HttpGet("api/tasks")]
        public IActionResult GetTasks(int? category, int? status, bool? completed, bool? overdue, int? upcoming)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return Unauthorized(new { message = "Lütfen giriş yapınız." });

            IQueryable<UserTask> query = _context.Tasks;

            if (userRole != "Admin")
            {
                query = query.Where(t => t.UserId == userId);
            }

            if (category.HasValue) query = query.Where(t => t.Category == category.Value);
            if (status.HasValue) query = query.Where(t => t.Status == status.Value);

            if (completed.HasValue)
            {
                query = completed.Value ? query.Where(t => t.Status == 2) : query.Where(t => t.Status != 2);
            }

            var tasks = query.ToList();

            if (overdue.HasValue && overdue.Value)
            {
                var now = DateTime.Now;
                tasks = tasks.Where(t => t.Status != 2 && (t.DueDate.Date + t.DueTime) < now).ToList();
            }

            if (upcoming.HasValue && upcoming.Value > 0)
            {
                var now = DateTime.Now;
                var limit = now.AddDays(upcoming.Value);
                tasks = tasks.Where(t => t.Status != 2 && (t.DueDate.Date + t.DueTime) >= now && (t.DueDate.Date + t.DueTime) <= limit).ToList();
            }

            var taskResponses = tasks.Select(t =>
            {
                var deadline = t.DueDate.Date + t.DueTime;
                var remaining = deadline - DateTime.Now;

                bool isOverdue = t.Status != 2 && remaining.TotalSeconds <= 0;
                bool isUrgent = t.Status != 2 && remaining.TotalHours > 0 && remaining.TotalHours <= 24;
                bool isApproaching = t.Status != 2 && remaining.TotalHours > 24 && remaining.TotalHours <= 72;

                return new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    category = new { id = t.Category, name = GetCategoryName(t.Category) },
                    status = new { id = t.Status, name = GetStatusName(t.Status) },
                    dueDate = t.DueDate.ToString("yyyy-MM-dd"),
                    dueTime = t.DueTime.ToString(@"hh\:mm"),
                    alert = isOverdue || isUrgent || isApproaching,
                    alertLevel = isOverdue ? "danger" : (isUrgent ? "warning" : (isApproaching ? "info" : "none"))
                };
            }).ToList();

            return Ok(new { success = true, count = taskResponses.Count, data = taskResponses });
        }

        [HttpGet("api/tasks/stats")]
        public IActionResult GetStats()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return Unauthorized();

            IQueryable<UserTask> query = _context.Tasks;
            if (userRole != "Admin") query = query.Where(t => t.UserId == userId);

            var userTasks = query.ToList();

            var categoryData = userTasks
                .GroupBy(t => t.Category)
                .Select(g => new { CategoryName = GetCategoryName(g.Key), Count = g.Count() })
                .ToList();

            return Ok(new TaskStatsViewModel
            {
                TotalTasks = userTasks.Count,
                CompletedTasks = userTasks.Count(t => t.Status == 2),
                PendingTasks = userTasks.Count(t => t.Status != 2),
                OverdueTasks = userTasks.Count(t => t.Status != 2 && (t.DueDate.Date + t.DueTime) < DateTime.Now),
                Categories = categoryData.Select(x => x.CategoryName).ToArray(),
                CategoryCounts = categoryData.Select(x => x.Count).ToArray()
            });
        }

        [HttpGet]
        public IActionResult PreviewAttachment(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login", "Account");

            // Dosyayı getir
            var attachment = _context.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefault(a => a.Id == id);

            if (attachment == null) return NotFound("Dosya bulunamadı.");

            // Yetki kontrolü
            if (userRole != "Admin" && attachment.Task.UserId != userId)
            {
                return Unauthorized("Bu dosyaya erişim yetkiniz yok.");
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", attachment.StoragePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Dosya sunucuda bulunamadı.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var ext = Path.GetExtension(attachment.OriginalFileName).ToLower();

            // Dosya tipine göre önizleme
            if (ext == ".pdf")
            {
                // PDF'i tarayıcıda aç (inline)
                return File(fileBytes, "application/pdf");
            }
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                // Resmi tarayıcıda göster
                return File(fileBytes, attachment.ContentType);
            }
            else if (ext == ".docx")
            {
                // Word dosyası için HTML önizleme sayfası
                return View("PreviewDocument", new PreviewViewModel
                {
                    FileName = attachment.OriginalFileName,
                    FileType = "Word Document",
                    FileSize = attachment.FileSize,
                    Message = "Word dosyalarını önizlemek için indirip Microsoft Word ile açabilirsiniz.",
                    DownloadUrl = Url.Action("DownloadAttachment", new { id = attachment.Id })
                });
            }
            else if (ext == ".xlsx")
            {
                // Excel dosyası için HTML önizleme sayfası
                return View("PreviewDocument", new PreviewViewModel
                {
                    FileName = attachment.OriginalFileName,
                    FileType = "Excel Spreadsheet",
                    FileSize = attachment.FileSize,
                    Message = "Excel dosyalarını önizlemek için indirip Microsoft Excel ile açabilirsiniz.",
                    DownloadUrl = Url.Action("DownloadAttachment", new { id = attachment.Id })
                });
            }
            else
            {
                // Desteklenmeyen dosya tipi - direkt indir
                return File(fileBytes, attachment.ContentType, attachment.OriginalFileName);
            }
        }

        private string GetCategoryName(int id) => id switch { 1 => "Work", 2 => "Personal", 3 => "Other", _ => "Unknown" };
        private string GetStatusName(int id) => id switch { 0 => "Not Started", 1 => "In Progress", 2 => "Completed", _ => "Unknown" };
    }
}