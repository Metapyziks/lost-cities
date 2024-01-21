
namespace LostCities;

public abstract class Bot : IPlayer
{
    public Random Random { get; private set; } = null!;
    public PlayerView View { get; private set; } = null!;

    public PlayerAction? MyLastAction { get; private set; }
    public PlayerAction? OpponentLastAction => View.LastAction;

    public GameMemory MyMemory { get; private set; }
    public GameMemory OpponentMemory { get; private set; }

    public TextWriter? Logger { get; set; }

    private int _turnCount;

    public async Task<PlayerAction?> TakeTurnAsync( PlayerView view )
    {
        View = view;

        if ( Random == null! )
        {
            Random = new Random( view.Seed );
            MyMemory = GameMemory.Create( view );
            OpponentMemory = GameMemory.Create( view with { Hand = Array.Empty<Card>() } );

            OnInitialize();
        }
        else
        {
            if ( OpponentLastAction is not null )
            {
                MyMemory = MyMemory.WithOpponentAction( OpponentLastAction );
            }

            if ( MyLastAction is not null )
            {
                OpponentMemory = OpponentMemory.WithOpponentAction( MyLastAction );

                if ( MyLastAction.DrawnCard is null )
                {
                    MyMemory = MyMemory.WithDrawnCard( View.Hand[^1] );
                }
            }
        }

        try
        {
            Logger?.WriteLine( $"Turn {++_turnCount}:" );

            return MyLastAction = await OnTakeTurnAsync();
        }
        finally
        {
            if ( Logger != null )
            {
                await Logger.FlushAsync();
            }
        }
    }

    protected virtual void OnInitialize()
    {

    }

    protected abstract Task<PlayerAction> OnTakeTurnAsync();

    public virtual void Dispose()
    {

    }

    public Task RunAsync()
    {
        return RunAsync( Console.In, Console.Out );
    }

    public async Task RunAsync( TextReader reader, TextWriter writer )
    {
        while ( PlayerView.Read( reader ) is { } view )
        {
            var action = await TakeTurnAsync( view );
            action?.Write( writer );
        }
    }
}
