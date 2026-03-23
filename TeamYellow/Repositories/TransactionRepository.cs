using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories;

/// <summary>
/// Repository providing data access operations for <see cref="PaymentTransaction"/> entities.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates and persists a new payment transaction record from the provided DTO.
    /// The transaction status is set to <see cref="PaymentTransactionStatus.Captured"/>
    /// and the payment timestamp is recorded as the current UTC time.
    /// </summary>
    /// <param name="addTransactionDto">The DTO containing transaction creation data.</param>
    /// <returns>The newly created and persisted <see cref="PaymentTransaction"/> entity.</returns>
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
            DiscountId = addTransactionDto.DiscountId,
            Status = PaymentTransactionStatus.Captured,
            PaidAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <summary>
    /// Checks whether a payment transaction with the specified provider order ID already exists.
    /// Used to detect and prevent duplicate payment processing (idempotency check).
    /// </summary>
    /// <param name="providerOrderId">The external payment provider's order identifier to search for.</param>
    /// <returns><c>true</c> if a matching transaction exists; otherwise <c>false</c>.</returns>
    public async Task<bool> ExistsByProviderOrderId(string providerOrderId)
    {
        return await _context.PaymentTransactions
            .AnyAsync(pt => pt.ProviderOrderId == providerOrderId);
    }
}
