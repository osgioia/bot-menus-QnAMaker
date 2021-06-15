using Microsoft.EntityFrameworkCore.ChangeTracking;
using QnABot.Models;

namespace QnABot.Services
{
    public interface QnAReceivedServices
    {
        EntityEntry<UserQnAReceived> save(UserQnAReceived qnAReceived);
    }
}