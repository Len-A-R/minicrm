using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Tests.Domain;

public sealed class ReferenceEntityTests
{
    [Fact]
    public void Specialist_RequiresValidProfileData()
    {
        var specialist = new Specialist("Maria Petrova", "maria@example.com", "+15550001122", "hashed-password");

        specialist.UpdateProfile("Maria P.", "+15550001123", "Main Studio", Guid.NewGuid());
        specialist.SetAvatarUrl("https://example.com/avatar.png");

        Assert.Equal("Maria P.", specialist.FullName);
        Assert.Equal("Main Studio", specialist.VenueName);
        Assert.Equal("https://example.com/avatar.png", specialist.AvatarUrl);
    }

    [Fact]
    public void SpecialistService_RequiresPositivePriceAndDuration()
    {
        var specialistService = new SpecialistService(Guid.NewGuid(), Guid.NewGuid(), 10.126m, 30);

        specialistService.SetPrice(20.555m);
        specialistService.SetDuration(45);

        Assert.Equal(20.56m, specialistService.Price);
        Assert.Equal(45, specialistService.DurationMinutes);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SpecialistService(Guid.NewGuid(), Guid.NewGuid(), 0m, 30));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SpecialistService(Guid.NewGuid(), Guid.NewGuid(), 10m, 0));
    }

    [Fact]
    public void Client_AllowsStatusAndTagChanges()
    {
        var client = new Client("John Smith", "+15557778899");

        client.Rename("John S.");
        client.ChangeStatus(ClientStatus.Vip);
        client.SetTag("regular customer");
        client.SetTag(null);

        Assert.Equal("John S.", client.FullName);
        Assert.Equal(ClientStatus.Vip, client.Status);
        Assert.Null(client.Tag);
    }

    [Fact]
    public void LocationAndService_TrimAndValidateText()
    {
        var location = new Location(" Downtown ", " 10 Main St ", " Description ");
        var service = new Service(" Consultation ", " General advice ");
        var vacation = new Vacation(Guid.NewGuid(), new DateOnly(2026, 7, 1), "Holiday");

        location.Rename("North Office");
        location.ChangeAddress("20 North St");
        location.SetDescription(null);
        service.Rename("Detailed Consultation");
        service.SetDescription(null);
        vacation.SetReason(null);

        Assert.Equal("North Office", location.Name);
        Assert.Equal("20 North St", location.Address);
        Assert.Null(location.Description);
        Assert.Equal("Detailed Consultation", service.Name);
        Assert.Null(service.Description);
        Assert.Null(vacation.Reason);
    }

    [Fact]
    public void Entities_RejectInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => new Location("", "Address"));
        Assert.Throws<ArgumentException>(() => new Location("Name", ""));
        Assert.Throws<ArgumentException>(() => new Location("Name", "Address", new string('x', 501)));
        Assert.Throws<ArgumentException>(() => new Service(""));
        Assert.Throws<ArgumentException>(() => new Service("Name", new string('x', 501)));
        Assert.Throws<ArgumentException>(() => new Client("A", "+15557778899"));
        Assert.Throws<ArgumentException>(() => new Specialist("A", "mail@example.com", "+1555", "hashed-password"));
        Assert.Throws<ArgumentException>(() => new Specialist("Full Name", "mail@example.com", "+1555", "short"));
        Assert.Throws<ArgumentException>(() => new SpecialistService(Guid.Empty, Guid.NewGuid(), 10m, 10));
        Assert.Throws<ArgumentException>(() => new SpecialistService(Guid.NewGuid(), Guid.Empty, 10m, 10));
        Assert.Throws<ArgumentException>(() => new Vacation(Guid.Empty, new DateOnly(2026, 7, 1)));

        var client = new Client("John Smith", "+15557778899");
        var specialist = new Specialist("Maria Petrova", "maria@example.com", "+15550001122", "hashed-password");
        var vacation = new Vacation(Guid.NewGuid(), new DateOnly(2026, 7, 1));

        Assert.Throws<ArgumentException>(() => client.SetTag(new string('x', 201)));
        Assert.Throws<ArgumentException>(() => specialist.SetAvatarUrl(new string('x', 501)));
        Assert.Throws<ArgumentException>(() => vacation.SetReason(new string('x', 251)));
    }
}
