using System.Net.Mail;
using ServiceBooking.Application.Auth;
using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;
using DomainService = ServiceBooking.Domain.Entities.Service;

namespace ServiceBooking.Application.Admin;

public sealed class AdminActionService(
    IAdminRepository repository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : IAdminActionService
{
    public async Task<ServiceResult<IReadOnlyCollection<AdminSpecialistResponse>>> ListSpecialistsAsync(
        string? search,
        bool? blocked,
        CancellationToken cancellationToken)
    {
        var specialists = await repository.ListSpecialistsAsync(cancellationToken);
        var subscriptions = await repository.ListSubscriptionsAsync(cancellationToken);
        var query = specialists.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(specialist =>
                specialist.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || specialist.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                || specialist.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (blocked.HasValue)
        {
            query = query.Where(specialist => specialist.IsBlocked == blocked.Value);
        }

        var items = query
            .OrderBy(specialist => specialist.FullName)
            .Select(specialist => ToSpecialistResponse(specialist, LatestSubscription(specialist.Id, subscriptions)))
            .ToArray();

        return Success<IReadOnlyCollection<AdminSpecialistResponse>>(items);
    }

    public async Task<ServiceResult<AdminSpecialistResponse>> GetSpecialistAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var specialist = await repository.GetSpecialistAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound<AdminSpecialistResponse>("specialist_not_found", "Specialist was not found.");
        }

        return Success(ToSpecialistResponse(specialist, await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken)));
    }

    public async Task<ServiceResult<AdminSpecialistResponse>> BlockSpecialistAsync(
        Guid specialistId,
        BlockSpecialistRequest request,
        CancellationToken cancellationToken)
    {
        var specialist = await repository.GetSpecialistAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound<AdminSpecialistResponse>("specialist_not_found", "Specialist was not found.");
        }

        try
        {
            specialist.Block(request.Reason, dateTimeProvider.UtcNow);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToSpecialistResponse(specialist, await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken)));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminSpecialistResponse>("invalid_block_request", exception.Message);
        }
    }

    public async Task<ServiceResult<AdminSpecialistResponse>> UnblockSpecialistAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var specialist = await repository.GetSpecialistAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound<AdminSpecialistResponse>("specialist_not_found", "Specialist was not found.");
        }

        specialist.Unblock();
        await repository.SaveChangesAsync(cancellationToken);
        return Success(ToSpecialistResponse(specialist, await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken)));
    }

    public async Task<ServiceResult<AdminSpecialistResponse>> ChangeSpecialistPlanAsync(
        Guid specialistId,
        ChangeSpecialistPlanRequest request,
        CancellationToken cancellationToken)
    {
        var specialist = await repository.GetSpecialistAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFound<AdminSpecialistResponse>("specialist_not_found", "Specialist was not found.");
        }

        var plan = await repository.GetPlanAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            return NotFound<AdminSpecialistResponse>("plan_not_found", "Subscription plan was not found.");
        }

        try
        {
            var now = dateTimeProvider.UtcNow;
            var expiresAt = request.ExpiresAt.HasValue
                ? new DateTimeOffset(request.ExpiresAt.Value, new TimeOnly(23, 59), TimeSpan.Zero)
                : now.AddMonths(1);
            var subscription = await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken);
            if (subscription is null)
            {
                subscription = new SpecialistSubscription(specialistId, plan.Id, now, expiresAt);
                await repository.AddSubscriptionAsync(subscription, cancellationToken);
            }
            else
            {
                subscription.ChangePlan(plan.Id);
                subscription.Renew(expiresAt, now);
            }

            await repository.SaveChangesAsync(cancellationToken);
            subscription = await repository.GetActiveSubscriptionAsync(specialistId, cancellationToken) ?? subscription;
            return Success(ToSpecialistResponse(specialist, subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminSpecialistResponse>("invalid_subscription", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteSpecialistAsync(Guid specialistId, CancellationToken cancellationToken)
    {
        var specialist = await repository.GetSpecialistAsync(specialistId, cancellationToken);
        if (specialist is null)
        {
            return NotFoundBool("specialist_not_found", "Specialist was not found.");
        }

        var hasBookings = (await repository.ListBookingsAsync(cancellationToken)).Any(booking => booking.SpecialistId == specialistId);
        if (hasBookings)
        {
            return Conflict("specialist_has_bookings", "Specialist has bookings and cannot be deleted.");
        }

        repository.RemoveSpecialist(specialist);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminBookingResponse>>> ListBookingsAsync(
        BookingStatus? status,
        Guid? specialistId,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken)
    {
        var bookings = await repository.ListBookingsAsync(cancellationToken);
        var query = bookings.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(booking => booking.Status == status.Value);
        }

        if (specialistId.HasValue)
        {
            query = query.Where(booking => booking.SpecialistId == specialistId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(booking => booking.RequestedDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(booking => booking.RequestedDate <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(booking =>
                booking.ClientName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || booking.ClientPhone.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Success<IReadOnlyCollection<AdminBookingResponse>>(query
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(ToBookingResponse)
            .ToArray());
    }

    public async Task<ServiceResult<AdminBookingResponse>> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await repository.GetBookingAsync(bookingId, cancellationToken);
        return booking is null
            ? NotFound<AdminBookingResponse>("booking_not_found", "Booking was not found.")
            : Success(ToBookingResponse(booking));
    }

    public async Task<ServiceResult<AdminBookingResponse>> ChangeBookingStatusAsync(
        Guid bookingId,
        AdminBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await repository.GetBookingAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound<AdminBookingResponse>("booking_not_found", "Booking was not found.");
        }

        try
        {
            if (request.Status == BookingStatus.New)
            {
                booking.Reopen();
            }
            else if (request.Status == BookingStatus.Confirmed)
            {
                booking.Confirm(booking.ConfirmedDate ?? booking.RequestedDate, booking.ConfirmedTime ?? booking.RequestedTime);
            }
            else if (request.Status == BookingStatus.Rejected)
            {
                booking.Reject(request.RejectionReason);
            }
            else if (request.Status == BookingStatus.Completed)
            {
                booking.Complete(request.ActualRevenue ?? booking.ActualRevenue ?? booking.TotalPrice, dateTimeProvider.UtcNow);
            }

            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToBookingResponse(booking));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminBookingResponse>("invalid_booking_status", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await repository.GetBookingAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFoundBool("booking_not_found", "Booking was not found.");
        }

        repository.RemoveBooking(booking);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminClientResponse>>> ListClientsAsync(
        string? search,
        ClientStatus? status,
        CancellationToken cancellationToken)
    {
        var clients = await repository.ListClientsAsync(cancellationToken);
        var query = clients.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(client => client.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(client =>
                client.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || client.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (client.Tag?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Success<IReadOnlyCollection<AdminClientResponse>>(query
            .OrderBy(client => client.FullName)
            .Select(ToClientResponse)
            .ToArray());
    }

    public async Task<ServiceResult<AdminClientResponse>> GetClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await repository.GetClientAsync(clientId, cancellationToken);
        return client is null
            ? NotFound<AdminClientResponse>("client_not_found", "Client was not found.")
            : Success(ToClientResponse(client));
    }

    public async Task<ServiceResult<AdminClientResponse>> UpdateClientAsync(
        Guid clientId,
        AdminClientUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var client = await repository.GetClientAsync(clientId, cancellationToken);
        if (client is null)
        {
            return NotFound<AdminClientResponse>("client_not_found", "Client was not found.");
        }

        try
        {
            client.Rename(request.FullName);
            client.ChangePhone(request.Phone);
            client.ChangeStatus(request.Status);
            client.SetTag(request.Tag);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToClientResponse(client));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminClientResponse>("invalid_client", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await repository.GetClientAsync(clientId, cancellationToken);
        if (client is null)
        {
            return NotFoundBool("client_not_found", "Client was not found.");
        }

        repository.RemoveClient(client);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminServiceResponse>>> ListServicesAsync(CancellationToken cancellationToken)
    {
        var services = await repository.ListServicesAsync(cancellationToken);
        return Success<IReadOnlyCollection<AdminServiceResponse>>(services.OrderBy(service => service.Name).Select(ToServiceResponse).ToArray());
    }

    public async Task<ServiceResult<AdminServiceResponse>> GetServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await repository.GetServiceAsync(serviceId, cancellationToken);
        return service is null
            ? NotFound<AdminServiceResponse>("service_not_found", "Service was not found.")
            : Success(ToServiceResponse(service));
    }

    public async Task<ServiceResult<AdminServiceResponse>> CreateServiceAsync(UpsertAdminServiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var service = new DomainService(request.Name, request.Description);
            await repository.AddServiceAsync(service, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToServiceResponse(service));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminServiceResponse>("invalid_service", exception.Message);
        }
    }

    public async Task<ServiceResult<AdminServiceResponse>> UpdateServiceAsync(
        Guid serviceId,
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await repository.GetServiceAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return NotFound<AdminServiceResponse>("service_not_found", "Service was not found.");
        }

        try
        {
            service.Rename(request.Name);
            service.SetDescription(request.Description);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToServiceResponse(service));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminServiceResponse>("invalid_service", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await repository.GetServiceAsync(serviceId, cancellationToken);
        if (service is null)
        {
            return NotFoundBool("service_not_found", "Service was not found.");
        }

        if (await repository.ServiceHasSpecialistServicesAsync(serviceId, cancellationToken))
        {
            return Conflict("service_in_use", "Service is used by specialists and cannot be deleted.");
        }

        repository.RemoveService(service);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminLocationResponse>>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        var locations = await repository.ListLocationsAsync(cancellationToken);
        return Success<IReadOnlyCollection<AdminLocationResponse>>(locations.OrderBy(location => location.Name).Select(ToLocationResponse).ToArray());
    }

    public async Task<ServiceResult<AdminLocationResponse>> GetLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationAsync(locationId, cancellationToken);
        return location is null
            ? NotFound<AdminLocationResponse>("location_not_found", "Location was not found.")
            : Success(ToLocationResponse(location));
    }

    public async Task<ServiceResult<AdminLocationResponse>> CreateLocationAsync(UpsertAdminLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var location = new Location(request.Name, request.Address, request.Description);
            await repository.AddLocationAsync(location, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToLocationResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminLocationResponse>("invalid_location", exception.Message);
        }
    }

    public async Task<ServiceResult<AdminLocationResponse>> UpdateLocationAsync(
        Guid locationId,
        UpsertAdminLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationAsync(locationId, cancellationToken);
        if (location is null)
        {
            return NotFound<AdminLocationResponse>("location_not_found", "Location was not found.");
        }

        try
        {
            location.Rename(request.Name);
            location.ChangeAddress(request.Address);
            location.SetDescription(request.Description);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToLocationResponse(location));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminLocationResponse>("invalid_location", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await repository.GetLocationAsync(locationId, cancellationToken);
        if (location is null)
        {
            return NotFoundBool("location_not_found", "Location was not found.");
        }

        if (await repository.LocationHasSpecialistsAsync(locationId, cancellationToken))
        {
            return Conflict("location_in_use", "Location is used by specialists and cannot be deleted.");
        }

        repository.RemoveLocation(location);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminPaymentResponse>>> ListPaymentsAsync(
        PaymentStatus? status,
        Guid? specialistId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var payments = await repository.ListPaymentsAsync(cancellationToken);
        var query = payments.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(payment => payment.Status == status.Value);
        }

        if (specialistId.HasValue)
        {
            query = query.Where(payment => payment.SpecialistId == specialistId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(payment => DateOnly.FromDateTime(payment.CreatedAt.DateTime) >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(payment => DateOnly.FromDateTime(payment.CreatedAt.DateTime) <= to.Value);
        }

        return Success<IReadOnlyCollection<AdminPaymentResponse>>(query
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(ToPaymentResponse)
            .ToArray());
    }

    public async Task<ServiceResult<AdminPaymentResponse>> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetPaymentAsync(paymentId, cancellationToken);
        return payment is null
            ? NotFound<AdminPaymentResponse>("payment_not_found", "Payment was not found.")
            : Success(ToPaymentResponse(payment));
    }

    public async Task<ServiceResult<PlatformFinanceSummaryResponse>> GetFinanceSummaryAsync(CancellationToken cancellationToken)
    {
        var payments = await repository.ListPaymentsAsync(cancellationToken);
        var subscriptions = await repository.ListSubscriptionsAsync(cancellationToken);
        var activeSubscriptions = subscriptions
            .Where(subscription => subscription.Plan is not null && subscription.IsUsable(dateTimeProvider.UtcNow))
            .ToArray();
        var totalRevenue = payments
            .Where(payment => payment.Status == PaymentStatus.Succeeded)
            .Sum(payment => payment.Amount);
        var mrr = activeSubscriptions.Sum(subscription => subscription.Plan?.MonthlyPrice ?? 0);
        var paidSpecialists = activeSubscriptions.Count(subscription => subscription.Plan?.MonthlyPrice > 0);
        var arpu = paidSpecialists == 0 ? 0 : decimal.Round(mrr / paidSpecialists, 2);

        return Success(new PlatformFinanceSummaryResponse(mrr, arpu, paidSpecialists, totalRevenue));
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminSubscriptionResponse>>> ListSubscriptionsAsync(
        SubscriptionStatus? status,
        Guid? specialistId,
        CancellationToken cancellationToken)
    {
        var subscriptions = await repository.ListSubscriptionsAsync(cancellationToken);
        var query = subscriptions.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(subscription => subscription.Status == status.Value);
        }

        if (specialistId.HasValue)
        {
            query = query.Where(subscription => subscription.SpecialistId == specialistId.Value);
        }

        return Success<IReadOnlyCollection<AdminSubscriptionResponse>>(query
            .OrderByDescending(subscription => subscription.ExpiresAt)
            .Select(ToSubscriptionResponse)
            .ToArray());
    }

    public async Task<ServiceResult<AdminSubscriptionResponse>> ChangeSubscriptionStatusAsync(
        Guid subscriptionId,
        AdminSubscriptionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await repository.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            return NotFound<AdminSubscriptionResponse>("subscription_not_found", "Subscription was not found.");
        }

        subscription.ChangeStatus(request.Status);
        await repository.SaveChangesAsync(cancellationToken);
        return Success(ToSubscriptionResponse(subscription));
    }

    public async Task<ServiceResult<AdminSubscriptionResponse>> RenewSubscriptionAsync(
        Guid subscriptionId,
        RenewSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await repository.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (subscription is null)
        {
            return NotFound<AdminSubscriptionResponse>("subscription_not_found", "Subscription was not found.");
        }

        try
        {
            subscription.Renew(new DateTimeOffset(request.ExpiresAt, new TimeOnly(23, 59), TimeSpan.Zero), dateTimeProvider.UtcNow);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToSubscriptionResponse(subscription));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminSubscriptionResponse>("invalid_subscription", exception.Message);
        }
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminSettingResponse>>> ListSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.ListSettingsAsync(cancellationToken);
        return Success<IReadOnlyCollection<AdminSettingResponse>>(settings.OrderBy(setting => setting.Key).Select(ToSettingResponse).ToArray());
    }

    public async Task<ServiceResult<AdminSettingResponse>> UpsertSettingAsync(
        UpsertSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await repository.GetSettingByKeyAsync(request.Key.Trim(), cancellationToken);
            if (existing is null)
            {
                existing = new SystemSetting(request.Key, request.Value, request.Description);
                await repository.AddSettingAsync(existing, cancellationToken);
            }
            else
            {
                existing.Update(request.Value, request.Description, dateTimeProvider.UtcNow);
            }

            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToSettingResponse(existing));
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminSettingResponse>("invalid_setting", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteSettingAsync(Guid settingId, CancellationToken cancellationToken)
    {
        var setting = await repository.GetSettingAsync(settingId, cancellationToken);
        if (setting is null)
        {
            return NotFoundBool("setting_not_found", "Setting was not found.");
        }

        repository.RemoveSetting(setting);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminUserResponse>>> ListAdminsAsync(CancellationToken cancellationToken)
    {
        var admins = await repository.ListAdminsAsync(cancellationToken);
        return Success<IReadOnlyCollection<AdminUserResponse>>(admins.OrderBy(admin => admin.FullName).Select(ToAdminResponse).ToArray());
    }

    public async Task<ServiceResult<AdminUserResponse>> UpsertAdminAsync(
        Guid? adminId,
        UpsertAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidEmail(request.Email))
        {
            return Validation<AdminUserResponse>("invalid_admin_email", "Admin email format is invalid.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await repository.AdminEmailExistsAsync(normalizedEmail, adminId, cancellationToken))
        {
            return ServiceResult<AdminUserResponse>.Failure(ResultStatus.Conflict, "admin_email_conflict", "Admin email is already used.");
        }

        try
        {
            AdminUser admin;
            if (adminId.HasValue)
            {
                admin = await repository.GetAdminByIdAsync(adminId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("not_found");
                admin.Update(request.FullName, normalizedEmail, request.IsActive);
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    admin.ChangePasswordHash(passwordHasher.Hash(request.Password));
                }
            }
            else
            {
                if (!IsStrongPassword(request.Password ?? string.Empty))
                {
                    return Validation<AdminUserResponse>("weak_admin_password", "Admin password must contain at least 8 characters, uppercase, lowercase and digit.");
                }

                admin = new AdminUser(request.FullName, normalizedEmail, passwordHasher.Hash(request.Password!));
                if (!request.IsActive)
                {
                    admin.Deactivate();
                }

                await repository.AddAdminAsync(admin, cancellationToken);
            }

            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToAdminResponse(admin));
        }
        catch (InvalidOperationException)
        {
            return NotFound<AdminUserResponse>("admin_not_found", "Admin was not found.");
        }
        catch (ArgumentException exception)
        {
            return Validation<AdminUserResponse>("invalid_admin", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteAdminAsync(Guid adminId, CancellationToken cancellationToken)
    {
        var admin = await repository.GetAdminByIdAsync(adminId, cancellationToken);
        if (admin is null)
        {
            return NotFoundBool("admin_not_found", "Admin was not found.");
        }

        var activeAdmins = (await repository.ListAdminsAsync(cancellationToken)).Count(item => item.IsActive);
        if (admin.IsActive && activeAdmins <= 1)
        {
            return Conflict("last_admin", "Last active admin cannot be deleted.");
        }

        repository.RemoveAdmin(admin);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyCollection<SubscriptionPlanResponse>>> ListPlansAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var plans = await repository.ListPlansAsync(cancellationToken);
        var query = activeOnly ? plans.Where(plan => plan.IsActive) : plans;
        return Success<IReadOnlyCollection<SubscriptionPlanResponse>>(query.OrderBy(plan => plan.MonthlyPrice).Select(ToPlanResponse).ToArray());
    }

    public async Task<ServiceResult<SubscriptionPlanResponse>> GetPlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await repository.GetPlanAsync(planId, cancellationToken);
        return plan is null
            ? NotFound<SubscriptionPlanResponse>("plan_not_found", "Subscription plan was not found.")
            : Success(ToPlanResponse(plan));
    }

    public async Task<ServiceResult<SubscriptionPlanResponse>> CreatePlanAsync(
        UpsertSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (await repository.PlanNameExistsAsync(request.Name.Trim(), null, cancellationToken))
        {
            return ServiceResult<SubscriptionPlanResponse>.Failure(ResultStatus.Conflict, "plan_name_conflict", "Plan name is already used.");
        }

        try
        {
            var plan = new SubscriptionPlan(
                request.Name,
                request.MonthlyPrice,
                request.BookingLimit,
                request.ServiceLimit,
                request.Description);
            if (!request.IsActive)
            {
                plan.Deactivate();
            }

            await repository.AddPlanAsync(plan, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToPlanResponse(plan));
        }
        catch (ArgumentException exception)
        {
            return Validation<SubscriptionPlanResponse>("invalid_plan", exception.Message);
        }
    }

    public async Task<ServiceResult<SubscriptionPlanResponse>> UpdatePlanAsync(
        Guid planId,
        UpsertSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetPlanAsync(planId, cancellationToken);
        if (plan is null)
        {
            return NotFound<SubscriptionPlanResponse>("plan_not_found", "Subscription plan was not found.");
        }

        if (await repository.PlanNameExistsAsync(request.Name.Trim(), planId, cancellationToken))
        {
            return ServiceResult<SubscriptionPlanResponse>.Failure(ResultStatus.Conflict, "plan_name_conflict", "Plan name is already used.");
        }

        try
        {
            plan.Update(request.Name, request.Description, request.MonthlyPrice, request.BookingLimit, request.ServiceLimit, request.IsActive);
            await repository.SaveChangesAsync(cancellationToken);
            return Success(ToPlanResponse(plan));
        }
        catch (ArgumentException exception)
        {
            return Validation<SubscriptionPlanResponse>("invalid_plan", exception.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await repository.GetPlanAsync(planId, cancellationToken);
        if (plan is null)
        {
            return NotFoundBool("plan_not_found", "Subscription plan was not found.");
        }

        if ((await repository.ListSubscriptionsAsync(cancellationToken)).Any(subscription => subscription.PlanId == planId))
        {
            return Conflict("plan_in_use", "Plan is used by subscriptions and cannot be deleted.");
        }

        repository.RemovePlan(plan);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public static AdminPaymentResponse ToPaymentResponse(PaymentTransaction payment)
    {
        return new AdminPaymentResponse(
            payment.Id,
            payment.SpecialistId,
            payment.Specialist?.FullName ?? string.Empty,
            payment.SubscriptionId,
            payment.Amount,
            payment.Currency,
            payment.Provider,
            payment.Status,
            payment.ExternalId,
            payment.FailureReason,
            payment.CreatedAt,
            payment.PaidAt);
    }

    private static SpecialistSubscription? LatestSubscription(Guid specialistId, IReadOnlyCollection<SpecialistSubscription> subscriptions)
    {
        return subscriptions
            .Where(subscription => subscription.SpecialistId == specialistId)
            .OrderByDescending(subscription => subscription.ExpiresAt)
            .FirstOrDefault();
    }

    private static AdminSpecialistResponse ToSpecialistResponse(Specialist specialist, SpecialistSubscription? subscription)
    {
        return new AdminSpecialistResponse(
            specialist.Id,
            specialist.FullName,
            specialist.Email,
            specialist.Phone,
            specialist.VenueName,
            specialist.LocationId,
            specialist.IsBlocked,
            specialist.BlockReason,
            specialist.CreatedAt,
            subscription?.Plan?.Name,
            subscription?.Status,
            subscription?.ExpiresAt);
    }

    private static AdminBookingResponse ToBookingResponse(Booking booking)
    {
        return new AdminBookingResponse(
            booking.Id,
            booking.SpecialistId,
            booking.Specialist?.FullName ?? string.Empty,
            booking.ClientName,
            booking.ClientPhone,
            booking.RequestedDate,
            booking.RequestedTime,
            booking.TotalPrice,
            booking.Status,
            booking.CreatedAt,
            string.Join(", ", booking.Services.Select(service => service.ServiceName)));
    }

    private static AdminClientResponse ToClientResponse(Client client)
    {
        return new AdminClientResponse(
            client.Id,
            client.FullName,
            client.Phone,
            client.Status,
            client.Tag,
            client.Bookings.Count,
            client.Bookings.OrderByDescending(booking => booking.CreatedAt).FirstOrDefault()?.CreatedAt);
    }

    private static AdminServiceResponse ToServiceResponse(DomainService service)
    {
        return new AdminServiceResponse(service.Id, service.Name, service.Description);
    }

    private static AdminLocationResponse ToLocationResponse(Location location)
    {
        return new AdminLocationResponse(location.Id, location.Name, location.Address, location.Description);
    }

    private static AdminSubscriptionResponse ToSubscriptionResponse(SpecialistSubscription subscription)
    {
        return new AdminSubscriptionResponse(
            subscription.Id,
            subscription.SpecialistId,
            subscription.Specialist?.FullName ?? string.Empty,
            subscription.PlanId,
            subscription.Plan?.Name ?? string.Empty,
            subscription.Status,
            subscription.StartedAt,
            subscription.ExpiresAt,
            subscription.RenewedAt);
    }

    private static AdminSettingResponse ToSettingResponse(SystemSetting setting)
    {
        return new AdminSettingResponse(setting.Id, setting.Key, setting.Value, setting.Description, setting.UpdatedAt);
    }

    private static AdminUserResponse ToAdminResponse(AdminUser admin)
    {
        return new AdminUserResponse(admin.Id, admin.FullName, admin.Email, admin.IsActive, admin.CreatedAt, admin.LastLoginAt);
    }

    private static SubscriptionPlanResponse ToPlanResponse(SubscriptionPlan plan)
    {
        return new SubscriptionPlanResponse(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.BookingLimit,
            plan.ServiceLimit,
            plan.IsActive);
    }

    private static ServiceResult<T> Success<T>(T value) => ServiceResult<T>.Success(value);

    private static ServiceResult<T> NotFound<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.NotFound, code, message);
    }

    private static ServiceResult<bool> NotFoundBool(string code, string message)
    {
        return ServiceResult<bool>.Failure(ResultStatus.NotFound, code, message);
    }

    private static ServiceResult<T> Validation<T>(string code, string message)
    {
        return ServiceResult<T>.Failure(ResultStatus.Validation, code, message);
    }

    private static ServiceResult<bool> Conflict(string code, string message)
    {
        return ServiceResult<bool>.Failure(ResultStatus.Conflict, code, message);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsStrongPassword(string password)
    {
        return password.Length >= 8
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
