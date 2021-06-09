using QnABot.Models;

namespace QnABot.Services
{
    public interface ReportedQuestionSendServices
    {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserReportedQuestion> save(UserReportedQuestion reportedQuestionSend);
    }
}