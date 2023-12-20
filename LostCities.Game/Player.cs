using System.Diagnostics;

namespace LostCities.Game;

public class PlayerHost : IDisposable
{
    private Process Process { get; }

    public PlayerHost( string command )
    {
        Process = Process.Start( new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        } )!;
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
