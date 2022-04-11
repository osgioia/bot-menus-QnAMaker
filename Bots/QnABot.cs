// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QnABot;
using QnABot.ContextServices;
using QnABot.Models;
using QnABot.Services;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QnAReceivedServices _qnaReceivedServices;
        private readonly ReportedQuestionServices _reportedQuestionServices;
        private readonly AppShortLinkServices _appShortLinkServices;
        private readonly ContextUserService _contextUserService;
        private readonly ValidDomainsServices _validDomainsServices;
        private readonly ApplicationContext _context;

        private static string debugName = "Osvaldo Gioia";
        private static string debugMail = "osvaldo.gioia@LA.LOGICALIS.COM";

        private readonly string _blobImagesUrl;
        private readonly string _microsoftAppId;

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
        private readonly string noAnswerMsg = "Todavía no tengo información sobre tu consulta. Sugiero que ingreses al sharepoint a través del siguiente link [https://metrogasar.sharepoint.com/sites/mo/SitePages/SoluIntegrar.aspx](https://metrogasar.sharepoint.com/sites/mo/SitePages/SoluIntegrar.aspx)" +
                        $" o enviar un mail a [integrar@metrogas.com.ar](integrar@metrogas.com.ar)" +
                        $"\r\n¿Puedo ayudarte con otro tema?";


        public QnABot(  IConfiguration configuration, 
                        ILogger<QnABot> logger, 
                        IHttpClientFactory httpClientFactory, 
                        QnAReceivedServices qnaReceivedServices, 
                        ReportedQuestionServices repostedQuestionServices,
                        AppShortLinkServices appShortLinkServices,
                        ContextUserService contextUserService,
                        ValidDomainsServices validDomainsServices, 
                        ApplicationContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _qnaReceivedServices = qnaReceivedServices;
            _reportedQuestionServices = repostedQuestionServices;
            _appShortLinkServices = appShortLinkServices;
            _validDomainsServices = validDomainsServices;
            _contextUserService = contextUserService;
            _blobImagesUrl = configuration["BlobImagesUrl"];
            _microsoftAppId = configuration["MicrosoftAppId"];
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
            UserDetails userDetails = await _contextUserService.getUserDetails(turnContext, cancellationToken);

            if (turnContext.Activity is Activity activity && activity.Value != null
                && ((dynamic)activity.Value).value is JValue value
              //  && ((dynamic)activity.Value).answer is JValue answer
              )
            {

                if (_validDomainsServices.isValidDomain(userDetails.UserEmail.ToLower().Split("@")[1]))
                {

                    turnContext.Activity.Value = null;
                    turnContext.Activity.Text = value.Value.ToString();
                    await interactWithUser(turnContext, userDetails, cancellationToken);
                }
            }
            else
            {
                if (_validDomainsServices.isValidDomain(userDetails.UserEmail.ToLower().Split("@")[1]))
                {
                    await interactWithUser(turnContext, userDetails, cancellationToken);
                }
                else
                {
                    string notAuthorize = "No esta autorizado a utilizar el servicio de Metrogas.";
                    SaveQnA(turnContext.Activity.Text, notAuthorize, 0, "not_authorized", userDetails);
                    await turnContext.SendActivityAsync(MessageFactory.Text(notAuthorize), cancellationToken);
                }
            }

        }

        private async Task interactWithUser(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, CancellationToken cancellationToken)
        {
            string appUrlMsTeams = _appShortLinkServices.getAppUrlMsTeams(_microsoftAppId);

            if (turnContext.Activity.Value != null)
            {
                await showAnswerFromChosenQuestionBetweenMultiplesQuestions(turnContext, userDetails, appUrlMsTeams);
            }
            else if (turnContext.Activity.Text.ToLower() == "ayuda")
            {
                await askingForHelp(turnContext, cancellationToken);
            }
            else if (turnContext.Activity.Text.ToLower().Contains("gracias") || (turnContext.Activity.Text.ToLower().Contains("no")) 
                        || (turnContext.Activity.Text.ToLower().Contains("no, gracias")) || (turnContext.Activity.Text.ToLower().Contains("chau")) 
                        || (turnContext.Activity.Text.ToLower().Contains("saludos")) || (turnContext.Activity.Text.ToLower().Contains("bye"))
                        || (turnContext.Activity.Text.ToLower().Contains("hasta pronto")) || (turnContext.Activity.Text.ToLower().Contains("listo")))
            {
                await youAreWelcome(turnContext, userDetails, cancellationToken);
            }
            else
            {
                await askToQnA(turnContext, userDetails, appUrlMsTeams);
            }
        }

        private async Task showAnswerFromChosenQuestionBetweenMultiplesQuestions(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, string appUrlMsTeams)
        {
            MultipleQuestionsAnswer choosenQuestion = JsonConvert.DeserializeObject<MultipleQuestionsAnswer>(turnContext.Activity.Value.ToString());
            SaveQnA(choosenQuestion.question, choosenQuestion.choosenQuestion.Split("|")[0], float.Parse(choosenQuestion.choosenQuestion.Split("|")[1]), choosenQuestion.choosenQuestion.Split("|")[2], userDetails);
            List<Attachment> bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(choosenQuestion.question, choosenQuestion.choosenQuestion.Split("|")[0], reportMsg, appUrlMsTeams) };
            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
        }

        private async Task askToQnA(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, string appUrlMsTeams)
        {
            var httpClient = _httpClientFactory.CreateClient();

            QnAMaker qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAEndpointKey"],
                Host = _configuration["QnAEndpointHostName"]
            },
            null,
            httpClient);

            _logger.LogInformation("Calling QnA Maker");

            var options = new QnAMakerOptions { Top = 3 };

            // The actual call to the QnA Maker service.
            QueryResult[] request = await qnaMaker.GetAnswersAsync(turnContext, options);

            var response = (from r in request
                            where r.Score >= 0.5 && (r.Source.ToLower().Contains("chatbot")
                              || r.Source.ToLower().Contains("editorial")
                              || r.Source.ToLower().Contains("bancos")
                              || r.Source.ToLower().Contains("bancos2")
                              || r.Source.ToLower().Contains("usuario de red")
                              || r.Source.ToLower().Contains("licencias")
                              || r.Source.ToLower().Contains("vacaciones")
                              || r.Source.ToLower().Contains("otras licencias")
                              || r.Source.ToLower().Contains("otras licencias2")
                              || r.Source.ToLower().Contains("otras licencias3")
                              || r.Source.ToLower().Contains("rrhh")
                              || r.Source.ToLower().Contains("rrhh2")
                              || r.Source.ToLower().Contains("rrhh3")
                              || r.Source.ToLower().Contains("rrhh4")
                              || r.Source.ToLower().Contains("rrhhinscripcion")
                              || r.Source.ToLower().Contains("rrhhformacion")
                              || r.Source.ToLower().Contains("horas extras2")
                              || r.Source.ToLower().Contains("integrar")
                              || r.Source.ToLower().Contains("integrar2")
                              || r.Source.ToLower().Contains("beneficios")
                              || r.Source.ToLower().Contains("beneficios2")
                              || r.Source.ToLower().Contains("beneficiosnewcard2")
                              || r.Source.ToLower().Contains("beneficiosnewcard3")
                              || r.Source.ToLower().Contains("flexible")
                              || r.Source.ToLower().Contains("flexible2")
                              || r.Source.ToLower().Contains("saludos"))
                            select r).ToArray();
            var noAnswer = (from r in request where r.Score >= 0.7 && r.Source.ToLower().Contains("sinrespuesta") select r).ToArray();
            //var options;
            if (request != null && (response.Length > 0 || noAnswer.Length > 0))
            {
                if (response.Length > 0 && response[0].Source.ToLower().Contains("editorial"))
                {
                   
                    await greeting(turnContext, userDetails, appUrlMsTeams, response);
                    
                }
                else if (response.Length > 0 && (response[0].Score >= 0.90 || response.Length == 1))
                {
                    
                    await showBestAnswer(turnContext, userDetails, appUrlMsTeams, response);
                }
                else if (response.Length > 1)
                {
                   
                    await showMultiplesAnswers(turnContext, userDetails, response);
                }
                else
                {
                    
                    string question = turnContext.Activity.Text;
                    SaveQnA(question, noAnswer[0].Answer, noAnswer[0].Score, noAnswer[0].Source, userDetails);
                    await notAnswerToThatQuestion(turnContext, appUrlMsTeams, noAnswer[0].Answer, noAnswer[0].Score, noAnswer[0].Source);
                }
            }
            else
            {
                string answer = noAnswerMsg;
                float score = 0;
                string source = "not_match_source";

                if (request != null && request.Length > 0)
                {
                    answer = request[0].Answer;
                    score = request[0].Score;
                    source = request[0].Source;
                }

                string question = turnContext.Activity.Text;
                SaveQnA(question, answer, score, source, userDetails);
                await notAnswerToThatQuestion(turnContext, appUrlMsTeams, noAnswerMsg, score, source);
            }
        }

        private async Task notAnswerToThatQuestion(ITurnContext<IMessageActivity> turnContext, string appUrlMsTeams, string noAnswer, float score, string source)
        {
            List<Attachment> bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(null, noAnswer, null, appUrlMsTeams) };
            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
        }

        private async Task greeting(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, string appUrlMsTeams, QueryResult[] response)
        {
            string answerWelcome = response[0].Answer.Replace("{name}", userDetails.UserName)
           .Replace("{helpInfo}", helpInfo);
            SaveQnA(turnContext.Activity.Text, answerWelcome, response[0].Score, response[0].Source, userDetails);

       

            List<Attachment> bookApprovedCard = new List<Attachment> { HelloCardAdaptiveCardAttachment(null, answerWelcome, reportMsg, appUrlMsTeams) };
            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
        }

        private async Task showBestAnswer(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, string appUrlMsTeams, QueryResult[] response)
        {
           
            string answerWelcome = response[0].Answer.Replace("{name}", userDetails.UserName)
           .Replace("{helpInfo}", helpInfo);

            
            //SaveQnA(turnContext.Activity.Text, response[0].Answer, response[0].Score, response[0].Source, userDetails);
            SaveQnA(turnContext.Activity.Text, answerWelcome, response[0].Score, response[0].Source, userDetails);
            
            //List<Attachment> bookApprovedCard = new List<Attachment> { AnswerCardAdaptiveCardAttachment(turnContext.Activity.Text, response[0].Answer, reportMsg, appUrlMsTeams) };
            List<Attachment> bookApprovedCard = new List<Attachment> { SelectAdaptiveCardAttachment(turnContext.Activity.Text, answerWelcome, reportMsg, appUrlMsTeams, response[0].Source) };
            
            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
        }

        private async Task showMultiplesAnswers(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, QueryResult[] response)
        {
            Dictionary<string, string> bookingChoices = new Dictionary<string, string>();
            string questions = "";
            string source = "";
            for (int i = 0; i < response.Length; i++)
            {
                bookingChoices.Add(response[i].Questions[0].Split("|")[0], response[i].Answer + "|" + Math.Round(response[i].Score, 2).ToString() + "|" + response[i].Source);
                questions = questions + response[i].Questions[0] + " | ";

                source = source + response[i].Source + " | ";
            }
            SaveQnA(turnContext.Activity.Text, questions, response[0].Score, source, userDetails);
            List<Attachment> bookApprovedCard = new List<Attachment> { CreateSelectQuestionCardAttachment(bookingChoices, turnContext.Activity.Text, reportQuestionsMsg) };

            await turnContext.SendActivityAsync((Activity)MessageFactory.Attachment(bookApprovedCard));
        }

        private static async Task youAreWelcome(ITurnContext<IMessageActivity> turnContext, UserDetails userDetails, CancellationToken cancellationToken)
        {

            string answerWelcome = "Hasta pronto 🖐 {name}, espero haberte ayudado!".Replace("{name}", userDetails.UserName);


            await turnContext.SendActivityAsync(MessageFactory.Text(answerWelcome), cancellationToken);
        }

        private async Task askingForHelp(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"{helpInfo}"), cancellationToken);
        }

        private Attachment AnswerCardAdaptiveCardAttachment(string question, string answer, string reportMsg, string appLink)
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
                    else
                    {
                        adaptiveCard = adaptiveCard.Replace("$(question)", question);
                        adaptiveCard = adaptiveCard.Replace("$(answer)", answer);
                        adaptiveCard = adaptiveCard.Replace("$(reportMsg)", reportMsg);
                    }
                    adaptiveCard = adaptiveCard.Replace("$(blobUrl)", _blobImagesUrl);
                    adaptiveCard = adaptiveCard.Replace("$(appLink)", appLink);
                    
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private Attachment SelectAdaptiveCardAttachment(string question, string answer, string reportMsg, string appLink, string source)
        {
          
            var cardResourcePath = "QnABot.Cards.AnswerCard.json";
            switch (source)
            {
                case "Bancos":
                    cardResourcePath = "QnABot.Cards.BancosCard.json";
                    break;
                case "Bancos2":
                    cardResourcePath = "QnABot.Cards.BancosReturn.json";
                    break;
                case "Usuario de Red":
                    cardResourcePath = "QnABot.Cards.MenúInicailReturn.json";
                    break;
                case "Licencias":
                    cardResourcePath = "QnABot.Cards.LicenciasCard.json";
                    break;
                case "Vacaciones":
                    cardResourcePath = "QnABot.Cards.LicenciasReturn.json";
                    break;
                case "Otras Licencias":
                    cardResourcePath = "QnABot.Cards.OtrasLicenciasCard.json";
                    break;
                case "Otras Licencias2":
                    cardResourcePath = "QnABot.Cards.OtrasLicencias_LicenciasReturn.json";
                    break;
                case "Otras Licencias3":
                    cardResourcePath = "QnABot.Cards.OtrasLicencias_LicenciasReturn.json";
                    break;
                case "RRHH":
                    cardResourcePath = "QnABot.Cards.RRHHCard.json";
                    break;
                case "RRHH2":
                    cardResourcePath = "QnABot.Cards.RRHHReturn.json";
                    break;
                case "RRHH3":
                    cardResourcePath = "QnABot.Cards.HorasExtras.json";
                    break;
                case "RRHH4":
                    cardResourcePath = "QnABot.Cards.RRHHReturn.json";
                    break;
                case "RRHHInscripcion":
                    cardResourcePath = "QnABot.Cards.RcCdInscribirmeRRHH.json";
                    break;
                case "RRHHFormacion":
                    cardResourcePath = "QnABot.Cards.FormacionRRHH.json";
                    break;
                case "Horas Extras2":
                    cardResourcePath = "QnABot.Cards.HorasExtrasRRHHInicial.json";
                    break;
                case "Integrar":
                    cardResourcePath = "QnABot.Cards.IntegrarCard.json";
                    break;
                case "Integrar2":
                    cardResourcePath = "QnABot.Cards.IntegrarReturn.json";
                    break;
                case "Beneficios":
                    cardResourcePath = "QnABot.Cards.Beneficios.json";
                    break;
                case "Beneficios2":
                    cardResourcePath = "QnABot.Cards.BeneficiosReturn.json";
                    break;
                case "BeneficiosNewCard2":
                    cardResourcePath = "QnABot.Cards.Beneficios2.json";
                    break;
                case "BeneficiosNewCard3":
                    cardResourcePath = "QnABot.Cards.Beneficios3.json";
                    break;
                case "Flexible":
                    cardResourcePath = "QnABot.Cards.FlexibleCard.json";
                    break;
                case "Flexible2":
                    cardResourcePath = "QnABot.Cards.horarioFlexibleBeneficios.json";
                    break;
                default:
                    cardResourcePath = "QnABot.Cards.BancosCard.json";
                    break;
            }


            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    if (question == null)
                        adaptiveCard = adaptiveCard.Replace("$(notify)", answer);
                    else
                    {
                        adaptiveCard = adaptiveCard.Replace("$(question)", question);
                        adaptiveCard = adaptiveCard.Replace("$(answer)", answer);
                        adaptiveCard = adaptiveCard.Replace("$(reportMsg)", reportMsg);
                    }
                    adaptiveCard = adaptiveCard.Replace("$(blobUrl)", _blobImagesUrl);
                    adaptiveCard = adaptiveCard.Replace("$(appLink)", appLink);

                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }


        private Attachment HelloCardAdaptiveCardAttachment(string question, string answer, string reportMsg, string appLink)
        {
            var cardResourcePath = "QnABot.Cards.HelloCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    adaptiveCard = adaptiveCard.Replace("$(blobUrl)", _blobImagesUrl);
                    adaptiveCard = adaptiveCard.Replace("$(answer)", answer);
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
                    adaptiveCard = adaptiveCard.Replace("$(blobUrl)", _blobImagesUrl);
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        private void SaveQnA(string question, string answer, float score, string source, UserDetails userDetails)
        {
            _qnaReceivedServices.save(new UserQnAReceived
            {
                UserId = userDetails.Id,
                UserEmail = userDetails.UserEmail,
                Question = question.TrimStart().TrimEnd(),
                AnswerShow = answer.TrimStart().TrimEnd(),
                Source = source,
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
                    adaptiveCard = adaptiveCard.Replace("$(blobUrl)", _blobImagesUrl);
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
