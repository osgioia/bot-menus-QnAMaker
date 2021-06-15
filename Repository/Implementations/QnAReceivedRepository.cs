using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class QnAReceivedRepository : GenericRepository<UserQnAReceived>, IQnAReceivedRepository
    {
        public QnAReceivedRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
