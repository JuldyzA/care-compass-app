using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PaymentTransaction> CreateTransaction(AddTransactionDto addTransactionDto)
    {
        var transaction = new PaymentTransaction
        {
            SubscriptionId = addTransactionDto.SubscriptionId,
            PayerName = addTransactionDto.PayerName,
            Amount = addTransactionDto.Amount,
            Currency = addTransactionDto.Currency,
            Provider = addTransactionDto.Provider,
            ProviderOrderId = addTransactionDto.ProviderOrderId,
            Status = PaymentTransactionStatus.Captured,
            PaidAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> ExistsByProviderOrderId(string providerOrderId)
    {
        return await _context.PaymentTransactions
            .AnyAsync(pt => pt.ProviderOrderId == providerOrderId);
    }
}
