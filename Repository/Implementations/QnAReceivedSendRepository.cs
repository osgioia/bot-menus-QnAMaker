using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class QnAReceivedSendRepository : GenericRepository<UserQnAReceived>, IQnAReceivedSendRepository
    {
        public QnAReceivedSendRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
