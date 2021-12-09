using QnABot.Models;

namespace QnABot.Repository.Implementations
{
    public class ReportedQuestionRepository : GenericRepository<UserReportedQuestion>, IReportedQuestionRepository
    {
        public ReportedQuestionRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
