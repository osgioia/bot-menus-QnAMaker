using QnABot.Repository.Implementations;
using Microsoft.Extensions.Logging;
using QnABot.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace QnABot.Services.Implementations
{
    public class ReportedQuestionServicesImpl : ReportedQuestionServices
    {
        private readonly ApplicationContext _context;
        private readonly ReportedQuestionRepository _reportedQuestionSendRepository;
        private readonly ILogger<ReportedQuestionServices> _logger;
        public ReportedQuestionServicesImpl(ApplicationContext context,
                                ILogger<ReportedQuestionServices> logger)
        {
            _context = context;
            _logger = logger;
            _reportedQuestionSendRepository = new ReportedQuestionRepository(_context);
        }

        public EntityEntry<UserReportedQuestion> save(UserReportedQuestion reportedQuestion) =>
            _reportedQuestionSendRepository.Add(reportedQuestion);
    }
}
