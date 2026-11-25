using System;

/// Game-wide events. Systems announce what happened here instead of holding
/// references to each other, so a new screen or system only has to listen.
public static class CafeEvents
{
    /// A new ticket went up on the line.
    public static event Action<OrderTicket> OrderPlaced;
    /// A drink was handed over: the recipe, stars (1-5) and tips earned.
    public static event Action<Recipe, int, int> OrderServed;
    /// A finished drink went out for this ticket: the cup and the customer's rating of it.
    /// The customer who placed the ticket comes to collect it.
    public static event Action<OrderTicket, DrinkRating, CupContents> DrinkHandedOff;
    /// The list of open orders changed for any reason.
    public static event Action OrdersChanged;

    public static event Action DayStarted;
    public static event Action DayEnded;

    /// A menu screen opened (true) or the player went back to the cafe (false).
    public static event Action<bool> MenuToggled;

    /// The player sat down at the shop computer; the menus show its screen.
    public static event Action ComputerOpened;

    /// The player started or finished using something (station, dispenser, door...).
    /// Visual-only listeners such as prop animations hook in here.
    public static event Action<IInteractable> InteractionStarted;
    public static event Action<IInteractable> InteractionEnded;

    /// A cup was set down on a surface or picked back up.
    public static event Action CupMoved;
    public static void RaiseCupMoved()                           { if (CupMoved != null) CupMoved(); }

    public static void RaiseInteractionStarted(IInteractable i)  { if (InteractionStarted != null) InteractionStarted(i); }
    public static void RaiseInteractionEnded(IInteractable i)    { if (InteractionEnded != null) InteractionEnded(i); }

    public static void RaiseOrderPlaced(OrderTicket t)           { if (OrderPlaced != null) OrderPlaced(t); RaiseOrdersChanged(); }
    public static void RaiseOrderServed(Recipe r, int stars, int tips) { if (OrderServed != null) OrderServed(r, stars, tips); RaiseOrdersChanged(); }
    public static void RaiseDrinkHandedOff(OrderTicket t, DrinkRating r, CupContents cup) { if (DrinkHandedOff != null) DrinkHandedOff(t, r, cup); }
    public static void RaiseOrdersChanged()                      { if (OrdersChanged != null) OrdersChanged(); }
    public static void RaiseDayStarted()                         { if (DayStarted != null) DayStarted(); }
    public static void RaiseDayEnded()                           { if (DayEnded != null) DayEnded(); }
    public static void RaiseMenuToggled(bool open)               { if (MenuToggled != null) MenuToggled(open); }
    public static void RaiseComputerOpened()                     { if (ComputerOpened != null) ComputerOpened(); }
}
