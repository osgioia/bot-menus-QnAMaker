using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.BotBuilderSamples;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QnABot.ContextServices
{
    public class ContextUserService
    {
        private static String debugName = "Lucas Rodriguez";
        private static String debugMail = "lucas.rodriguez@LA.LOGICALIS.COM";

        public ContextUserService()
        {
        }

        public async Task<UserDetails> getUserDetails(ITurnContext context, CancellationToken cancellationToken)
        {
            UserDetails userDetails = new UserDetails();
            switch (context.Activity.ChannelId)
            {
                case "emulator":
                    userDetails.Id = "1";
                    userDetails.UserName = debugName;
                    userDetails.UserEmail = debugMail;
                    break;
                case "webchat":
                    userDetails.Id = "1";
                    userDetails.UserName = debugName;
                    userDetails.UserEmail = debugMail;
                    break;
                default:  //MS-TEAMS
                    var member = await TeamsInfo.GetMemberAsync(context, context.Activity.From.Id, cancellationToken);
                    userDetails.Id = member.Id;
                    userDetails.UserName = member.Name;
                    userDetails.UserEmail = member.Email;
                    break;
            }

            return userDetails;
        }
    }
}
