using ServiceBooking.Application.Common;

namespace ServiceBooking.Application.Admin;

public interface IPaymentService
{
    Task<ServiceResult<AdminPaymentResponse>> CreateAsync(PaymentCreateRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AdminPaymentResponse>> ProcessWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken);
}
