namespace ServiceBooking.Application.Catalog;

public sealed record LocationResponse(Guid Id, string Name, string Address, string? Description);
