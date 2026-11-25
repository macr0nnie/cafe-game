using NUnit.Framework;
using UnityEngine;

public class CustomerSeatingTests
{
    GameObject _root;

    [SetUp]    public void SetUp()    { _root = new GameObject("test"); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(_root); }

    Seat NewSeat() { return new GameObject("seat").AddComponent<Seat>(); }

    [Test]
    public void Seat_TakesOneCustomerAtATime()
    {
        var s = NewSeat(); s.transform.SetParent(_root.transform);
        var a = new GameObject("a"); a.transform.SetParent(_root.transform);
        var b = new GameObject("b"); b.transform.SetParent(_root.transform);
        Assert.IsTrue(s.Claim(a));
        Assert.IsFalse(s.Claim(b));
        s.Release(b);                       // only the occupant can free it
        Assert.IsFalse(s.IsFree);
        s.Release(a);
        Assert.IsTrue(s.IsFree);
    }

    [Test]
    public void PickFree_SkipsTakenSeatsAndReturnsNullWhenFull()
    {
        var seats = new[] { NewSeat(), NewSeat(), NewSeat() };
        foreach (var s in seats) s.transform.SetParent(_root.transform);
        seats[0].Claim(_root);
        seats[2].Claim(_root);
        for (int roll = -3; roll < 5; roll++) Assert.AreSame(seats[1], Seat.PickFree(seats, roll));
        seats[1].Claim(_root);
        Assert.IsNull(Seat.PickFree(seats, 0));
    }

    [Test]
    public void Queue_MovesUpWhenTheFrontLeaves()
    {
        var q = _root.AddComponent<CustomerQueue>();
        q.Spots = new[] { new GameObject("0").transform, new GameObject("1").transform };
        foreach (var t in q.Spots) t.SetParent(_root.transform);
        var a = new GameObject("a"); a.transform.SetParent(_root.transform);
        var b = new GameObject("b"); b.transform.SetParent(_root.transform);

        q.Join(a); q.Join(b); q.Join(b);
        Assert.AreEqual(2, q.Count);
        Assert.IsFalse(q.HasRoom);
        Assert.IsTrue(q.IsFront(a));
        Assert.AreSame(q.Spots[1], q.SpotFor(b));

        q.Leave(a);
        Assert.IsTrue(q.IsFront(b));
        Assert.AreSame(q.Spots[0], q.SpotFor(b));
        Assert.IsNull(q.SpotFor(a));
    }
}
