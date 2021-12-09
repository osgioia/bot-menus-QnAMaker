using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class ValidDomainsRepository : GenericRepository<ValidDomains>, IValidDomainsRepository
    {
        public ValidDomainsRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
