namespace TravelAgencyAPI.Models;

public class ClientTrip
{
    public int IdClient { get; set; }
    public int IdTrip { get; set; }
    public int RegisteredAt { get; set; } 
    public int? PaymentDate { get; set; } 
    public Client Client { get; set; } = null!; 
    public Trip Trip { get; set; } = null!;
}