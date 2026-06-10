namespace ServiceBooking.Application.Catalog;

public sealed record UpsertLocationRequest(string Name, string Address, string? Description);
