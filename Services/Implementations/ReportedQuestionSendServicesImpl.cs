using QnABot.Repository.Implementations;
using Microsoft.Extensions.Logging;
using QnABot.Models;

namespace QnABot.Services.Implementations
{
    public class ReportedQuestionSendServicesImpl : ReportedQuestionSendServices
    {
        private readonly ApplicationContext _context;
        private readonly ReportedQuestionSendRepository _reportedQuestionSendRepository;
        private readonly ILogger<ReportedQuestionSendServices> _logger;
        public ReportedQuestionSendServicesImpl(ApplicationContext context,
                                ILogger<ReportedQuestionSendServices> logger)
        {
            _context = context;
            _logger = logger;
            _reportedQuestionSendRepository = new ReportedQuestionSendRepository(_context);
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserReportedQuestion> save(UserReportedQuestion reportedQuestion) =>
            _reportedQuestionSendRepository.Add(reportedQuestion);
    }
}
