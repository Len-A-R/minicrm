using ServiceBooking.Application.Common;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Domain.Enums;

namespace ServiceBooking.Application.Admin;

public sealed class PaymentService(
    IAdminRepository repository,
    IDateTimeProvider dateTimeProvider) : IPaymentService
{
    public async Task<ServiceResult<AdminPaymentResponse>> CreateAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        if (await repository.GetSpecialistAsync(request.SpecialistId, cancellationToken) is null)
        {
            return ServiceResult<AdminPaymentResponse>.Failure(ResultStatus.NotFound, "specialist_not_found", "Specialist was not found.");
        }

        if (request.SubscriptionId.HasValue
            && await repository.GetSubscriptionAsync(request.SubscriptionId.Value, cancellationToken) is null)
        {
            return ServiceResult<AdminPaymentResponse>.Failure(ResultStatus.NotFound, "subscription_not_found", "Subscription was not found.");
        }

        try
        {
            var payment = new PaymentTransaction(request.SpecialistId, request.SubscriptionId, request.Amount, request.Currency, "mock");
            await repository.AddPaymentAsync(payment, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            payment = await repository.GetPaymentAsync(payment.Id, cancellationToken) ?? payment;
            return ServiceResult<AdminPaymentResponse>.Success(AdminActionService.ToPaymentResponse(payment));
        }
        catch (ArgumentException exception)
        {
            return ServiceResult<AdminPaymentResponse>.Failure(ResultStatus.Validation, "invalid_payment", exception.Message);
        }
    }

    public async Task<ServiceResult<AdminPaymentResponse>> ProcessWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetPaymentAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return ServiceResult<AdminPaymentResponse>.Failure(ResultStatus.NotFound, "payment_not_found", "Payment was not found.");
        }

        try
        {
            if (request.Status == PaymentStatus.Succeeded)
            {
                var now = dateTimeProvider.UtcNow;
                payment.MarkSucceeded(request.ExternalId, now);
                if (payment.SubscriptionId.HasValue)
                {
                    var subscription = await repository.GetSubscriptionAsync(payment.SubscriptionId.Value, cancellationToken);
                    if (subscription is not null)
                    {
                        var anchor = subscription.ExpiresAt > now ? subscription.ExpiresAt : now;
                        subscription.Renew(anchor.AddMonths(1), now);
                    }
                }
            }
            else if (request.Status == PaymentStatus.Failed)
            {
                payment.MarkFailed(request.FailureReason ?? "Mock payment failed.");
            }

            await repository.SaveChangesAsync(cancellationToken);
            payment = await repository.GetPaymentAsync(payment.Id, cancellationToken) ?? payment;
            return ServiceResult<AdminPaymentResponse>.Success(AdminActionService.ToPaymentResponse(payment));
        }
        catch (ArgumentException exception)
        {
            return ServiceResult<AdminPaymentResponse>.Failure(ResultStatus.Validation, "invalid_payment_webhook", exception.Message);
        }
    }
}
