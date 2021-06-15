using System.ComponentModel.DataAnnotations;

namespace QnABot.Models
{
    public class AppShortLink
    {
        public long ID { get; set; }
        [Required]
        public string AppId { get; set; }
        [Required]
        public string ShortLinkMsTeams { get; set; }
    }
}
