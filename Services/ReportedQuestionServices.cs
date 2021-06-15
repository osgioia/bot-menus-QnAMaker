using Microsoft.EntityFrameworkCore.ChangeTracking;
using QnABot.Models;

namespace QnABot.Services
{
    public interface ReportedQuestionServices
    {
        EntityEntry<UserReportedQuestion> save(UserReportedQuestion reportedQuestion);
    }
}