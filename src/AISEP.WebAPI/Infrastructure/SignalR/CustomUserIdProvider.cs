using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AISEP.WebAPI.Infrastructure.SignalR;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Use 'sub' claim which contains our internal UserID as a string
        return connection.User?.FindFirst("sub")?.Value 
            ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
