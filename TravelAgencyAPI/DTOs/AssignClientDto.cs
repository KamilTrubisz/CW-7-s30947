namespace TravelAgencyAPI.DTOs;

public class AssignClientDto
{
    public int IdClient { get; set; }
    public int IdTrip { get; set; }
    public int? PaymentDate { get; set; }
}