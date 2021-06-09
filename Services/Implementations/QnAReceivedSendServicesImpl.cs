using QnABot.Repository.Implementations;
using Microsoft.Extensions.Logging;
using QnABot.Models;

namespace QnABot.Services.Implementations
{
    public class QnAReceivedSendServicesImpl : QnAReceivedSendServices
    {
        private readonly ApplicationContext _context;
        private readonly QnAReceivedSendRepository _qnAReceivedSendRepository;
        private readonly ILogger<QnAReceivedSendServices> _logger;
        public QnAReceivedSendServicesImpl(ApplicationContext context,
                                ILogger<QnAReceivedSendServices> logger)
        {
            _context = context;
            _logger = logger;
            _qnAReceivedSendRepository = new QnAReceivedSendRepository(_context);
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserQnAReceived> save(UserQnAReceived qnAReceivedSend) =>
            _qnAReceivedSendRepository.Add(qnAReceivedSend);
    }
}
