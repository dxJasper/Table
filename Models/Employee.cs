namespace Table.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? HireDate { get; set; }
        public decimal Salary { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public Employee Clone()
        {
            return new Employee
            {
                Id = this.Id,
                Name = this.Name,
                Email = this.Email,
                Age = this.Age,
                Department = this.Department,
                IsActive = this.IsActive,
                HireDate = this.HireDate,
                Salary = this.Salary,
                Position = this.Position,
                Notes = this.Notes
            };
        }
    }
}
