using System.Threading.Tasks;

namespace ServiceCore.Services
{
    public interface INotificationService
    {
        Task NotifyUserAsync(int userId, string title, string message, string? linkAction = null);
        Task NotifyTicketUpdateAsync(int ticketId, string message);
        Task NotifyNewCommentAsync(int ticketId, int commentId);
    }
}
