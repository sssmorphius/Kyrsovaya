using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events;

public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
    public string? FullName { get; init; }
    public string Role { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserUpdatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
    public DateTime UpdatedAt { get; init; }
}
