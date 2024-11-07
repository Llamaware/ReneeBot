using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ReneeBot.Services;

namespace ReneeBot
{
    class Program
    {
        // setup our fields we assign later
        private readonly IConfiguration _config;
        private DiscordSocketClient _client;
        private InteractionService _commands;
        private ulong _testGuildId;

        public static Task Main(string[] args) => new Program().MainAsync();

        public Program()
        {
            // create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");

            // build the configuration and assign to _config          
            _config = _builder.Build();
            _testGuildId = ulong.Parse(_config["GuildId"]);
        }

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                var commands = services.GetRequiredService<InteractionService>();
                var quotesService = services.GetRequiredService<Quotes>();

                _client = client;
                _commands = commands;

                // Set up logging and events
                client.Log += LogAsync;
                commands.Log += LogAsync;
                client.Ready += ReadyAsync;

                // Register the MessageReceived handler
                client.MessageReceived += quotesService.MessageReceived;

                // Log in and start the client
                await client.LoginAsync(TokenType.Bot, _config["Token"]);
                await client.StartAsync();

                // Initialize CommandHandler
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                // Keep the application running
                CancellationTokenSource = new CancellationTokenSource();

                try
                {
                    await Task.Delay(Timeout.Infinite, CancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Bot is shutting down.");
                }
            }
        }


        public static CancellationTokenSource CancellationTokenSource { get; private set; }

        public static void StopBot()
        {
            CancellationTokenSource.Cancel();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            if (IsDebug())
            {
                // this is where you put the id of the test discord guild
                Console.WriteLine($"In debug mode, adding commands to {_testGuildId}...");
                await _commands.RegisterCommandsToGuildAsync(_testGuildId);
            }
            else
            {
                // this method will add commands globally, but can take around an hour
                await _commands.RegisterCommandsGloballyAsync(true);
            }
            Console.WriteLine($"Connected as -> [{_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}] :)");
        }

        // this method handles the ServiceCollection creation/configuration, and builds out the service provider we can call on later
        private static ServiceProvider ConfigureServices()
        {
            // Create the DiscordSocketConfig with required intents
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            return new ServiceCollection()
                // Create the DiscordSocketClient with the specified config
                .AddSingleton(new DiscordSocketClient(config))

                // Use lambda to create InteractionService with the DiscordSocketClient dependency
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))

                .AddSingleton<CommandHandler>()
                .AddSingleton<Quotes>()
                .BuildServiceProvider();
        }



        static bool IsDebug()
        {
            return true;
        }
    }
}