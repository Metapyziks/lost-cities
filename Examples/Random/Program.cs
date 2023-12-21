using LostCities;

Random? random = null;

while ( PlayerView.ReadFromConsole() is {} view )
{
    random ??= new Random( view.Seed );

    var actions = view.ValidActions.ToArray();
    actions[random.Next( actions.Length )].WriteToConsole();
}
