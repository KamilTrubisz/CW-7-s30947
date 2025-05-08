using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TravelAgencyAPI.DTOs;
using TravelAgencyAPI.Services;
using System.Text.RegularExpressions;

namespace TravelAgencyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IDatabaseService _dbService;

    public ClientsController(IDatabaseService dbService)
    {
        _dbService = dbService;
    }
    //GET /api/trips  Pobieranie wszystkich dostępnych wycieczek
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var clientExists = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Client WHERE IdClient = @IdClient",
                new SqlParameter("@IdClient", id));

            if (clientExists == 0) return NotFound($"Client with ID {id} not found");

            var trips = new List<object>();
            var reader = await _dbService.ExecuteReaderAsync(@"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo,
                       ct.RegisteredAt, ct.PaymentDate
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                WHERE ct.IdClient = @IdClient
                ORDER BY ct.RegisteredAt DESC",
                new SqlParameter("@IdClient", id));

            while (await reader.ReadAsync())
            {
                trips.Add(new 
                {
                    IdTrip = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    RegisteredAt = reader.GetInt32(5),
                    PaymentDate = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                });
            }
            
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    //POST Dodawanie Klienta do bazy danych
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDto clientDto)
    {
        if (string.IsNullOrWhiteSpace(clientDto.FirstName))
            return BadRequest("FirstName is required");
        if (string.IsNullOrWhiteSpace(clientDto.LastName))
            return BadRequest("LastName is required");
        if (string.IsNullOrWhiteSpace(clientDto.Email))
            return BadRequest("Email is required");
        if (string.IsNullOrWhiteSpace(clientDto.Pesel))
            return BadRequest("PESEL is required");
        
        if (clientDto.Pesel.Length != 11 || !clientDto.Pesel.All(char.IsDigit))
            return BadRequest("PESEL must be exactly 11 digits");

        // Walidacja telefonu
        if (!string.IsNullOrWhiteSpace(clientDto.Telephone))
        {
            if (!clientDto.Telephone.StartsWith("+") || clientDto.Telephone.Length != 12 || !clientDto.Telephone[1..].All(char.IsDigit))
                return BadRequest("Telephone must start with '+' and contain 11 digits after");
        }

        // Walidacja emaila
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(clientDto.Email))
        {
        return BadRequest("Email must be in valid format: user@domain.com");
        }
        try
        {
            var peselExists = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Client WHERE Pesel = @Pesel",
                new SqlParameter("@Pesel", clientDto.Pesel));

            if (peselExists > 0)
                return Conflict("Client with this PESEL already exists");

            var newId = await _dbService.ExecuteScalarAsync<int>(@"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)",
                new SqlParameter("@FirstName", clientDto.FirstName),
                new SqlParameter("@LastName", clientDto.LastName),
                new SqlParameter("@Email", clientDto.Email),
                new SqlParameter("@Telephone", clientDto.Telephone ?? (object)DBNull.Value),
                new SqlParameter("@Pesel", clientDto.Pesel));

            return CreatedAtAction(nameof(GetClient), new { id = newId }, new { IdClient = newId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Database error: {ex.Message}");
        }
    }
    //PUT ID/trips/tripID Dodawanie wycieczki do klienta
    [HttpPut("{idClient}/trips/{idTrip}")]
    public async Task<IActionResult> AssignToTrip(
        int idClient,
        int idTrip,
        [FromBody] AssignClientDto dto)
    {
        try
        {
            // 1. Sprawdź czy klient istnieje
            var clientExists = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Client WHERE IdClient = @IdClient",
                new SqlParameter("@IdClient", idClient));

            if (clientExists == 0) return NotFound($"Client with ID {idClient} not found");

            // 2. Sprawdź czy wycieczka istnieje
            var tripExists = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Trip WHERE IdTrip = @IdTrip",
                new SqlParameter("@IdTrip", idTrip));

            if (tripExists == 0) return NotFound($"Trip with ID {idTrip} not found");

            // 3. Sprawdź czy połączenie istnieje
            var existingRegistration = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                new SqlParameter("@IdClient", idClient),
                new SqlParameter("@IdTrip", idTrip));

            if (existingRegistration > 0) return Conflict("Client is already registered for this trip");

            // 4. Czy dostępne miejsce
            var maxPeople = await _dbService.ExecuteScalarAsync<int>(
                "SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip",
                new SqlParameter("@IdTrip", idTrip));

            var currentParticipants = await _dbService.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip",
                new SqlParameter("@IdTrip", idTrip));

            if (currentParticipants >= maxPeople)
                return BadRequest("Trip has reached maximum capacity");

            // 5. Dopisanie klienta do wycieczki
            var registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            var paymentDate = dto.PaymentDate.HasValue 
                ? dto.PaymentDate.Value 
                : (object)DBNull.Value;

            await _dbService.ExecuteNonQueryAsync(
                @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate)",
                new SqlParameter("@IdClient", idClient),
                new SqlParameter("@IdTrip", idTrip),
                new SqlParameter("@RegisteredAt", registeredAt),
                new SqlParameter("@PaymentDate", paymentDate));

            return Ok(new { Message = "Client successfully registered for the trip" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    // DEL ID/trip/tripID Usuwanie klienta z wycieczki
    [HttpDelete("{idClient}/trips/{idTrip}")]
    public async Task<IActionResult> RemoveFromTrip(int idClient, int idTrip)
    {
        try
        {
            var affectedRows = await _dbService.ExecuteNonQueryAsync(
                "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                new SqlParameter("@IdClient", idClient),
                new SqlParameter("@IdTrip", idTrip));

            return affectedRows > 0 
                ? NoContent() 
                : NotFound("Registration not found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    //GET Wypisanie klienta
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        try
        {
            var reader = await _dbService.ExecuteReaderAsync(
                "SELECT * FROM Client WHERE IdClient = @IdClient",
                new SqlParameter("@IdClient", id));

            if (!await reader.ReadAsync())
                return NotFound();

            return Ok(new
            {
                IdClient = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Telephone = reader.IsDBNull(4) ? null : reader.GetString(4),
                Pesel = reader.GetString(5)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}