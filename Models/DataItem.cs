namespace Table.Models
{
    public class DataItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Department { get; set; } = string.Empty;

        public DataItem Clone()
        {
            return new DataItem
            {
                Id = this.Id,
                Name = this.Name,
                Email = this.Email,
                Age = this.Age,
                Department = this.Department
            };
        }
    }
}
