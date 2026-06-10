using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.SpecialistClients;

public sealed record UpdateClientStatusRequest(ClientStatus Status);

public sealed record UpdateClientTagRequest(string? Tag);
