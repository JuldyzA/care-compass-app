using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Helpers
{
    /// <summary>
    /// Provides helper methods for mapping client entities and DTOs to view models.
    /// </summary>
    public class ClientHelper
    {
        /// <summary>
        /// Maps a client table DTO to a View Model, processing initials, status boolean flags, 
        /// and calculating the human-readable record range (e.g., "Showing 1 to 5 of 20") for pagination.
        /// </summary>
        /// <param name="dto">The source DTO containing the list of clients and pagination metadata.</param>
        /// <returns>A view model formatted for display in the client table UI.</returns>
        public static ClientTableVM MapToVm(ClientTableDto dto)
        {
            List<ClientVM> clientVMs = dto.Clients.Select(c =>
                new ClientVM
                {
                    ClientId = c.ClientId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Initials = $"{GetInitial(c.FirstName)}{GetInitial(c.LastName)}",
                    Email = c.Email,
                    Phone = c.Phone,
                    Status = c.Status == ClientStatus.Active ? true : false,
                    CreatedAt = c.CreatedAt.ToLocalTime()
                }).ToList();

            int startEntry;
            int endEntry;
            if (dto.TotalCount == 0)
            {
                startEntry = 0;
                endEntry = 0;
            }
            else
            {
                int calculatedStart = (dto.Page - 1) * dto.PageSize + 1;
                int calculatedEnd = dto.Page * dto.PageSize;
                startEntry = Math.Max(0, Math.Min(dto.TotalCount, calculatedStart));
                endEntry = Math.Max(0, Math.Min(dto.TotalCount, calculatedEnd));
            }

            int totalPages = (dto.PageSize > 0) ? (int)Math.Ceiling(dto.TotalCount / (double)dto.PageSize) : 1;

            return new ClientTableVM
            {
                Page = dto.Page,
                PageSize = dto.PageSize,
                TotalPages = Math.Max(1, totalPages),
                StartEntry = startEntry,
                EndEntry = endEntry,
                TotalCount = dto.TotalCount,
                Clients = clientVMs,
                SearchTerm = dto.SearchTerm,
                StartDate = dto.StartDate.HasValue ? dto.StartDate.Value.ToString("yyyy-MM-dd") : null,
                EndDate = dto.EndDate.HasValue ? dto.EndDate.Value.ToString("yyyy-MM-dd") : null
            };
        }

        /// <summary>
        /// Maps a Client model entity to a ClientVM view model.
        /// </summary>
        /// <param name="client">The client entity to map.</param>
        /// <returns>A ClientVM instance ready for display.</returns>
        public static ClientVM MapToVm(Client client)
        {
            return new ClientVM
            {
                ClientId = client.ClientId,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Initials = $"{GetInitial(client.FirstName)}{GetInitial(client.LastName)}",
                Email = client.Email,
                Phone = client.Phone,
                Status = client.Status == ClientStatus.Active ? true : false,
                CreatedAt = client.CreatedAt.ToLocalTime()
            };
        }

        /// <summary>
        /// Maps a ClientVM to a Client model entity for create operations.
        /// </summary>
        /// <param name="vm">The view model containing client data.</param>
        /// <param name="counsellorId">The ID of the counsellor creating the client.</param>
        /// <returns>A Client model instance ready to be persisted.</returns>
        public static Client MapVmToEntity(ClientVM vm, int counsellorId)
        {
            return new Client
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Phone = vm.Phone,
                Status = vm.Status ? ClientStatus.Active : ClientStatus.Inactive,
                CreatedAt = DateTime.UtcNow,
                CounsellorId = counsellorId
            };
        }

        /// <summary>
        /// Maps a ClientVM to a Client model entity for update operations.
        /// Preserves the original client ID, counsellor ID, and creation timestamp.
        /// </summary>
        /// <param name="vm">The view model containing updated client data.</param>
        /// <param name="existingClient">The existing client entity to extract immutable properties from.</param>
        /// <returns>A Client model instance ready to be persisted for update.</returns>
        public static Client MapVmToEntityForUpdate(ClientVM vm, Client existingClient)
        {
            return new Client
            {
                ClientId = existingClient.ClientId,
                CounsellorId = existingClient.CounsellorId,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Phone = vm.Phone,
                Status = vm.Status ? ClientStatus.Active : ClientStatus.Inactive,
                CreatedAt = existingClient.CreatedAt
            };
        }

        /// <summary>
        /// Extracts the first character of a string to be used as an initial.
        /// </summary>
        /// <param name="name">The string (e.g., first or last name) to process.</param>
        /// <returns>The first character of the string, or an empty string if the input is null or empty.</returns>
        private static string GetInitial(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            return name.Substring(0, 1).ToUpper();
        }
    }
}