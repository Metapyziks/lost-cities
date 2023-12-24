using System.Text.Json.Serialization;

namespace LostCities.Client;

[JsonDerivedType( typeof(NewGameMessage), typeDiscriminator: "new_game" )]
[JsonDerivedType( typeof(TurnMessage), typeDiscriminator: "turn" )]
[JsonDerivedType( typeof(EndGameMessage), typeDiscriminator: "end_game" )]
[JsonDerivedType( typeof(KickMessage), typeDiscriminator: "kick" )]
public abstract record ServerMessage;

[JsonDerivedType( typeof(InitializeMessage), typeDiscriminator: "init" )]
[JsonDerivedType( typeof(AcceptGameMessage), typeDiscriminator: "accept_game" )]
[JsonDerivedType( typeof(ActionMessage), "action" )]
public abstract record ClientMessage;

public record InitializeMessage(
    [property: JsonPropertyName( "token" )]
    string Token,
    [property: JsonPropertyName( "version" )]
    string? Version,
    [property: JsonPropertyName( "max_parallel" )]
    int MaxParallel )
    : ClientMessage;

public record NewGameMessage(
    [property: JsonPropertyName( "id" )] string Id )
    : ServerMessage;

public record AcceptGameMessage(
    [property: JsonPropertyName( "id" )] string Id,
    [property: JsonPropertyName( "accepted" )] bool Accepted )
    : ClientMessage;

public record TurnMessage(
    [property: JsonPropertyName( "id" )] string Id,
    [property: JsonPropertyName( "view_json" )] string ViewJson )
    : ServerMessage;

public record ActionMessage(
    [property: JsonPropertyName( "id" )] string Id,
    [property: JsonPropertyName( "action_json" )] string ViewJson )
    : ClientMessage;

public record EndGameMessage(
    [property: JsonPropertyName( "id" )] string Id,
    [property: JsonPropertyName( "result_json" )] string ResultJson )
    : ServerMessage;

public record KickMessage(
    [property: JsonPropertyName("reason")] string Reason )
    : ServerMessage;
