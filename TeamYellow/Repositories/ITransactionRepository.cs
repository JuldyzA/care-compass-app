using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

/// <summary>
/// Defines data access operations for <see cref="PaymentTransaction"/> entities.
/// </summary>
public interface ITransactionRepository
{
    Task<PaymentTransaction> CreateTransaction(AddTransactionDto addTransactionDto);
    Task<bool> ExistsByProviderOrderId(string providerOrderId);
}
