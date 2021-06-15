using Microsoft.Extensions.Logging;
using QnABot.Repository.Implementations;
using System.Linq;

namespace QnABot.Services.Implementations
{
    public class ValidDomainsServicesImpl : ValidDomainsServices
    {
        private readonly ApplicationContext _context;
        private readonly ValidDomainsRepository _validDomainsRepository;
        private readonly ILogger<ValidDomainsServices> _logger;
        public ValidDomainsServicesImpl(ApplicationContext context,
                                ILogger<ValidDomainsServices> logger)
        {
            _context = context;
            _logger = logger;
            _validDomainsRepository = new ValidDomainsRepository(_context);
        }

        public bool isValidDomain(string domain) =>
            _validDomainsRepository.Find(d => d.Domain == domain).Any(d => d.Domain == domain);
    }
}
