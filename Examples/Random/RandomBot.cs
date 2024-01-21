namespace LostCities.Random;

public class RandomBot : Bot
{
    protected override Task<PlayerAction> OnTakeTurnAsync()
    {
        var actions = View.ValidActions.ToArray();
        return Task.FromResult( actions[Random.Next( actions.Length )] );
    }
}
