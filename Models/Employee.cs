using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama wajib diisi")]
        [StringLength(150, ErrorMessage = "Nama maksimal 150 karakter")]
        [Display(Name = "Nama")]
        public string? Nama { get; set; }

        [Required(ErrorMessage = "Badge Number wajib diisi")]
        [StringLength(50, ErrorMessage = "Badge Number maksimal 50 karakter")]
        [Display(Name = "Badge Number")]
        public string BadgeNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Divisi maksimal 100 karakter")]
        [Display(Name = "Divisi")]
        public string? Divisi { get; set; }

        [StringLength(100, ErrorMessage = "Departemen maksimal 100 karakter")]
        [Display(Name = "Departemen")]
        public string? Departemen { get; set; }
    }

    // sesuai response /api/employee { page, pageSize, total, items: [...] }
    public class EmployeePagedResponse
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int total { get; set; }
        public List<Employee> items { get; set; } = new();
    }
}
