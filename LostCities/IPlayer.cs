
using System.Diagnostics;

namespace LostCities;

public interface IPlayer : IDisposable
{
    Task<PlayerAction?> TakeTurnAsync( PlayerView view );
}

public class ConsolePlayer : IPlayer
{
    public Player Player { get; }

    public ConsolePlayer( Player player )
    {
        Player = player;
    }

    public virtual async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        Console.WriteLine( $"Turn: {Player}" );
        Console.WriteLine( $"View: {view.ToJson()}" );

        var response = await Task.Run( Console.ReadLine );

        return response == null ? null : PlayerAction.FromJson( response );
    }

    public void Dispose()
    {

    }
}

public class HumanConsolePlayer : ConsolePlayer
{
    public HumanConsolePlayer( Player player )
        : base( player )
    {

    }

    public override Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        throw new NotImplementedException();
    }
}

public class ChildProcessPlayer : IPlayer
{
    public Process Process { get; }

    public ChildProcessPlayer( string fileName, params string[] args )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach ( var arg in args )
        {
            startInfo.ArgumentList.Add( arg );
        }

        Process = Process.Start( startInfo )!;
    }

    public async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        try
        {
            await Process.StandardInput.WriteLineAsync( view.ToJson() );

            var response = await Process.StandardOutput.ReadLineAsync();
            return response == null ? null : PlayerAction.FromJson( response );
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Process.Dispose();
    }
}
