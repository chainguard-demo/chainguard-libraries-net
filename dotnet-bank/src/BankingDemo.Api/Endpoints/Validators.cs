using FluentValidation;

namespace BankingDemo.Api.Endpoints;

public class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    public OpenAccountRequestValidator()
    {
        RuleFor(r => r.OwnerName).NotEmpty().MaximumLength(100);
        RuleFor(r => r.InitialDeposit).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000).PrecisionScale(12, 2, true);
    }
}

public class MoneyRequestValidator : AbstractValidator<MoneyRequest>
{
    public MoneyRequestValidator()
    {
        RuleFor(r => r.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(12, 2, true);
        RuleFor(r => r.Description).MaximumLength(200);
    }
}

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(r => r.FromAccountId).NotEmpty();
        RuleFor(r => r.ToAccountId).NotEmpty().NotEqual(r => r.FromAccountId)
            .WithMessage("Destination account must differ from the source account.");
        RuleFor(r => r.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(12, 2, true);
        RuleFor(r => r.Description).MaximumLength(200);
    }
}

/// <summary>Runs the registered FluentValidation validator for <typeparamref name="T"/> before the endpoint.</summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var request = context.Arguments.OfType<T>().First();

        var result = await validator.ValidateAsync(request);
        return result.IsValid
            ? await next(context)
            : Results.ValidationProblem(result.ToDictionary());
    }
}
