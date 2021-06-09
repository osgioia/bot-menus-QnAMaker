using QnABot.Models;

namespace QnABot.Services
{
    public interface QnAReceivedSendServices
    {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserQnAReceived> save(UserQnAReceived qnAReceivedSend);
    }
}