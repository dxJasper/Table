namespace Table.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? HireDate { get; set; }
        public decimal Salary { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
