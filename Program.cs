using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SixLabors.ImageSharp;
using PuppeteerSharp;
using System.Runtime.InteropServices;
using DiscordSpellBot.Services;

public class Program 
{
    private DiscordSocketClient _client;
    private IConfiguration _configuration;
    private InteractionService _commands;

    private static Task Main(string[] args) => new Program().MainAsync();

    private async Task MainAsync() {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var config = new DiscordSocketConfig() {
            GatewayIntents = GatewayIntents.AllUnprivileged
        };

        _client = new DiscordSocketClient(config);
        _commands = new InteractionService(_client.Rest);

        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton(_configuration)
            .AddSingleton(HtmlToImageService)
            .AddSingleton(SpellService)
            .BuildServiceProvider();

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.InteractionCreated += HandleInteraction;

        await _client.LoginAsync(TokenType.Bot, _configuration["DiscordToken"]);
        await _client.StopAsync();

        await Task.Delay(-1);
    }

    private async Task HandleInteraction(SocketInteraction interaction) {
        try {
            var context = new SocketInteractionContext(_client, interaction);
            await _commands.ExecuteCommandAsync(context, services);
        }
        catch (Exception ex) {
            Console.WriteLine(ex);
            if (interaction.Type == InteractionType.ApplicationCommand) {
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }
    }

    private Task LogAsync(LogMessage log) {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync() {
        await _commands.RegisterCommandsGloballyAsync();
    }
}