
using System.Diagnostics;
using System.Reflection;

namespace LostCities;

public interface IPlayer : IDisposable
{
    Task<PlayerAction?> TakeTurnAsync( PlayerView view );
}

public class ChildProcessPlayer : IPlayer
{
    public Process Process { get; }

    public static IPlayer Create( Player player, string fileName, params string[] args )
    {
        try
        {
            var asm = Assembly.LoadFrom( fileName );
            var botType = asm.ExportedTypes
                .First( x => !x.IsAbstract && x.IsAssignableTo( typeof(IPlayer) ) );

            if ( botType.GetConstructor( new[] { typeof(string[]) } ) is { } ctor )
            {
                return (IPlayer)ctor.Invoke( new object[] { args } );
            }

            return (IPlayer)Activator.CreateInstance( botType )!;
        }
        catch
        {
            //
        }

        return new ChildProcessPlayer( player, fileName, args );
    }

    private ChildProcessPlayer( Player player, string fileName, params string[] args )
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

        Task.Run( async () =>
        {
            while ( await Process.StandardError.ReadLineAsync() is { } line )
            {
                Console.Error.WriteLine( $"{player}: {line}" );
            }
        } );
    }

    public async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        await Process.StandardInput.WriteLineAsync( view.ToJson() );

        var response = await Process.StandardOutput.ReadLineAsync();
        return response == null ? null : PlayerAction.FromJson( response );
    }

    public void Dispose()
    {
        Process.Dispose();
    }
}
