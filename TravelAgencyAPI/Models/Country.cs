namespace TravelAgencyAPI.Models;

public class Country
{
    public int IdCountry { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<CountryTrip> CountryTrips { get; set; } = new List<CountryTrip>();
}