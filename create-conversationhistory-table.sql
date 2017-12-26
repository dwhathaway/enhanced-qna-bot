/****** Object:  Table [dbo].[ConversationHistory]    Script Date: 12/26/2017 2:37:32 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO 

CREATE TABLE [dbo].[ConversationHistory](
	[conversationId] [nchar](255) NOT NULL,
	[senderId] [nchar](255) NOT NULL,
	[recipientId] [nchar](255) NOT NULL,
	[messageText] [text] NOT NULL,
	[timeStamp] [datetime] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


