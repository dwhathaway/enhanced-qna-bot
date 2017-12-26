using System;
using System.Threading;
using System.Threading.Tasks;

// Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;
using AdaptiveCards;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using QnABot;

namespace Microsoft.Bot.Sample.QnABot
{
    [Serializable]
    public class RootDialog :  IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            /* Wait until the first message is received from the conversation and call MessageReceviedAsync 
            *  to process that message. */
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /* When MessageReceivedAsync is called, it's passed an IAwaitable<IMessageActivity>. To get the message,
            *  await the result. */
            var message = await result;
            
            var qnaSubscriptionKey = Utils.GetAppSetting("QnASubscriptionKey");
            var qnaKBId = Utils.GetAppSetting("QnAKnowledgebaseId");

            // QnA Subscription Key and KnowledgeBase Id null verification
            if (!string.IsNullOrEmpty(qnaSubscriptionKey) && !string.IsNullOrEmpty(qnaKBId))
            {
                await context.Forward(new BasicQnAMakerDialog(), AfterAnswerAsync, message, CancellationToken.None);
            }
            else
            {
                await context.PostAsync("Please set QnAKnowledgebaseId and QnASubscriptionKey in App Settings. Get them at https://qnamaker.ai.");
            }            
        }

        private async Task AfterAnswerAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // wait for the next user message
            context.Wait(MessageReceivedAsync);
        }
    }

    // For more information about this template visit http://aka.ms/azurebots-csharp-qnamaker
    [Serializable]
    public class BasicQnAMakerDialog : QnAMakerDialog
    {
        // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.        
        // Parameters to QnAMakerService are:
        // Required: subscriptionKey, knowledgebaseId, 
        // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]

        public BasicQnAMakerDialog() : base(new QnAMakerService(new QnAMakerAttribute(Utils.GetAppSetting("QnASubscriptionKey"), Utils.GetAppSetting("QnAKnowledgebaseId"), "No good match in FAQ.", 0.5))) { }

        protected override async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResults result)
        {
            // Add code to format QnAMakerResults 'result' 

            // answer is a string
            var answer = result.Answers.First().Answer;

            Activity reply = ((Activity)context.Activity).CreateReply();

            reply.Text = answer;

            try
            {
                string[] qnaAnswerData = answer.Split(';');

                string[] supportedCardTypes = new string[] { "videocard" };

                //If the string came back as ; separated, and the first item in the array is one of the supported rich card types
                if (qnaAnswerData.Length > 1 && supportedCardTypes.Contains(qnaAnswerData[0].ToLower()))
                {
                    string cardType = qnaAnswerData[0];
                    string title = qnaAnswerData[1];
                    string description = qnaAnswerData[2];
                    string videoUrl = qnaAnswerData[3];
                    string learnMoreUrl = qnaAnswerData[4];
                    string thumbnailUrl = qnaAnswerData[5];

                    switch (cardType)
                    {
                        case "VideoCard":
                            VideoCard videoCard = new VideoCard()
                            {
                                Title = title,
                                Text = description,
                                Image = new ThumbnailUrl(thumbnailUrl)
                            };

                            videoCard.Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.OpenUrl, "Learn More", value: learnMoreUrl)
                            };

                            videoCard.Media = new List<MediaUrl>
                            {
                                new MediaUrl(videoUrl)
                            };

                            reply.Attachments.Add(videoCard.ToAttachment());

                            //Clear out the text, no longer needed since Video Card replied
                            reply.Text = string.Empty;

                            break;
                    }
                }
                else
                {
                    AdaptiveCard card = new AdaptiveCard();

                    //Set the fallback text in case someone sends a request from a client that doesn't yet support Adaptive Cards fully
                    //card.FallbackText = answer;

                    // Add text to the card.
                    //card.Body.Add(new TextBlock()
                    //{
                    //    Text = answer,
                    //    Wrap = true,

                    //});

                    // Add text to the card.
                    card.Body.Add(new TextBlock()
                    {
                        Text = "Was this answer helpful?",
                        Size = TextSize.Small
                    });

                    // Add buttons to the card.
                    card.Actions.Add(new SubmitAction()
                    {
                        Title = "Yes",
                        Data = "Yes, this was helpful"
                    });

                    card.Actions.Add(new SubmitAction()
                    {
                        Title = "No",
                        Data = "No, this was not helpful"
                    });

                    // Create the attachment.
                    Attachment attachment = new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    };

                    reply.Attachments.Add(attachment);
                }
            }
            catch (Exception ex)
            {
                reply.Text = ex.ToString();
            }

            await context.PostAsync(reply);
            context.Wait(this.MessageReceivedAsync);
        }
    }
}