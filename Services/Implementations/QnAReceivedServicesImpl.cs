using QnABot.Repository.Implementations;
using Microsoft.Extensions.Logging;
using QnABot.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace QnABot.Services.Implementations
{
    public class QnAReceivedServicesImpl : QnAReceivedServices
    {
        private readonly ApplicationContext _context;
        private readonly QnAReceivedRepository _qnAReceivedSendRepository;
        private readonly ILogger<QnAReceivedServices> _logger;
        public QnAReceivedServicesImpl(ApplicationContext context,
                                ILogger<QnAReceivedServices> logger)
        {
            _context = context;
            _logger = logger;
            _qnAReceivedSendRepository = new QnAReceivedRepository(_context);
        }

        public EntityEntry<UserQnAReceived> save(UserQnAReceived qnAReceivedSend) =>
            _qnAReceivedSendRepository.Add(qnAReceivedSend);
    }
}
