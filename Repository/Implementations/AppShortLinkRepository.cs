using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class AppShortLinkRepository : GenericRepository<AppShortLink>, IAppShortLinkRepository
    {
        public AppShortLinkRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
