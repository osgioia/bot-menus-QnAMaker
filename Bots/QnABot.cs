﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QnABot;
using QnABot.Models;
using QnABot.Services;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QnAReceivedSendServices _qnaReceived;
        private readonly ReportedQuestionSendServices _reportedQuestion;
        private readonly ApplicationContext _context;
        private readonly string LOGICALIS_DOMAIN = "@la.logicalis.com";

        private static string debugName = "Matias Cirillo";
        private static string debugMail = "matias.cirillo@LA.LOGICALIS.COM";

        public class MultipleQuestionsAnswer
        {
            public string choosenQuestion { get; set; }
            public string question { get; set; }
        }

        private readonly string helpInfo = "Actualmente tengo información sobre los siguientes productos:\r\n" +
                        $"\r\n* Microsoft Teams\r\n" +
                        $"\r\n* Microsoft Outlook\r\n" +
                        $"\r\n* Microsoft Planner\r\n" +
                        $"\r\n* Microsoft Sharepoint\r\n\r\n" +
                        $"\r\nEsta información se actualiza periódicamente en base a las preguntas que realicen los usuarios, por lo que si no encontras tu respuesta hoy, no te preocupes, pronto va a aparecer! \r\n\r\n" +
                        $"\r\nY si ves que la respuesta que te di no te sirve o es incorrecta, hace click en el botón 'Reportar', así puedo aprender donde tengo que mejorar! \r\n\r\n" +
                        $"\r\nQue tengas un buen día!";

        private readonly string reportMsg = "Sí la respuesta no es lo que esperabas podes reportarla y así poder mejorar tu experiencia. Gracias!";
        private readonly string reportQuestionsMsg = "Sí ninguna de las preguntas es la que buscabas podes reportarlo y así poder mejorar tu experiencia. Gracias!";
        private readonly string noAnswerMsg = "Todavía no tengo información sobre eso, pero sigo aprendiendo! \r\n Mientras tanto, podrías ver si la respuesta que precisas está acá: [https://support.microsoft.com/es-es/](https://support.microsoft.com/es-es/)";

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory, QnAReceivedSendServices qnaReceived, ReportedQuestionSendServices repostedQuestion, ApplicationContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _qnaReceived = qnaReceived;
            _reportedQuestion = repostedQuestion;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = CreateAdaptiveCardAttachment();
                    var response = MessageFactory.Attachment(welcomeCard);
                    await turnContext.SendActivityAsync(response, cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            UserReportedQuestion userReportedQuestion = new UserReportedQuestion();
            UserDetails userDetails = new UserDetails();
            switch (turnContext.Activity.ChannelId)
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
                    var member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
                    userDetails.Id = member.Id;
                    userDetails.UserName = member.Name;
                    userDetails.UserEmail = member.Email;
                    break;
            }

            if (turnContext.Activity is Activity activity && activity.Value != null
                && ((dynamic)activity.Value).question is JValue question
                && ((dynamic)activity.Value).answer is JValue answer)
            {
                _reportedQuestion.save(new UserReportedQuestion
                {
                    UserId = userDetails.Id,
                    UserEmail = userDetails.UserEmail,
                    Question = (string)question.Value,
                    AnswerShow = (string)answer.Value,
                    DateCreated = DateTime.Now
                });

                await turnContext.SendActivityAsync(MessageFactory.Text("Gracias por tu feedback. ¿Te puedo ayudar en algo más?"), cancellationToken);
            }
            else
            {
                var httpClient = _httpClientFactory.CreateClient();

                var qnaMaker = new QnAMaker(new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                    EndpointKey = _configuration["QnAEndpointKey"],
                    Host = _configuration["QnAEndpointHostName"]
                },
                null,
                httpClient);

                _logger.LogInformation("Calling QnA Maker");

                if (userDetails.UserEmail.ToLower().EndsWith(LOGICALIS_DOMAIN))
                {
                    var options = new QnAMakerOptions { Top = 3 };
                    List<Attachment> bookApprovedCard;
                    if (turnContext.Activity.Value != null)
                    {
                        MultipleQuestionsAnswer choosenQuestion = JsonConvert.DeserializeObject<MultipleQuestionsAnswer>(turnContext.Activity.Value.ToString());
                        SaveQnA(choosenQuestion.question, choosenQuestion.choosenQuestion.Split("|")[0], float.Parse(choosenQuestion.choosenQuestion.Split("|")[1]), userDetails);
                        bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(choosenQuestion.question, choosenQuestion.choosenQuestion.Split("|")[0], reportMsg) };
                        await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                    }

                    else if (turnContext.Activity.Text.ToLower() == "ayuda")
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"{helpInfo}"), cancellationToken);
                    }
                    else if (turnContext.Activity.Text.ToLower().Contains("gracias"))
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Espero haber podido ayudarte 😄\n\n Nos vemos !"), cancellationToken);
                    }
                    else
                    {
                        // The actual call to the QnA Maker service.
                        QueryResult[] request = await qnaMaker.GetAnswersAsync(turnContext, options);

                        var response = (from r in request where r.Score >= 0.5 && (r.Source.ToLower().Contains("chatbot")
                                        || r.Source.ToLower().Contains("editorial")
                                        || r.Source.ToLower().Contains("saludos"))select r).ToArray();
                        var noAnswer = (from r in request where r.Score >= 0.7 && r.Source.ToLower().Contains("sinrespuesta") select r).ToArray();
                        //var options;
                        if (request != null && (response.Length > 0 || noAnswer.Length > 0))
                        {
                            if (response.Length > 0 && response[0].Source.ToLower().Contains("saludo"))
                            {
                                string answerWelcome = response[0].Answer.Replace("{name}", userDetails.UserName)
                                                                .Replace("{helpInfo}", helpInfo);
                                SaveQnA(turnContext.Activity.Text, answerWelcome, response[0].Score, userDetails);

                                bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(null, answerWelcome, reportMsg) };
                                await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                            }
                            else if (response.Length > 0 && (response[0].Score >= 0.90 || response.Length == 1))
                            {
                                SaveQnA(turnContext.Activity.Text, response[0].Answer, response[0].Score, userDetails);
                                bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(turnContext.Activity.Text, response[0].Answer, reportMsg) };
                                await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                            }
                            else if (response.Length > 1)
                            {
                                Dictionary<string, string> bookingChoices = new Dictionary<string, string>();
                                string questions = "";
                                for (int i = 0; i < response.Length; i++)
                                {
                                    bookingChoices.Add(response[i].Questions[0].Split("|")[0], response[i].Answer + "|" + Math.Round(response[i].Score, 2).ToString());
                                    questions = questions + response[i].Questions[0] + " | ";
                                }
                                SaveQnA(turnContext.Activity.Text, questions, response[0].Score, userDetails);
                                bookApprovedCard = new List<Attachment> { CreateSelectQuestionCardAttachment(bookingChoices, turnContext.Activity.Text, reportQuestionsMsg) };

                                await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                            }
                            else
                            {
                                bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(null, noAnswer[0].Answer, null) };
                                await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                            }

                        }
                        else
                        {
                            bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(null, noAnswerMsg, null) };
                            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
                        }
                    }
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("No esta autorizado a utilizar el servicio de Logicalis."), cancellationToken);
                }
            }
                
        }
        private Attachment AnswerCardAdaptiveCardAttachment(string question, string answer, string reportMsg)
        {
            var cardResourcePath = "QnABot.Cards.AnswerCard.json";
            if (question == null)
                cardResourcePath = "QnABot.Cards.NotifyCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    if (question == null)
                        adaptiveCard = adaptiveCard.Replace("$(notify)", answer);
                    else {
                        adaptiveCard = adaptiveCard.Replace("$(question)", question);
                        adaptiveCard = adaptiveCard.Replace("$(answer)", answer);
                        adaptiveCard = adaptiveCard.Replace("$(reportMsg)", reportMsg);
                    }
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "QnABot.Cards.WelcomeCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private void SaveQnA(string question, string answer, float score, UserDetails userDetails)
        {
            _qnaReceived.save(new UserQnAReceived
            {
                UserId = userDetails.Id,
                UserEmail = userDetails.UserEmail,
                Question = question,
                AnswerShow = answer,
                DateCreated = DateTime.Now,
                Score = score
            });
        }

        private Attachment CreateSelectQuestionCardAttachment(Dictionary<string, string> bookingChoices, string question, string reportQuestionsMsg)
        {
            var cardResourcePath = "QnABot.Cards.SelectQuestionCard.json";

            var choices = bookingChoices.Select(x => new { title = x.Key, value = x.Value });
            var serializedChoices = JsonConvert.SerializeObject(choices);

            var toReport = bookingChoices.Select(x => x.Key).ToList();

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("\"$(choices)\"", serializedChoices);
                    adaptiveCard = adaptiveCard.Replace("$(question)", question);
                    adaptiveCard = adaptiveCard.Replace("$(reportMsg)", reportQuestionsMsg);
                    adaptiveCard = adaptiveCard.Replace("$(value)", bookingChoices.FirstOrDefault().Value);
                    adaptiveCard = adaptiveCard.Replace("$(answers)", string.Join("| ", toReport));
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}
