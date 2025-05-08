namespace TravelAgencyAPI.Models;

public class CountryTrip
{
    public int IdCountry { get; set; }
    public Country Country { get; set; } = null!;
    public int IdTrip { get; set; }
    public Trip Trip { get; set; } = null!;
}