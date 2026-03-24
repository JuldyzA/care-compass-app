using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository providing data access operations for <see cref="PaymentTransaction"/> entities.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(ApplicationDbContext context, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates and persists a new payment transaction record from the provided data transfer object.
        /// </summary>
        /// <param name="addTransactionDto">The DTO containing transaction creation data.</param>
        /// <returns>The newly created payment transaction.</returns>
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

            try
            {
                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment transaction {PaymentTransactionId} created successfully for subscription {SubscriptionId}.", 
                    transaction.PaymentTransactionId, transaction.SubscriptionId);
                return transaction;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating payment transaction for subscription {SubscriptionId} and provider order {ProviderOrderId}.",
                    transaction.SubscriptionId,
                    transaction.ProviderOrderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating payment transaction for subscription {SubscriptionId} and provider order {ProviderOrderId}.",
                    transaction.SubscriptionId,
                    transaction.ProviderOrderId);
                throw;
            }
        }

        /// <summary>
        /// Checks whether a payment transaction already exists for the specified provider order identifier.
        /// </summary>
        /// <param name="providerOrderId">The external provider order identifier.</param>
        /// <returns><c>true</c> if a matching transaction exists; otherwise <c>false</c>.</returns>
        public async Task<bool> ExistsByProviderOrderId(string providerOrderId)
        {
            return await _context.PaymentTransactions
                .AnyAsync(pt => pt.ProviderOrderId == providerOrderId);
        }
    }
}