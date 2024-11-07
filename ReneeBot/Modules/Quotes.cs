using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ReneeBot.Services
{
    public class QuoteEntry
    {
        public string ID { get; set; }
        public string Source { get; set; }
        public string English { get; set; }
    }

    public class Quotes : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CommandHandler _handler;
        private static readonly Random _random = new();
        private static List<QuoteEntry> _quotes;

        bool autoReply;
        int wordsToMatch;
        public Quotes(CommandHandler handler, IConfiguration config)
        {
            _handler = handler;
            LoadQuotesFromFile("dialogue.csv"); // Initialize the quotes on startup
            autoReply = bool.Parse(config["AutoReply"]);
            wordsToMatch = int.Parse(config["WordsToMatch"]);
        }

        public InteractionService Commands { get; set; }

        private void LoadQuotesFromFile(string filePath)
        {
            _quotes = new List<QuoteEntry>();

            try
            {
                // Read all lines in the file
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    // Skip lines that start with "Section" or "ID" as they are headers
                    if (line.StartsWith("Section") || line.StartsWith("ID") || string.IsNullOrWhiteSpace(line))
                        continue;

                    // Split each line by commas
                    var parts = line.Split(new[] { ',' }, 3); // Split at the first three commas

                    // Ensure the line has the correct number of columns
                    if (parts.Length < 3) continue;

                    // Read the columns into variables
                    string id = parts[0];
                    string source = parts[1];
                    string english = parts[2];

                    // Remove the extraneous quotes (inner quotes) and trim leading/trailing quotes from the entire string
                    string cleanedEnglish = english.Replace("\"", "").Trim();

                    // Trim any trailing commas (if present) after quote removal
                    cleanedEnglish = cleanedEnglish.TrimEnd(',');

                    // Regular expression to remove backslash tags and bracketed content
                    cleanedEnglish = Regex.Replace(cleanedEnglish, @"\\\w+|\[.*?\]", "").Trim();

                    // Replace multiple whitespace with a single space after removing tags
                    cleanedEnglish = Regex.Replace(cleanedEnglish, @"\s+", " ");

                    // Check if the last entry in the list has the same ID
                    if (_quotes.Count > 0 && _quotes.Last().ID == id)
                    {
                        // Concatenate the English text if this ID already exists, and normalize whitespace
                        _quotes.Last().English = Regex.Replace(_quotes.Last().English + " " + cleanedEnglish, @"\s+", " ");
                    }
                    else
                    {
                        // Add a new entry to the list with cleaned and normalized text
                        _quotes.Add(new QuoteEntry
                        {
                            ID = id,
                            Source = source,
                            English = cleanedEnglish
                        });
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading quotes: {ex.Message}");
            }
        }

        private string GetNextQuote(string currentId)
        {
            // Find the index of the current quote in the list
            int currentIndex = _quotes.FindIndex(q => q.ID == currentId);

            // Return the next quote in the list, wrapping around if needed
            int nextIndex = (currentIndex + 1) % _quotes.Count;
            var nextQuote = _quotes[nextIndex];
            return $"{nextQuote.Source}: {nextQuote.English}";
        }

        private bool IsSimilarToQuote(string message, string quote)
        {
            // Normalize and tokenize message and quote
            var messageWords = Regex.Split(message.ToLower(), @"\W+").Where(w => w.Length > 0).ToArray();
            var quoteWords = Regex.Split(quote.ToLower(), @"\W+").Where(w => w.Length > 0).ToArray();

            // Check for any sequence of consecutive words in the quote that matches a sequence in the message
            for (int i = 0; i <= quoteWords.Length - wordsToMatch; i++)
            {
                var quoteSequence = quoteWords.Skip(i).Take(wordsToMatch);
                if (messageWords.ContainsSubsequence(quoteSequence))
                    return true;
            }

            return false;
        }

        [SlashCommand("quote", "Get a random quote from the database.")]
        public async Task Quote()
        {
            var randomQuote = _quotes[_random.Next(_quotes.Count)];
            await RespondAsync($"{randomQuote.Source}: {randomQuote.English}");
        }

        // This function listens for all messages and responds if a message matches a quote
        public async Task MessageReceived(SocketMessage message)
        {
            if (!autoReply)
            {
                return;
            }

            // Ignore the bot's own messages or system messages
            if (message.Author.IsBot || message is not SocketUserMessage userMessage)
            {
                return;
            }

            //Console.WriteLine("Message received: " + message.Content);
            // Check the message against each quote in the database
            foreach (var quote in _quotes)
            {
                if (IsSimilarToQuote(userMessage.Content, quote.English))
                {
                    // If a similar quote is found, respond with the next quote in the sequence
                    string response = GetNextQuote(quote.ID);
                    await message.Channel.SendMessageAsync(response);
                    break;
                }
            }
        }
    }

    public static class Extensions
    {
        public static bool ContainsSubsequence(this IEnumerable<string> source, IEnumerable<string> subsequence)
        {
            var subsequenceArray = subsequence.ToArray();
            int subseqLength = subsequenceArray.Length;

            for (int i = 0; i <= source.Count() - subseqLength; i++)
            {
                if (source.Skip(i).Take(subseqLength).SequenceEqual(subsequenceArray))
                    return true;
            }
            return false;
        }
    }
}
