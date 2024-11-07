# ReneeBot

A Discord bot for posting random quotes and replying to user messages with quotes.

## Commands

`/ping` - Pings the bot.

`/shutdown` - Shut down the bot. (Owner only)

`/quote` - Get a random quote from the database.

## Config

The bot needs a `config.json` and `dialogue.csv` file in the same directory in order to function.

In `config.json`:

```
{
    "Token": "PutYourBotTokenHere",
    "GuildId": "1234567890",
    "AutoReply" : "true",
    "WordsToMatch" : "7"
}
```

In `dialogue.csv` (obtained using the Translation Tool):

```
JhcpXsLh,Narrator,"Oh my god, it's white noise! How thrilling!! ",
jhBRjxkP,Narrator,Who wouldn't want to be stuck at home ,
jhBRjxkP,Narrator,when you've got entertainment like this?,
```

`Token` - Your bot token.

`GuildId` - Your server ID. (Get it by using Developer Mode)

`AutoReply` - Whether to auto-reply to messages that match quotes in the database.

`WordsToMatch` - How many consecutive words to consider a message to be matching.