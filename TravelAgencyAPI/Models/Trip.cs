namespace TravelAgencyAPI.Models;

public class Trip
{
    public int IdTrip { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DateFrom { get; set; }
    public int DateTo { get; set; }
    public int MaxPeople { get; set; }
}