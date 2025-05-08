namespace TravelAgencyAPI.DTOs;

public class ClientTripDetailsDto
{
    public int IdTrip { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int DateFrom { get; set; }
    public int DateTo { get; set; }
    public int RegisteredAt { get; set; } 
    public int? PaymentDate { get; set; }
}