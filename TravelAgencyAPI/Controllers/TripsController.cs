using Microsoft.AspNetCore.Mvc;
using TravelAgencyAPI.Services;
using TravelAgencyAPI.DTOs;

namespace TravelAgencyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly IDatabaseService _dbService;

    public TripsController(IDatabaseService dbService)
    {
        _dbService = dbService;
    }
    // GET Zwracanie wszystkich wycieczek
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        try
        {
            var query = @"
                        SELECT 
                            t.IdTrip, 
                            t.Name, 
                            t.Description, 
                            t.DateFrom, 
                            t.DateTo, 
                            t.MaxPeople,
                            STUFF((
                                SELECT ', ' + c.Name
                                FROM Country c
                                JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                                WHERE ct.IdTrip = t.IdTrip
                                FOR XML PATH(''), TYPE
                            ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS Countries
                        FROM Trip t
                        ORDER BY t.DateFrom DESC";

            await using var reader = await _dbService.ExecuteReaderAsync(query);
            var trips = new List<TripDto>();

            while (await reader.ReadAsync())
            {
                trips.Add(new TripDto
                {
                    IdTrip = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = reader.GetString(6).Split(", ").ToList()
                });
            }
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}