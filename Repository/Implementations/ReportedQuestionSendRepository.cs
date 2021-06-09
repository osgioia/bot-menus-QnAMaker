using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class ReportedQuestionSendRepository : GenericRepository<UserReportedQuestion>, IReportedQuestionSendRepository
    {
        public ReportedQuestionSendRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
