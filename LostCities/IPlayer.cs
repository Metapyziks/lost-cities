
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LostCities;

public interface IPlayer : IDisposable
{
    Task<PlayerAction?> TakeTurnAsync( PlayerView view );
}

public class ChildProcessPlayer : IPlayer
{
    public Process Process { get; }

    private static ConcurrentDictionary<string, Func<string[], IPlayer>?> BotCtorCache { get; } = new ();

    private static Func<string[], IPlayer> GetBotCtorUncached( string fileName )
    {
        var asm = Assembly.LoadFrom( fileName );
        var botType = asm.ExportedTypes
            .First( x => !x.IsAbstract && x.IsAssignableTo( typeof( IPlayer ) ) );

        var argsParam = Expression.Parameter( typeof(string[]), "args" );

        Expression ctorCall;

        if ( botType.GetConstructor( new[] { typeof( string[] ) } ) is { } ctor )
        {
            ctorCall = Expression.New( ctor, argsParam );
        }
        else
        {
            ctorCall = Expression.New( botType );
        }

        return Expression.Lambda<Func<string[], IPlayer>>( ctorCall, argsParam ).Compile();
    }

    public static IPlayer Create( Player player, string fileName, params string[] args )
    {
        if ( !BotCtorCache.TryGetValue( fileName, out var ctor ) )
        {
            lock ( BotCtorCache )
            {
                if ( !BotCtorCache.TryGetValue( fileName, out ctor ) )
                {
                    try
                    {
                        ctor = GetBotCtorUncached( fileName );

                        BotCtorCache[fileName] = ctor;

                        return ctor( args );
                    }
                    catch
                    {
                        BotCtorCache[fileName] = null;
                    }
                }
            }
        }

        return ctor?.Invoke( args ) ?? new ChildProcessPlayer( player, fileName, args );
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
