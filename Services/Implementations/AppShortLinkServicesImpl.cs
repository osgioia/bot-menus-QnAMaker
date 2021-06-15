using Microsoft.Extensions.Logging;
using QnABot.Models;
using QnABot.Repository.Implementations;
using System.Linq;

namespace QnABot.Services.Implementations
{
    public class AppShortLinkServicesImpl : AppShortLinkServices
    {
        private readonly ApplicationContext _context;
        private readonly AppShortLinkRepository _appShortLinkRepository;
        private readonly ILogger<AppShortLinkServices> _logger;
        public AppShortLinkServicesImpl(ApplicationContext context,
                                ILogger<AppShortLinkServices> logger)
        {
            _context = context;
            _logger = logger;
            _appShortLinkRepository = new AppShortLinkRepository(_context);
        }

        public string getAppUrlMsTeams(string AppId)
        {
            AppShortLink appShortLink = _appShortLinkRepository.Find(d => d.AppId == AppId).FirstOrDefault();
            if (appShortLink != null)
                return appShortLink.ShortLinkMsTeams;
            return $"https://teams.microsoft.com/l/chat/0/0?users=28:{AppId}";
        }
    }
}
