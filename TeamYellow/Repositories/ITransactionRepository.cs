using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public interface ITransactionRepository
{
    Task<PaymentTransaction> CreateTransaction(AddTransactionDto addTransactionDto);
    Task<bool> ExistsByProviderOrderId(string providerOrderId);
}
