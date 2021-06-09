using System;

namespace QnABot.Models
{
    public class UserQnAReceived
    {
        public long ID { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string Question { get; set; }
        public string AnswerShow { get; set; }
        public DateTime DateCreated { get; set; }
        public float Score { get; set; }
    }
}
