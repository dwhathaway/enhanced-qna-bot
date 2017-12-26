using AdaptiveCards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.History;
using Microsoft.Bot.Connector;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

namespace QnABot
{
    public class ConversationLogger : IActivityLogger
    {
        public async Task LogAsync(IActivity activity)
        {
            try
            { 
                var conversationId = activity.Conversation.Id;
                var senderId = activity.From.Id;
                var recipientId = activity.Recipient.Id;
                var message = activity.AsMessageActivity();
                var messageText = message.Text;

                //If the response is a Rich/Adaptive card, then grab the appropriate attachment(s) text as well
                if (message.Attachments.Count > 0)
                {
                    //Grab the response from the message attachments
                    foreach (Attachment a in message.Attachments)
                    {
                        switch (a.ContentType)
                        {
                            case "application/vnd.microsoft.card.video":
                                messageText += (a.Content as VideoCard)?.Text;
                                break;
                            case "application/vnd.microsoft.card.adaptive":
                                //If you have important information to capture in that's actually inside the Adaptive card, insert logic here to parse that out of each TextBlock.
                            
                                //messageText += (a.Content as AdaptiveCard)?.FallbackText;

                                //var adaptiveCard = a.Content as AdaptiveCard;
                                //foreach (CardElement item in adaptiveCard.Body)
                                //{
                                //    if(item.GetType().Name == "TextBlock")
                                //    {
                                //        messageText += (item as TextBlock).Text;
                                //    }
                                //}

                                break;
                            default:
                                break;
                        }
                    }
                }

                var timeStamp = DateTime.Now;

                //ToDo: Make this read from Utils.GetSetting so it can be configured in Azure Portal
                var connString = ConfigurationManager.ConnectionStrings["ChatHistory"].ConnectionString;

                using (var conn = new SqlConnection(connString))
                {
                    var cmd = new SqlCommand("INSERT INTO ConversationHistory(conversationId, senderId, recipientId, messageText, timeStamp) VALUES (@conversationId, @senderId, @recipientId, @messageText, @timeStamp)", conn);

                    cmd.Parameters.AddWithValue("@conversationId", conversationId);
                    cmd.Parameters.AddWithValue("@senderId", senderId);
                    cmd.Parameters.AddWithValue("@recipientId", recipientId);
                    cmd.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "@messageText",
                        Value = messageText,
                        SqlDbType = System.Data.SqlDbType.Text
                    });
                    cmd.Parameters.AddWithValue("@timeStamp", timeStamp);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }
    
            Debug.WriteLine($"From:{activity.From.Id} - To:{activity.Recipient.Id} - Message:{activity.AsMessageActivity()?.Text}");
        }
    }
}