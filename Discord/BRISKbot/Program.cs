namespace BRISKbot
{
    using Discord;
    using Discord.WebSocket;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

	class Program
    {
        private DiscordSocketClient _client;
        const string TwitchSubscriberRole = "Twitch Subscriber";
        const string SuperSubscriberRole = "Super Subscriber";

        public static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
            // When working with events that have Cacheable<IMessage, ulong> parameters,
            // you must enable the message cache in your config settings if you plan to
            // use the cached message entity. 
            var _config = new DiscordSocketConfig { MessageCacheSize = 1000 };
            _client = new DiscordSocketClient(_config);

            _client.Log += Log;
            
            var token = File.ReadAllText("token.txt").Trim();
            // alternatively, we could use JSON
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += OnMessageReceived;
            _client.MessageUpdated += OnMessageUpdate;
            _client.ReactionAdded += OnReactionAdded;
            
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            // log user roles
            var user = arg3.User.Value as SocketGuildUser;

            if (user != null)
            {
                Log(new LogMessage(LogSeverity.Info, "Reaction", $"[{user.Guild.Name}] {user.Username} reacted to #{arg3.Channel.Name} message #{arg3.MessageId} with :{arg3.Emote.Name}:"));

                ////// enumerate all the roles
                ////foreach (var role in user.Roles)
                ////{
                ////    Log(new LogMessage(LogSeverity.Debug, "Reaction", $"{user.Nickname} has role {role.Guild}.{role.Name}"));
                ////}

                if (arg3.MessageId == 683884610936897598u
                    && user.Roles.Any(x => x.Name.Equals("Twitch Subscriber", StringComparison.OrdinalIgnoreCase)))
                {
                    if (! user.Roles.Any(x => x.Name.Equals(SuperSubscriberRole)))
                    {
                        Log(new LogMessage(LogSeverity.Critical, "Reaction", $"[{user.Guild.Name}] {user.Username} is a [{TwitchSubscriberRole}] but lacks the [{SuperSubscriberRole}] role. Adding it."));

                        var roleToAdd = user.Guild.Roles.First(r => r.Name.Equals(SuperSubscriberRole));

                        if (roleToAdd != null)
                        {
                            var t = user.AddRoleAsync(roleToAdd);
                            t.Wait();

                            if (t.IsCompletedSuccessfully)
                            {
                                Log(new LogMessage(LogSeverity.Info, "Reaction", $"[{user.Guild.Name}] Successfully added role [{SuperSubscriberRole}] to {user.Username}."));
                            }

                            var msg = $"{user.Mention}, thanks for being a {TwitchSubscriberRole}! You've been granted the **{roleToAdd.Name}** role!";
                            t = arg3.Channel.SendMessageAsync(msg);
                            t.Wait();

                            if (!t.IsCompletedSuccessfully)
                            {
                                Log(new LogMessage(LogSeverity.Error, "Reaction", $"[{user.Guild.Name} {arg3.Channel.Name}] Unable to post message: {msg}"));
                            }
                        }
                        else
                        {
                            Log(new LogMessage(LogSeverity.Error, "Reaction", $"[{user.Guild.Name}] Couldn't find role [{SuperSubscriberRole}]."));
                        }
                    }
                    else
                    {
                        Log(new LogMessage(LogSeverity.Debug, "Reaction", $"[{user.Guild.Name}] {user.Username} already has the [{SuperSubscriberRole}] role."));
                    }
                }
            }

            return Task.CompletedTask;
        }

        private Task OnMessageUpdate(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            var msg = new LogMessage(LogSeverity.Warning, "Update", $"[{(arg2.Channel as SocketGuildChannel)?.Guild.Name ?? "<unknown>"}] {arg2.Author.Username} to #{arg2.Channel.Name}: {arg2.Content}");
            Log(msg);
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(SocketMessage arg)
        {
            var msg = new LogMessage(LogSeverity.Info, "Message", $"[{(arg.Channel as SocketGuildChannel)?.Guild.Name ?? "<unknown>"}] {arg.Author.Username} to #{arg.Channel.Name}: {arg.Content}");
            Log(msg);
            return Task.CompletedTask;
        }

        private Task Log(LogMessage message)
		{
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();
            return Task.CompletedTask;
		}
	}
}
