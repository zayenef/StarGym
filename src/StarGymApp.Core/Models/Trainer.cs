namespace StarGymApp.Core.Models;

public class Trainer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
